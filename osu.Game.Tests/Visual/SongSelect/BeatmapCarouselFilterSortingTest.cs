// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Carousel;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.SongSelect
{
    [TestFixture]
    public partial class BeatmapCarouselFilterSortingTest
    {
        [Test]
        public async Task TestSorting()
        {
            List<BeatmapSetInfo> beatmapSets = new List<BeatmapSetInfo>();

            const string zzz_lowercase = "zzzzz";
            const string zzz_uppercase = "ZZZZZ";
            const int diff_count = 5;

            for (int i = 0; i < 20; i++)
            {
                var set = TestResources.CreateTestBeatmapSetInfo(diff_count);

                if (i == 4)
                    set.Beatmaps.ForEach(b => b.Metadata.Artist = zzz_uppercase);

                if (i == 8)
                    set.Beatmaps.ForEach(b => b.Metadata.Artist = zzz_lowercase);

                if (i == 12)
                    set.Beatmaps.ForEach(b => b.Metadata.Author.Username = zzz_uppercase);

                if (i == 16)
                    set.Beatmaps.ForEach(b => b.Metadata.Author.Username = zzz_lowercase);

                beatmapSets.Add(set);
            }

            var results = await runSorting(SortMode.Author, beatmapSets);

            Assert.That(results.Last().Metadata.Author.Username, Is.EqualTo(zzz_uppercase));
            Assert.That(results.SkipLast(diff_count).Last().Metadata.Author.Username, Is.EqualTo(zzz_lowercase));

            results = await runSorting(SortMode.Artist, beatmapSets);

            Assert.That(results.Last().Metadata.Artist, Is.EqualTo(zzz_uppercase));
            Assert.That(results.SkipLast(diff_count).Last().Metadata.Artist, Is.EqualTo(zzz_lowercase));
        }

        [Test]
        public async Task TestSortingDateSubmitted()
        {
            List<BeatmapSetInfo> beatmapSets = new List<BeatmapSetInfo>();

            const string zzz_string = "zzzzz";

            for (int i = 0; i < 10; i++)
            {
                var set = TestResources.CreateTestBeatmapSetInfo(5);

                // A total of 6 sets have date submitted (4 don't)
                // A total of 5 sets have artist string (3 of which also have date submitted)

                if (i >= 2 && i < 8) // i = 2, 3, 4, 5, 6, 7 have submitted date
                    set.DateSubmitted = DateTimeOffset.Now.AddMinutes(i);
                if (i < 5) // i = 0, 1, 2, 3, 4 have matching string
                    set.Beatmaps.ForEach(b => b.Metadata.Artist = zzz_string);

                set.Beatmaps.ForEach(b => b.Metadata.Title = $"submitted: {set.DateSubmitted}");

                beatmapSets.Add(set);
            }

            var results = await runSorting(SortMode.DateSubmitted, beatmapSets);

            Assert.That(results.Count(), Is.EqualTo(50));

            Assert.That(results.Reverse().TakeWhile(b => b.BeatmapSet!.DateSubmitted == null).Count(), Is.EqualTo(20), () => "missing dates should be at the end");
            Assert.That(results.TakeWhile(b => b.BeatmapSet!.DateSubmitted != null).Count(), Is.EqualTo(30), () => "non-missing dates should be at the start");
        }

        [Test]
        public async Task TestSortingDateUpdatedPutsTheSetSavedLastFirst()
        {
            var addedEarlier = TestResources.CreateTestBeatmapSetInfo(3);
            addedEarlier.DateAdded = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);

            var addedLongAgoButSavedLately = TestResources.CreateTestBeatmapSetInfo(3);
            addedLongAgoButSavedLately.DateAdded = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            addedLongAgoButSavedLately.Beatmaps[1].LastLocalUpdate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

            var addedLately = TestResources.CreateTestBeatmapSetInfo(3);
            addedLately.DateAdded = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);

            var results = await runSorting(SortMode.DateUpdated, [addedEarlier, addedLongAgoButSavedLately, addedLately]);

            Assert.That(results.Select(b => b.BeatmapSet).Distinct(), Is.EqualTo(new[] { addedLongAgoButSavedLately, addedLately, addedEarlier }));
        }

        [Test]
        public async Task TestSortingDateUpdatedOrdersSplitDifficultiesByTheirOwnDates()
        {
            var set = TestResources.CreateTestBeatmapSetInfo(3);
            set.DateAdded = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            set.Beatmaps[1].LastLocalUpdate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

            var other = TestResources.CreateTestBeatmapSetInfo(1);
            other.DateAdded = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);

            var results = (await runSorting(SortMode.DateUpdated, [set, other], GroupMode.Difficulty)).ToList();

            Assert.That(results[0], Is.EqualTo(set.Beatmaps[1]));
            Assert.That(results[1], Is.EqualTo(other.Beatmaps[0]));
            Assert.That(results.Skip(2), Is.EquivalentTo(new[] { set.Beatmaps[0], set.Beatmaps[2] }));
        }

        [Test]
        public async Task TestDateUpdatedSplitsDifficulties()
        {
            var set = TestResources.CreateTestBeatmapSetInfo(3);
            set.DateAdded = new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);
            set.Beatmaps[1].LastLocalUpdate = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

            var other = TestResources.CreateTestBeatmapSetInfo(1);
            other.DateAdded = new DateTimeOffset(2025, 3, 1, 0, 0, 0, TimeSpan.Zero);

            var criteria = new FilterCriteria { Sort = SortMode.DateUpdated, DateUpdatedSplitsDifficulties = true };

            Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(criteria), Is.False);
            Assert.That(BeatmapCarouselFilterGrouping.ShouldGroupBeatmapsTogether(new FilterCriteria { Sort = SortMode.Title, DateUpdatedSplitsDifficulties = true }), Is.True);

            var results = (await runSorting(criteria, [set, other])).ToList();

            Assert.That(results[0], Is.EqualTo(set.Beatmaps[1]));
            Assert.That(results[1], Is.EqualTo(other.Beatmaps[0]));
        }

        [Test]
        public async Task TestSortByBpmUsesTitleAsTiebreaker()
        {
            List<BeatmapSetInfo> beatmapSets = [];

            // 2 sets with same BPM but different titles
            const int diff_count = 1;
            {
                var set = TestResources.CreateTestBeatmapSetInfo(diff_count);
                set.DateAdded = new DateTimeOffset(2025, 6, 11, 10, 0, 0, TimeSpan.Zero);
                set.Beatmaps.ForEach(b =>
                {
                    b.ID = Guid.Parse("00000000-0000-0000-0000-000000000000");
                    b.BPM = 175;
                    b.Metadata.Title = "ZZZ";
                });
                beatmapSets.Add(set);
            }
            {
                var set = TestResources.CreateTestBeatmapSetInfo(diff_count);
                set.DateAdded = new DateTimeOffset(2025, 6, 10, 10, 0, 0, TimeSpan.Zero);
                set.Beatmaps.ForEach(b =>
                {
                    b.ID = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
                    b.BPM = 175;
                    b.Metadata.Title = "AAA";
                });
                beatmapSets.Add(set);
            }

            var results = (await runSorting(SortMode.BPM, beatmapSets)).ToList();

            Assert.That(results[0].Metadata.Title, Is.EqualTo("AAA"));
            Assert.That(results[1].Metadata.Title, Is.EqualTo("ZZZ"));
        }

        [Test]
        public async Task TestSortByArtistUsesTitleAsTiebreaker()
        {
            List<BeatmapSetInfo> beatmapSets = new List<BeatmapSetInfo>();

            const int diff_count = 5;

            for (int i = 0; i < 20; i++)
            {
                var set = TestResources.CreateTestBeatmapSetInfo(diff_count);

                if (i == 4)
                {
                    set.Beatmaps.ForEach(b =>
                    {
                        b.Metadata.Artist = "ZZZ";
                        b.Metadata.Title = "AAA";
                    });
                }

                if (i == 8)
                {
                    set.Beatmaps.ForEach(b =>
                    {
                        b.Metadata.Artist = "ZZZ";
                        b.Metadata.Title = "ZZZ";
                    });
                }

                beatmapSets.Add(set);
            }

            var results = await runSorting(SortMode.Artist, beatmapSets);

            Assert.That(() =>
            {
                var lastItem = results.Last();
                return lastItem.Metadata.Artist == "ZZZ" && lastItem.Metadata.Title == "ZZZ";
            });

            Assert.That(() =>
            {
                var secondLastItem = results.SkipLast(diff_count).Last();
                return secondLastItem.Metadata.Artist == "ZZZ" && secondLastItem.Metadata.Title == "AAA";
            });
        }

        /// <summary>
        /// Ensures stability is maintained on different sort modes for items with equal properties.
        /// </summary>
        [Test]
        public async Task TestSortingStabilityDateAdded()
        {
            List<BeatmapSetInfo> beatmapSets = new List<BeatmapSetInfo>();

            for (int i = 0; i < 10; i++)
            {
                var set = TestResources.CreateTestBeatmapSetInfo();

                set.DateAdded = DateTimeOffset.FromUnixTimeSeconds(i);

                // only need to set the first as they are a shared reference.
                var beatmap = set.Beatmaps.First();

                beatmap.Metadata.Artist = "a";
                beatmap.Metadata.Title = "b";

                beatmapSets.Add(set);
            }

            var results = await runSorting(SortMode.Title, beatmapSets);

            Assert.That(results.Select(b => b.BeatmapSet!.DateAdded), Is.Ordered.Descending);

            results = await runSorting(SortMode.Artist, beatmapSets);

            Assert.That(results.Select(b => b.BeatmapSet!.DateAdded), Is.Ordered.Descending);
        }

        private static Task<IEnumerable<BeatmapInfo>> runSorting(SortMode sort, List<BeatmapSetInfo> beatmapSets, GroupMode group = GroupMode.None)
            => runSorting(new FilterCriteria { Sort = sort, Group = group }, beatmapSets);

        private static async Task<IEnumerable<BeatmapInfo>> runSorting(FilterCriteria criteria, List<BeatmapSetInfo> beatmapSets)
        {
            var sorter = new BeatmapCarouselFilterSorting(() => criteria);
            var carouselItems = await sorter.Run(beatmapSets.SelectMany(s => s.Beatmaps.Select(b => new CarouselItem(b))), CancellationToken.None);
            return carouselItems.Select(ci => ci.Model).OfType<BeatmapInfo>();
        }
    }
}
