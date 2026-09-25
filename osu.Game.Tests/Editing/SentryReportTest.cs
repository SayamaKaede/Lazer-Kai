// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Game.Utils;
using Sentry;
using Sentry.Extensibility;
using Sentry.Protocol.Envelopes;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// What an error report sends: nothing that says who sent it, and not so many that one run uses up the month.
    /// </summary>
    [TestFixture]
    public class SentryReportTest
    {
        private const string osu_username = @"SomeMapper";

        /// <summary>
        /// Everything the report carries, as it would go out: the exception's message, the paths in its stack trace, the
        /// log lines before it, and whatever Sentry adds of its own about the machine.
        /// </summary>
        [Test]
        public void TestAReportNamesNobody()
        {
            string account = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Split('\\', '/')[^1];

            string sent = send(osu_username, $@"Could not read C:\Users\{account}\AppData\Roaming\osu\a.osu for {osu_username} on {Environment.MachineName}",
                $@"{osu_username} opened C:\Users\{account}\Songs\a.osu");

            Assert.That(sent, Does.Contain(nameof(InvalidOperationException)), "the error itself is still there");
            Assert.That(sent, Does.Contain(@"Could not read"), "and what it said");

            Assert.That(sent, Does.Not.Contain(account).IgnoreCase, "the Windows account");
            Assert.That(sent, Does.Not.Contain(Environment.MachineName).IgnoreCase, "the machine");
            Assert.That(sent, Does.Not.Contain(osu_username).IgnoreCase, "the osu! account");

            // an address of its own, so that Sentry does not place the report by the one it arrived from.
            Assert.That(sent, Does.Contain($@"""ip_address"":""{SentryLogger.NO_ADDRESS}"""), "where it came from");
        }

        /// <summary>
        /// A username that is also a word of this game's namespaces is taken out where it is a name, and not out of the
        /// stack trace, which would be left with nothing to read it by.
        /// </summary>
        [Test]
        public void TestAUsernameThatIsAlsoANamespaceLeavesTheStackTraceReadable()
        {
            string sent = send(@"Game", @"Signed in as Game.", @"Game opened the editor");

            Assert.That(sent, Does.Not.Contain(@"as Game."), "where it is a name");
            Assert.That(sent, Does.Not.Contain(@"Game opened"), "in the log lines too");
            Assert.That(sent, Does.Contain(@"osu.Game.Tests, Version"), "the stack trace's assembly");
        }

        [Test]
        public void TestTheSameErrorIsSentOnce()
        {
            var limit = new SentryReportLimit();
            var error = thrown(@"the same");

            Assert.That(limit.Allows(error), Is.True);
            Assert.That(limit.Allows(error), Is.False);
            Assert.That(limit.Allows(thrown(@"another")), Is.True);
        }

        [Test]
        public void TestOneRunSendsNoMoreThanTheLimit()
        {
            var limit = new SentryReportLimit();

            int allowed = Enumerable.Range(0, SentryReportLimit.MAX_PER_RUN + 5).Count(i => limit.Allows(thrown($@"error {i}")));

            Assert.That(allowed, Is.EqualTo(SentryReportLimit.MAX_PER_RUN));
        }

        /// <summary>
        /// Captures an error the way the game does, through Sentry's own client with the game's options, and answers with
        /// what would have gone out. Nothing leaves the machine: the transport keeps it.
        /// </summary>
        private static string send(string username, string message, string logLine)
        {
            var transport = new KeepingTransport();

            var options = new SentryOptions();
            SentryLogger.Configure(options, @"https://key@sentry.invalid/1", @"test", () => username);
            options.Transport = transport;

            using (var client = new SentryClient(options))
            {
                var report = new SentryEvent(thrown(message)) { Message = @"An unhandled error has occurred." };
                report.AddBreadcrumb(new Breadcrumb(logLine, @"navigation"));

                client.CaptureEvent(report);
                client.FlushAsync(TimeSpan.FromSeconds(10)).WaitSafely();
            }

            Assert.That(transport.Sent, Has.Count.EqualTo(1), "one report went out");
            return transport.Sent.Single();
        }

        private static Exception thrown(string message)
        {
            try
            {
                throw new InvalidOperationException(message);
            }
            catch (Exception e)
            {
                return e;
            }
        }

        private class KeepingTransport : ITransport
        {
            public readonly List<string> Sent = new List<string>();

            public async Task SendEnvelopeAsync(Envelope envelope, CancellationToken cancellationToken = default)
            {
                using (var stream = new MemoryStream())
                {
                    await envelope.SerializeAsync(stream, null, cancellationToken).ConfigureAwait(false);

                    lock (Sent)
                        Sent.Add(Encoding.UTF8.GetString(stream.ToArray()));
                }
            }
        }
    }
}
