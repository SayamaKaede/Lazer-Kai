// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Utils;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Decides what a slider's control point types become when the user ctrl+clicks an anchor.
    /// </summary>
    /// <remarks>
    /// Only the part of the path between the corners either side of the anchor changes. A corner is where the path may
    /// turn sharply: either end, an anchor that starts a segment, and every anchor of a straight segment. Moving the
    /// whole segment the anchor arrived in on to its next type reached past corners the user had made: a straight path
    /// with a corner in it turned into one bezier, and an anchor in an arc went on to a b-spline when the user wanted a
    /// corner of it (2026-10-07).
    ///
    /// Kept free of any drawable state, like <see cref="SliderAnchorInsertion"/>.
    /// </remarks>
    public static class SliderAnchorCycling
    {
        /// <summary>
        /// The shapes the part around an anchor goes through. A straight one is the anchor being a corner.
        /// </summary>
        private static readonly PathType[] shapes =
        [
            PathType.LINEAR,
            PathType.BEZIER,
            PathType.PERFECT_CURVE,
            PathType.BSpline(4),
        ];

        /// <param name="controlPoints">The path's control points.</param>
        /// <param name="index">Index of the anchor that was clicked.</param>
        /// <param name="carryOn">Whether the click follows one on the same anchor, with no type changed since.</param>
        public static void Cycle(IList<PathControlPoint> controlPoints, int index, bool carryOn)
        {
            int last = controlPoints.Count - 1;

            if (last < 1 || index < 0 || index > last)
                return;

            // an end is a corner whatever happens, so what it goes through are the shapes of the part next to it.
            if (index == 0 || index == last)
            {
                int from = index == 0 ? 0 : cornerBefore(controlPoints, last);
                int to = index == 0 ? cornerAfter(controlPoints, 0) : last;

                shapePart(controlPoints, from, to, nextShape(controlPoints, controlPoints[from].Type ?? PathType.LINEAR, from, to));
                return;
            }

            bool isCorner = IsCorner(controlPoints, index);

            // an anchor in a curve becomes a corner first, as in stable; only going on clicking it goes on to the curves.
            if (!isCorner && !carryOn)
            {
                makeCorner(controlPoints, index);
                return;
            }

            int before = cornerBefore(controlPoints, index);
            int after = cornerAfter(controlPoints, index);

            PathType next = nextShape(controlPoints, isCorner ? PathType.LINEAR : typeOfSegmentHolding(controlPoints, index), before, after);

            if (next == PathType.LINEAR)
                makeCorner(controlPoints, index);
            else
                shapePart(controlPoints, before, after, next);
        }

        /// <summary>
        /// Whether the path may turn sharply at the anchor at <paramref name="index"/>.
        /// </summary>
        public static bool IsCorner(IList<PathControlPoint> controlPoints, int index) =>
            index == 0
            || index == controlPoints.Count - 1
            || controlPoints[index].Type != null
            || typeOfSegmentHolding(controlPoints, index) == PathType.LINEAR;

        /// <summary>
        /// Divides the segment holding the anchor at <paramref name="index"/> there, each side keeping its shape where it
        /// can. A side of two points is a line whatever its type, and is made a straight one so that it shows as such.
        /// </summary>
        private static void makeCorner(IList<PathControlPoint> controlPoints, int index)
        {
            int start = segmentStart(controlPoints, index);
            int end = cornerAfter(controlPoints, index);
            PathType type = controlPoints[start].Type ?? PathType.LINEAR;

            controlPoints[start].Type = shapeOfSide(type, index - start + 1);
            controlPoints[index].Type = shapeOfSide(type, end - index + 1);
        }

        private static PathType shapeOfSide(PathType type, int points)
        {
            if (points <= 2)
                return PathType.LINEAR;

            return type == PathType.PERFECT_CURVE && points != 3 ? PathType.BEZIER : type;
        }

        /// <summary>
        /// Makes the part from the corner at <paramref name="from"/> to the one at <paramref name="to"/> a single segment
        /// of <paramref name="type"/>, leaving the path either side of it as it was.
        /// </summary>
        private static void shapePart(IList<PathControlPoint> controlPoints, int from, int to, PathType type)
        {
            // a corner without a type of its own is one for being in a straight segment, which goes on after it.
            bool straightOn = to < controlPoints.Count - 1 && controlPoints[to].Type == null;

            controlPoints[from].Type = type;

            for (int i = from + 1; i < to; i++)
                controlPoints[i].Type = null;

            if (straightOn)
                controlPoints[to].Type = PathType.LINEAR;
        }

        private static PathType nextShape(IList<PathControlPoint> controlPoints, PathType current, int from, int to)
        {
            int index = Array.IndexOf(shapes, current);

            for (int step = 1; step <= shapes.Length; step++)
            {
                var candidate = shapes[(index + step) % shapes.Length];

                // passed over rather than left to become a bezier, which would come back to it on the next click.
                if (candidate == PathType.PERFECT_CURVE && !canBeAnArc(controlPoints, from, to))
                    continue;

                return candidate;
            }

            return current;
        }

        /// <summary>
        /// Whether the part from <paramref name="from"/> to <paramref name="to"/> can be a perfect curve: three points, on
        /// an arc no larger than the one the editor makes a bezier of when checking a path's types.
        /// </summary>
        private static bool canBeAnArc(IList<PathControlPoint> controlPoints, int from, int to)
        {
            if (to - from != 2)
                return false;

            ReadOnlySpan<Vector2> arc = [controlPoints[from].Position, controlPoints[from + 1].Position, controlPoints[to].Position];
            RectangleF bounds = PathApproximator.CircularArcBoundingBox(arc);

            return bounds.Width < 640 && bounds.Height < 480;
        }

        private static int cornerBefore(IList<PathControlPoint> controlPoints, int index)
        {
            for (int i = index - 1; i > 0; i--)
            {
                if (IsCorner(controlPoints, i))
                    return i;
            }

            return 0;
        }

        private static int cornerAfter(IList<PathControlPoint> controlPoints, int index)
        {
            for (int i = index + 1; i < controlPoints.Count - 1; i++)
            {
                if (IsCorner(controlPoints, i))
                    return i;
            }

            return controlPoints.Count - 1;
        }

        /// <summary>
        /// The anchor that starts the segment the one at <paramref name="index"/> is in, which is the nearest one before it
        /// with a type.
        /// </summary>
        private static int segmentStart(IList<PathControlPoint> controlPoints, int index)
        {
            for (int i = index - 1; i > 0; i--)
            {
                if (controlPoints[i].Type != null)
                    return i;
            }

            return 0;
        }

        private static PathType typeOfSegmentHolding(IList<PathControlPoint> controlPoints, int index) =>
            controlPoints[segmentStart(controlPoints, index)].Type ?? PathType.LINEAR;
    }
}
