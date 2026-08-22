// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit;

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
                    LabelText = EditorStrings.ShowPlayAreaBorder,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowPlayAreaBorder)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.DetectOffscreen,
                    Current = config.GetBindable<bool>(OsuSetting.EditorDetectOffscreen)
                },
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
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.LoopPlayback,
                    Current = config.GetBindable<bool>(OsuSetting.EditorLoopPlayback)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandLeftToolbox,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideKiaiFlash,
                    TooltipText = EditorStrings.HideKiaiFlashDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideKiaiFlash)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideGridFromPointsTool,
                    TooltipText = EditorStrings.HideGridFromPointsToolDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideGridFromPointsTool)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ToolButtonSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorToolButtonSquareness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ToggleButtonSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorToggleButtonSquareness),
                    DisplayAsPercentage = true,
                }),
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandTimelinePointLabels,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandTimelinePointLabels)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideTimelinePoints,
                    TooltipText = EditorStrings.HideTimelinePointsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.TimelineDragAreaScale,
                    Current = config.GetBindable<float>(OsuSetting.EditorTimelineDragAreaScale),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = EditorStrings.SeekAnimationDuration,
                    Current = config.GetBindable<double>(OsuSetting.EditorSeekAnimationDuration),
                    KeyboardStep = 10,
                    LabelFormat = v => v == 0 ? LazerKaiSettingsStrings.NoDelay : $"{v:N0} ms",
                }),
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
