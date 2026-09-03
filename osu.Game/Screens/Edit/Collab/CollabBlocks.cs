// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// Cuts a beatmap into the stretches a collaboration is divided along.
    /// </summary>
    /// <remarks>
    /// The cuts are the beatmap's own bookmarks and nothing else. A collaboration is agreed in timestamps before any
    /// of it is mapped, and those timestamps are already bookmarks — asking for them a second time in a screen of
    /// their own would be asking the same question twice and leaving two answers to disagree.
    /// </remarks>
    public static class CollabBlocks
    {
        public static List<CollabBlock> For(IBeatmap beatmap, IEnumerable<int> bookmarks)
        {
            var blocks = new List<CollabBlock>();

            if (beatmap.HitObjects.Count == 0)
                return blocks;

            double first = beatmap.HitObjects.Min(h => h.StartTime);
            double last = beatmap.HitObjects.Max(h => h.GetEndTime());

            // a bookmark outside the beatmap cuts nothing: there is no play on the far side of it to give anyone.
            int[] cuts = bookmarks.Distinct().Where(b => b > first && b < last).OrderBy(b => b).ToArray();

            double start = first;
            int key = CollabBlock.HEAD;

            foreach (int cut in cuts)
            {
                blocks.Add(block(beatmap, key, start, cut));
                start = cut;
                key = cut;
            }

            blocks.Add(block(beatmap, key, start, last));

            return blocks;
        }

        /// <summary>
        /// Objects are counted by where they begin, so one held across a cut belongs to the block it was hit in. Its
        /// time on the far side still counts towards that side's drain, which is time the player spends on it.
        /// </summary>
        private static CollabBlock block(IBeatmap beatmap, int key, double start, double end)
        {
            int count = beatmap.HitObjects.Count(h => h.StartTime >= start && h.StartTime < end);

            // the last block has to take the object that ends it, which starts before its own end.
            if (end >= beatmap.HitObjects.Max(h => h.GetEndTime()))
                count = beatmap.HitObjects.Count(h => h.StartTime >= start);

            return new CollabBlock(key, start, end, ComposeStatusDisplay.DrainTimeBetween(beatmap, start, end), count);
        }
    }
}
