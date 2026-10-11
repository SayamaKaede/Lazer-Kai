// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.IO;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Overlays.Changelog
{
    /// <summary>
    /// One of the fork's releases, drawn the way osu!'s changelog draws a build (<see cref="ChangelogBuild"/>,
    /// <see cref="ChangelogEntry"/>).
    /// </summary>
    public partial class LazerKaiChangelogRelease : FillFlowContainer
    {
        public readonly LazerKaiChangelog.Release Release;

        /// <summary>
        /// Whether this is drawn from the release's Japanese changelog, rather than its English one.
        /// </summary>
        public readonly bool InJapanese;

        /// <param name="release">The release.</param>
        /// <param name="inJapanese">Whether the game is being read in Japanese. In any other language it is the
        /// release's English changelog that is shown, where it has one.</param>
        public LazerKaiChangelogRelease(LazerKaiChangelog.Release release, bool inJapanese)
        {
            Release = release;
            InJapanese = inJapanese;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
            Direction = FillDirection.Vertical;
            Padding = new MarginPadding { Horizontal = WaveOverlayContainer.HORIZONTAL_PADDING };
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Add(new FillFlowContainer
            {
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Margin = new MarginPadding { Top = 20 },
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = @"Lazer-Kai ",
                        Font = OsuFont.GetFont(weight: FontWeight.Medium, size: 24),
                    },
                    new OsuSpriteText
                    {
                        Text = Release.Version,
                        Font = OsuFont.GetFont(weight: FontWeight.Light, size: 24),
                        Colour = colourProvider.Highlight1,
                    },
                }
            });

            var icon = FontAwesome.Regular.Circle;

            foreach (var line in Release.LinesFor(InJapanese))
            {
                switch (line.Kind)
                {
                    case LazerKaiChangelog.LineKind.Heading:
                        icon = iconUnder(line.Text);

                        Add(new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(weight: FontWeight.Bold, size: 18))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Margin = new MarginPadding { Top = 35, Bottom = 15 },
                            Text = line.Text,
                        });
                        break;

                    case LazerKaiChangelog.LineKind.Change:
                        Add(new Change(line, icon));
                        break;

                    case LazerKaiChangelog.LineKind.Text:
                        Add(new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 16))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Margin = new MarginPadding { Vertical = 3 },
                            Text = line.Text,
                        });
                        break;
                }
            }
        }

        // osu!'s changelog marks an addition with a plus and a fix with a tick, from the kind of change it is told.
        // The fork's changelogs say the kind in their headings: 機能追加 and 修正, or Additions and Fixes.
        private static IconUsage iconUnder(string heading)
        {
            if (heading.Contains(@"修正") || heading.Contains(@"fix", StringComparison.OrdinalIgnoreCase))
                return FontAwesome.Solid.Check;

            if (heading.Contains(@"追加") || heading.Contains(@"add", StringComparison.OrdinalIgnoreCase))
                return FontAwesome.Solid.Plus;

            return FontAwesome.Regular.Circle;
        }

        private partial class Change : FillFlowContainer
        {
            private readonly LazerKaiChangelog.Line line;
            private readonly IconUsage icon;

            public Change(LazerKaiChangelog.Line line, IconUsage icon)
            {
                this.line = line;
                this.icon = icon;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Direction = FillDirection.Vertical;
                Margin = new MarginPadding { Vertical = 5 };
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Add(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Children = new Drawable[]
                    {
                        // hangs to the left of the text, as in osu!'s changelog, so that the lines of text start level.
                        new SpriteIcon
                        {
                            Origin = Anchor.TopRight,
                            Size = new Vector2(10),
                            Icon = icon,
                            Colour = Color4.White.Opacity(0.5f),
                            Margin = new MarginPadding { Right = 5, Top = 4 },
                        },
                        new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 16))
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = line.Text,
                        },
                    }
                });

                if (line.Details.Count > 0)
                {
                    Add(new OsuTextFlowContainer(t =>
                    {
                        t.Font = OsuFont.GetFont(size: 14);
                        t.Colour = colourProvider.Content2;
                    })
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Margin = new MarginPadding { Top = 3 },
                        Text = string.Join('\n', line.Details),
                    });
                }
            }
        }
    }
}
