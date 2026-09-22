// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorStatusSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.StatusHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowStarRating,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowStarRating)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowMaxPerformance,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowMaxPerformance)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowScore,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowScore)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowLegacyScore,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowLegacyScore)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowSliderVelocity,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowSliderVelocity)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowNormalBank,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowNormalBank)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowAdditionBank,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowAdditionBank)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowSampleVolume,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowSampleVolume)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowKiai,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowKiai)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowMods,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowMods)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowCombo,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowCombo)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowObjectCount,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowObjectCount)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowDrainTime,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowDrainTime)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowSaveFormat,
                    TooltipText = EditorStrings.StatusShowSaveFormatDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowSaveFormat)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.StatusShowDifficultySettings,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowDifficultySettings)
                },
            };
        }
    }
}
