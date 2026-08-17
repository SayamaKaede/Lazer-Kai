// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class TestPlaySettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.TestPlayHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ApplyTestPlayModsToComposer,
                    Current = config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.LiveModPreview,
                    Current = config.GetBindable<bool>(OsuSetting.EditorLiveModPreview)
                },
            };
        }
    }
}
