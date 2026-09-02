// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Rulesets.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit
{
    internal partial class BottomBar : CompositeDrawable
    {
        /// <summary>
        /// How tall the bar is, so anything drawing over the editor can leave it out.
        /// </summary>
        public const float HEIGHT = 50;

        public TestGameplayButton TestGameplayButton { get; private set; } = null!;

        private SummaryTimeline summaryTimeline = null!;

        private IBindable<bool> saveInProgress = null!;
        private Bindable<bool> composerFocusMode = null!;

        [BackgroundDependencyLoader]
        private void load(Editor editor)
        {
            Anchor = Anchor.BottomLeft;
            Origin = Anchor.BottomLeft;

            RelativeSizeAxes = Axes.X;

            Height = HEIGHT;

            Masking = true;
            EdgeEffect = new EdgeEffectParameters
            {
                Colour = Color4.Black.Opacity(0.2f),
                Type = EdgeEffectType.Shadow,
                Radius = 10f,
            };

            InternalChildren = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[]
                    {
                        // slightly wider than upstream to fit the star rating readout alongside the timestamp.
                        new Dimension(GridSizeMode.Absolute, 190),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 220),
                        new Dimension(GridSizeMode.Absolute, HitObjectComposer.TOOLBOX_CONTRACTED_SIZE_RIGHT),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new TimeInfoContainer { RelativeSizeAxes = Axes.Both },
                            summaryTimeline = new SummaryTimeline { RelativeSizeAxes = Axes.Both },
                            new PlaybackControl { RelativeSizeAxes = Axes.Both },
                            TestGameplayButton = new TestGameplayButton
                            {
                                RelativeSizeAxes = Axes.Both,
                                Size = new Vector2(1),
                                Action = editor.TestGameplay,
                            }
                        },
                    }
                }
            };

            saveInProgress = editor.MutationTracker.InProgress.GetBoundCopy();
            composerFocusMode = editor.ComposerFocusMode.GetBoundCopy();
            flashlightPreviewed = editor.FlashlightPreviewed.GetBoundCopy();
        }

        private IBindable<bool> flashlightPreviewed = null!;

        /// <summary>
        /// How see-through the bar's own backgrounds are.
        /// </summary>
        /// <remarks>
        /// Working in the playfield takes them half-transparent so as not to draw the eye, which puts the
        /// beatmap's own background back on screen along the bottom. Under flashlight that is the one bright strip
        /// left in an otherwise dark picture, so the bar is kept solid for as long as it is on.
        /// </remarks>
        private void updateBackgroundOpacity()
        {
            // Transforms should be kept in sync with other usages of composer focus mode.
            foreach (var c in this.ChildrenOfType<BottomBarContainer>())
            {
                if (!composerFocusMode.Value || flashlightPreviewed.Value)
                    c.Background.FadeIn(750, Easing.OutQuint);
                else
                    c.Background.Delay(600).FadeTo(0.5f, 4000, Easing.OutQuint);
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            saveInProgress.BindValueChanged(_ => TestGameplayButton.Enabled.Value = !saveInProgress.Value, true);
            composerFocusMode.BindValueChanged(_ => updateBackgroundOpacity());
            flashlightPreviewed.BindValueChanged(_ => updateBackgroundOpacity(), true);
        }

        /// <summary>
        /// The bar takes the mouse before the beatmap behind it does, so a click on a button here does not also
        /// reach into the beatmap. Where the timeline has been turned off, it lets go instead.
        /// </summary>
        /// <remarks>
        /// Reaching an object that runs under the bar is the reason for turning the timeline off at all: a slider
        /// tail or an anchor down here cannot be dragged while the bar is holding the mouse down. Only that share of
        /// the bar opens up — the clock, playback and the test button keep taking it, since a click meant for one of
        /// them starting a drag in the beatmap would be worse than not reaching it.
        /// </remarks>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            if (!summaryTimeline.IsPresent && summaryTimeline.ScreenSpaceDrawQuad.Contains(screenSpacePos))
                return false;

            return base.ReceivePositionalInputAt(screenSpacePos);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;
        protected override bool OnClick(ClickEvent e) => true;
    }
}
