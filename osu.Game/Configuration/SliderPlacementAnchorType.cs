// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Configuration
{
    /// <summary>
    /// The shape a slider is drawn with while it is being placed.
    /// </summary>
    /// <remarks>
    /// A slider being placed is otherwise shaped by how many points the segment holds, and alt and a number override
    /// that for one segment. Mapping in one style means pressing those keys for every slider, so the shape can be
    /// settled once instead. It is separate from the shape an added anchor makes: one is about drawing a slider, the
    /// other about reshaping a finished one.
    /// </remarks>
    public enum SliderPlacementAnchorType
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.PlacementAnchorTypeAutomatic))]
        Automatic,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeLinear))]
        Linear,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeBezier))]
        Bezier,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypePerfectCurve))]
        PerfectCurve,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeCatmull))]
        Catmull,
    }

    public static class SliderPlacementAnchorTypeExtensions
    {
        /// <summary>
        /// The path type to draw with, or null to leave it to the number of points in the segment.
        /// </summary>
        public static PathType? ToPathType(this SliderPlacementAnchorType type)
        {
            switch (type)
            {
                case SliderPlacementAnchorType.Linear:
                    return PathType.LINEAR;

                case SliderPlacementAnchorType.Bezier:
                    return PathType.BEZIER;

                case SliderPlacementAnchorType.PerfectCurve:
                    return PathType.PERFECT_CURVE;

                case SliderPlacementAnchorType.Catmull:
                    return PathType.CATMULL;

                default:
                    return null;
            }
        }
    }
}
