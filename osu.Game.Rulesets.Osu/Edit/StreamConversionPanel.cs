// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The settings for turning a slider into a stream, shown while it is being turned.
    /// </summary>
    /// <remarks>
    /// A window rather than a popover, for one reason: a popover is placed against whatever opened it and stays
    /// there, and this one has to be moved off the stream it is about. It opens in the bottom right and is
    /// dragged by its title.
    ///
    /// The slider is taken out while the panel is open so that nothing is drawn over the stream being judged, and
    /// put back if the panel is closed without the change being taken.
    /// </remarks>
    public partial class StreamConversionPanel : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        private const float width = 300;
        private const float margin = 20;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private IEditorChangeHandler? changeHandler { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        /// <summary>
        /// The division the stream is built on, which is the panel's own rather than the editor's — changing it
        /// here should not move every other snap in the editor.
        /// </summary>
        [Cached]
        private readonly BindableBeatDivisor divisor = new BindableBeatDivisor(4);

        private readonly BindableFloat acceleration = new BindableFloat(1) { MinValue = 0.25f, MaxValue = 4, Precision = 0.05f };
        private readonly Bindable<StreamEasing> easing = new Bindable<StreamEasing>();
        private readonly BindableFloat zigzag = new BindableFloat { MinValue = 0, MaxValue = 60, Precision = 1 };
        private readonly BindableInt zigzagPeriod = new BindableInt(1) { MinValue = 1, MaxValue = 8, Precision = 1 };
        private readonly Bindable<ZigzagShape> zigzagShape = new Bindable<ZigzagShape>();
        private readonly BindableBool drawnCurve = new BindableBool();

        private FormEnumDropdown<StreamEasing> easingDropdown = null!;
        private FormSliderBar<float> accelerationSlider = null!;
        private FormSliderBar<int> zigzagPeriodSlider = null!;
        private FormEnumDropdown<ZigzagShape> zigzagShapeDropdown = null!;
        private StreamCurveEditor curveEditor = null!;

        private readonly List<HitCircle> preview = new List<HitCircle>();

        private Slider? source;
        private bool changing;
        private bool placed;

        private Container body = null!;
        private Box header = null!;
        private Sample? openSample;

        protected override bool StartHidden => true;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            RelativeSizeAxes = Axes.Both;

            openSample = audio.Samples.Get(@"UI/overlay-pop-in");

            InternalChild = body = new Container
            {
                Width = width,
                AutoSizeAxes = Axes.Y,
                Masking = true,
                CornerRadius = 10,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Colour4.Black.Opacity(0.4f),
                    Radius = 12,
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 34,
                                Children = new Drawable[]
                                {
                                    header = new Box
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Colour = colourProvider.Background3,
                                    },
                                    new OsuSpriteText
                                    {
                                        Anchor = Anchor.CentreLeft,
                                        Origin = Anchor.CentreLeft,
                                        Margin = new MarginPadding { Left = 12 },
                                        Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                                        Text = EditorStrings.ConvertToStream,
                                    },
                                    new IconButton
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Margin = new MarginPadding { Right = 4 },
                                        Scale = new Vector2(0.6f),
                                        Icon = FontAwesome.Solid.Times,
                                        Action = () => Hide(),
                                    },
                                },
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding(10),
                                Spacing = new Vector2(5),
                                Children = new Drawable[]
                                {
                                    // the editor's own divisor control, which says what a division is by showing
                                    // it. A caption over it would be naming something already on screen.
                                    new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 90,
                                        Masking = true,
                                        CornerRadius = 5,
                                        Child = new BeatDivisorControl { RelativeSizeAxes = Axes.Both },
                                    },
                                    accelerationSlider = new FormSliderBar<float> { Caption = EditorStrings.StreamAcceleration, Current = acceleration, TabbableContentContainer = this },
                                    new FormCheckBox { Caption = EditorStrings.StreamDrawTheCurve, Current = drawnCurve },
                                    easingDropdown = new FormEnumDropdown<StreamEasing> { Caption = EditorStrings.StreamEasing, Current = easing },
                                    curveEditor = new StreamCurveEditor
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 150,
                                        Alpha = 0,
                                    },
                                    new FormSliderBar<float> { Caption = EditorStrings.StreamZigzag, Current = zigzag, TabbableContentContainer = this },
                                    zigzagPeriodSlider = new FormSliderBar<int> { Caption = EditorStrings.StreamZigzagPeriod, Current = zigzagPeriod, TabbableContentContainer = this },
                                    zigzagShapeDropdown = new FormEnumDropdown<ZigzagShape> { Caption = EditorStrings.StreamZigzagShape, Current = zigzagShape },
                                    new RoundedButton
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Text = EditorStrings.ConvertToStream,
                                        Action = commit,
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            divisor.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            acceleration.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            easing.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            zigzag.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            zigzagPeriod.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            zigzagShape.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            curveEditor.Points.BindCollectionChanged((_, _) => Scheduler.AddOnce(rebuild));

            // one or the other on screen: two curves over the same thing is a puzzle rather than a setting.
            // acceleration goes with the list, because it is part of the same reckoning — a drawn curve says
            // everything acceleration was for, and the slider does nothing while one is being used. Leaving it
            // there would be a control that looks live and is not.
            drawnCurve.BindValueChanged(drawn =>
            {
                easingDropdown.Alpha = drawn.NewValue ? 0 : 1;
                accelerationSlider.Alpha = drawn.NewValue ? 0 : 1;
                curveEditor.Alpha = drawn.NewValue ? 1 : 0;

                Scheduler.AddOnce(rebuild);
            }, true);

            // the two below the zigzag only mean something once it reaches somewhere. Dimmed rather than taken
            // away, so they can still be set up before the reach is raised.
            zigzag.BindValueChanged(reach =>
            {
                float alpha = reach.NewValue > 0 ? 1 : 0.4f;

                zigzagPeriodSlider.Alpha = alpha;
                zigzagShapeDropdown.Alpha = alpha;
            }, true);
        }

        /// <summary>
        /// Opens on a slider, and starts showing what it would become.
        /// </summary>
        public void Open(Slider slider)
        {
            if (changing)
                cancel();

            source = slider;

            changeHandler?.BeginChange();
            changing = true;

            // out of the way: the stream is what is being judged, and the slider it came from drawn over the top
            // of it makes that hard to see. It comes back if the panel is closed without taking the change.
            editorBeatmap.Remove(slider);

            // only the first time: after that it opens where it was last put, which is the point of being able
            // to move it. Measured from the top because the panel's height is not known until it has been laid
            // out — taking it off the bottom on the first open put the panel through the floor.
            if (!placed)
            {
                body.Position = new Vector2(DrawWidth - width - margin, margin);
                placed = true;
            }

            Show();
            rebuild();
        }

        private void rebuild()
        {
            if (!changing || source == null)
                return;

            double beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(source.StartTime).BeatLength;

            var plan = StreamConversion.Plan(source, beatLength / divisor.Value, acceleration.Value,
                easing.Value.ToEasing(), zigzag.Value, zigzagPeriod.Value, zigzagShape.Value,
                drawnCurve.Value ? StreamCurve.Build(curveEditor.Points) : null);

            if (preview.Count > plan.Count)
            {
                editorBeatmap.RemoveRange(preview.GetRange(plan.Count, preview.Count - plan.Count));
                preview.RemoveRange(plan.Count, preview.Count - plan.Count);
            }

            var added = new List<HitCircle>();

            for (int i = 0; i < plan.Count; i++)
            {
                if (i < preview.Count)
                {
                    preview[i].StartTime = plan[i].Time;
                    preview[i].Position = plan[i].Position;
                    continue;
                }

                added.Add(new HitCircle
                {
                    StartTime = plan[i].Time,
                    Position = plan[i].Position,
                    NewCombo = i == 0 && source.NewCombo,
                    Samples = source.HeadCircle.Samples.Select(s => s.With()).ToList(),
                });
            }

            preview.AddRange(added);
            editorBeatmap.AddRange(added);
        }

        private void commit()
        {
            if (!changing)
                return;

            changeHandler?.EndChange();
            changing = false;
            source = null;
            preview.Clear();

            Hide();
        }

        private void cancel()
        {
            if (!changing)
                return;

            editorBeatmap.RemoveRange(preview);
            preview.Clear();

            if (source != null)
                editorBeatmap.Add(source);

            changeHandler?.EndChange();
            changing = false;
            source = null;
        }

        protected override void PopIn()
        {
            openSample?.Play();
            this.FadeIn(150, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            cancel();
            this.FadeOut(150, Easing.OutQuint);
        }

        /// <summary>
        /// Only the window itself takes the mouse.
        /// </summary>
        /// <remarks>
        /// This container is the size of the editor so that the window can be dragged anywhere in it, and taking
        /// input over all of that would leave the playfield and the timeline unclickable while the panel is open
        /// — with nothing on screen to say why.
        /// </remarks>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            body.ScreenSpaceDrawQuad.Contains(screenSpacePos);

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        /// <summary>
        /// Closes on the back action, which also stops it reaching the editor behind — where it would walk out
        /// of the beatmap rather than out of this panel.
        /// </summary>
        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action != GlobalAction.Back || State.Value != Visibility.Visible)
                return false;

            Hide();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        // dragging is only offered by the title: the controls below it are all things that take a drag of their own.
        protected override bool OnDragStart(DragStartEvent e) => header.ScreenSpaceDrawQuad.Contains(e.ScreenSpaceMouseDownPosition);

        protected override void OnDrag(DragEvent e)
        {
            var position = body.Position + e.Delta;

            body.Position = new Vector2(
                Math.Clamp(position.X, 0, Math.Max(DrawWidth - body.DrawWidth, 0)),
                Math.Clamp(position.Y, 0, Math.Max(DrawHeight - body.DrawHeight, 0)));
        }
    }
}
