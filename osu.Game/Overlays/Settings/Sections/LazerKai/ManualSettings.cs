// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// Opens the manual that ships beside the game.
    /// </summary>
    /// <remarks>
    /// First in the section on purpose: whoever opens the fork's settings without knowing what is in them is
    /// exactly who the manual is for, and a row further down is one they have to already be looking for.
    ///
    /// Whether this group exists at all is decided by <see cref="LazerKaiSection"/>, which is given the manual's
    /// path or nothing. It is not decided here because a subsection cannot usefully hide itself: the settings
    /// search drives <see cref="SettingsSubsection.MatchingFilter"/>, which sets alpha directly, so anything that
    /// faded itself out would be faded back in the moment the panel filtered.
    /// </remarks>
    public partial class ManualSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ManualHeader;

        private readonly string path;

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        public ManualSettings(string path)
        {
            this.path = path;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.OpenManual,
                    TooltipText = LazerKaiSettingsStrings.OpenManualDescription,
                    Action = () => gameHost.OpenFileExternally(path),
                },
            };
        }
    }
}
