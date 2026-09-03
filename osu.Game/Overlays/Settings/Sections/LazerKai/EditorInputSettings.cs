// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorInputSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.InputHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
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
        }
    }
}
