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
    public partial class EditorDisplaySettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.DisplayHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowPlayAreaBorder,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowPlayAreaBorder)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.DetectOffscreen,
                    TooltipText = EditorStrings.DetectOffscreenDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorDetectOffscreen)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.ShowUnsnapMarkers,
                    TooltipText = EditorStrings.ShowUnsnapMarkersDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorShowUnsnapMarkers)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideKiaiFlash,
                    TooltipText = EditorStrings.HideKiaiFlashDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideKiaiFlash)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.LoopPlayback,
                    Current = config.GetBindable<bool>(OsuSetting.EditorLoopPlayback)
                },
                new SettingsItemV2(new FormSliderBar<double>
                {
                    Caption = EditorStrings.SeekAnimationDuration,
                    HintText = EditorStrings.SeekAnimationDurationDescription,
                    Current = config.GetBindable<double>(OsuSetting.EditorSeekAnimationDuration),
                    KeyboardStep = 10,
                    LabelFormat = v => v == 0 ? LazerKaiSettingsStrings.NoDelay : $"{v:N0} ms",
                }),
                new SettingsEnumDropdown<OverlayColourScheme>
                {
                    LabelText = EditorStrings.EditorColourScheme,
                    TooltipText = LazerKaiSettingsStrings.TakesEffectOnReopen,
                    Current = config.GetBindable<OverlayColourScheme>(OsuSetting.EditorColourScheme)
                },
            };
        }
    }
}
