// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// What a profile listing's "download all" actually asks for.
    /// </summary>
    [TestFixture]
    public class BulkDownloadPlanTest
    {
        private static readonly HashSet<int> nothing_installed = new HashSet<int>();

        [Test]
        public void TestEverythingNewIsDownloaded()
        {
            var plan = BulkDownloadPlan.For(sets(1, 2, 3), nothing_installed, _ => false);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1, 2, 3 }));
            Assert.That(plan.AlreadyHave, Is.Zero);
            Assert.That(plan.Unavailable, Is.Zero);
        }

        [Test]
        public void TestWhatIsDownloadedAlreadyIsLeftOut()
        {
            var plan = BulkDownloadPlan.For(sets(1, 2, 3), new HashSet<int> { 2 }, _ => false);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(plan.AlreadyHave, Is.EqualTo(1));
        }

        [Test]
        public void TestWhatHasBeenTakenDownIsLeftOut()
        {
            var listing = sets(1, 2, 3);
            listing[1].Availability = new BeatmapSetOnlineAvailability { DownloadDisabled = true };

            var plan = BulkDownloadPlan.For(listing, nothing_installed, _ => false);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1, 3 }));
            Assert.That(plan.Unavailable, Is.EqualTo(1));
        }

        /// <summary>
        /// Downloaded already wins over taken down: a set somebody has is not one they are missing, whatever became of it.
        /// </summary>
        [Test]
        public void TestATakenDownSetThatIsDownloadedCountsAsDownloaded()
        {
            var listing = sets(1);
            listing[0].Availability = new BeatmapSetOnlineAvailability { DownloadDisabled = true };

            var plan = BulkDownloadPlan.For(listing, new HashSet<int> { 1 }, _ => false);

            Assert.That(plan.AlreadyHave, Is.EqualTo(1));
            Assert.That(plan.Unavailable, Is.Zero);
        }

        [Test]
        public void TestWhatIsOnItsWayAlreadyIsNotStartedAgain()
        {
            var plan = BulkDownloadPlan.For(sets(1, 2, 3), nothing_installed, set => set.OnlineID == 3);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TestASetListedTwiceIsDownloadedOnce()
        {
            var plan = BulkDownloadPlan.For(sets(1, 2, 1), nothing_installed, _ => false);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1, 2 }));
        }

        [Test]
        public void TestASetWithoutAnOnlineIdIsLeftOut()
        {
            var plan = BulkDownloadPlan.For(sets(0, 1, -1), nothing_installed, _ => false);

            Assert.That(ids(plan), Is.EqualTo(new[] { 1 }));
        }

        private static List<APIBeatmapSet> sets(params int[] onlineIds) => onlineIds.Select(id => new APIBeatmapSet { OnlineID = id }).ToList();

        private static int[] ids(BulkDownloadPlan plan) => plan.ToDownload.Select(s => s.OnlineID).ToArray();
    }
}
