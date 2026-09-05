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
    public partial class EditorButtonShapeSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ButtonShapeHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
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
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SampleBankButtonSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorSampleBankButtonSquareness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.GridButtonSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorGridButtonSquareness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ActionButtonSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorActionButtonSquareness),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.SliderVelocityPresetSquareness,
                    Current = config.GetBindable<float>(OsuSetting.EditorSliderVelocityPresetSquareness),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
