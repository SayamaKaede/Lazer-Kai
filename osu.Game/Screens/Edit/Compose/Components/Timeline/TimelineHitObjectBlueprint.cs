// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    public partial class TimelineHitObjectBlueprint : SelectionBlueprint<HitObject>
    {
        private const float circle_size = 32;

        /// <summary>
        /// The height to draw at once the velocity and volume rows are hidden, which is theirs to give up.
        /// </summary>
        private const float expanded_circle_size = 52;

        private Container? repeatsContainer;

        public Action<DragEvent?>? OnDragHandled = null!;

        [UsedImplicitly]
        private readonly Bindable<double> startTime;

        private Bindable<int>? indexInCurrentComboBindable;

        private Bindable<int>? comboIndexBindable;
        private Bindable<int>? comboIndexWithOffsetsBindable;

        private Bindable<Color4> displayColourBindable = null!;

        private readonly ExtendableCircle circle;
        private readonly Border border;

        private readonly Container colouredComponents;
        private readonly Container sampleComponents;
        private readonly OsuSpriteText comboIndexText;
        private readonly SamplePointPiece samplePointPiece;
        private readonly DifficultyPointPiece? difficultyPointPiece;
        private readonly DragArea? dragArea;

        private readonly Bindable<bool> hideTimelinePoints = new Bindable<bool>();

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public TimelineHitObjectBlueprint(HitObject item)
            : base(item)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            startTime = item.StartTimeBindable.GetBoundCopy();
            startTime.BindValueChanged(time => X = (float)time.NewValue, true);

            RelativePositionAxes = Axes.X;

            RelativeSizeAxes = Axes.X;
            Height = circle_size;

            AddRangeInternal(new Drawable[]
            {
                circle = new ExtendableCircle
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                border = new Border
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                colouredComponents = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        comboIndexText = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.Centre,
                            Y = -1,
                            Font = OsuFont.Default.With(size: circle_size * 0.5f, weight: FontWeight.Regular),
                        },
                    }
                },
                samplePointPiece = new SamplePointPiece(Item)
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.TopCentre,
                    RelativePositionAxes = Axes.X,
                    AlternativeColor = Item is IHasRepeats
                },
                sampleComponents = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
            });

            if (item is IHasDuration)
            {
                colouredComponents.Add(dragArea = new DragArea(item)
                {
                    OnDragHandled = e => OnDragHandled?.Invoke(e)
                });
            }

            if (item is IHasSliderVelocity)
            {
                AddInternal(difficultyPointPiece = new DifficultyPointPiece(Item)
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.BottomCentre
                });
            }
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorHideTimelinePoints, hideTimelinePoints);

            hideTimelinePoints.BindValueChanged(_ => updateSizing());

            updateSizing();
        }

        /// <summary>
        /// The height objects are currently drawn at, which grows into the space the velocity and volume rows leave behind.
        /// </summary>
        private float currentCircleSize => hideTimelinePoints.Value ? expanded_circle_size : circle_size;

        private void updateSizing()
        {
            float size = currentCircleSize;

            Height = size;
            circle.CircleSize = size;
            border.CircleSize = size;
            comboIndexText.Font = OsuFont.Default.With(size: size * 0.5f, weight: FontWeight.Regular);

            if (dragArea != null)
                dragArea.CircleSize = size;

            if (repeatsContainer != null)
            {
                foreach (var tick in repeatsContainer.Children.OfType<Tick>())
                    tick.CircleSize = size;
            }

            // hidden rather than removed, so the pieces come back the moment the setting does.
            float pointAlpha = hideTimelinePoints.Value ? 0 : 1;

            samplePointPiece.Alpha = pointAlpha;
            sampleComponents.Alpha = pointAlpha;

            if (difficultyPointPiece != null)
                difficultyPointPiece.Alpha = pointAlpha;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            switch (Item)
            {
                case IHasDisplayColour displayColour:
                    displayColourBindable = displayColour.DisplayColour.GetBoundCopy();
                    displayColourBindable.BindValueChanged(_ => updateColour(), true);
                    break;

                case IHasComboInformation comboInfo:
                    indexInCurrentComboBindable = comboInfo.IndexInCurrentComboBindable.GetBoundCopy();
                    indexInCurrentComboBindable.BindValueChanged(_ =>
                    {
                        comboIndexText.Text = (indexInCurrentComboBindable.Value + 1).ToString();
                    }, true);

                    comboIndexBindable = comboInfo.ComboIndexBindable.GetBoundCopy();
                    comboIndexWithOffsetsBindable = comboInfo.ComboIndexWithOffsetsBindable.GetBoundCopy();

                    comboIndexBindable.BindValueChanged(_ => updateColour());
                    comboIndexWithOffsetsBindable.BindValueChanged(_ => updateColour(), true);

                    skin.SourceChanged += updateColour;
                    break;
            }
        }

        protected override void OnSelected()
        {
            // base logic hides selected blueprints when not selected, but timeline doesn't do that.
            updateColour();
        }

        protected override void OnDeselected()
        {
            // base logic hides selected blueprints when not selected, but timeline doesn't do that.
            updateColour();
        }

        private void updateColour()
        {
            Color4 colour;

            switch (Item)
            {
                case IHasDisplayColour displayColour:
                    colour = displayColour.DisplayColour.Value;
                    break;

                case IHasComboInformation combo:
                    colour = combo.GetComboColour(skin);
                    break;

                default:
                    colour = colourProvider.Highlight1;
                    break;
            }

            if (IsSelected)
                border.Show();
            else
                border.Hide();

            if (Item is IHasDuration duration && duration.Duration > 0)
                circle.Colour = ColourInfo.GradientHorizontal(colour, colour.Lighten(0.4f));
            else
                circle.Colour = colour;

            var averageColour = Interpolation.ValueAt(0.5, circle.Colour.TopLeft, circle.Colour.TopRight, 0, 1);
            colouredComponents.Colour = OsuColour.ForegroundTextColourFor(averageColour);
        }

        protected override void Update()
        {
            base.Update();

            // no bindable so we perform this every update
            float duration = (float)(Item.GetEndTime() - Item.StartTime);

            if (Width != duration)
            {
                Width = duration;

                // kind of haphazard but yeah, no bindables.
                if (Item is IHasRepeats repeats)
                    updateRepeats(repeats);
            }
        }

        private void updateRepeats(IHasRepeats repeats)
        {
            repeatsContainer?.Expire();

            colouredComponents.Add(repeatsContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            for (int i = 0; i < repeats.RepeatCount; i++)
            {
                repeatsContainer.Add(new Tick
                {
                    CircleSize = currentCircleSize,
                    X = (float)(i + 1) / (repeats.RepeatCount + 1)
                });
            }

            // Add node sample pieces
            sampleComponents.Clear();

            for (int i = 0; i < repeats.RepeatCount + 2; i++)
            {
                sampleComponents.Add(new NodeSamplePointPiece(Item, i)
                {
                    X = (float)i / (repeats.RepeatCount + 1),
                    RelativePositionAxes = Axes.X,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.TopCentre
                });
            }

            samplePointPiece.X = 1f / (repeats.RepeatCount + 1) / 2;
        }

        protected override bool ShouldBeConsideredForInput(Drawable child) => true;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            circle.ReceivePositionalInputAt(screenSpacePos);

        public override Quad SelectionQuad => circle.ScreenSpaceDrawQuad;

        public override Vector2 ScreenSpaceSelectionPoint => ScreenSpaceDrawQuad.TopLeft;

        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds)
        {
            // Since children are exceeding the component size, we need to use a custom quad to compute whether it should be masked away.

            // If the component isn't considered masked away by itself, there's no need to apply custom logic.
            if (!base.ComputeIsMaskedAway(maskingBounds))
                return false;

            // If the component is considered masked away, we'll use children to create an extended quad that encapsulates all parts of this blueprint
            // to ensure it doesn't pop in and out of existence abruptly when scrolling the timeline.
            var rect = RectangleF.Union(ScreenSpaceDrawQuad.AABBFloat, circle.ScreenSpaceDrawQuad.AABBFloat);
            rect = RectangleF.Union(rect, samplePointPiece.ScreenSpaceDrawQuad.AABBFloat);

            if (difficultyPointPiece != null)
                rect = RectangleF.Union(rect, difficultyPointPiece.ScreenSpaceDrawQuad.AABBFloat);

            return !Precision.AlmostIntersects(maskingBounds, rect);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (skin.IsNotNull())
                skin.SourceChanged -= updateColour;
        }

        private partial class Tick : Circle
        {
            /// <summary>
            /// The height objects are drawn at, which this is sized from.
            /// </summary>
            public float CircleSize
            {
                set => Size = new Vector2(value / 4);
            }

            public Tick()
            {
                Size = new Vector2(circle_size / 4);
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.Centre;
                RelativePositionAxes = Axes.X;
            }
        }

        public partial class DragArea : Circle
        {
            private readonly HitObject? hitObject;

            private readonly List<HitObject> objsToAdjust = new List<HitObject>();

            [Resolved]
            private EditorBeatmap beatmap { get; set; } = null!;

            [Resolved]
            private IBeatSnapProvider beatSnapProvider { get; set; } = null!;

            [Resolved]
            private Timeline timeline { get; set; } = null!;

            [Resolved]
            private IEditorChangeHandler? changeHandler { get; set; }

            private ScheduledDelegate? dragOperation;

            private readonly Bindable<float> repeatDragThreshold = new Bindable<float>(0.8f);
            private readonly Bindable<float> restingScale = new Bindable<float>(resting_scale);

            /// <summary>
            /// The scale the handle sits at while it is not being pointed at, which the user's setting replaces.
            /// </summary>
            private const float resting_scale = 0.5f;

            public Action<DragEvent?>? OnDragHandled;

            public override bool HandlePositionalInput => hitObject != null;

            /// <summary>
            /// The height objects are drawn at, which this is sized from so it stays round.
            /// </summary>
            public float CircleSize
            {
                set
                {
                    CornerRadius = value / 2;
                    Size = new Vector2(value, 1);
                }
            }

            public DragArea(HitObject? hitObject)
            {
                this.hitObject = hitObject;

                CornerRadius = circle_size / 2;
                Masking = true;
                Size = new Vector2(circle_size, 1);
                Anchor = Anchor.CentreRight;
                Origin = Anchor.Centre;
                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config)
            {
                config.BindWith(OsuSetting.EditorTimelineRepeatDragThreshold, repeatDragThreshold);
                config.BindWith(OsuSetting.EditorTimelineDragAreaScale, restingScale);

                restingScale.BindValueChanged(_ => updateState());
            }

            /// <summary>
            /// The repeat count a slider dragged out to <paramref name="proposedDuration"/> should take.
            /// </summary>
            /// <param name="proposedDuration">How long the drag has made the object, measured from its start.</param>
            /// <param name="lengthOfOneRepeat">How long one pass of the slider takes.</param>
            /// <param name="threshold">How far towards the next repeat the drag has to reach before it counts.</param>
            /// <remarks>
            /// <c>Math.Round</c> used to decide this, which puts the change exactly halfway but rounds its midpoint to
            /// even - so a drag landing on the halfway beat, which is exactly where the snapping puts it, did nothing,
            /// and the next tick along was already three quarters of the way there. Measuring the fraction directly
            /// keeps the point where it says it is, and lets the user move it.
            /// </remarks>
            internal static int RepeatCountFor(double proposedDuration, double lengthOfOneRepeat, float threshold)
            {
                double repeatsDragged = proposedDuration / lengthOfOneRepeat - threshold;

                // the drag lands on a snapped time, so the user can hit the boundary exactly. nudge it rather than let
                // floating point decide which side of it they are on.
                return Math.Max(0, (int)Math.Floor(repeatsDragged + Precision.FLOAT_EPSILON));
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                updateState();
                FinishTransforms();
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
                base.OnHoverLost(e);
            }

            private bool hasMouseDown;

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                hasMouseDown = true;
                updateState();
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                hasMouseDown = false;
                updateState();
                base.OnMouseUp(e);
            }

            private void updateState()
            {
                // pointing at the handle grows it, and pressing it settles back part of the way. those are the
                // affordance, so they are kept as a proportion of whatever size the user asked for rather than
                // as the fixed sizes they used to be.
                float scale = restingScale.Value;

                if (hasMouseDown)
                    scale *= 1.2f;
                else if (IsHovered)
                    scale *= 1.4f;

                this.ScaleTo(scale, 200, Easing.OutQuint);
                this.FadeTo(IsHovered || hasMouseDown ? 1f : 0.9f, 200, Easing.OutQuint);
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                changeHandler?.BeginChange();

                var selectionItems = beatmap.SelectedHitObjects;

                if (!selectionItems.Contains(hitObject))
                    return true;

                foreach (var item in selectionItems)
                {
                    if (item == hitObject || item is not IHasDuration durationItem) continue;

                    if (Precision.AlmostEquals(durationItem.Duration, (hitObject as IHasDuration)!.Duration, 1) &&
                        Precision.AlmostEquals(item.StartTime, hitObject!.StartTime, 1))
                    {
                        objsToAdjust.Add(item);
                    }
                }

                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                base.OnDrag(e);

                // schedule is temporary to ensure we don't process multiple times on a single update frame. we need to find a better method of doing this.
                // without it, a hitobject's endtime may not always be in a valid state (ie. sliders, which needs to recompute their path).
                dragOperation?.Cancel();
                dragOperation = Scheduler.Add(() =>
                {
                    OnDragHandled?.Invoke(e);

                    if (timeline.FindSnappedPositionAndTime(e.ScreenSpaceMousePosition).Time is double time)
                    {
                        switch (hitObject)
                        {
                            case IHasRepeats repeatHitObject:
                                double proposedDuration = time - hitObject.StartTime;

                                if (e.CurrentState.Keyboard.ShiftPressed && hitObject is IHasSliderVelocity hasSliderVelocity)
                                {
                                    double newVelocity = hasSliderVelocity.SliderVelocityMultiplier * (repeatHitObject.Duration / proposedDuration);

                                    if (Precision.AlmostEquals(newVelocity, hasSliderVelocity.SliderVelocityMultiplier))
                                        return;

                                    hasSliderVelocity.SliderVelocityMultiplier = newVelocity;
                                    beatmap.Update(hitObject);
                                }
                                else
                                {
                                    // find the number of repeats which can fit in the requested time.
                                    double lengthOfOneRepeat = repeatHitObject.Duration / (repeatHitObject.RepeatCount + 1);
                                    int proposedCount = RepeatCountFor(proposedDuration, lengthOfOneRepeat, repeatDragThreshold.Value);

                                    if (proposedCount == repeatHitObject.RepeatCount || Precision.AlmostEquals(lengthOfOneRepeat, 0, 1))
                                        return;

                                    repeatHitObject.RepeatCount = proposedCount;
                                    beatmap.Update(hitObject);
                                }

                                break;

                            case IHasDuration endTimeHitObject:
                                double snappedTime = Math.Max(hitObject.StartTime + beatSnapProvider.GetBeatLengthAtTime(hitObject.StartTime), beatSnapProvider.SnapTime(time));

                                if (endTimeHitObject.EndTime == snappedTime)
                                    return;

                                if (!objsToAdjust.Contains(hitObject))
                                    objsToAdjust.Add(hitObject);

                                foreach (var obj in objsToAdjust)
                                {
                                    (obj as IHasDuration)!.Duration = snappedTime - obj.StartTime;
                                    beatmap.Update(obj);
                                }

                                break;
                        }
                    }
                });
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                base.OnDragEnd(e);

                dragOperation?.Cancel();
                dragOperation = null;

                changeHandler?.EndChange();
                OnDragHandled?.Invoke(null);
                objsToAdjust.Clear();
            }
        }

        public partial class Border : ExtendableCircle
        {
            /// <summary>
            /// Matches the border thickness <see cref="ExtendableCircle"/> sets up.
            /// </summary>
            private const float default_border_thickness = 4;

            [BackgroundDependencyLoader]
            private void load(OsuColour colours, OsuConfigManager config)
            {
                Content.Child.Alpha = 0;
                Content.Child.AlwaysPresent = true;

                Content.EdgeEffect = new EdgeEffectParameters();

                var appearance = new EditorSelectionAppearance(config);

                void updateAppearance()
                {
                    Content.BorderColour = appearance.GetColour(colours.Yellow);
                    Content.BorderThickness = appearance.GetThickness(default_border_thickness);
                    Alpha = appearance.Opacity;
                }

                appearance.Changed += updateAppearance;
                updateAppearance();
            }
        }

        /// <summary>
        /// A circle with externalised end caps so it can take up the full width of a relative width area.
        /// </summary>
        public partial class ExtendableCircle : CompositeDrawable
        {
            public new ColourInfo Colour
            {
                get => Content.Colour;
                set => Content.Colour = value;
            }

            protected readonly Circle Content;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Content.ReceivePositionalInputAt(screenSpacePos);

            public override Quad ScreenSpaceDrawQuad => Content.ScreenSpaceDrawQuad;

            /// <summary>
            /// The height objects are drawn at, which decides how far the end caps reach past the object's bounds.
            /// </summary>
            public float CircleSize
            {
                set => Padding = new MarginPadding { Horizontal = -value / 2f };
            }

            public ExtendableCircle()
            {
                Padding = new MarginPadding { Horizontal = -circle_size / 2f };
                InternalChild = Content = new Circle
                {
                    BorderColour = OsuColour.Gray(0.75f),
                    BorderThickness = 4,
                    Masking = true,
                    RelativeSizeAxes = Axes.Both,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Radius = 5,
                        Colour = Color4.Black.Opacity(0.05f)
                    }
                };
            }
        }
    }
}
