// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// A line across the top timeline at each bookmark (2026-11-09), in the blue the bottom timeline marks them in.
    /// </summary>
    /// <remarks>
    /// Drawn the way <see cref="TimelineSavedLineDisplay"/> draws the green lines: over the objects, taking no clicks,
    /// and only in the styles that keep the ticks down at the objects. In lazer's own the ticks run the full height,
    /// and a quarter-beat tick is the same blue.
    /// </remarks>
    public partial class TimelineBookmarkDisplay : TimelinePart<TimelineBookmarkDisplay.BookmarkPiece>
    {
        private const float line_width = 1;

        private const float line_opacity = 0.6f;

        [Resolved]
        private Timeline timeline { get; set; } = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();
        private readonly Bindable<EditorTimelineStyle> style = new Bindable<EditorTimelineStyle>();

        private bool visible => enabled.Value && style.Value != EditorTimelineStyle.Lazer;

        private readonly BindableList<int> bookmarks = new BindableList<int>();

        private readonly Cached piecesCache = new Cached();

        private (double min, double max) visibleRange = (double.MinValue, double.MaxValue);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorTimelineShowBookmarks, enabled);
            config.BindWith(OsuSetting.EditorTimelineStyle, style);
        }

        protected override void LoadBeatmap(EditorBeatmap beatmap)
        {
            base.LoadBeatmap(beatmap);

            bookmarks.UnbindAll();
            bookmarks.BindTo(beatmap.Bookmarks);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.BindValueChanged(_ => updateVisibility());
            style.BindValueChanged(_ => updateVisibility(), true);

            bookmarks.BindCollectionChanged((_, _) => piecesCache.Invalidate());
        }

        private void updateVisibility()
        {
            Alpha = visible ? 1 : 0;
            piecesCache.Invalidate();
        }

        protected override void Update()
        {
            base.Update();

            if (!visible || DrawWidth <= 0)
                return;

            (double, double) newRange = (
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopLeft).X - line_width) / DrawWidth * Content.RelativeChildSize.X,
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopRight).X + line_width) / DrawWidth * Content.RelativeChildSize.X);

            if (visibleRange != newRange)
            {
                visibleRange = newRange;
                piecesCache.Invalidate();
            }

            if (!piecesCache.IsValid)
            {
                recreatePieces();
                piecesCache.Validate();
            }
        }

        private void recreatePieces()
        {
            Clear();

            if (!visible)
                return;

            foreach (int time in bookmarks)
            {
                if (time < visibleRange.min || time > visibleRange.max)
                    continue;

                Add(new BookmarkPiece(time));
            }
        }

        public partial class BookmarkPiece : CompositeDrawable
        {
            public readonly double BookmarkTime;

            public BookmarkPiece(double time)
            {
                BookmarkTime = time;

                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;
                X = (float)time;
                Width = line_width;
                Origin = Anchor.TopCentre;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                InternalChild = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = line_opacity,
                    Colour = colours.Blue,
                };
            }
        }
    }
}
