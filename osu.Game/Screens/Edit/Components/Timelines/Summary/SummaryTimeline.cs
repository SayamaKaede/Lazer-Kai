// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Input;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osuTK;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary
{
    /// <summary>
    /// The timeline that sits at the bottom of the editor.
    /// </summary>
    public partial class SummaryTimeline : BottomBarContainer, IFineDragArea
    {
        /// <summary>
        /// Nothing here reads the mouse itself — the markers inside do — so this would otherwise be left out of
        /// the positional input queue entirely, and <see cref="IFineDragArea"/> would never be seen on it.
        /// </summary>
        public override bool HandlePositionalInput => true;

        /// <summary>
        /// Follows the same switch as the timeline along the top of the editor.
        /// </summary>
        /// <remarks>
        /// Turning the timeline off is asking to look at the beatmap rather than at the instruments around it, and
        /// this strip is as much of the instruments as that one. Only the timeline goes: the clock, playback and the
        /// test button beside it are how the editor is operated.
        /// </remarks>
        private readonly Bindable<bool> shown = new Bindable<bool>(true);

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorShowTimeline, shown);

            Background.Colour = colourProvider.Background6;

            Children = new Drawable[]
            {
                // sits behind everything else: it is a backdrop for the markers, not something to read precisely.
                new DifficultyGraphPart(),
                new Container
                {
                    Name = "centre line",
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background2,
                    Children = new Drawable[]
                    {
                        new Circle
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.Centre,
                            Size = new Vector2(5)
                        },
                        new Box
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            RelativeSizeAxes = Axes.X,
                            Height = 1,
                            EdgeSmoothness = new Vector2(0, 1),
                        },
                        new Circle
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.Centre,
                            Size = new Vector2(5)
                        },
                    }
                },
                new BreakPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
                new KiaiPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
                new ControlPointPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.4f
                },
                new BookmarkPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.Both,
                    Height = 0.4f
                },
                new PreviewTimePart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
                new CollabPlayheadPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
                new CollabPart
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    RelativeSizeAxes = Axes.Both,
                },
                new MarkerPart { RelativeSizeAxes = Axes.Both },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // hidden rather than removed, so the rest of the bar stays where the hand expects to find it.
            shown.BindValueChanged(show =>
            {
                this.FadeTo(show.NewValue ? 1 : 0, 200, Easing.OutQuint);
                AlwaysPresent = false;
            }, true);
        }
    }
}
