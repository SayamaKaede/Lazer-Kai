// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// How loud a beatmap is over its length, as the moments where that changes.
    /// </summary>
    /// <remarks>
    /// Read off the objects rather than off the control points. A legacy green line that only moves the volume is
    /// decoded onto the objects it covers and leaves no control point behind, so a beatmap timed that way -- which
    /// is most of them -- would look as though its volume never moved at all.
    /// </remarks>
    public static class VolumeCurve
    {
        /// <summary>A moment the volume is known at.</summary>
        public readonly record struct Point(double Time, int Volume);

        /// <summary>The room left above and below the curve, as a fraction of the height it is drawn in.</summary>
        public const float VERTICAL_PADDING = 0.12f;

        /// <summary>
        /// Lays the curve out as a line, in the space it is drawn in.
        /// </summary>
        /// <param name="points">The moments the volume changes at.</param>
        /// <param name="until">Where the last volume is held to, which is the end of the last object.</param>
        /// <param name="trackLength">
        /// How long the song is. Time is placed against this rather than against the stretch the objects cover:
        /// every other part of the summary timeline is laid out across the track, and a curve laid out across the
        /// objects instead would drift further from the playhead the further along it went.
        /// </param>
        /// <param name="size">The size of the space to lay it out in.</param>
        /// <remarks>
        /// Stepped rather than run from one point to the next: a volume holds until something changes it, so a slope
        /// between two moments would draw a fade the beatmap does not have.
        /// </remarks>
        public static IReadOnlyList<Vector2> Trace(IReadOnlyList<Point> points, double until, double trackLength, Vector2 size)
        {
            var vertices = new List<Vector2>();

            if (points.Count == 0 || trackLength <= 0 || size.X <= 0 || size.Y <= 0)
                return vertices;

            foreach (var point in points)
            {
                var at = new Vector2(xOf(point.Time), yOf(point.Volume));

                // held until something changes it: across to the new moment at the old height, then up or down.
                if (vertices.Count > 0)
                    vertices.Add(new Vector2(at.X, vertices[^1].Y));

                vertices.Add(at);
            }

            // and held to the end of the last object, so the final stretch is not left looking unset.
            if (until > points[^1].Time)
                vertices.Add(new Vector2(xOf(until), vertices[^1].Y));

            return vertices;

            float xOf(double time) => (float)(time / trackLength) * size.X;

            float yOf(int volume)
            {
                float room = size.Y * (1 - VERTICAL_PADDING * 2);

                // volume runs up the screen, so a hundred per cent belongs at the top rather than at the bottom.
                return size.Y * VERTICAL_PADDING + room * (1 - Math.Clamp(volume, 0, 100) / 100f);
            }
        }

        /// <summary>
        /// Reads the volume out of a beatmap, in time order, one point per moment it is set at.
        /// </summary>
        /// <remarks>
        /// A slider's head, its repeats and its tail each carry their own volume, so each is a moment of its own --
        /// a slider that fades out over its length is the sort of thing this exists to show. Moments that repeat the
        /// volume already in effect are dropped: they draw nothing and the beatmaps that set a volume on every
        /// object would otherwise fill the curve with thousands of them.
        /// </remarks>
        public static IReadOnlyList<Point> Read(IBeatmap beatmap)
        {
            var points = new List<Point>();
            int last = -1;

            foreach (var hitObject in beatmap.HitObjects)
            {
                foreach ((double time, int volume) in momentsOf(hitObject))
                {
                    if (volume == last)
                        continue;

                    points.Add(new Point(time, volume));
                    last = volume;
                }
            }

            return points;
        }

        private static IEnumerable<(double time, int volume)> momentsOf(HitObject hitObject)
        {
            yield return (hitObject.StartTime, volumeOf(hitObject.Samples));

            if (hitObject is not IHasRepeats repeats || repeats.NodeSamples.Count == 0)
                yield break;

            double lengthOfOneSpan = (hitObject.GetEndTime() - hitObject.StartTime) / repeats.SpanCount();

            // the first node is the head, which has already been given above.
            for (int node = 1; node < repeats.NodeSamples.Count; node++)
                yield return (hitObject.StartTime + lengthOfOneSpan * node, volumeOf(repeats.NodeSamples[node]));
        }

        /// <remarks>
        /// The loudest of them: an object's samples are played together, so what it sounds like is the loudest one
        /// rather than an average of a normal and whatever additions sit on it.
        /// </remarks>
        private static int volumeOf(IList<Audio.HitSampleInfo> samples) => samples.Count > 0 ? samples.Max(s => s.Volume) : 100;
    }
}
