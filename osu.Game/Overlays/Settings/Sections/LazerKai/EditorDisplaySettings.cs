// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.LazerKai.Controls;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorDisplaySettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.DisplayHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowOffscreenObjects,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowOffscreenObjects)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowDifficultyGraph,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowDifficultyGraph)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowTimeline,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowTimeline)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandLeftToolbox,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandTimelinePointLabels,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandTimelinePointLabels)
                },
                new SettingsSlider<double, MillisecondsSlider>
                {
                    LabelText = EditorStrings.SeekAnimationDuration,
                    Current = config.GetBindable<double>(OsuSetting.EditorSeekAnimationDuration)
                },
                new SettingsEnumDropdown<OverlayColourScheme>
                {
                    LabelText = EditorStrings.EditorColourScheme,
                    TooltipText = LazerKaiSettingsStrings.TakesEffectOnReopen,
                    Current = config.GetBindable<OverlayColourScheme>(OsuSetting.EditorColourScheme)
                },
            };
        }
    }
}
