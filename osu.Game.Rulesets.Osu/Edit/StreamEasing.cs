// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;
using osu.Framework.Graphics;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The curves a stream's spacing along the slider can follow.
    /// </summary>
    /// <remarks>
    /// A chosen few rather than the framework's <see cref="Easing"/>, which has some thirty members and includes
    /// curves that overshoot — a circle placed past the end of the path is not on the slider any more. These are
    /// the families that stay within their ends, from the gentlest to the sharpest.
    /// </remarks>
    public enum StreamEasing
    {
        [Description("None")]
        None,

        [Description("Sine in")]
        InSine,

        [Description("Sine out")]
        OutSine,

        [Description("Sine both")]
        InOutSine,

        [Description("Quad in")]
        InQuad,

        [Description("Quad out")]
        OutQuad,

        [Description("Quad both")]
        InOutQuad,

        [Description("Cubic in")]
        InCubic,

        [Description("Cubic out")]
        OutCubic,

        [Description("Cubic both")]
        InOutCubic,

        [Description("Quart in")]
        InQuart,

        [Description("Quart out")]
        OutQuart,

        [Description("Quart both")]
        InOutQuart,

        [Description("Circle in")]
        InCirc,

        [Description("Circle out")]
        OutCirc,

        [Description("Circle both")]
        InOutCirc,

        [Description("Exponential in")]
        InExpo,

        [Description("Exponential out")]
        OutExpo,

        [Description("Exponential both")]
        InOutExpo,
    }

    public static class StreamEasingExtensions
    {
        public static Easing ToEasing(this StreamEasing easing) => easing switch
        {
            StreamEasing.InSine => Easing.InSine,
            StreamEasing.OutSine => Easing.OutSine,
            StreamEasing.InOutSine => Easing.InOutSine,
            StreamEasing.InQuad => Easing.InQuad,
            StreamEasing.OutQuad => Easing.OutQuad,
            StreamEasing.InOutQuad => Easing.InOutQuad,
            StreamEasing.InCubic => Easing.InCubic,
            StreamEasing.OutCubic => Easing.OutCubic,
            StreamEasing.InOutCubic => Easing.InOutCubic,
            StreamEasing.InQuart => Easing.InQuart,
            StreamEasing.OutQuart => Easing.OutQuart,
            StreamEasing.InOutQuart => Easing.InOutQuart,
            StreamEasing.InCirc => Easing.InCirc,
            StreamEasing.OutCirc => Easing.OutCirc,
            StreamEasing.InOutCirc => Easing.InOutCirc,
            StreamEasing.InExpo => Easing.InExpo,
            StreamEasing.OutExpo => Easing.OutExpo,
            StreamEasing.InOutExpo => Easing.InOutExpo,
            _ => Easing.None,
        };
    }

    /// <summary>
    /// How far a zigzag reaches as the stream goes on.
    /// </summary>
    public enum ZigzagShape
    {
        [Description("Even")]
        Even,

        [Description("Widening")]
        Widening,

        [Description("Narrowing")]
        Narrowing,

        [Description("Widest in the middle")]
        WidestInTheMiddle,

        [Description("Narrowest in the middle")]
        NarrowestInTheMiddle,
    }
}
