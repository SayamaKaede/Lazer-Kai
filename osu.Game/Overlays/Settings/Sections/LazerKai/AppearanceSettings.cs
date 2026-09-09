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
    /// Which look the game wears.
    /// </summary>
    public partial class AppearanceSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.AppearanceHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<UiTheme>
                {
                    Caption = LazerKaiSettingsStrings.UiTheme,
                    HintText = LazerKaiSettingsStrings.UiThemeDescription,
                    Current = config.GetBindable<UiTheme>(OsuSetting.UiTheme),
                }),
            };
        }
    }
}
