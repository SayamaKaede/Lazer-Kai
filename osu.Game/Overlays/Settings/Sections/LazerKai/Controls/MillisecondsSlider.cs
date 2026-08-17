// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai.Controls
{
    /// <summary>
    /// A duration in milliseconds.
    /// </summary>
    public partial class MillisecondsSlider : RoundedSliderBar<double>
    {
        public override LocalisableString TooltipText =>
            Current.Value == 0
                // zero is worth spelling out: "0ms" reads as a duration, when the point is that there is not one.
                ? LazerKaiSettingsStrings.NoDelay
                : $"{Current.Value.ToString("N0", NumberFormatInfo.CurrentInfo)}ms";
    }

    /// <summary>
    /// A multiplier, shown as a percentage.
    /// </summary>
    public partial class PercentageSlider : RoundedSliderBar<float>
    {
        public override LocalisableString TooltipText => Current.Value.ToString("0%", NumberFormatInfo.CurrentInfo);
    }

    /// <summary>
    /// A distance in pixels.
    /// </summary>
    public partial class PixelsSlider : RoundedSliderBar<float>
    {
        public override LocalisableString TooltipText => $"{Current.Value.ToString("N0", NumberFormatInfo.CurrentInfo)}px";
    }

    /// <summary>
    /// A duration in minutes.
    /// </summary>
    public partial class MinutesSlider : RoundedSliderBar<double>
    {
        public override LocalisableString TooltipText => LazerKaiSettingsStrings.Minutes(Current.Value.ToString("N0", NumberFormatInfo.CurrentInfo));
    }
}
