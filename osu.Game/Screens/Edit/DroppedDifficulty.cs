// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// What happens to a `.osu` file dropped onto the editor.
    /// </summary>
    /// <remarks>
    /// Kept apart from the editor so that the decision can be read and tested without one. There is not much to
    /// it — whether the set already has that difficulty, and whether it is the one being looked at — but the
    /// answer decides whether work is destroyed, so it is worth being able to point at.
    /// </remarks>
    public static class DroppedDifficulty
    {
        public enum Outcome
        {
            /// <summary>
            /// Nothing in the set answers to that name and mode; it joins as a new difficulty.
            /// </summary>
            Added,

            /// <summary>
            /// A difficulty of that name and mode is in the set, and is replaced.
            /// </summary>
            Replaced,

            /// <summary>
            /// The difficulty being replaced is the one open in the editor. The only case where unsaved work is
            /// at stake, and so the only one worth stopping to ask about.
            /// </summary>
            ReplacedCurrent,

            /// <summary>
            /// The name is already in the set under a different mode. Nothing is done: the file cannot replace a
            /// difficulty it does not match, and adding it would leave the set with two difficulties of one name.
            /// </summary>
            NameTaken,
        }

        /// <summary>
        /// Works out what a dropped file would do to the set.
        /// </summary>
        /// <param name="set">The set the editor has open.</param>
        /// <param name="current">The difficulty the editor is showing.</param>
        /// <param name="difficultyName">The name read out of the dropped file.</param>
        /// <param name="rulesetId">The mode read out of the dropped file.</param>
        /// <param name="target">The difficulty that would be replaced, if any.</param>
        public static Outcome DecideFor(BeatmapSetInfo set, BeatmapInfo? current, string difficultyName, int rulesetId, out BeatmapInfo? target)
        {
            // matched the way a person reads it rather than the way the file stores it: somebody dropping
            // "kaede's extra.osu" over "Kaede's Extra" means the one that is there.
            List<BeatmapInfo> sharingName = set.Beatmaps
                                               .Where(b => string.Equals(b.DifficultyName, difficultyName, StringComparison.OrdinalIgnoreCase))
                                               .ToList();

            // a set can hold one name per mode — converted sets routinely carry a "Normal" in each of them — so
            // the mode is part of asking which difficulty a file means.
            target = sharingName.FirstOrDefault(b => b.Ruleset.OnlineID == rulesetId);

            if (target == null)
                return sharingName.Count > 0 ? Outcome.NameTaken : Outcome.Added;

            if (current != null && target.ID == current.ID)
                return Outcome.ReplacedCurrent;

            return Outcome.Replaced;
        }
    }
}
