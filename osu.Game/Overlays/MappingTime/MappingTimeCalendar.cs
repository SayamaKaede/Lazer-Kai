// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;
using osuTK.Graphics;

namespace osu.Game.Overlays.MappingTime
{
    /// <summary>
    /// A month of days, six weeks from the Sunday on or before its first, with the month's total beside its name.
    /// </summary>
    public partial class MappingTimeCalendar : CompositeDrawable
    {
        public const float HEADER_HEIGHT = 36;
        public const float WEEKDAYS_HEIGHT = 22;

        /// <summary>
        /// The first day of the month shown.
        /// </summary>
        public readonly Bindable<DateTime> Month = new Bindable<DateTime>();

        public readonly Bindable<DateTime> SelectedDay = new Bindable<DateTime>();

        private MappingTimeSummary summary = new MappingTimeSummary(Array.Empty<MappingSpan>(), TimeZoneInfo.Local);

        public MappingTimeSummary Summary
        {
            get => summary;
            set
            {
                summary = value;

                if (IsLoaded)
                    populate();
            }
        }

        private OsuSpriteText monthText = null!;
        private OsuSpriteText totalText = null!;
        private GridContainer days = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        /// <summary>
        /// Saturday in blue and Sunday in red, as Japanese calendars mark them; null on weekdays.
        /// </summary>
        public static Color4? WeekdayColour(DayOfWeek day, OsuColour colours) => day switch
        {
            DayOfWeek.Saturday => colours.Blue1,
            DayOfWeek.Sunday => colours.Red1,
            _ => null,
        };

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = HEADER_HEIGHT,
                    Children = new Drawable[]
                    {
                        new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new osuTK.Vector2(4, 0),
                            Children = new Drawable[]
                            {
                                new IconButton
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = FontAwesome.Solid.ChevronLeft,
                                    Action = () => Month.Value = Month.Value.AddMonths(-1),
                                },
                                monthText = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold),
                                    Colour = colourProvider.Light1,
                                },
                                new IconButton
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Icon = FontAwesome.Solid.ChevronRight,
                                    Action = () => Month.Value = Month.Value.AddMonths(1),
                                },
                            }
                        },
                        totalText = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                            Font = OsuFont.GetFont(size: 15, weight: FontWeight.SemiBold),
                            Colour = colourProvider.Content2,
                        },
                    }
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = WEEKDAYS_HEIGHT,
                    Y = HEADER_HEIGHT,
                    Content = new[]
                    {
                        Enumerable.Range(0, 7).Select(d => (Drawable)new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Text = MappingTimeStrings.Weekday((DayOfWeek)d),
                            Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                            Colour = WeekdayColour((DayOfWeek)d, colours) ?? colourProvider.Foreground1,
                        }).ToArray()
                    }
                },
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = HEADER_HEIGHT + WEEKDAYS_HEIGHT },
                    Child = days = new GridContainer { RelativeSizeAxes = Axes.Both },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Month.BindValueChanged(_ => populate());
            SelectedDay.BindValueChanged(_ => populate());
            populate();
        }

        private void populate()
        {
            var month = Month.Value;

            monthText.Text = MappingTimeStrings.Month(month);
            totalText.Text = MappingTimeStrings.Total(MappingTimeStrings.Length(summary.TotalIn(month.Year, month.Month)));

            var first = month.AddDays(-(int)month.DayOfWeek);
            var today = DateTime.Today;

            days.Content = Enumerable.Range(0, 6).Select(week => Enumerable.Range(0, 7).Select(d =>
            {
                var day = first.AddDays(week * 7 + d);

                return (Drawable)new MappingTimeDayCell(day, day.Month == month.Month, day == SelectedDay.Value, day == today,
                    summary.PiecesOn(day), selected => SelectedDay.Value = selected);
            }).ToArray()).ToArray();
        }
    }
}
