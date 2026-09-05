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
    public partial class EditorToolboxSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ToolboxHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideGridFromPointsTool,
                    TooltipText = EditorStrings.HideGridFromPointsToolDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideGridFromPointsTool)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandLeftToolbox,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.LeftToolboxWidth,
                    Current = config.GetBindable<float>(OsuSetting.EditorLeftToolboxWidth),
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.RightToolboxWidth,
                    Current = config.GetBindable<float>(OsuSetting.EditorRightToolboxWidth),
                }),
            };
        }
    }
}
