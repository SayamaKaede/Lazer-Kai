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
    /// The editor's ASMR mode: see <see cref="Screens.Edit.EditorAsmr"/>.
    /// </summary>
    public partial class EditorAsmrSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.AsmrHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = LazerKaiSettingsStrings.EditorAsmrSounds,
                    HintText = LazerKaiSettingsStrings.EditorAsmrSoundsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAsmrSounds),
                })
                {
                    Keywords = new[] { "asmr", "sound", "sfx" },
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = LazerKaiSettingsStrings.EditorAsmrVolume,
                    Current = config.GetBindable<float>(OsuSetting.EditorAsmrVolume),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
