// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Graphics.Containers;
using osu.Game.Localisation;
using osu.Game.Overlays.MappingTime;
using osu.Game.Screens.Edit;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    /// <summary>
    /// How long was mapped each day, and on what, in a panel at the side from the toolbar.
    /// </summary>
    /// <remarks>
    /// In the editor's colours rather than the purple of the other toolbar overlays: what it shows is the editor's.
    /// It is read afresh each time it is opened, as the editor is never open at the same time as the toolbar.
    ///
    /// Tried across the whole screen as well, with the calendar and the day side by side; the side was kept (2026-10-20).
    /// </remarks>
    public partial class MappingTimeOverlay : OsuFocusedOverlayContainer, INamedOverlayComponent
    {
        public const float WIDTH = 420;

        /// <summary>
        /// The height of a week in the calendar.
        /// </summary>
        private const float week_height = 52;

        private const float padding = 16;

        public IconUsage Icon => FontAwesome.Solid.CalendarAlt;
        public LocalisableString Title => MappingTimeStrings.Title;
        public LocalisableString Description => MappingTimeStrings.Description;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        [Resolved]
        private Storage storage { get; set; } = null!;

        /// <summary>
        /// The game's running time, which is otherwise written out only once a minute.
        /// </summary>
        [Resolved(canBeNull: true)]
        private RunningTimeRecorder? gameTime { get; set; }

        private MappingTimeCalendar calendar = null!;
        private MappingTimeDayDetail detail = null!;

        private MappingTimeSummary summary = new MappingTimeSummary(Array.Empty<MappingSpan>(), TimeZoneInfo.Local);

        [BackgroundDependencyLoader]
        private void load()
        {
            const float calendar_height = MappingTimeCalendar.HEADER_HEIGHT + MappingTimeCalendar.WEEKDAYS_HEIGHT + week_height * 6;

            Anchor = Anchor.TopRight;
            Origin = Anchor.TopRight;
            RelativeSizeAxes = Axes.Y;
            Width = WIDTH;
            Alpha = 0;

            Child = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                EdgeEffect = new EdgeEffectParameters
                {
                    Colour = Color4.Black.Opacity(0.25f),
                    Type = EdgeEffectType.Shadow,
                    Radius = 10,
                    Hollow = true,
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(padding),
                        Children = new Drawable[]
                        {
                            calendar = new MappingTimeCalendar
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = calendar_height,
                            },
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding { Top = calendar_height + padding },
                                Child = detail = new MappingTimeDayDetail { RelativeSizeAxes = Axes.Both },
                            },
                        }
                    },
                }
            };

            var today = DateTime.Today;
            calendar.Month.Value = new DateTime(today.Year, today.Month, 1);
            calendar.SelectedDay.Value = today;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            calendar.SelectedDay.BindValueChanged(_ => showDay());
        }

        private void showDay() => detail.Show(calendar.SelectedDay.Value, summary);

        protected override void PopIn()
        {
            gameTime?.Tick(DateTimeOffset.Now);

            summary = new MappingTimeSummary(new MappingTimeStore(storage).Load(), TimeZoneInfo.Local);
            calendar.Summary = summary;
            showDay();

            this.FadeIn(WaveContainer.APPEAR_DURATION, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            base.PopOut();
            this.FadeOut(WaveContainer.DISAPPEAR_DURATION, Easing.OutQuint);
        }
    }
}
