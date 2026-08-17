// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.LazerKai.Controls;

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
                new SettingsSlider<float, PixelsSlider>
                {
                    LabelText = EditorStrings.DragStartDistance,
                    // the only setting here that is not confined to the editor, which is worth saying plainly.
                    TooltipText = LazerKaiSettingsStrings.AppliesGameWide,
                    Current = config.GetBindable<float>(OsuSetting.DragStartDistance)
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
