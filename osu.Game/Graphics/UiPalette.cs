// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Configuration;
using osuTK;
using osuTK.Graphics;
using Colour4 = osu.Framework.Graphics.Colour4;

namespace osu.Game.Graphics
{
    /// <summary>
    /// A look, as the handful of numbers it takes to describe one.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="UiPalette"/> so that a look can be held without being the one the game is wearing,
    /// which is what the tests compare against.
    /// </remarks>
    public readonly record struct UiPaletteValues(
        UiTheme Theme,
        float Hue,
        float HuePull,
        float Saturation,
        float LightnessGamma,
        float GreyTint,
        float ShearAmount)
    {
        /// <summary>The slant upstream uses, and the one the base theme keeps.</summary>
        public const float LAZER_SHEAR = 0.2f;

        /// <summary>This fork's look, as it was settled on 2026-09-08 by moving sliders against the real game.</summary>
        public static UiPaletteValues Default => new UiPaletteValues(
            Theme: UiTheme.LazerKai,
            // steel rather than the game's pinks and purples, with every overlay pulled half of the way to it so
            // the game reads as one machine rather than a room of differently coloured panels.
            Hue: 205,
            HuePull: 0.5f,
            Saturation: 0.55f,
            LightnessGamma: 1.15f,
            GreyTint: 0.15f,
            ShearAmount: 0);

        /// <summary>The look wearing the given theme, everything else being this fork's.</summary>
        public static UiPaletteValues For(UiTheme theme) => Default with { Theme = theme };

        public Vector2 Shear => new Vector2(Theme == UiTheme.Lazer ? LAZER_SHEAR : ShearAmount, 0);

        /// <summary>
        /// How much of upstream's slant this look uses, as a fraction of it.
        /// </summary>
        /// <remarks>
        /// A few layouts hold a hand-picked number that exists only to make room for the slant -- the leaderboard's
        /// left padding is one. Scaling those by this leaves them exactly as upstream wrote them at the full slant,
        /// and takes them to nothing when there is no slant to make room for.
        /// </remarks>
        public float ShearScale => Shear.X / LAZER_SHEAR;

        /// <summary>
        /// Picks between a number written for no slant and the one upstream wrote for its own.
        /// </summary>
        /// <remarks>
        /// For the offsets that exist only to make room for the slant and cannot simply be scaled away, because
        /// something still has to be left once there is no slant to make room for.
        /// </remarks>
        public float RoomForSlant(float noneAtAll, float upstreams) => noneAtAll + (upstreams - noneAtAll) * ShearScale;

        /// <summary>
        /// The colour an overlay of the given hue wants at this saturation and lightness.
        /// </summary>
        public Color4 Resolve(int hue, float saturation, float lightness)
        {
            if (Theme == UiTheme.Lazer)
                return Colour4.FromHSL(hue / 360f, saturation, lightness);

            return Colour4.FromHSL(
                pullHue(hue) / 360f,
                Math.Clamp(saturation * Saturation, 0, 1),
                Math.Clamp(MathF.Pow(lightness, LightnessGamma), 0, 1));
        }

        /// <summary>
        /// A flat grey of the given lightness, given whatever colour this look puts in its greys.
        /// </summary>
        /// <remarks>
        /// The lightness is left exactly as asked for. Greys are picked one at a time to sit at a particular depth
        /// against what is around them, so bending them the way the overlay curve is bent would undo those choices.
        /// </remarks>
        public Color4 Grey(float amount)
        {
            if (Theme == UiTheme.Lazer || GreyTint <= 0)
                return new Color4(amount, amount, amount, 1f);

            return Colour4.FromHSL(Hue / 360f, GreyTint, Math.Clamp(amount, 0, 1));
        }

        /// <summary>
        /// Moves <paramref name="hue"/> towards <see cref="Hue"/> the short way around the circle.
        /// </summary>
        /// <remarks>
        /// Going the long way would walk a blue overlay through green and yellow on its way to becoming a slightly
        /// bluer blue, so which direction is shorter has to be worked out rather than assumed.
        /// </remarks>
        private float pullHue(int hue)
        {
            float distance = ((Hue - hue + 540f) % 360f) - 180f;

            return (hue + distance * HuePull + 360f) % 360f;
        }
    }

    /// <summary>
    /// The look the game is currently painting itself in.
    /// </summary>
    /// <remarks>
    /// Three places decide almost everything about how the game looks, and each of them is a single point that
    /// hundreds of call sites go through: <see cref="Overlays.OverlayColourProvider"/> builds every shade an overlay
    /// uses out of one hue and a fixed table of saturations and lightnesses (707 uses), <see cref="OsuColour.Gray(float)"/>
    /// makes every flat grey (81 uses), and <see cref="OsuGame.SHEAR"/> is the slant on everything built out of
    /// parallelograms (140 uses). Bending those three repaints the game without touching any of the call sites,
    /// which is also what keeps this out of the way of upstream merges.
    ///
    /// Corner radii are deliberately not here: there are 385 of them written as literals on individual components,
    /// with no single point to go through, so rounding is not something this can change.
    ///
    /// Static rather than injected because two of the three are reached from constructors and static methods, where
    /// there is nothing to resolve a dependency from. Written once at startup, read from then on.
    /// </remarks>
    public static class UiPalette
    {
        public static UiPaletteValues Current { get; private set; } = UiPaletteValues.Default;

        /// <summary>
        /// Reads the look out of configuration. Called once during startup.
        /// </summary>
        /// <remarks>
        /// Nothing watches for later changes on purpose. Every screen reads its colours once while it loads, so a
        /// theme swapped mid-session would only reach the parts built after it -- which looks worse than not
        /// changing at all. The setting says that it takes a restart.
        /// </remarks>
        public static void Apply(OsuConfigManager config) => Current = UiPaletteValues.For(config.Get<UiTheme>(OsuSetting.UiTheme));

        public static Color4 Resolve(int hue, float saturation, float lightness) => Current.Resolve(hue, saturation, lightness);

        public static Color4 Grey(float amount) => Current.Grey(amount);

        public static Vector2 Shear => Current.Shear;

        public static float ShearScale => Current.ShearScale;

        public static float RoomForSlant(float noneAtAll, float upstreams) => Current.RoomForSlant(noneAtAll, upstreams);
    }
}
