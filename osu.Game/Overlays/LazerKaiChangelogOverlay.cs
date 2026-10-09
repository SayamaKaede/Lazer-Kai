// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.IO;
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
    /// </remarks>
    public partial class LazerKaiChangelogOverlay : OnlineOverlay<LazerKaiChangelogHeader>
    {
        private readonly FillFlowContainer<Drawable> releaseFlow;

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

        protected override LazerKaiChangelogHeader CreateHeader() => new LazerKaiChangelogHeader();

        protected override Color4 BackgroundColour => ColourProvider.Background4;

        /// <summary>
        /// Shows <paramref name="releases"/>, in the order given.
        /// </summary>
        public void ShowReleases(IReadOnlyList<LazerKaiChangelog.Release> releases)
        {
            releaseFlow.Clear();

            for (int i = 0; i < releases.Count; i++)
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

                releaseFlow.Add(new LazerKaiChangelogRelease(releases[i]));
            }

            ScrollFlow.ScrollToStart(false);
            Show();
        }
    }
}
