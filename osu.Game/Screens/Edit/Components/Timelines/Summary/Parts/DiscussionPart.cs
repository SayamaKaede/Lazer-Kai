// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Discussion;
using osuTK;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Marks where in the song the threads the discussion panel lists are, by their kind or as resolved,
    /// the way the website's beatmap page does along its timeline (2026-10-20 request).
    /// </summary>
    /// <remarks>
    /// Clicking one goes to its time and brings it into view in the panel.
    /// </remarks>
    public partial class DiscussionPart : TimelinePart
    {
        /// <summary>
        /// How large each mark is drawn, chosen by the mapper on the real thing (2026-10-20).
        /// </summary>
        private const float mark_size = 12;

        [Resolved]
        private DiscussionMarks marks { get; set; } = null!;

        private readonly IBindable<APIBeatmapsetDiscussion[]> timeline = new Bindable<APIBeatmapsetDiscussion[]>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            timeline.BindTo(marks.Timeline);
            timeline.BindValueChanged(_ => rebuild(), true);
        }

        private void rebuild()
        {
            Clear();

            // resolved first, so that what is still waiting is drawn over it where two are close together.
            foreach (var discussion in timeline.Value.OrderByDescending(d => d.Resolved))
                Add(new Mark(discussion) { Action = () => marks.Pick(discussion) });
        }

        public partial class Mark : ClickableContainer, IHasTooltip
        {
            public readonly APIBeatmapsetDiscussion Discussion;

            [Resolved]
            private EditorClock clock { get; set; } = null!;

            public Mark(APIBeatmapsetDiscussion discussion)
            {
                Discussion = discussion;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours, OverlayColourProvider colourProvider)
            {
                var (icon, colour) = DiscussionRow.MarkOf(Discussion, colours, colourProvider);

                RelativePositionAxes = Axes.X;
                X = Discussion.Timestamp ?? 0;

                // along the bottom edge, below the bookmarks, chosen by the mapper on the real thing (2026-10-20).
                Anchor = Anchor.BottomLeft;
                Origin = Anchor.BottomCentre;
                Size = new Vector2(mark_size);

                Child = new SpriteIcon
                {
                    RelativeSizeAxes = Axes.Both,
                    Icon = icon,
                    Colour = colour,
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                if (Discussion.Timestamp is int time)
                    clock.SeekSmoothlyTo(time);

                return base.OnClick(e);
            }

            public LocalisableString TooltipText
            {
                get
                {
                    string time = ((double)(Discussion.Timestamp ?? 0)).ToEditorFormattedString();
                    string firstLine = Discussions.StartingText(Discussion).Split('\n').FirstOrDefault()?.Trim() ?? string.Empty;

                    return $@"{time} {firstLine.Truncate(80)}";
                }
            }
        }
    }
}
