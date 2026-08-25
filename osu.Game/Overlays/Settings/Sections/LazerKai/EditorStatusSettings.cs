// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// Which rows the composer's status panel shows.
    /// </summary>
    /// <remarks>
    /// The panel is a narrow column down the right of the editor, and every row it carries is one less line of the
    /// beatmap visible behind it. Which of them earn that space depends on what the mapper is doing.
    /// </remarks>
    public partial class EditorStatusSettings : SettingsSubsection
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
                    LabelText = EditorStrings.StatusShowDifficultySettings,
                    Current = config.GetBindable<bool>(OsuSetting.EditorStatusShowDifficultySettings)
                },
            };
        }
    }
}
