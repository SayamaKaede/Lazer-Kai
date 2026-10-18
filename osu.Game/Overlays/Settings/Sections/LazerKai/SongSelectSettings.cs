// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class SongSelectSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.SongSelectHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = LazerKaiSettingsStrings.SongSelectOpensEditor,
                    TooltipText = LazerKaiSettingsStrings.SongSelectOpensEditorDescription,
                    Keywords = new[] { @"edit", @"play", @"alt" },
                    Current = config.GetBindable<bool>(OsuSetting.SongSelectOpensEditor)
                },
            };
        }
    }
}
