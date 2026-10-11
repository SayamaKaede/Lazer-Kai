// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// What is the fork's own in the osu!mania editor.
    /// </summary>
    public partial class EditorManiaSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ManiaHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = LazerKaiSettingsStrings.ManiaHideKeys,
                    TooltipText = LazerKaiSettingsStrings.ManiaHideKeysDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorManiaHideKeys)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = LazerKaiSettingsStrings.ManiaStageSpacing,
                    HintText = LazerKaiSettingsStrings.ManiaStageSpacingDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorManiaStageSpacing),
                    KeyboardStep = 1,
                    LabelFormat = v => $"{v:N0} px",
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = LazerKaiSettingsStrings.ManiaTickThickness,
                    HintText = LazerKaiSettingsStrings.ManiaTickDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorManiaTickThickness),
                    KeyboardStep = 0.5f,
                    LabelFormat = v => $"{v:0.#} px",
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = LazerKaiSettingsStrings.ManiaTickOpacity,
                    HintText = LazerKaiSettingsStrings.ManiaTickDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorManiaTickOpacity),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
