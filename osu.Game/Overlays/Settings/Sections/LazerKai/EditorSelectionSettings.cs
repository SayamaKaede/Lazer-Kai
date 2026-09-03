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
                    LabelText = EditorStrings.HideSelectionButtons,
                    TooltipText = EditorStrings.HideSelectionButtonsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideSelectionButtons)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideRotationHandles,
                    TooltipText = EditorStrings.HideRotationHandlesDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideRotationHandles)
                },
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = EditorStrings.SelectableAfterEndTime,
                    HintText = EditorStrings.SelectableAfterEndTimeDescription,
                    Current = config.GetBindable<double>(OsuSetting.EditorSelectableAfterEndTime),
                    KeyboardStep = 10,
                    LabelFormat = v => v == 0 ? LazerKaiSettingsStrings.NoDelay : $"{v:N0} ms",
                }),
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.ObjectOutlineColour,
                    TooltipText = EditorStrings.ObjectOutlineColourDescription,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorObjectOutlineColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ObjectOutlineThickness,
                    HintText = EditorStrings.ObjectOutlineThicknessDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorObjectOutlineThickness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ObjectOutlineOpacity,
                    HintText = EditorStrings.ObjectOutlineOpacityDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorObjectOutlineOpacity),
                    DisplayAsPercentage = true,
                }),
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.SelectionColour,
                    TooltipText = EditorStrings.SelectionColourDescription,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SelectionThickness,
                    HintText = EditorStrings.SelectionThicknessDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionThickness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SelectionOpacity,
                    HintText = EditorStrings.SelectionOpacityDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionOpacity),
                    DisplayAsPercentage = true,
                }),
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.SelectionHandleColour,
                    TooltipText = EditorStrings.SelectionHandleColourDescription,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionHandleColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SelectionHandleSize,
                    HintText = EditorStrings.SelectionHandleSizeDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorSelectionHandleScale),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SliderAnchorSize,
                    Current = config.GetBindable<float>(OsuSetting.EditorSliderAnchorScale),
                    DisplayAsPercentage = true,
                }),
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.SliderEndDragMarkerColour,
                    TooltipText = EditorStrings.SliderEndDragMarkerColourDescription,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSliderEndDragMarkerColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SliderEndDragMarkerScale,
                    HintText = EditorStrings.SliderEndDragMarkerScaleDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorSliderEndDragMarkerScale),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SliderEndDragMarkerOpacity,
                    HintText = EditorStrings.SliderEndDragMarkerOpacityDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorSliderEndDragMarkerOpacity),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
