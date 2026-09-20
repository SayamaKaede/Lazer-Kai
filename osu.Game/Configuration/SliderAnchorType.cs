// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Configuration
{
    /// <summary>
    /// The shape an anchor added with ctrl+click gives the part of the path it lands in.
    /// </summary>
    /// <remarks>
    /// Stable has no per-anchor types: a slider is one shape, and adding a point to it leaves that shape's kind alone.
    /// This is the kind an added anchor asks for, which is as close as a path of typed anchors gets to that.
    /// </remarks>
    public enum SliderAnchorType
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeLinear))]
        Linear,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeBezier))]
        Bezier,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypePerfectCurve))]
        PerfectCurve,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AnchorTypeCatmull))]
        Catmull,
    }

    public static class SliderAnchorTypeExtensions
    {
        public static PathType ToPathType(this SliderAnchorType type)
        {
            switch (type)
            {
                case SliderAnchorType.Linear:
                    return PathType.LINEAR;

                case SliderAnchorType.Bezier:
                    return PathType.BEZIER;

                case SliderAnchorType.Catmull:
                    return PathType.CATMULL;

                default:
                    return PathType.PERFECT_CURVE;
            }
        }
    }
}
