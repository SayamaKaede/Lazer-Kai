// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.IO;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// Opens the manual and the changelogs that ship beside the game.
    /// </summary>
    /// <remarks>
    /// First in the section on purpose: whoever opens the fork's settings without knowing what is in them is
    /// exactly who the manual is for, and a row further down is one they have to already be looking for.
    ///
    /// Whether this group exists at all is decided by <see cref="LazerKaiSection"/>, which gives it what was found
    /// beside the game. It is not decided here because a subsection cannot usefully hide itself: the settings
    /// search drives <see cref="SettingsSubsection.MatchingFilter"/>, which sets alpha directly, so anything that
    /// faded itself out would be faded back in the moment the panel filtered.
    /// </remarks>
    public partial class ManualSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ManualHeader;

        private readonly string? path;
        private readonly IReadOnlyList<LazerKaiChangelog.Release> changelog;

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        [Resolved]
        private LazerKaiChangelogOverlay? changelogOverlay { get; set; }

        [Resolved]
        private SettingsOverlay? settings { get; set; }

        /// <param name="path">The manual, or null when there is none.</param>
        /// <param name="changelog">Every release's changelog, newest first. Empty when there are none.</param>
        public ManualSettings(string? path, IReadOnlyList<LazerKaiChangelog.Release> changelog)
        {
            this.path = path;
            this.changelog = changelog;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            if (path != null)
            {
                Add(new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.OpenManual,
                    TooltipText = LazerKaiSettingsStrings.OpenManualDescription,
                    Action = () => gameHost.OpenFileExternally(path),
                });
            }

            if (changelog.Count > 0)
            {
                Add(new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.ShowChangelog,
                    // only under the manual's button: as the first row it sits where the heading leaves it.
                    Margin = new MarginPadding { Top = path != null ? BUTTON_GAP : 0 },
                    Action = () =>
                    {
                        // the changelog fills the screen, and the settings would otherwise stay on top of it.
                        settings?.Hide();
                        changelogOverlay?.ShowReleases(changelog);
                    },
                });
            }
        }
    }
}
