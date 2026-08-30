// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Copying logs somewhere they can be handed over without handing over who wrote them.
    /// </summary>
    [TestFixture]
    public class LogExportTest
    {
        private static string accountName => Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Split('\\', '/')[^1];

        /// <summary>
        /// The line that actually carries the name, found by reading the logs this machine had already written:
        /// out of everything they say, this is the one that prints where the game is installed, once a launch.
        /// </summary>
        [Test]
        public void TestTheLineThatNamesTheMachineIsMasked()
        {
            string line = $@"2026-08-23 19:22:12 [verbose]: Opened realm ""C:\Users\{accountName}\Documents\Lazer-Kai\client.realm"" at version 52";

            string masked = PathDisplay.MaskIn(line);

            Assert.That(masked, Does.Not.Contain(accountName));
            Assert.That(masked, Does.Contain(PathDisplay.MASK));

            // and the rest of the line has to survive, or the log stops being a log
            Assert.That(masked, Does.Contain(@"Opened realm"));
            Assert.That(masked, Does.Contain(@"at version 52"));
            Assert.That(masked, Does.Contain(@"client.realm"));
        }

        [Test]
        public void TestAnExceptionMessageQuotingAPathIsMasked()
        {
            string line = $@"System.IO.FileNotFoundException: Could not find file 'D:\Users\{accountName}\Downloads\thing.osu'.";

            Assert.That(PathDisplay.MaskIn(line), Does.Not.Contain(accountName));
        }

        /// <summary>
        /// A log line is not a path, and taking it apart on its separators would wreck it.
        /// </summary>
        [Test]
        public void TestAnOrdinaryLineIsUntouched()
        {
            const string line = @"2026-08-23 19:22:17 [verbose]: Request to https://assets.ppy.sh/main-menu/banner@2x.png successfully completed!";

            Assert.That(PathDisplay.MaskIn(line), Is.EqualTo(line));
        }

        [Test]
        public void TestTheNetworkLogIsNotSent()
        {
            Assert.That(LogExport.ShouldExport(@"1787512930.runtime.log"), Is.True);
            Assert.That(LogExport.ShouldExport(@"1787511398.database.log"), Is.True);
            Assert.That(LogExport.ShouldExport(@"1787512930.performance.log"), Is.True);

            // by far the largest, a record of what somebody was browsing, and never where a crash was hiding
            Assert.That(LogExport.ShouldExport(@"1787512930.network.log"), Is.False);

            Assert.That(LogExport.ShouldExport(@"client.realm"), Is.False);
            Assert.That(LogExport.ShouldExport(@"notes.txt"), Is.False);
        }

        [Test]
        public void TestOnlyTheRecentLaunchesAreCopied()
        {
            string source = newFolder();
            string destination = newFolder();

            try
            {
                foreach (string session in new[] { @"100", @"200", @"300", @"400" })
                    File.WriteAllText(Path.Combine(source, $@"{session}.runtime.log"), $@"session {session}");

                File.WriteAllText(Path.Combine(source, @"400.network.log"), @"not this one");

                Assert.That(LogExport.Write(source, destination, sessions: 2), Is.EqualTo(2));

                var written = Directory.GetFiles(destination).Select(Path.GetFileName).ToArray();

                Assert.That(written, Is.EquivalentTo(new[] { @"400.runtime.log", @"300.runtime.log" }));
            }
            finally
            {
                Directory.Delete(source, true);
                Directory.Delete(destination, true);
            }
        }

        /// <summary>
        /// Somebody diagnosing their own problem needs the real paths, so the originals stay as they were.
        /// </summary>
        [Test]
        public void TestTheOriginalsAreNotTouched()
        {
            string source = newFolder();
            string destination = newFolder();

            string line = $@"Opened realm ""C:\Users\{accountName}\client.realm""";

            try
            {
                string original = Path.Combine(source, @"1.runtime.log");
                File.WriteAllText(original, line);

                LogExport.Write(source, destination);

                Assert.That(File.ReadAllText(original).Trim(), Is.EqualTo(line));
                Assert.That(File.ReadAllText(Path.Combine(destination, @"1.runtime.log")), Does.Not.Contain(accountName));
            }
            finally
            {
                Directory.Delete(source, true);
                Directory.Delete(destination, true);
            }
        }

        /// <summary>
        /// The whole point, checked against logs this machine really wrote rather than lines made up here.
        /// </summary>
        /// <remarks>
        /// Made-up lines only test the cases somebody thought of. Real logs are 85,000 lines of whatever the
        /// game felt like saying, and the question being answered — "is my name in what I am about to send" —
        /// is only answered by looking at all of it.
        /// </remarks>
        [Test]
        public void TestRealLogsComeOutWithNoNameInThem()
        {
            string logs = Path.Combine(TestContext.CurrentContext.TestDirectory, @"..", @"..", @"..", @"..", @"..", @"Lazer-Kai", @"logs");

            if (!Directory.Exists(logs))
                Assert.Ignore(@"No real log folder beside this checkout to read.");

            string destination = newFolder();

            try
            {
                int written = LogExport.Write(logs, destination, sessions: 100);

                Assert.That(written, Is.GreaterThan(0), @"nothing was copied, so nothing was checked");

                foreach (string file in Directory.GetFiles(destination))
                {
                    string[] lines = File.ReadAllLines(file);

                    for (int i = 0; i < lines.Length; i++)
                    {
                        Assert.That(lines[i].Contains(accountName, StringComparison.OrdinalIgnoreCase), Is.False,
                            $@"{Path.GetFileName(file)} line {i + 1} still names the account");
                    }
                }
            }
            finally
            {
                Directory.Delete(destination, true);
            }
        }

        private static string newFolder()
        {
            string path = Path.Combine(Path.GetTempPath(), $"lazer-kai-logs-{Guid.NewGuid()}");
            Directory.CreateDirectory(path);
            return path;
        }
    }
}
