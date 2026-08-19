// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// Works out the combo skip every new combo needs for the colours to land where the colour points ask for.
    /// </summary>
    /// <remarks>
    /// A translation of the same calculation in OliBomby's Mapping Tools (MIT), so that a layout built there and one
    /// built here come out the same. Kept as a plain function over plain values: the result is a set of combo skips,
    /// which the caller writes to the beatmap.
    ///
    /// Each new combo advances the colour by one on its own, so a skip of `n` lands `n + 1` colours further along.
    /// </remarks>
    public static class ComboColourAssignment
    {
        /// <summary>
        /// A single combo, as far as this calculation is concerned.
        /// </summary>
        /// <param name="Time">When the combo starts.</param>
        /// <param name="Length">How many objects are in it, used to decide whether a burst point may take it.</param>
        public readonly record struct Combo(double Time, int Length);

        /// <summary>
        /// Points may sit a hair off the object they were placed on, so times are compared with this much slack.
        /// </summary>
        private const double time_tolerance = 5;

        /// <summary>
        /// Returns the combo skip for each combo, in the order given.
        /// </summary>
        /// <param name="combos">The new combos of the beatmap, in time order, spinners excluded.</param>
        /// <param name="project">The colour points to satisfy.</param>
        /// <param name="colourCount">How many combo colours the beatmap has.</param>
        public static int[] Calculate(IReadOnlyList<Combo> combos, ComboColourProject project, int colourCount)
        {
            ArgumentNullException.ThrowIfNull(combos);
            ArgumentNullException.ThrowIfNull(project);

            var points = project.Points.OrderBy(p => p.Time).ToList();
            int[] skips = new int[combos.Count];

            if (colourCount <= 0 || points.Count == 0)
                return skips;

            var spent = new HashSet<ComboColourPoint>();

            ComboColourPoint? lastPoint = null;
            int lastSequenceIndex = -1;

            // where the palette pointer sits before the first combo. The game adds one per new combo and the
            // editor's palette is rotated one against that, which together put the first combo on exactly the
            // entry its skip names — so counting has to start one below the first entry, not at it. Starting at 0
            // instead lands every colour one place early.
            int lastColour = colourCount - 1;

            for (int i = 0; i < combos.Count; i++)
            {
                var combo = combos[i];
                var point = pointFor(points, combo.Time, spent, combo.Length <= project.MaxBurstLength);

                if (point == null)
                    continue;

                // a burst takes one combo and is then done with, so the next combo falls back to the point behind it.
                if (point.Mode == ComboColourPointMode.Burst)
                    spent.Add(point);

                var sequence = point.Colours;

                // where the colour just used sits in this point's sequence, so a change of point can carry on from it
                // rather than jumping back to the start.
                if (lastSequenceIndex != -1 && !ReferenceEquals(lastPoint, point))
                    lastSequenceIndex = sequence.IndexOf(lastColour);

                int sequenceIndex;

                if (lastSequenceIndex == -1 || sequence.Count == 0)
                    sequenceIndex = 0;
                else if (ReferenceEquals(lastPoint, point))
                    sequenceIndex = mod(lastSequenceIndex + 1, sequence.Count);
                else
                    // starting a new point, begin at its first colour — unless that is the colour just used, in which
                    // case take the second so two combos running do not share a colour.
                    sequenceIndex = lastSequenceIndex == 0 && sequence.Count > 1 ? 1 : 0;

                int colour = sequence.Count == 0
                    ? mod(lastColour + 1, colourCount)
                    : sequence[sequenceIndex];

                if (colour < 0 || colour >= colourCount)
                    throw new InvalidOperationException($"The colour point at {point.Time} asks for combo colour {colour + 1}, but the beatmap only has {colourCount}.");

                skips[i] = mod(mod(colour - lastColour, colourCount) - 1, colourCount);

                lastSequenceIndex = sequenceIndex;
                lastPoint = point;
                lastColour = colour;
            }

            return skips;
        }

        /// <summary>
        /// Builds the combo list from a beatmap's objects: every object which starts a combo, with how long that combo
        /// runs. Spinners are left out, matching how the game skips them when advancing the colour.
        /// </summary>
        public static (List<Combo> Combos, List<IHasComboInformation> Objects) CombosOf(IReadOnlyList<HitObject> hitObjects)
        {
            var combos = new List<Combo>();
            var objects = new List<IHasComboInformation>();

            var starts = new List<int>();

            for (int i = 0; i < hitObjects.Count; i++)
            {
                if (hitObjects[i] is IHasComboInformation combo && (combo.NewCombo || starts.Count == 0))
                    starts.Add(i);
            }

            for (int s = 0; s < starts.Count; s++)
            {
                var hitObject = hitObjects[starts[s]];

                if (hitObject is not IHasComboInformation combo || hitObject is IHasDuration and not IHasPath)
                    continue;

                int end = s + 1 < starts.Count ? starts[s + 1] : hitObjects.Count;

                combos.Add(new Combo(hitObject.StartTime, end - starts[s]));
                objects.Add(combo);
            }

            return (combos, objects);
        }

        private static ComboColourPoint? pointFor(List<ComboColourPoint> points, double time, HashSet<ComboColourPoint> spent, bool burstAllowed)
        {
            // the last point at or before this combo. A burst only counts when it sits on this combo exactly, and only
            // when the combo is short enough to be worth bursting.
            var applicable = points.LastOrDefault(p => !spent.Contains(p)
                                                      && p.Time <= time + time_tolerance
                                                      && (p.Mode != ComboColourPointMode.Burst || (p.Time >= time - time_tolerance && burstAllowed)));

            // before the first point, carry on with the earliest ordinary one rather than leaving the combos untouched.
            return applicable ?? points.FirstOrDefault(p => !spent.Contains(p) && p.Mode != ComboColourPointMode.Burst);
        }

        /// <summary>
        /// A true modulus: the built-in operator can return a negative result, which would not serialise.
        /// </summary>
        private static int mod(int value, int divisor) => value - (int)Math.Floor((double)value / divisor) * divisor;
    }
}
