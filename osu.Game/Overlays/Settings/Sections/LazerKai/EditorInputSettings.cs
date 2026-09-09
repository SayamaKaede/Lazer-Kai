// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorInputSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.InputHeader;

        private readonly Bindable<bool> autoNewCombo = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorAutoNewCombo, autoNewCombo);

            var autoNewComboInterval = new FormEnumDropdown<AutoNewComboInterval>
            {
                Caption = EditorStrings.AutoNewComboIntervalLabel,
                Current = config.GetBindable<AutoNewComboInterval>(OsuSetting.EditorAutoNewComboInterval),
            };

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AdvanceByPreviousSpacingOnPlacement,
                    TooltipText = EditorStrings.AdvanceByPreviousSpacingOnPlacementDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AutoNewCombo,
                    TooltipText = EditorStrings.AutoNewComboDescription,
                    Current = autoNewCombo
                },
                new SettingsItemV2(autoNewComboInterval),
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.FreehandSliderPlacement,
                    TooltipText = EditorStrings.FreehandSliderPlacementDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorFreehandSliderPlacement)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ArcOnAnchorAdd,
                    TooltipText = EditorStrings.ArcOnAnchorAddDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorArcOnAnchorAdd)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.CtrlClickCyclesCurveType,
                    TooltipText = EditorStrings.CtrlClickCyclesCurveTypeDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorCtrlClickCyclesCurveType)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.RemoveDragThreshold,
                    TooltipText = EditorStrings.RemoveDragThresholdDescription,
                    Current = config.GetBindable<bool>(OsuSetting.RemoveDragThreshold)
                },
                new SettingsCheckbox
                {
                    TooltipText = EditorStrings.SwapRightAndMiddleMouseDescription,
                    LabelText = EditorStrings.SwapRightAndMiddleMouse,
                    Current = config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AutoGenerateBreaks,
                    TooltipText = EditorStrings.AutoGenerateBreaksDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAutoGenerateBreaks)
                },
            };

            // how often says nothing while nothing is being started, so it follows the switch above it.
            autoNewCombo.BindValueChanged(on => autoNewComboInterval.Current.Disabled = !on.NewValue, true);
        }
    }
}
