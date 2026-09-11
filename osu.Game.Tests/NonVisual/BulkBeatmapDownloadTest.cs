// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// A profile listing's "download all" working through its downloads a few at a time.
    /// </summary>
    [TestFixture]
    public class BulkBeatmapDownloadTest
    {
        private readonly List<FakeDownload> started = new List<FakeDownload>();
        private readonly HashSet<int> refused = new HashSet<int>();

        private BulkBeatmapDownload batch = null!;
        private int completions;

        [SetUp]
        public void SetUp()
        {
            started.Clear();
            refused.Clear();
            completions = 0;
        }

        [Test]
        public void TestOnlyAFewStartAtOnce()
        {
            begin(10);

            Assert.That(started, Has.Count.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));
        }

        [Test]
        public void TestEachEndStartsTheNext()
        {
            begin(10);

            started[0].End(BulkDownloadOutcome.Downloaded);

            Assert.That(started, Has.Count.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS + 1));
            Assert.That(started.Last().Set.OnlineID, Is.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS + 1));
            Assert.That(batch.Finished, Is.EqualTo(1));
        }

        [Test]
        public void TestCompletesOnceEverythingHasEnded()
        {
            begin(5);

            endEverything(BulkDownloadOutcome.Downloaded);

            Assert.That(started, Has.Count.EqualTo(5));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(batch.Downloaded, Is.EqualTo(5));
            Assert.That(batch.Finished, Is.EqualTo(5));
            Assert.That(batch.Failed, Is.Zero);
        }

        [Test]
        public void TestFailuresAreCountedAndPassedOver()
        {
            begin(5);

            started[1].End(BulkDownloadOutcome.Failed);
            endEverything(BulkDownloadOutcome.Downloaded);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(batch.Downloaded, Is.EqualTo(4));
            Assert.That(batch.Failed, Is.EqualTo(1));
        }

        /// <summary>
        /// Once the server refuses, asking again only gets refused again: what is running finishes, nothing more starts,
        /// and what never started counts as not downloaded.
        /// </summary>
        [Test]
        public void TestARefusalStopsTheRestFromStarting()
        {
            begin(10);

            started[0].End(BulkDownloadOutcome.RateLimited);

            Assert.That(started, Has.Count.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));
            Assert.That(completions, Is.Zero, "not while the others are still running");

            endEverything(BulkDownloadOutcome.Downloaded);

            Assert.That(started, Has.Count.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));
            Assert.That(completions, Is.EqualTo(1));
            Assert.That(batch.RateLimited, Is.True);
            Assert.That(batch.Downloaded, Is.EqualTo(2));
            Assert.That(batch.Failed, Is.EqualTo(8));
            Assert.That(batch.Finished, Is.EqualTo(10));
        }

        [Test]
        public void TestCancellingCancelsWhatIsRunningAndStartsNothingMore()
        {
            begin(10);

            batch.Cancel();

            Assert.That(started.All(d => d.WasCancelled), Is.True);

            // the requests report back as cancelled some time after.
            foreach (var download in started.ToArray())
                download.End(BulkDownloadOutcome.Cancelled);

            Assert.That(started, Has.Count.EqualTo(BulkBeatmapDownload.CONCURRENT_DOWNLOADS));
            Assert.That(completions, Is.Zero);
        }

        /// <summary>
        /// One started from its card while the batch was working is left to that card, and the batch carries on past it.
        /// </summary>
        [Test]
        public void TestOneThatCannotBeStartedIsPassedOver()
        {
            refused.Add(2);

            begin(5);

            Assert.That(started.Select(d => d.Set.OnlineID), Is.EqualTo(new[] { 1, 3, 4 }));

            endEverything(BulkDownloadOutcome.Downloaded);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(batch.Downloaded, Is.EqualTo(4));
            Assert.That(batch.Failed, Is.Zero);
            Assert.That(batch.Finished, Is.EqualTo(5));
        }

        /// <summary>
        /// One cancelled from its card is somebody deciding against it, not a failure.
        /// </summary>
        [Test]
        public void TestOneCancelledOnItsOwnIsNotAFailure()
        {
            begin(3);

            started[0].End(BulkDownloadOutcome.Cancelled);
            endEverything(BulkDownloadOutcome.Downloaded);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(batch.Downloaded, Is.EqualTo(2));
            Assert.That(batch.Failed, Is.Zero);
        }

        [Test]
        public void TestAnEndReportedTwiceCountsOnce()
        {
            begin(3);

            started[0].End(BulkDownloadOutcome.Downloaded);
            started[0].End(BulkDownloadOutcome.Downloaded);

            Assert.That(batch.Downloaded, Is.EqualTo(1));
            Assert.That(batch.Finished, Is.EqualTo(1));
        }

        [Test]
        public void TestNothingToDownloadCompletesAtOnce()
        {
            begin(0);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(started, Is.Empty);
        }

        private void begin(int count)
        {
            var sets = Enumerable.Range(1, count).Select(id => new APIBeatmapSet { OnlineID = id }).ToArray();

            batch = new BulkBeatmapDownload(sets, (set, finished) =>
            {
                if (refused.Contains(set.OnlineID))
                    return null;

                var download = new FakeDownload(set, finished);
                started.Add(download);
                return () => download.WasCancelled = true;
            });

            batch.Completed += () => completions++;
            batch.Begin();
        }

        /// <summary>
        /// Ends every download, in the order they started — including those that start as earlier ones end.
        /// </summary>
        private void endEverything(BulkDownloadOutcome outcome)
        {
            for (int i = 0; i < started.Count; i++)
                started[i].End(outcome);
        }

        private class FakeDownload
        {
            public readonly APIBeatmapSet Set;
            public bool WasCancelled;

            private readonly Action<BulkDownloadOutcome> finished;

            public FakeDownload(APIBeatmapSet set, Action<BulkDownloadOutcome> finished)
            {
                Set = set;
                this.finished = finished;
            }

            public void End(BulkDownloadOutcome outcome) => finished(outcome);
        }
    }
}
