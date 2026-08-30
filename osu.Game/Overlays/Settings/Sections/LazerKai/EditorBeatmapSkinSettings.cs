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
    /// Whether the editor shows the beatmap's own colours and plays its own hitsounds.
    /// </summary>
    /// <remarks>
    /// Gameplay has had these two switches for years; the editor has them forced on and no way to say otherwise.
    /// Turned off, the selected skin's colours and samples are used instead — the same thing that happens when
    /// they are turned off for playing.
    ///
    /// They are their own settings rather than the gameplay ones read from a second place: a mapper turning the
    /// beatmap's hitsounds off to hear what they are writing has said nothing about how the beatmap should play.
    /// </remarks>
    public partial class EditorBeatmapSkinSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.BeatmapSkinHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                // worded the way the same two switches are worded for gameplay, rather than as "turn it off":
                // somebody who knows the gameplay ones should not have to work out that these are the same
                // switch said backwards.
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = LazerKaiSettingsStrings.EditorBeatmapColours,
                    HintText = LazerKaiSettingsStrings.EditorBeatmapColoursDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorBeatmapColours),
                })
                {
                    Keywords = new[] { "combo", "colour", "color", "skin" },
                },
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = LazerKaiSettingsStrings.EditorBeatmapHitsounds,
                    HintText = LazerKaiSettingsStrings.EditorBeatmapHitsoundsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorBeatmapHitsounds),
                })
                {
                    Keywords = new[] { "hitsound", "sample", "audio", "skin" },
                },
            };
        }
    }
}
