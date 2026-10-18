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
    ///
    /// The stretches are the ones between two bookmarks, so the beatmap's two ends are bookmarked as well (2026-10-18).
    /// They used to run from the first object to the last instead, which left nothing to divide until the beatmap was
    /// mapped — and how long each collaborator has is wanted before that, to agree the split in the first place.
    /// </remarks>
    public static class CollabBlocks
    {
        public static List<CollabBlock> For(IBeatmap beatmap, IEnumerable<int> bookmarks)
        {
            int[] cuts = bookmarks.Distinct().OrderBy(b => b).ToArray();

            var blocks = new List<CollabBlock>();

            for (int i = 0; i + 1 < cuts.Length; i++)
                blocks.Add(block(beatmap, cuts[i], cuts[i + 1], last: i + 2 == cuts.Length));

            return blocks;
        }

        /// <summary>
        /// Objects are counted by where they begin, so one held across a cut belongs to the block it was hit in. Its
        /// time on the far side still counts towards that side's drain, which is time the player spends on it. The last
        /// block takes an object on the bookmark it ends at as well, there being no block after it to.
        /// </summary>
        private static CollabBlock block(IBeatmap beatmap, int start, int end, bool last)
        {
            int count = beatmap.HitObjects.Count(h => h.StartTime >= start && (h.StartTime < end || (last && h.StartTime <= end)));

            return new CollabBlock(start, start, end, ComposeStatusDisplay.DrainTimeBetween(beatmap, start, end), count);
        }
    }
}
