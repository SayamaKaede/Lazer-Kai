// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorOtherCustomisationSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.OtherCustomisationHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<EditorGridShape>
                {
                    LabelText = EditorStrings.GridDefaultShape,
                    TooltipText = EditorStrings.GridDefaultsDescription,
                    Current = config.GetBindable<EditorGridShape>(OsuSetting.EditorGridDefaultType)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.GridDefaultSpacing,
                    HintText = EditorStrings.GridDefaultsDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorGridDefaultSpacing),
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.GridDefaultRotation,
                    Current = config.GetBindable<float>(OsuSetting.EditorGridDefaultRotation),
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.GridDefaultX,
                    Current = config.GetBindable<float>(OsuSetting.EditorGridDefaultX),
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.GridDefaultY,
                    Current = config.GetBindable<float>(OsuSetting.EditorGridDefaultY),
                }),
            };
        }
    }
}
