// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorDifficultyGraphSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.DifficultyGraphHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowDifficultyGraph,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowDifficultyGraph)
                },
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.DifficultyGraphColour,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorDifficultyGraphColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.DifficultyGraphOpacity,
                    Current = config.GetBindable<float>(OsuSetting.EditorDifficultyGraphOpacity),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.DifficultyGraphDetail,
                    HintText = EditorStrings.DifficultyGraphDetailDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorDifficultyGraphDetail),
                    DisplayAsPercentage = true,
                }),
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowVolumeGraph,
                    TooltipText = EditorStrings.ShowVolumeGraphDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowVolumeGraph)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.VolumeGraphIgnoreFivePercent,
                    TooltipText = EditorStrings.VolumeGraphIgnoreFivePercentDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorVolumeGraphIgnoreFivePercent)
                },
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.VolumeGraphColour,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorVolumeGraphColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.VolumeGraphThickness,
                    Current = config.GetBindable<float>(OsuSetting.EditorVolumeGraphThickness),
                    LabelFormat = v => $"{v:0.#} px",
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.VolumeGraphOpacity,
                    Current = config.GetBindable<float>(OsuSetting.EditorVolumeGraphOpacity),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
