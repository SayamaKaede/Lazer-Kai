// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// Writes a colour layout into the beatmap being edited.
    /// </summary>
    public static class ComboColourApplier
    {
        /// <summary>
        /// Applies the layout, replacing every combo skip in the beatmap.
        /// </summary>
        /// <remarks>
        /// Done inside one change so the whole thing comes back with a single undo. Every combo is written, not only
        /// the ones the layout moves: a skip left over from a previous layout would otherwise survive and throw the
        /// colours off further down the map.
        /// </remarks>
        /// <returns>How many combos ended up with a different skip than before.</returns>
        public static int Apply(EditorBeatmap beatmap, ComboColourProject project, int colourCount)
        {
            var (combos, objects) = ComboColourAssignment.CombosOf(beatmap.HitObjects.ToList());

            int[] skips = ComboColourAssignment.Calculate(combos, project, colourCount);

            int changed = 0;

            beatmap.BeginChange();

            for (int i = 0; i < objects.Count; i++)
            {
                if (objects[i].ComboOffset == skips[i])
                    continue;

                objects[i].ComboOffset = skips[i];

                // a skip only takes effect on an object which starts a combo, and the first object of the beatmap does
                // so without the flag being set.
                if (skips[i] != 0)
                    objects[i].NewCombo = true;

                beatmap.Update((Rulesets.Objects.HitObject)objects[i]);
                changed++;
            }

            beatmap.EndChange();

            return changed;
        }
    }
}
