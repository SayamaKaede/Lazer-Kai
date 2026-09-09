// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components.Timeline;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// How a beatmap was built, read off the beatmap itself.
    /// </summary>
    /// <remarks>
    /// Song select says what a beatmap is called, how hard it is and how many objects it has. It says nothing about
    /// how it was put together, which is what someone reading it as a mapper came for, and the only way to find that
    /// out has been to open it in the editor.
    /// </remarks>
    public class MappingSummary
    {
        /// <summary>Objects a second, over the stretch the beatmap actually plays for.</summary>
        public double Density { get; private init; }

        /// <summary>The beat divisors the beatmap is snapped to, ascending.</summary>
        public IReadOnlyList<int> Divisors { get; private init; } = Array.Empty<int>();

        /// <summary>
        /// Moments the editor would mark as off the beat.
        /// </summary>
        /// <remarks>
        /// Counted the way the editor marks them, at <see cref="UnsnapMarker.TOLERATED_MS"/> and per moment rather
        /// than per object -- an object's end and its repeats each land somewhere of their own and are each marked.
        /// Answering differently here would have song select and the editor disagreeing about the same beatmap.
        /// </remarks>
        public int UnsnappedObjects { get; private init; }

        /// <summary>How many times the beatmap changes tempo, which is how much of it is timed by hand.</summary>
        public int TimingPoints { get; private init; }

        /// <remarks>
        /// Green lines are not counted alongside them. A legacy green line that only moves the volume or the slider
        /// velocity is decoded onto the objects themselves and leaves no control point behind, so a count of what is
        /// left would read as a number of green lines while being nothing of the sort.
        /// </remarks>
        public int LowestVolume { get; private init; }

        public int HighestVolume { get; private init; }

        public double LowestVelocity { get; private init; }
        public double HighestVelocity { get; private init; }

        public double KiaiTime { get; private init; }

        /// <summary>How long the beatmap asks nothing of the player.</summary>
        public double BreakTime { get; private init; }

        public static MappingSummary Read(IBeatmap beatmap)
        {
            var objects = beatmap.HitObjects;
            var controlPoints = beatmap.ControlPointInfo;

            double start = objects.Count > 0 ? objects[0].StartTime : 0;
            double end = objects.Count > 0 ? objects.Max(h => h.GetEndTime()) : 0;
            double played = Math.Max(end - start, 1);

            var samples = objects.SelectMany(allSamplesOf).ToArray();
            var velocities = objects.OfType<IHasSliderVelocity>().Select(v => v.SliderVelocityMultiplier).ToArray();

            var divisors = new SortedSet<int>();
            int unsnapped = 0;

            foreach (var hitObject in objects)
            {
                int divisor = divisorOf(controlPoints, hitObject.StartTime);

                if (divisor > 0)
                    divisors.Add(divisor);

                foreach (double moment in markedMomentsOf(hitObject))
                {
                    if (isUnsnapped(controlPoints, moment))
                        unsnapped++;
                }
            }

            return new MappingSummary
            {
                Density = objects.Count / (played / 1000),

                Divisors = divisors.ToArray(),
                UnsnappedObjects = unsnapped,

                TimingPoints = controlPoints.TimingPoints.Count,

                LowestVolume = samples.Length > 0 ? samples.Min(s => s.Volume) : 100,
                HighestVolume = samples.Length > 0 ? samples.Max(s => s.Volume) : 100,

                LowestVelocity = velocities.Length > 0 ? velocities.Min() : 1,
                HighestVelocity = velocities.Length > 0 ? velocities.Max() : 1,

                KiaiTime = kiaiTime(controlPoints, end),
                BreakTime = beatmap.Breaks.Sum(b => b.Duration),
            };
        }

        /// <summary>
        /// The divisor <paramref name="time"/> is snapped to, or 0 when it is snapped to nothing.
        /// </summary>
        /// <remarks>
        /// The closest divisor is always some divisor, so asking for it alone would report an object sitting between
        /// two snaps as 1/16 rather than as the mistake it is. Which is worth knowing on its own.
        /// </remarks>
        private static int divisorOf(ControlPointInfo controlPoints, double time)
        {
            if (controlPoints.TimingPoints.Count == 0)
                return 0;

            int divisor = controlPoints.GetClosestBeatDivisor(time);

            return Math.Abs(controlPoints.GetClosestSnappedTime(time, divisor) - time) <= UnsnapMarker.TOLERATED_MS ? divisor : 0;
        }

        /// <summary>
        /// Whether the editor would put a mark over <paramref name="time"/>.
        /// </summary>
        private static bool isUnsnapped(ControlPointInfo controlPoints, double time) =>
            controlPoints.TimingPoints.Count > 0
            && Math.Abs(time - controlPoints.GetClosestSnappedTime(time)) > UnsnapMarker.TOLERATED_MS;

        /// <summary>
        /// The moments of an object the editor puts a mark over: where it starts, where it ends, and each repeat.
        /// </summary>
        private static IEnumerable<double> markedMomentsOf(HitObject hitObject)
        {
            yield return hitObject.StartTime;

            if (hitObject is not IHasDuration duration)
                yield break;

            yield return duration.EndTime;

            if (hitObject is not IHasRepeats repeats)
                yield break;

            double lengthOfOneSpan = (duration.EndTime - hitObject.StartTime) / repeats.SpanCount();

            for (int repeat = 1; repeat <= repeats.RepeatCount; repeat++)
                yield return hitObject.StartTime + lengthOfOneSpan * repeat;
        }

        /// <remarks>
        /// A slider's own samples say how its head is sounded; its ends and repeats carry their own. Counting only
        /// the first would read a beatmap sounded entirely on slider ends as having no hitsounds at all.
        /// </remarks>
        private static IEnumerable<HitSampleInfo> allSamplesOf(HitObject hitObject)
        {
            foreach (var sample in hitObject.Samples)
                yield return sample;

            if (hitObject is not IHasRepeats withRepeats)
                yield break;

            foreach (var node in withRepeats.NodeSamples)
            {
                foreach (var sample in node)
                    yield return sample;
            }
        }

        private static double kiaiTime(ControlPointInfo controlPoints, double end)
        {
            double total = 0;
            double? openedAt = null;

            foreach (var effect in controlPoints.EffectPoints)
            {
                if (effect.KiaiMode && openedAt == null)
                    openedAt = effect.Time;
                else if (!effect.KiaiMode && openedAt != null)
                {
                    total += effect.Time - openedAt.Value;
                    openedAt = null;
                }
            }

            // a section left open runs to the end of the beatmap, which is where it stops mattering.
            if (openedAt != null)
                total += Math.Max(end - openedAt.Value, 0);

            return total;
        }
    }
}
