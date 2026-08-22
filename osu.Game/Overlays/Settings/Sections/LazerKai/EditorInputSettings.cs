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
                    LabelText = EditorStrings.RemoveDragThreshold,
                    TooltipText = EditorStrings.RemoveDragThresholdDescription,
                    Current = config.GetBindable<bool>(OsuSetting.RemoveDragThreshold)
                },
                new SettingsCheckbox
                {
                    TooltipText = LazerKaiSettingsStrings.EditorOnly,
                    LabelText = EditorStrings.SwapRightAndMiddleMouse,
                    Current = config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse)
                },
            };
        }
    }
}
