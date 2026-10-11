// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays.Changelog;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    /// <summary>
    /// The fork's changelogs, in the look of osu!'s own changelog.
    /// </summary>
    /// <remarks>
    /// Shown once after an update with the releases not read yet, and from the settings with every release. What it
    /// shows is the changelogs shipped beside the game (<see cref="LazerKaiChangelog"/>): osu!'s changelog is read
    /// from its website, which has nothing on the fork's builds.
    ///
    /// They are written in Japanese, and from 2026-10-11 in English as well. The English is what is shown while the
    /// game is in any language other than Japanese.
    /// </remarks>
    public partial class LazerKaiChangelogOverlay : OnlineOverlay<LazerKaiChangelogHeader>
    {
        private readonly FillFlowContainer<Drawable> releaseFlow;

        private readonly Bindable<Language> language = new Bindable<Language>();

        private IReadOnlyList<LazerKaiChangelog.Release> shown = Array.Empty<LazerKaiChangelog.Release>();

        public LazerKaiChangelogOverlay()
            : base(OverlayColourScheme.Purple, false)
        {
            Child = releaseFlow = new FillFlowContainer<Drawable>
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding { Bottom = 50 },
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuGameBase game)
        {
            language.BindTo(game.CurrentLanguage);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // run now as well: after an update the releases are given before this has loaded, and with it the language.
            // Later it is the language being changed while the changelog is up, the settings being open over it.
            language.BindValueChanged(_ => populate(), true);
        }

        protected override LazerKaiChangelogHeader CreateHeader() => new LazerKaiChangelogHeader();

        protected override Color4 BackgroundColour => ColourProvider.Background4;

        /// <summary>
        /// Shows <paramref name="releases"/>, in the order given.
        /// </summary>
        public void ShowReleases(IReadOnlyList<LazerKaiChangelog.Release> releases)
        {
            shown = releases;
            populate();

            ScrollFlow.ScrollToStart(false);
            Show();
        }

        private void populate()
        {
            releaseFlow.Clear();

            for (int i = 0; i < shown.Count; i++)
            {
                if (i > 0)
                {
                    releaseFlow.Add(new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 2,
                        Colour = ColourProvider.Background6,
                        Margin = new MarginPadding { Top = 30 },
                    });
                }

                releaseFlow.Add(new LazerKaiChangelogRelease(shown[i], language.Value == Language.ja));
            }
        }
    }
}
