// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.LazerKai.Controls;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorSelectionSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.SelectionHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowSelectionButtons,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowSelectionButtons)
                },
                new SettingsSlider<double, MillisecondsSlider>
                {
                    LabelText = EditorStrings.SelectableAfterEndTime,
                    Current = config.GetBindable<double>(OsuSetting.EditorSelectableAfterEndTime)
                },
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.SelectionColour,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionColour)
                },
                new SettingsSlider<float, PercentageSlider>
                {
                    LabelText = EditorStrings.SelectionThickness,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionThickness)
                },
                new SettingsSlider<float, PercentageSlider>
                {
                    LabelText = EditorStrings.SelectionOpacity,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionOpacity)
                },
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.SelectionHandleColour,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionHandleColour)
                },
                new SettingsSlider<float, PercentageSlider>
                {
                    LabelText = EditorStrings.SelectionHandleSize,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionHandleScale)
                },
                new SettingsSlider<float, PercentageSlider>
                {
                    LabelText = EditorStrings.SliderAnchorSize,
                    Current = config.GetBindable<float>(OsuSetting.EditorSliderAnchorScale)
                },
            };
        }
    }
}
