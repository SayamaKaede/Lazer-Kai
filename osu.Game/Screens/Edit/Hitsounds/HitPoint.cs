// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// One set of sounds an object plays: its own samples, or those of one of its nodes — the head, a reverse or the tail
    /// of a slider.
    /// </summary>
    /// <param name="HitObject">The object the sounds belong to.</param>
    /// <param name="Node">The node whose sounds these are, or null for the object's own.</param>
    /// <param name="Time">When they are played.</param>
    public readonly record struct HitPoint(HitObject HitObject, int? Node, double Time)
    {
        public IList<HitSampleInfo> Samples => Node is int node ? ((IHasRepeats)HitObject).NodeSamples[node] : HitObject.Samples;

        public void SetSamples(IList<HitSampleInfo> samples)
        {
            if (Node is int node)
                ((IHasRepeats)HitObject).NodeSamples[node] = samples;
            else
                HitObject.Samples = samples;
        }

        /// <summary>
        /// Whether these are the sounds a slider plays as it reaches its end.
        /// </summary>
        public bool IsTail => Node is int node && node == ((IHasRepeats)HitObject).NodeSamples.Count - 1;
    }

    public static class HitPoints
    {
        /// <summary>
        /// How far from the playhead a point still counts as at it: the clock sits on a beat worked out to a fraction of
        /// a millisecond, where a beatmap from stable has its objects on whole ones.
        /// </summary>
        public const double LENIENCY = 5;

        /// <summary>
        /// Every point <paramref name="hitObject"/> plays sounds at, as they are heard: each node of something that has
        /// them, and otherwise the object's own sounds — at its end, for a spinner.
        /// </summary>
        /// <param name="hitObject">The object.</param>
        /// <param name="durationSoundsAtEnd">Whether an object with a length and no nodes is heard as it ends, as an osu!
        /// spinner is. A hold note in mania is heard as it starts.</param>
        public static IEnumerable<HitPoint> Of(HitObject hitObject, bool durationSoundsAtEnd)
        {
            if (hitObject is IHasRepeats repeats && repeats.NodeSamples.Count > 0)
            {
                double spanDuration = repeats.Duration / repeats.SpanCount();

                for (int i = 0; i < repeats.NodeSamples.Count; i++)
                    yield return new HitPoint(hitObject, i, hitObject.StartTime + i * spanDuration);

                yield break;
            }

            yield return new HitPoint(hitObject, null, durationSoundsAtEnd && hitObject is IHasDuration duration ? duration.EndTime : hitObject.StartTime);
        }

        /// <summary>
        /// Every set of sounds on <paramref name="hitObjects"/>: what changing the hitsounds of a selection changes, as
        /// the compose screen does.
        /// </summary>
        public static IEnumerable<HitPoint> OfSelection(IEnumerable<HitObject> hitObjects)
        {
            foreach (var hitObject in hitObjects)
            {
                yield return new HitPoint(hitObject, null, hitObject.StartTime);

                if (hitObject is IHasRepeats repeats)
                {
                    for (int i = 0; i < repeats.NodeSamples.Count; i++)
                        yield return new HitPoint(hitObject, i, hitObject.StartTime);
                }
            }
        }

        /// <summary>
        /// The points sounding at <paramref name="time"/>: those at the time nearest to it, if that is within
        /// <see cref="LENIENCY"/>.
        /// </summary>
        public static IReadOnlyList<HitPoint> At(IEnumerable<HitObject> hitObjects, double time, bool durationSoundsAtEnd)
        {
            var near = hitObjects.SelectMany(h => Of(h, durationSoundsAtEnd)).Where(p => Math.Abs(p.Time - time) <= LENIENCY).ToArray();

            if (near.Length == 0)
                return Array.Empty<HitPoint>();

            double nearest = near.MinBy(p => Math.Abs(p.Time - time)).Time;
            return near.Where(p => Math.Abs(p.Time - nearest) < 1).ToArray();
        }

        /// <summary>
        /// The points heard last, at or before <paramref name="time"/>: what the playhead has just passed while playing.
        /// </summary>
        public static IReadOnlyList<HitPoint> LastAtOrBefore(IEnumerable<HitObject> hitObjects, double time, bool durationSoundsAtEnd)
        {
            var passed = hitObjects.SelectMany(h => Of(h, durationSoundsAtEnd)).Where(p => p.Time <= time + LENIENCY).ToArray();

            if (passed.Length == 0)
                return Array.Empty<HitPoint>();

            double latest = passed.Max(p => p.Time);
            return passed.Where(p => Math.Abs(p.Time - latest) < 1).ToArray();
        }

        /// <summary>
        /// The points heard last before <paramref name="time"/>, leaving out any heard with what is at it.
        /// </summary>
        public static IReadOnlyList<HitPoint> LastBefore(IEnumerable<HitObject> hitObjects, double time, bool durationSoundsAtEnd)
        {
            var before = hitObjects.SelectMany(h => Of(h, durationSoundsAtEnd)).Where(p => p.Time <= time - 1).ToArray();

            if (before.Length == 0)
                return Array.Empty<HitPoint>();

            double latest = before.Max(p => p.Time);
            return before.Where(p => Math.Abs(p.Time - latest) < 1).ToArray();
        }
    }
}
