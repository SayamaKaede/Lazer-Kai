// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Works out where the circles go when a slider is turned into a stream.
    /// </summary>
    /// <remarks>
    /// Kept apart from the panel that drives it so that what it produces can be read and tested without a screen.
    ///
    /// The times are always the beat: the first circle sits where the slider started and every one after it a
    /// division later, with nothing warping them. Everything the settings do, they do to where along the path
    /// each circle is taken from. Moving the times instead would space a stream out beautifully and leave every
    /// circle in it unsnapped, which the timeline would then be marking.
    /// </remarks>
    public static class StreamConversion
    {
        /// <summary>
        /// One circle of a stream: when it is hit, and where.
        /// </summary>
        public readonly record struct Note(double Time, Vector2 Position);

        /// <summary>
        /// What one stretch of a stream is told to do. A shape that turns a corner is laid down a stretch at a
        /// time, so that a circle lands on the corner itself rather than the run stepping over it.
        /// </summary>
        public record struct Stretch
        {
            /// <summary>
            /// How many circles this stretch holds, counting the ones on the anchors at both of its ends. The
            /// circle on a corner belongs to the stretches either side of it and is only laid down once, so a
            /// shape's total is one fewer than these add up to for every corner it turns.
            /// </summary>
            public int Count { get; set; }

            public double Acceleration { get; set; }

            public StreamEasing Easing { get; set; }

            public float Zigzag { get; set; }

            public int ZigzagPeriod { get; set; }

            public ZigzagShape ZigzagShape { get; set; }

            public Stretch(int count)
            {
                Count = count;
                Acceleration = 1;
                Easing = StreamEasing.None;
                Zigzag = 0;
                ZigzagPeriod = 1;
                ZigzagShape = ZigzagShape.Even;
            }
        }

        /// <summary>
        /// Where the slider turns, as progress along the whole of it, repeats and all. A corner is a red anchor:
        /// the point where one segment of the path ends and the next begins. A slider with repeats passes each
        /// corner once a span, and turns round at the end of every span but the last.
        /// </summary>
        /// <remarks>
        /// The start and end of the slider are not among these. What comes back is only the turns in between, in
        /// the order they are reached.
        /// </remarks>
        public static IReadOnlyList<double> CornersOf(Slider slider)
        {
            var ends = slider.Path.GetSegmentEnds().ToList();

            // the last one is the end of the path rather than a corner, and anything at or past the end is not a
            // corner either -- a path told it is longer than it is reports those.
            var corners = ends.Take(Math.Max(ends.Count - 1, 0))
                              .Where(c => c > 0.001 && c < 0.999)
                              .ToList();

            int spans = Math.Max(slider.SpanCount(), 1);
            var turns = new List<double>();

            for (int span = 0; span < spans; span++)
            {
                // every other span runs back along the path, reaching its corners last first.
                var inThisSpan = span % 2 == 0 ? corners : corners.Select(c => 1 - c).Reverse();

                turns.AddRange(inThisSpan.Select(c => (span + c) / spans));

                if (span < spans - 1)
                    turns.Add((double)(span + 1) / spans);
            }

            return turns;
        }

        /// <summary>
        /// The circles a slider becomes, laid down a stretch at a time.
        /// </summary>
        /// <remarks>
        /// Each stretch runs from one corner to the next and puts a circle on both, so the corner itself is played
        /// rather than stepped over. The stretch after it starts from that same circle rather than adding another.
        ///
        /// The times are the beat throughout: one circle every <paramref name="spacing"/>, however the stretches
        /// divide the shape up.
        /// </remarks>
        public static IReadOnlyList<Note> PlanStretches(Slider slider, double spacing, IReadOnlyList<Stretch> stretches,
                                                        Func<double, double>? curve = null)
        {
            var notes = new List<Note>();

            if (spacing <= 0 || stretches.Count == 0)
                return notes;

            var corners = CornersOf(slider);
            double from = 0;

            for (int s = 0; s < stretches.Count; s++)
            {
                var stretch = stretches[s];
                double to = s < corners.Count ? corners[s] : 1;

                // the first circle of a stretch is the last of the one before it, already laid down.
                int first = s == 0 ? 0 : 1;

                for (int i = first; i < stretch.Count; i++)
                {
                    double within = stretch.Count > 1 ? (double)i / (stretch.Count - 1) : 0;

                    float amplitude = stretch.Zigzag * Reach(stretch.ZigzagShape, within);

                    double along = curve != null
                        ? Math.Clamp(curve(Math.Clamp(within, 0, 1)), 0, 1)
                        : Warp(within, stretch.Acceleration, stretch.Easing.ToEasing());

                    double progress = from + (to - from) * along;

                    notes.Add(new Note(slider.StartTime + notes.Count * spacing,
                        PositionAt(slider, progress, notes.Count, amplitude, stretch.ZigzagPeriod)));
                }

                from = to;
            }

            return notes;
        }

        /// <summary>
        /// The circles a slider becomes.
        /// </summary>
        /// <param name="slider">The slider being replaced.</param>
        /// <param name="spacing">Milliseconds between circles — a beat over the chosen division.</param>
        /// <param name="acceleration">
        /// How the distance between circles changes over the stream. 1 leaves it even; above 1 starts them close
        /// together and opens out; below 1 does the reverse. It is the power the progress along the path is
        /// raised to, so the times stay on the beat and only the positions move.
        /// </param>
        /// <param name="easing">A curve laid over the progress, before <paramref name="acceleration"/>.</param>
        /// <param name="zigzag">How far to either side of the path to push circles, in osu! pixels. 0 leaves them on it.</param>
        /// <param name="zigzagPeriod">How many circles in a row go to the same side.</param>
        /// <param name="zigzagShape">How far the zigzag reaches as the stream goes on.</param>
        /// <param name="curve">
        /// A curve drawn by hand, standing in for <paramref name="easing"/> when there is one. The panel offers
        /// one or the other rather than both, since two curves over the same thing is a puzzle rather than a
        /// setting.
        /// </param>
        /// <param name="noteCount">
        /// How many circles to lay down, in place of however many the division fits. The spacing is untouched, so
        /// asking for more carries the stream on past the end of the slider at the same rate rather than packing
        /// them in tighter -- the division is what the hand is playing to, and that should not change underneath.
        /// The positions still run from one end of the path to the other.
        /// </param>
        public static IReadOnlyList<Note> Plan(Slider slider, double spacing, double acceleration = 1,
                                               Easing easing = Easing.None, float zigzag = 0, int zigzagPeriod = 1,
                                               ZigzagShape zigzagShape = ZigzagShape.Even, Func<double, double>? curve = null,
                                               int? noteCount = null)
        {
            var notes = new List<Note>();

            int count;

            if (spacing <= 0)
                return notes;

            count = noteCount is int asked && asked > 1 ? asked : CountFor(slider, spacing);

            for (int i = 0; i < count; i++)
            {
                double time = slider.StartTime + i * spacing;
                double progress = count > 1 ? (double)i / (count - 1) : 0;

                float amplitude = zigzag * Reach(zigzagShape, progress);

                double along = curve != null
                    ? Math.Clamp(curve(Math.Clamp(progress, 0, 1)), 0, 1)
                    : Warp(progress, acceleration, easing);

                notes.Add(new Note(time, PositionAt(slider, along, i, amplitude, zigzagPeriod)));
            }

            return notes;
        }

        /// <summary>
        /// How many circles a division puts on a slider: one every division for as long as the slider lasts, the
        /// last landing on its end when it divides evenly. The same reckoning the game's own conversion uses.
        /// </summary>
        public static int CountFor(Slider slider, double spacing)
        {
            double duration = slider.GetEndTime() - slider.StartTime;

            // worked out rather than counted a step at a time. A slider whose velocity has not been applied yet has
            // no length in time, and walking towards an end that never arrives does not come back.
            if (spacing <= 0 || !double.IsFinite(duration) || duration < 0)
                return 0;

            // a millisecond of slack, so a division that all but divides evenly still reaches the end.
            return (int)Math.Floor((duration + 1) / spacing) + 1;
        }

        /// <summary>
        /// Bends progress along the path, leaving both ends where they were.
        /// </summary>
        public static double Warp(double progress, double acceleration, Easing easing)
        {
            progress = Math.Clamp(progress, 0, 1);

            if (easing != Easing.None)
                progress = Interpolation.ApplyEasing(easing, progress);

            if (Math.Abs(acceleration - 1) > 1e-6)
                progress = Math.Pow(Math.Max(progress, 0), acceleration);

            // some curves overshoot on their way, and a circle off the end of the path is not on the slider.
            return Math.Clamp(progress, 0, 1);
        }

        /// <summary>
        /// How much of the zigzag's reach is used at a point along the stream.
        /// </summary>
        public static float Reach(ZigzagShape shape, double progress)
        {
            progress = Math.Clamp(progress, 0, 1);

            switch (shape)
            {
                case ZigzagShape.Widening:
                    return (float)progress;

                case ZigzagShape.Narrowing:
                    return (float)(1 - progress);

                case ZigzagShape.WidestInTheMiddle:
                    return (float)Math.Sin(Math.PI * progress);

                case ZigzagShape.NarrowestInTheMiddle:
                    return (float)(1 - Math.Sin(Math.PI * progress));

                default:
                    return 1;
            }
        }

        /// <summary>
        /// Where a circle sits, having decided how far along the slider it belongs.
        /// </summary>
        public static Vector2 PositionAt(Slider slider, double progress, int index, float zigzag, int zigzagPeriod)
        {
            Vector2 position = slider.Position + slider.Path.PositionAt(pathPosition(slider, progress));

            if (zigzag == 0)
                return position;

            // to one side of the path, then the other, in runs of however many were asked for.
            int side = index / Math.Max(zigzagPeriod, 1);
            float direction = side % 2 == 0 ? 1 : -1;

            return position + perpendicular(slider, progress) * zigzag * direction;
        }

        /// <summary>
        /// How far along the slider's own path a progress lands, once the slider's repeats are taken into
        /// account — every other span runs backwards.
        /// </summary>
        private static double pathPosition(Slider slider, double progress)
        {
            double withRepeats = progress * slider.SpanCount();
            double position = withRepeats - (int)withRepeats;

            if (withRepeats % 2 >= 1)
                position = 1 - position;

            // progress of exactly 1 lands on the start of a span that does not exist; it belongs at the far end
            // of the one before it.
            if (Precision.AlmostEquals(withRepeats, slider.SpanCount()))
                position = slider.SpanCount() % 2 == 0 ? 0 : 1;

            return position;
        }

        /// <summary>
        /// A unit vector across the path at the given progress, for pushing circles off it.
        /// </summary>
        private static Vector2 perpendicular(Slider slider, double progress)
        {
            const double step = 0.01;

            double at = pathPosition(slider, progress);
            Vector2 ahead = slider.Path.PositionAt(Math.Min(at + step, 1));
            Vector2 behind = slider.Path.PositionAt(Math.Max(at - step, 0));

            Vector2 along = ahead - behind;

            if (along.LengthSquared < 1e-6)
                return Vector2.Zero;

            along.Normalize();

            return new Vector2(-along.Y, along.X);
        }
    }
}
