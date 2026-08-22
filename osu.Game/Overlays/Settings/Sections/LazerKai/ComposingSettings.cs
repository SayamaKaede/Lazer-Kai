// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class ComposingSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ComposingHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<EditorAdditionBank>
                {
                    LabelText = EditorStrings.DefaultAdditionBank,
                    TooltipText = EditorStrings.DefaultAdditionBankDescription,
                    Current = config.GetBindable<EditorAdditionBank>(OsuSetting.EditorDefaultAdditionBank)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AdvanceByPreviousSpacingOnPlacement,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AutoGenerateBreaks,
                    TooltipText = EditorStrings.AutoGenerateBreaksDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAutoGenerateBreaks)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.FreehandSliderPlacement,
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
                    Current = config.GetBindable<bool>(OsuSetting.EditorCtrlClickCyclesCurveType)
                },
            };
        }
    }
}
