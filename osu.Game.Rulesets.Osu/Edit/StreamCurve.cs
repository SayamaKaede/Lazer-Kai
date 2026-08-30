// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// A curve drawn by hand, read left to right.
    /// </summary>
    /// <remarks>
    /// The points given are ones the curve goes through rather than ones it is pulled towards, because a point
    /// that is dragged somewhere and does not end up there is a control that argues with the person using it.
    /// Between them it runs as a Catmull-Rom spline, which is smooth without needing a second handle per point.
    ///
    /// Reading a spline at a given x means going the awkward way round, since it is written in terms of its own
    /// parameter. Rather than solve for that every time it is asked, the whole curve is walked once into a line
    /// of points and read off that.
    /// </remarks>
    public static class StreamCurve
    {
        /// <summary>
        /// How finely the curve is walked before it is read. Enough that a stream of any sane length lands
        /// within a pixel of the curve it was drawn on.
        /// </summary>
        private const int steps = 256;

        /// <summary>
        /// The points making up the curve: the two fixed ends, and whatever was put between them.
        /// </summary>
        /// <remarks>
        /// Both ends are fixed. A stream that started somewhere other than the head of the slider would not be
        /// a conversion of it.
        /// </remarks>
        public static IReadOnlyList<Vector2> WithEnds(IEnumerable<Vector2> points)
        {
            var all = new List<Vector2> { Vector2.Zero };

            // held just inside the ends rather than allowed onto them: a point dropped on the left edge at half
            // height would be saying the stream both does and does not start at the head of the slider, and one
            // of the two has to give. It is not this one.
            all.AddRange(points.Select(p => new Vector2(Math.Clamp(p.X, edge, 1 - edge), Math.Clamp(p.Y, 0, 1)))
                               .OrderBy(p => p.X));

            all.Add(Vector2.One);

            return all;
        }

        private const float edge = 0.002f;

        /// <summary>
        /// Turns a set of points into something that can be asked where the stream is at a given moment.
        /// </summary>
        public static Func<double, double> Build(IEnumerable<Vector2> points)
        {
            var walked = Walk(points);

            return x => Read(walked, x);
        }

        /// <summary>
        /// The curve as a line of points, in order of x.
        /// </summary>
        public static IReadOnlyList<Vector2> Walk(IEnumerable<Vector2> points)
        {
            var through = WithEnds(points);

            if (through.Count == 2)
                return through;

            var walked = new List<Vector2>();

            for (int segment = 0; segment < through.Count - 1; segment++)
            {
                Vector2 before = through[Math.Max(segment - 1, 0)];
                Vector2 start = through[segment];
                Vector2 end = through[segment + 1];
                Vector2 after = through[Math.Min(segment + 2, through.Count - 1)];

                int within = Math.Max(steps / (through.Count - 1), 2);

                for (int i = 0; i < within; i++)
                    walked.Add(catmullRom(before, start, end, after, (float)i / within));
            }

            walked.Add(through[^1]);

            // a spline through unevenly spaced points can swing back on itself, and a curve that goes left is
            // not a reading of anything: the same moment in the stream would be two places along the slider.
            // Held to going forwards, and inside the square.
            float furthest = 0;

            for (int i = 0; i < walked.Count; i++)
            {
                furthest = Math.Max(furthest, Math.Clamp(walked[i].X, 0, 1));
                walked[i] = new Vector2(furthest, Math.Clamp(walked[i].Y, 0, 1));
            }

            return walked;
        }

        /// <summary>
        /// Where the curve is at a given point across, read off the walked line.
        /// </summary>
        public static double Read(IReadOnlyList<Vector2> walked, double x)
        {
            x = Math.Clamp(x, 0, 1);

            // the ends answer for themselves. Holding the curve to going forwards can leave a run of points
            // sharing an x at either end, and which of them to read there is a question with no answer — but
            // there is nothing to ask: a stream starts at the head of the slider and finishes at its tail.
            if (x <= walked[0].X)
                return Math.Clamp(walked[0].Y, 0, 1);

            if (x >= walked[^1].X)
                return Math.Clamp(walked[^1].Y, 0, 1);

            // the last point at or before the moment asked about, rather than the first at or after it. Holding
            // the curve to going forwards leaves runs of points sharing an x where it tried to swing back, and
            // the answer there is where the curve came out of that, not where it went in.
            int at = 0;

            for (int i = 1; i < walked.Count; i++)
            {
                if (walked[i].X > x)
                    break;

                at = i;
            }

            if (at >= walked.Count - 1)
                return Math.Clamp(walked[^1].Y, 0, 1);

            float span = walked[at + 1].X - walked[at].X;
            double within = span <= 1e-6 ? 0 : (x - walked[at].X) / span;

            return Math.Clamp(walked[at].Y + (walked[at + 1].Y - walked[at].Y) * within, 0, 1);
        }

        private static Vector2 catmullRom(Vector2 before, Vector2 start, Vector2 end, Vector2 after, float t)
        {
            float t2 = t * t;
            float t3 = t2 * t;

            return 0.5f * ((2 * start)
                           + (-before + end) * t
                           + (2 * before - 5 * start + 4 * end - after) * t2
                           + (-before + 3 * start - 3 * end + after) * t3);
        }
    }
}
