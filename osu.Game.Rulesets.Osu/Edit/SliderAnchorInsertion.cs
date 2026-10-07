// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Decides what a slider's control point types should become when the user adds an anchor.
    /// </summary>
    /// <remarks>
    /// Kept separate from the blueprint that calls it, and free of any drawable state, so every path shape it has to
    /// cope with can be checked directly in tests rather than by eye.
    /// </remarks>
    public static class SliderAnchorInsertion
    {
        /// <summary>
        /// Gives the part of the path the new anchor landed in the shape that was asked for.
        /// </summary>
        /// <remarks>
        /// Nothing is split. In stable a slider is one shape and an added point is just another point in it, so the
        /// only anchors that divide a path are the ones the user makes themselves; splitting a path here to fit a
        /// three point arc in put anchors in the map that nobody asked for (2026-09-19).
        ///
        /// A perfect curve is exactly three points, so where the part being shaped holds more than that it becomes a
        /// bezier instead — the same fall-back stable makes when a curve outgrows three points.
        ///
        /// A straight part is divided at every corner already, so there the part being shaped is only the straight
        /// piece the anchor landed in, and the pieces either side stay straight. Shaped as a whole, a straight path
        /// with a corner became one bezier when the user asked for an arc between the corner and its end (2026-10-07).
        /// </remarks>
        /// <param name="controlPoints">The path's control points, with the new anchor already inserted.</param>
        /// <param name="insertedIndex">Index of the newly inserted anchor.</param>
        /// <param name="type">The shape asked for.</param>
        public static void ShapeAroundInsertion(IList<PathControlPoint> controlPoints, int insertedIndex, PathType type)
        {
            // the new anchor always has a point on either side: it is placed between two existing ones.
            if (insertedIndex <= 0 || insertedIndex >= controlPoints.Count - 1)
                return;

            int segmentStart = 0;

            // a typed point starts a segment, so walking back through untyped ones finds the one this anchor is in.
            for (int i = insertedIndex - 1; i > 0; i--)
            {
                if (controlPoints[i].Type != null)
                {
                    segmentStart = i;
                    break;
                }
            }

            if (controlPoints[segmentStart].Type == PathType.LINEAR)
            {
                if (type == PathType.LINEAR)
                    return;

                // the corner after it goes on being straight, unless it ends the path or already starts a part of its own.
                var cornerAfter = controlPoints[insertedIndex + 1];

                if (insertedIndex + 1 < controlPoints.Count - 1 && cornerAfter.Type == null)
                    cornerAfter.Type = PathType.LINEAR;

                // three points: the corners either side, and the anchor between them.
                controlPoints[insertedIndex - 1].Type = type;
                return;
            }

            controlPoints[segmentStart].Type = FitToSegment(type, LengthOfSegment(controlPoints, segmentStart));
        }

        /// <summary>
        /// The shape a segment of <paramref name="pointsInSegment"/> points can hold out of the one asked for.
        /// </summary>
        public static PathType FitToSegment(PathType type, int pointsInSegment) =>
            type == PathType.PERFECT_CURVE && pointsInSegment > 3 ? PathType.BEZIER : type;

        /// <summary>
        /// How many points the segment starting at <paramref name="segmentStart"/> holds, counting the point that
        /// closes it — which is the next typed point, or the end of the path.
        /// </summary>
        public static int LengthOfSegment(IList<PathControlPoint> controlPoints, int segmentStart)
        {
            for (int i = segmentStart + 1; i < controlPoints.Count; i++)
            {
                if (controlPoints[i].Type != null)
                    return i - segmentStart + 1;
            }

            return controlPoints.Count - segmentStart;
        }
    }
}
