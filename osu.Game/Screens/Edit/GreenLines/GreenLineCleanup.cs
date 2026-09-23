// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Screens.Edit.GreenLines
{
    /// <summary>
    /// Finds and takes out the green lines that change nothing stable plays or shows.
    /// </summary>
    /// <remarks>
    /// A green line changes nothing when it says what the line before it does, or when another line at the same time
    /// replaces it. Red lines are never taken out: they are the timing.
    ///
    /// A line nothing reads yet stays, as it does in stable's own clean-up: the next object put under it reads it, and
    /// mappers leave such lines in on purpose to set the index back between notes.
    ///
    /// Taking one out can leave the next saying what the one before it now does, so it goes round until none is left.
    /// </remarks>
    public static class GreenLineCleanup
    {
        /// <summary>
        /// Takes out every green line that changes nothing, as one step to undo.
        /// </summary>
        /// <returns>How many were taken out.</returns>
        public static int Remove(EditorBeatmap beatmap, SavedTimingLines lines)
        {
            var unneeded = Unneeded(lines.Lines.ToList());

            if (unneeded.Count == 0)
                return 0;

            beatmap.BeginChange();

            foreach (var line in unneeded)
                lines.Lines.Remove(line);

            beatmap.EndChange();
            return unneeded.Count;
        }

        /// <summary>
        /// Every green line that changes nothing, in the order they are found.
        /// </summary>
        public static List<SavedTimingLine> Unneeded(IReadOnlyList<SavedTimingLine> lines)
        {
            var kept = lines.ToList();
            var removed = new List<SavedTimingLine>();

            int i = 0;

            while (i < kept.Count)
            {
                if (!kept[i].Uninherited && isUnneeded(kept, i))
                {
                    removed.Add(kept[i]);
                    kept.RemoveAt(i);

                    // the line after it now comes straight after the one before, and may say what it does.
                    i = Math.Max(0, i - 1);
                    continue;
                }

                i++;
            }

            return removed;
        }

        private static bool isUnneeded(List<SavedTimingLine> lines, int index)
        {
            var line = lines[index];

            // another line at the same time is read in its place, for everything.
            if (index + 1 < lines.Count && lines[index + 1].Time == line.Time)
                return true;

            if (index == 0)
                return false;

            var before = lines[index - 1];

            // what a slider under the line before gets: always 1 under a red line.
            double velocityBefore = before.Uninherited ? 1 : before.ClampedSliderVelocity;

            return line.SampleSet == before.SampleSet && line.SampleIndex == before.SampleIndex && line.Volume == before.Volume
                   && line.Kiai == before.Kiai && line.ClampedSliderVelocity == velocityBefore;
        }
    }
}
