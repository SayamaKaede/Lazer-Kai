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
    public partial class EditorGhostDifficultySettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.GhostDifficultyHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<EditorSelectionColour>
                {
                    LabelText = EditorStrings.ReferenceDifficultyColour,
                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorReferenceDifficultyColour)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.ReferenceDifficultyOpacity,
                    Current = config.GetBindable<float>(OsuSetting.EditorReferenceDifficultyOpacity),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
