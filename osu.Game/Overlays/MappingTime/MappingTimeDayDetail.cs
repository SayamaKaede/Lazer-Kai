// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Overlays.MappingTime
{
    /// <summary>
    /// What was mapped on one day: how long the game ran, how long the beatmaps changed were open and how long went on
    /// changing them, then each set with the same two, opening onto each of its difficulties.
    /// </summary>
    /// <remarks>
    /// Each row sets the time spent changing a beatmap against the time it was open, with the share that makes, so
    /// how much of the time in the editor was spent on it reads at a glance (2026-10-20, chosen by the user).
    /// </remarks>
    public partial class MappingTimeDayDetail : CompositeDrawable
    {
        private const float title_height = 30;
        private const float times_height = 24;

        /// <summary>
        /// The room for the share at the end of each row.
        /// </summary>
        private const float share_width = 44;

        /// <summary>
        /// The room for the times and the share, which the names give way to.
        /// </summary>
        private const float right_width = 170;

        private OsuSpriteText dayText = null!;
        private OsuSpriteText timesText = null!;
        private FillFlowContainer sets = null!;

        /// <summary>
        /// The sets opened, kept as the day changes so one followed across days stays open.
        /// </summary>
        private readonly HashSet<Guid> opened = new HashSet<Guid>();

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                dayText = new OsuSpriteText
                {
                    Height = title_height,
                    Font = OsuFont.GetFont(size: 18, weight: FontWeight.SemiBold),
                    Colour = colourProvider.Light1,
                },
                timesText = new OsuSpriteText
                {
                    Y = title_height,
                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                    Colour = colourProvider.Content2,
                },
                new OsuScrollContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = title_height + times_height + 6 },
                    Child = sets = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2),
                    },
                },
            };
        }

        public void Show(DateTime day, MappingTimeSummary summary)
        {
            dayText.Text = MappingTimeStrings.Day(day);
            timesText.Text = MappingTimeStrings.DayTimes(
                MappingTimeStrings.Length(summary.GameOn(day)),
                MappingTimeStrings.Length(summary.OpenOn(day)),
                MappingTimeStrings.Length(summary.TotalOn(day)));

            sets.Clear();

            var setTimes = summary.SetsOn(day);

            if (setTimes.Count == 0)
            {
                sets.Add(new OsuSpriteText
                {
                    Text = MappingTimeStrings.NothingThisDay,
                    Font = OsuFont.GetFont(size: 14),
                    Colour = colourProvider.Content2,
                    Margin = new MarginPadding { Top = 8 },
                });
                return;
            }

            foreach (var set in setTimes)
                sets.Add(new SetRow(set, opened));
        }

        /// <summary>
        /// "changed / open" and the share, or the time changed alone where nothing says how long it was open: what was
        /// changed before the open time was kept.
        /// </summary>
        private static (LocalisableString times, string share) describe(TimeSpan changed, TimeSpan open)
        {
            if (open <= TimeSpan.Zero)
                return (MappingTimeStrings.Length(changed), string.Empty);

            int share = (int)Math.Round(Math.Min(changed / open, 1) * 100);
            return (LocalisableString.Interpolate($@"{MappingTimeStrings.Length(changed)} / {MappingTimeStrings.Length(open)}"), $@"{share}%");
        }

        private partial class SetRow : FillFlowContainer
        {
            private const float row_height = 30;
            private const float difficulty_height = 24;
            private const float indent = 22;

            private readonly MappingSetTime set;
            private readonly HashSet<Guid> opened;

            private SpriteIcon chevron = null!;
            private FillFlowContainer difficulties = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SetRow(MappingSetTime set, HashSet<Guid> opened)
            {
                this.set = set;
                this.opened = opened;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;
                Direction = FillDirection.Vertical;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    new OsuClickableContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = row_height,
                        Action = toggle,
                        Children = new Drawable[]
                        {
                            chevron = new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.Centre,
                                X = indent / 2,
                                Size = new Vector2(10),
                                Icon = FontAwesome.Solid.ChevronRight,
                                Colour = colourProvider.Foreground1,
                            },
                            createLine($@"{set.Artist} - {set.Title}", indent, set.Total, set.Open, 15, FontWeight.SemiBold, colourProvider.Content1),
                        }
                    },
                    difficulties = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = set.Difficulties.Select(d => (Drawable)new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = difficulty_height,
                            Child = createLine(d.DifficultyName, indent * 2, d.Total, d.Open, 13, FontWeight.Regular, colourProvider.Content2),
                        }).ToArray(),
                    },
                };

                updateOpened();
            }

            private static Drawable createLine(string name, float left, TimeSpan changed, TimeSpan open, float size, FontWeight weight, osuTK.Graphics.Color4 colour)
            {
                var (times, share) = describe(changed, open);

                return new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Left = left, Right = right_width },
                            Child = new TruncatingSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.X,
                                Text = name,
                                Font = OsuFont.GetFont(size: size + 1, weight: weight),
                                Colour = colour,
                            },
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            X = -share_width,
                            Text = times,
                            Font = OsuFont.GetFont(size: size, weight: weight),
                            Colour = colour,
                        },
                        new OsuSpriteText
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Text = share,
                            Font = OsuFont.GetFont(size: size, weight: FontWeight.SemiBold),
                            Colour = colour,
                        },
                    }
                };
            }

            private void toggle()
            {
                if (!opened.Remove(set.BeatmapSetID))
                    opened.Add(set.BeatmapSetID);

                updateOpened();
            }

            private void updateOpened()
            {
                bool isOpen = opened.Contains(set.BeatmapSetID);

                chevron.Icon = isOpen ? FontAwesome.Solid.ChevronDown : FontAwesome.Solid.ChevronRight;
                difficulties.Alpha = isOpen ? 1 : 0;
            }
        }
    }
}
