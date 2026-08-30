// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
        public static IReadOnlyList<Note> Plan(Slider slider, double spacing, double acceleration = 1,
                                               Easing easing = Easing.None, float zigzag = 0, int zigzagPeriod = 1,
                                               ZigzagShape zigzagShape = ZigzagShape.Even, Func<double, double>? curve = null)
        {
            var notes = new List<Note>();

            if (spacing <= 0)
                return notes;

            int count = 0;

            // the same reckoning the game's own conversion uses: a circle every division for as long as the
            // slider lasts, the last one landing on its end when it divides evenly.
            for (double time = slider.StartTime; !Precision.DefinitelyBigger(time, slider.GetEndTime(), 1); time += spacing)
                count++;

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
