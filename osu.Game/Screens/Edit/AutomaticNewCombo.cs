// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Decides whether an object just placed by hand should start a combo.
    /// </summary>
    /// <remarks>
    /// A combo a bar is the usual reading, and doing it by hand means pressing the new combo button on the first
    /// object of every bar for the length of a beatmap.
    ///
    /// What cannot be done by position alone is the phrase that starts early. A note sitting just before a bar line
    /// might be the last of the bar it is in or the first of the bar it is about to open, and nothing about where it
    /// sits says which. What says it is whether it carries over: a slider laid down before the line whose body runs
    /// past it belongs to the bar it runs into.
    ///
    /// Nothing already placed is touched. This only ever answers for the object being put down.
    /// </remarks>
    public static class AutomaticNewCombo
    {
        /// <summary>
        /// How far from a line an object still counts as being on it.
        /// </summary>
        /// <remarks>
        /// Placed objects are snapped, so this is for the rounding rather than for any real leeway.
        /// </remarks>
        private const double leniency = 1;

        /// <summary>
        /// Whether the object being placed should be given a new combo.
        /// </summary>
        /// <param name="placed">The object about to go in.</param>
        /// <param name="controlPoints">The beatmap's timing, for working out where the lines are.</param>
        /// <param name="existing">Everything already in the beatmap.</param>
        /// <param name="bars">How many bars apart the combos are wanted.</param>
        public static bool ShouldStartCombo(HitObject placed, ControlPointInfo controlPoints, IEnumerable<HitObject> existing, double bars)
        {
            double line = LineOwning(placed, controlPoints, bars);

            var sharing = existing.OfType<IHasComboInformation>()
                                  .Where(h => LineOwning((HitObject)h, controlPoints, bars) == line)
                                  .ToArray();

            // not the first of its stretch: the combo belongs to whatever opens it, not to a note added inside.
            if (sharing.Any(h => ((HitObject)h).StartTime < placed.StartTime - leniency))
                return false;

            // the stretch already reads as its own, which is what a phrase that started early has done to it.
            return !sharing.Any(h => h.NewCombo);
        }

        /// <summary>
        /// The line an object belongs to.
        /// </summary>
        /// <remarks>
        /// Usually the one it is at or after. The exception is an object that begins after a line and carries over
        /// the next: that is a phrase starting early, and it belongs to the stretch it runs into rather than to the
        /// one it began in. An object beginning on a line opens that line's stretch however long it runs for.
        /// </remarks>
        public static double LineOwning(HitObject hitObject, ControlPointInfo controlPoints, double bars)
        {
            double time = hitObject.StartTime;

            var timing = controlPoints.TimingPointAt(time);

            double length = timing.BeatLength * timing.TimeSignature.Numerator * bars;

            if (length <= 0)
                return time;

            double start = timing.Time + Math.Floor((time - timing.Time) / length + leniency / length) * length;
            double next = start + length;

            bool beginsOnTheLine = time <= start + leniency;
            bool carriesOver = hitObject.GetEndTime() > next + leniency;

            return !beginsOnTheLine && carriesOver ? next : start;
        }
    }
}
