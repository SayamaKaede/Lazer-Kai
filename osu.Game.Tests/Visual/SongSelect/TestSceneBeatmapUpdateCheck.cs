// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Game.Beatmaps;
using osu.Game.Database;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// The update mark waits to be told a beatmap changed, so song select can ask about one now.
    /// </summary>
    public partial class TestSceneBeatmapUpdateCheck : SongSelectTestScene
    {
        [Cached(typeof(IBeatmapUpdater))]
        private readonly TestBeatmapUpdater updater = new TestBeatmapUpdater();

        [Test]
        public void TestAsksOnlineAboutTheBeatmapInTheDatabase()
        {
            BeatmapSetInfo beatmapSet = null!;

            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();

            AddStep("check the imported set for an update", () =>
            {
                // song select hands out copies detached from the database, which is what the panels carry.
                beatmapSet = Beatmaps.GetAllUsableBeatmapSets().Last();
                updater.Asked.Clear();
                SongSelect.CheckForUpdate(beatmapSet);
            });

            AddUntilStep("asked about that set", () => updater.Asked.Any(a => a.id == beatmapSet.ID));
            AddAssert("asked online rather than the cache", () => updater.Asked.Single(a => a.id == beatmapSet.ID).scope, () => Is.EqualTo(MetadataLookupScope.OnlineFirst));
            AddAssert("asked about the one in the database", () => updater.Asked.Single(a => a.id == beatmapSet.ID).managed, () => Is.True);
        }

        private class TestBeatmapUpdater : IBeatmapUpdater
        {
            public readonly ConcurrentBag<(Guid id, MetadataLookupScope scope, bool managed)> Asked = new ConcurrentBag<(Guid, MetadataLookupScope, bool)>();

            public void Queue(Live<BeatmapSetInfo> beatmapSet, MetadataLookupScope lookupScope = MetadataLookupScope.LocalCacheFirst)
                => beatmapSet.PerformRead(s => Process(s, lookupScope));

            public void Process(BeatmapSetInfo beatmapSet, MetadataLookupScope lookupScope = MetadataLookupScope.LocalCacheFirst)
                => Asked.Add((beatmapSet.ID, lookupScope, beatmapSet.IsManaged));

            public void ProcessObjectCounts(BeatmapInfo beatmapInfo, MetadataLookupScope lookupScope = MetadataLookupScope.LocalCacheFirst)
            {
            }

            public void Dispose()
            {
            }
        }
    }
}
