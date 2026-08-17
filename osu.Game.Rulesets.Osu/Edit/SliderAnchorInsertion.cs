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
        /// Turns the three points around a newly inserted anchor into an arc, where doing so would not disturb a
        /// curve the user shaped on purpose.
        /// </summary>
        /// <remarks>
        /// An anchor is nearly always added to bend a path, but a new anchor carries no type of its own, so it
        /// extends whatever the segment already was and a straight segment stays straight.
        ///
        /// Only straight segments are reshaped. A bezier or catmull segment holds a shape the user built, and
        /// splitting it into pieces to fit an arc in would change the whole curve, not just the part being added to.
        /// </remarks>
        /// <param name="controlPoints">The path's control points, with the new anchor already inserted.</param>
        /// <param name="insertedIndex">Index of the newly inserted anchor.</param>
        public static void MakeArcAroundInsertion(IList<PathControlPoint> controlPoints, int insertedIndex)
        {
            // the new anchor always has a point on either side: it is placed between two existing ones.
            if (insertedIndex <= 0 || insertedIndex >= controlPoints.Count - 1)
                return;

            int segmentStart = 0;

            // a typed point starts a segment, so walking back through untyped ones finds the type in effect here.
            for (int i = insertedIndex - 1; i > 0; i--)
            {
                if (controlPoints[i].Type != null)
                {
                    segmentStart = i;
                    break;
                }
            }

            if (controlPoints[segmentStart].Type != PathType.LINEAR)
                return;

            int before = insertedIndex - 1;
            int after = insertedIndex + 1;

            // an arc takes exactly three points, so the following point has to close the segment. Where it does not
            // already, it starts a straight one instead, leaving the rest of the path as it was.
            if (after < controlPoints.Count - 1 && controlPoints[after].Type == null)
                controlPoints[after].Type = PathType.LINEAR;

            controlPoints[before].Type = PathType.PERFECT_CURVE;
        }
    }
}
