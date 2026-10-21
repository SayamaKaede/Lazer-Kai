// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.MappingTime
{
    /// <summary>
    /// One day of the calendar: its date, how long was mapped on it, and a strip from midnight to midnight showing
    /// when.
    /// </summary>
    /// <remarks>
    /// The strip is drawn the way the editor's timeline is, as that is what the time was spent looking at. The cell
    /// is filled more strongly the more was mapped, up to <see cref="full_hours"/>.
    /// </remarks>
    public partial class MappingTimeDayCell : OsuClickableContainer
    {
        private const double full_hours = 6;

        private const float strip_height = 4;

        public readonly DateTime Day;

        private readonly bool inMonth;
        private readonly bool selected;
        private readonly bool today;
        private readonly IReadOnlyList<MappingDayPiece> pieces;

        private Box background = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public MappingTimeDayCell(DateTime day, bool inMonth, bool selected, bool today, IReadOnlyList<MappingDayPiece> pieces, Action<DateTime> select)
        {
            Day = day;
            this.inMonth = inMonth;
            this.selected = selected;
            this.today = today;
            this.pieces = pieces;

            RelativeSizeAxes = Axes.Both;
            Padding = new MarginPadding(2);
            Action = () => select(day);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            var total = pieces.Aggregate(TimeSpan.Zero, (sum, p) => sum + p.Length);

            var strip = new Container
            {
                // only on days something was mapped, so an empty month is not a page of lines (2026-10-21).
                Alpha = total == TimeSpan.Zero ? 0 : 1,
                Anchor = Anchor.BottomLeft,
                Origin = Anchor.BottomLeft,
                RelativeSizeAxes = Axes.X,
                Height = strip_height,
                Masking = true,
                CornerRadius = strip_height / 2,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6,
                    },
                }
            };

            foreach (var piece in pieces)
            {
                strip.Add(new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.X,
                    X = (float)(piece.From.TimeOfDay.TotalHours / 24),
                    // never thinner than a line, or a few minutes would not show at all.
                    Width = (float)Math.Max(piece.Length.TotalHours / 24, 0.01),
                    Colour = colourProvider.Highlight1,
                });
            }

            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                CornerRadius = 4,
                BorderThickness = selected ? 2 : 0,
                BorderColour = colourProvider.Light1,
                Alpha = inMonth ? 1 : 0.35f,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        // the accent, as the overlay's own colours are too close to the cell's to tell the days apart (2026-10-21).
                        Colour = colourProvider.Highlight1,
                        Alpha = total == TimeSpan.Zero ? 0 : 0.08f + 0.42f * (float)Math.Min(total.TotalHours / full_hours, 1),
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(5),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = Day.Day.ToString(),
                                Font = OsuFont.GetFont(size: 14, weight: today ? FontWeight.Bold : FontWeight.SemiBold),
                                Colour = today ? colourProvider.Highlight1 : MappingTimeCalendar.WeekdayColour(Day.DayOfWeek, colours) ?? colourProvider.Light1,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Y = -(strip_height + 3),
                                Text = total == TimeSpan.Zero ? string.Empty : $@"{(int)total.TotalHours}:{total.Minutes:00}",
                                Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Content2,
                            },
                            strip,
                        }
                    },
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeColour(colourProvider.Background3, 100, Easing.OutQuint);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background4, 100, Easing.OutQuint);
            base.OnHoverLost(e);
        }
    }
}
