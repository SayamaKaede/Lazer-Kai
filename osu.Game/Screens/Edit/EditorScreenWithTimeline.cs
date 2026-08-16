// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.Edit.Compose.Components.Timeline;

namespace osu.Game.Screens.Edit
{
    [Cached]
    public abstract partial class EditorScreenWithTimeline : EditorScreen
    {
        public TimelineArea TimelineArea { get; private set; } = null!;

        public Container MainContent { get; private set; } = null!;

        private LoadingSpinner spinner = null!;
        private Container timelineContent = null!;
        private Container timelineContainer = null!;

        private readonly Bindable<bool> showTimeline = new Bindable<bool>(true);

        protected EditorScreenWithTimeline(EditorScreenMode type)
            : base(type)
        {
        }

        [BackgroundDependencyLoader(true)]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorShowTimeline, showTimeline);

            // Grid with only two rows.
            // First is the timeline area, which should be allowed to expand as required.
            // Second is the main editor content, including the playfield and side toolbars (but not the bottom).
            Child = new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                RowDimensions = new[]
                {
                    new Dimension(GridSizeMode.AutoSize),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        timelineContainer = new Container
                        {
                            Name = "Timeline",
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Children = new Drawable[]
                            {
                                new GridContainer
                                {
                                    Name = "Timeline content",
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Content = new[]
                                    {
                                        new Drawable[]
                                        {
                                            timelineContent = new Container
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                            },
                                        },
                                    },
                                    RowDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.AutoSize),
                                    },
                                    ColumnDimensions = new[]
                                    {
                                        new Dimension(),
                                        new Dimension(GridSizeMode.Absolute, 90),
                                    }
                                }
                            }
                        },
                    },
                    new Drawable[]
                    {
                        MainContent = new Container
                        {
                            Name = "Main content",
                            RelativeSizeAxes = Axes.Both,
                            Depth = float.MaxValue,
                            Child = spinner = new LoadingSpinner(true)
                            {
                                State = { Value = Visibility.Visible },
                            },
                        },
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            showTimeline.BindValueChanged(show =>
            {
                // the row sizes itself to the timeline, so it has to be excluded from that measurement as well as
                // hidden, otherwise collapsing would leave the gap behind.
                timelineContainer.Alpha = show.NewValue ? 1 : 0;
                timelineContainer.BypassAutoSizeAxes = show.NewValue ? Axes.None : Axes.Y;
            }, true);

            LoadComponentAsync(CreateMainContent(), content =>
            {
                spinner.State.Value = Visibility.Hidden;

                MainContent.Add(content);
                content.FadeInFromZero(300, Easing.OutQuint);

                LoadComponentAsync(TimelineArea = new TimelineArea(CreateTimelineContent()), timeline =>
                {
                    ConfigureTimeline(timeline);
                    timelineContent.Add(timeline);
                });
            });
        }

        protected virtual void ConfigureTimeline(TimelineArea timelineArea)
        {
        }

        protected abstract Drawable CreateMainContent();

        protected virtual Drawable CreateTimelineContent() => new Container();
    }
}
