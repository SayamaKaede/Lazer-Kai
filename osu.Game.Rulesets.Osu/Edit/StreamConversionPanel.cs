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
using osu.Framework.Utils;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK.Input;
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

        /// <summary>How many circles one stretch is normally allowed.</summary>
        private const int ordinary_limit = 16;

        /// <summary>And how many once the limit is lifted.</summary>
        private const int extended_limit = 64;


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
        public readonly BindableBeatDivisor Divisor = new BindableBeatDivisor(4);

        /// <summary>
        /// The editor's own division, which <see cref="Divisor"/> starts from. Kept apart so that moving the
        /// panel's does not move every other snap in the editor.
        /// </summary>
        [Resolved]
        private BindableBeatDivisor editorBeatDivisor { get; set; } = null!;

        /// <summary>
        /// How many circles to lay down. Follows the division until it is set by hand, which is what lets more of
        /// them into a short slider than the division has room for.
        /// </summary>
        public readonly BindableInt NoteCount = new BindableInt(2) { MinValue = 2, MaxValue = ordinary_limit, Precision = 1 };

        /// <summary>
        /// Whether the count is allowed past what one stretch usually holds.
        /// </summary>
        /// <remarks>
        /// Sixteen covers all but a few stretches, and one that reaches sixteen is already a wide sweep of the bar.
        /// Leaving the whole range open makes every drag of the count coarse for the sake of the rare one.
        /// </remarks>
        public readonly BindableBool ExtendedLimits = new BindableBool();

        /// <summary>
        /// How many circles apart a new combo starts. Nothing by default: a stream is one run, and breaking it up
        /// is something asked for rather than assumed.
        /// </summary>
        public readonly BindableInt NewComboEvery = new BindableInt { MinValue = 0, MaxValue = 16, Precision = 1 };

        /// <summary>
        /// Whether the shape's own end is dragged to whole distance snap ticks.
        /// </summary>
        /// <remarks>
        /// The last stretch's spacing is its share of the path divided by the circles in it, so where the shape's end
        /// is put is what sets it. Snapping keeps that spacing the same as everywhere else in the beatmap; without it
        /// the end goes where it is put, which is the only way to set a spacing between two ticks.
        ///
        /// It is only about that end, so it is shown while the last stretch is the one being looked at. The stretches
        /// before it end on corners, which go where they are dragged either way.
        /// </remarks>
        public readonly BindableBool SnapSpacing = new BindableBool(true);

        /// <summary>What each stretch has been told about that, in the same order as <see cref="stretches"/>.</summary>
        private readonly List<bool> snapSpacingOfStretches = new List<bool>();

        private FormCheckBox snapSpacingCheckBox = null!;

        /// <summary>What a stream of one stretch was last told, which the next one starts out with.</summary>
        private Bindable<bool> snapSpacingWhenWhole = null!;

        /// <summary>
        /// And what the last stretch of a shape with corners was last told, which is remembered apart from it.
        /// </summary>
        /// <remarks>
        /// A shape being shaped into corners is being placed by hand, and a shape left as it was drawn is being laid
        /// along the ticks. Sharing one memory between them meant every corner added undid the answer to the other.
        /// </remarks>
        private Bindable<bool> snapSpacingWhenLastOfSeveral = null!;

        /// <summary>Which of the two the shape as it stands is remembered in.</summary>
        private Bindable<bool> snapSpacingRemembered => stretches.Count > 1 ? snapSpacingWhenLastOfSeveral : snapSpacingWhenWhole;

        private readonly BindableFloat acceleration = new BindableFloat(1) { MinValue = 0.25f, MaxValue = 4, Precision = 0.05f };
        private readonly Bindable<StreamEasing> easing = new Bindable<StreamEasing>();
        private readonly BindableFloat zigzag = new BindableFloat { MinValue = 0, MaxValue = 60, Precision = 1 };
        private readonly BindableInt zigzagPeriod = new BindableInt(1) { MinValue = 1, MaxValue = 8, Precision = 1 };
        private readonly Bindable<ZigzagShape> zigzagShape = new Bindable<ZigzagShape>();
        private readonly BindableBool drawnCurve = new BindableBool();

        private OsuScrollContainer settings = null!;
        private FillFlowContainer flow = null!;
        private FormEnumDropdown<StreamEasing> easingDropdown = null!;
        private FormSliderBar<float> accelerationSlider = null!;
        private FormSliderBar<int> zigzagPeriodSlider = null!;
        private FormEnumDropdown<ZigzagShape> zigzagShapeDropdown = null!;
        private StreamCurveEditor curveEditor = null!;

        private StreamPreview preview = null!;

        [Resolved(canBeNull: true)]
        private HitObjectComposer? composer { get; set; }

        /// <summary>Resolved on its own for the tool switch, which the shared base does not offer.</summary>
        [Resolved(canBeNull: true)]
        private OsuHitObjectComposer? osuComposer { get; set; }

        private Slider? source;

        /// <summary>Whether <see cref="source"/> was already in the beatmap when the panel was opened.</summary>
        private bool shapeCameFromTheBeatmap = true;

        /// <summary>
        /// What each stretch of the shape has been told to do. One per stretch between corners, oldest first.
        /// </summary>
        private readonly List<StreamConversion.Stretch> stretches = new List<StreamConversion.Stretch>();

        public StreamStretchSelector Selector = null!;

        /// <summary>
        /// Set while the controls are being pointed at a different stretch, so that filling them in does not read
        /// as the user having changed something.
        /// </summary>
        private bool showing;

        /// <summary>Where the circles were last put, which is how far they have since been dragged.</summary>
        private IReadOnlyList<StreamConversion.Note> planned = Array.Empty<StreamConversion.Note>();

        /// <summary>Ticks over whenever the shape is reshaped, which the stream is rebuilt from.</summary>
        private IBindable<int>? pathVersion;

        /// <summary>
        /// The drawn shape whose anchors are to be shown, or null when there is nothing being shaped.
        /// </summary>
        /// <remarks>
        /// Read by whatever draws those anchors. The shape is not in the beatmap, so nothing else knows about it.
        /// </remarks>
        public readonly Bindable<Slider?> Shape = new Bindable<Slider?>();
        private bool changing;
        private bool placed;

        private Container body = null!;
        private Box header = null!;
        private Sample? openSample;

        protected override bool StartHidden => true;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio, OsuConfigManager config)
        {
            preview = new StreamPreview(editorBeatmap, composer);

            // a new stretch starts out however the last one was left, which is remembered between runs and sessions.
            snapSpacingWhenWhole = config.GetBindable<bool>(OsuSetting.EditorStreamSnapSpacingWhole);
            snapSpacingWhenLastOfSeveral = config.GetBindable<bool>(OsuSetting.EditorStreamSnapSpacingLastOfSeveral);
            SnapSpacing.Value = snapSpacingWhenWhole.Value;

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
                            settings = new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                ScrollbarVisible = false,
                                Child = flow = new FillFlowContainer
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
                                        Selector = new StreamStretchSelector(),
                                        new FormSliderBar<int> { Caption = EditorStrings.StreamCount, Current = NoteCount, TabbableContentContainer = this },
                                        new FormCheckBox { Caption = EditorStrings.StreamExtendedLimits, Current = ExtendedLimits },
                                        snapSpacingCheckBox = new FormCheckBox
                                        {
                                            Caption = EditorStrings.StreamSnapSpacing,
                                            HintText = EditorStrings.StreamSnapSpacingDescription,
                                            Current = SnapSpacing,
                                        },
                                        new FormSliderBar<int>
                                        {
                                            Caption = EditorStrings.StreamNewComboEvery,
                                            Current = NewComboEvery,
                                            TabbableContentContainer = this,
                                            LabelFormat = v => v == 0 ? EditorStrings.StreamNewComboNone : $@"{v}",
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
    },
                                },
                            },
                            // outside the scrolling part: the button that takes the change must never be the thing
                            // that scrolled away.
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding { Horizontal = 10, Bottom = 10 },
                                Child = new RoundedButton
                                {
                                    RelativeSizeAxes = Axes.X,
                                    Text = EditorStrings.ConvertToStream,
                                    Action = commit,
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

            // the division goes on saying how many fit, so moving it is the way back from a count set by hand.
            Divisor.BindValueChanged(_ => takeCountFromDivisor());
            Selector.Current.BindValueChanged(_ => showStretch());

            ExtendedLimits.BindValueChanged(_ =>
            {
                if (source == null)
                    return;

                NoteCount.MaxValue = limit(NoteCount.Value);
            });
            NoteCount.BindValueChanged(_ => takeFromControls());
            SnapSpacing.BindValueChanged(_ => takeFromControls());
            NewComboEvery.BindValueChanged(_ => Scheduler.AddOnce(rebuild));
            acceleration.BindValueChanged(_ => takeFromControls());
            easing.BindValueChanged(_ => takeFromControls());
            zigzag.BindValueChanged(_ => takeFromControls());
            zigzagPeriod.BindValueChanged(_ => takeFromControls());
            zigzagShape.BindValueChanged(_ => takeFromControls());
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
        /// <param name="slider">The shape the stream is laid along.</param>
        /// <param name="fromTheBeatmap">
        /// Whether the slider is one the beatmap already holds.
        ///
        /// One that is gets taken out while the panel is open: the stream is what is being judged, and the slider it
        /// came from drawn over the top makes that hard to see. It goes back if the change is not taken.
        ///
        /// A shape just drawn with the Streams tool goes the other way -- it is put in and selected, so its anchors
        /// can be pulled about the way any slider's can while the stream is watched. It is taken out again either
        /// way: replaced by the circles when the conversion is taken, and removed when it is not.
        /// </param>
        public void Open(Slider slider, bool fromTheBeatmap = true)
        {
            if (changing)
                cancel();

            source = slider;
            shapeCameFromTheBeatmap = fromTheBeatmap;

            changeHandler?.BeginChange();
            changing = true;

            // starts from the division the editor is set to, which is what the hand is already playing to. Read on
            // every open rather than held, so it follows the editor rather than whatever was last reached for here.
            // Moving it afterwards still only moves the panel's own.
            //
            // the list first: changing it snaps the value to something in it, undoing a value set before it.
            Divisor.ValidDivisors.Value = editorBeatDivisor.ValidDivisors.Value;
            Divisor.Value = editorBeatDivisor.Value;

            // a shape drawn with the Streams tool never goes into the beatmap. What is being made is a run of
            // circles, and a slider drawn among them says nothing about how that run reads. Its anchors are shown
            // on their own instead, so the shape can still be pulled about.
            if (fromTheBeatmap)
                editorBeatmap.Remove(slider);
            else
            {
                Shape.Value = slider;

                // the shape is as long as it is drawn, and no longer. A slider carries a length of its own, set
                // from its velocity and kept while its anchors move -- so a shape pulled shorter than that length
                // was still being walked to the old one, and the circles that ran past the end piled up on it.
                slider.Path.ExpectedDistance.Value = null;

                // back to the select tool, and the run left selected. The stream is the thing on screen now, and
                // wanting to move it or reach its anchors is the first thing to do with it -- neither of which the
                // Streams tool is holding the mouse for.
                osuComposer?.SetSelectTool();
            }

            // the stream follows the shape as it is reshaped.
            pathVersion?.UnbindAll();
            pathVersion = slider.Path.Version.GetBoundCopy();
            pathVersion.BindValueChanged(_ => Scheduler.AddOnce(rebuild));

            // only the first time: after that it opens where it was last put, which is the point of being able
            // to move it. Measured from the top because the panel's height is not known until it has been laid
            // out — taking it off the bottom on the first open put the panel through the floor.
            if (!placed)
            {
                body.Position = new Vector2(DrawWidth - width - margin, margin);
                placed = true;
            }

            Show();
            takeCountFromDivisor();

        }

        /// <summary>
        /// Puts the count back to what the division asks for, and leaves room above it to ask for more.
        /// </summary>
        private void takeCountFromDivisor()
        {
            if (source == null)
                return;

            stretches.Clear();
            stretches.AddRange(sharesOfTheDivision().Select(share => new StreamConversion.Stretch(share)));

            snapSpacingOfStretches.Clear();
            snapSpacingOfStretches.AddRange(Enumerable.Repeat(snapSpacingRemembered.Value, stretches.Count));

            Selector.Count.Value = stretches.Count;
            Selector.Current.Value = Math.Min(Selector.Current.Value, stretches.Count - 1);

            showStretch();
            Scheduler.AddOnce(rebuild);
        }

        /// <summary>
        /// How many circles the division puts in each stretch: the whole shape's worth, shared out by how much of
        /// the path each stretch covers.
        /// </summary>
        /// <remarks>
        /// The count is a stretch's own, so the division has to be shared out rather than handed to each of them --
        /// giving every stretch the whole shape's count made a two-cornered shape twice as long as it should be,
        /// and put the top of the bar somewhere nobody asked for.
        /// </remarks>
        private int[] sharesOfTheDivision()
        {
            double beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(source!.StartTime).BeatLength;

            // the gaps rather than the circles: a stretch holds one more circle than it has gaps, and the corner
            // circles are shared, so the gaps are what shares out evenly.
            int gaps = Math.Max(StreamConversion.CountFor(source, beatLength / Divisor.Value) - 1, 1);

            var corners = StreamConversion.CornersOf(source);
            var shares = new int[corners.Count + 1];
            double from = 0;

            for (int s = 0; s < shares.Length; s++)
            {
                double to = s < corners.Count ? corners[s] : 1;

                shares[s] = Math.Max(2, (int)Math.Round(gaps * (to - from)) + 1);
                from = to;
            }

            return shares;
        }

        protected override void Update()
        {
            base.Update();

            // as tall as the list wants, up to what is left on screen. The panel grows as a stream is given more
            // to say, and one taller than the window would put its own button out of reach.
            settings.Height = Math.Min(flow.DrawHeight, Math.Max(DrawHeight - 2 * margin - 34 - 60, 0));

            if (!changing)
                return;

            // put back before anything else is worked out from it: half a run says nothing about where it was.
            if (preview.AnythingWasTakenAway())
            {
                preview.Forget();
                rebuild();
            }

            preview.HoldOnScreen();
            followTheCircles();
            keepTheRunSelected();
        }

        /// <summary>
        /// Keeps the run selected while the panel is up.
        /// </summary>
        /// <remarks>
        /// Clicking anywhere else in the editor clears the selection, which is right everywhere but here: the run
        /// is what the panel is about, and losing hold of it means finding it again before it can be moved.
        /// </remarks>
        private void keepTheRunSelected()
        {
            if (shapeCameFromTheBeatmap || preview.Circles.Count == 0)
                return;

            if (editorBeatmap.SelectedHitObjects.Count == preview.Circles.Count)
                return;

            editorBeatmap.SelectedHitObjects.Clear();
            editorBeatmap.SelectedHitObjects.AddRange(preview.Circles);
        }

        /// <summary>
        /// Carries the shape along when the run is moved, across the playfield or along the timeline.
        /// </summary>
        /// <remarks>
        /// The circles are what is selected, so moving them leaves the shape where it was. Touching an anchor after
        /// that put the whole run back where the shape still thought it was -- in place and in time both, since the
        /// times are counted from the shape's own start. The shape is moved by however far the run went instead,
        /// which keeps the two the same thing.
        /// </remarks>
        private void followTheCircles()
        {
            if (source == null || shapeCameFromTheBeatmap || planned.Count == 0 || preview.Circles.Count != planned.Count)
                return;

            var moved = preview.Circles[0].Position - planned[0].Position;
            double waited = preview.Circles[0].StartTime - planned[0].Time;

            if (moved == Vector2.Zero && Precision.AlmostEquals(waited, 0, 0.01))
                return;

            source.Position += moved;
            source.StartTime += waited;

            Scheduler.AddOnce(rebuild);
        }

        /// <summary>
        /// Brings the list of stretches into line with the shape, which gains and loses corners as it is pulled
        /// about. Ones that were already there keep what they were told.
        /// </summary>
        private void countTheStretches()
        {
            if (source == null)
                return;

            int wanted = StreamConversion.CornersOf(source).Count + 1;
            bool wasOfSeveral = stretches.Count > 1;

            while (stretches.Count > wanted)
            {
                stretches.RemoveAt(stretches.Count - 1);
                snapSpacingOfStretches.RemoveAt(snapSpacingOfStretches.Count - 1);
            }

            if (stretches.Count < wanted)
            {
                // a corner was added, so the shape is divided up differently. What each stretch was told is no
                // longer about the same piece of path, so the division shares itself out again.
                var shares = sharesOfTheDivision();

                while (stretches.Count < wanted)
                {
                    stretches.Add(new StreamConversion.Stretch(shares[stretches.Count]));
                    snapSpacingOfStretches.Add(true);
                }
            }

            // the first corner turns a whole shape into one with an end of its own to place, and taking it away turns
            // it back. Either way the end takes the answer remembered for the shape it is now.
            if (stretches.Count > 0 && wasOfSeveral != stretches.Count > 1)
            {
                snapSpacingOfStretches[^1] = snapSpacingRemembered.Value;
                showStretch();
            }

            Selector.Count.Value = wanted;
        }

        /// <summary>
        /// Whether the shape's own end is dragged to whole distance snap ticks, which is what the last stretch has
        /// been told: that end is where the last stretch stops.
        /// </summary>
        /// <param name="hitObject">The slider whose end is being dragged. Anything but the one being converted
        /// snaps as it always has.</param>
        public bool SnapsTheEndOf(HitObject hitObject) =>
            !changing
            || !ReferenceEquals(source, hitObject)
            || snapSpacingOfStretches.Count == 0
            || snapSpacingOfStretches[^1];

        /// <summary>
        /// The most circles a stretch may hold: what is asked for, but never fewer than the division itself fits.
        /// </summary>
        private int limit(int fits) => Math.Max(fits, ExtendedLimits.Value ? extended_limit : ordinary_limit);

        /// <summary>Points the controls at the stretch now being looked at.</summary>
        private void showStretch()
        {
            if (Selector.Current.Value >= stretches.Count)
                return;

            var stretch = stretches[Selector.Current.Value];

            showing = true;

            // the top of the bar before what it holds, or the value would be clamped on the way in.
            NoteCount.MaxValue = limit(stretch.Count);
            NoteCount.Value = stretch.Count;
            SnapSpacing.Value = snapSpacingOfStretches[Selector.Current.Value];

            // only the last stretch ends on the shape's own end, which is the end this is about. A stretch before it
            // ends on a corner, and a corner goes where it is dragged either way.
            snapSpacingCheckBox.Alpha = Selector.Current.Value == stretches.Count - 1 ? 1 : 0;

            acceleration.Value = (float)stretch.Acceleration;
            easing.Value = stretch.Easing;
            zigzag.Value = stretch.Zigzag;
            zigzagPeriod.Value = stretch.ZigzagPeriod;
            zigzagShape.Value = stretch.ZigzagShape;

            showing = false;
        }

        /// <summary>Writes what the controls say back to the stretch being looked at.</summary>
        private void takeFromControls()
        {
            if (showing || Selector.Current.Value >= stretches.Count)
                return;

            snapSpacingOfStretches[Selector.Current.Value] = SnapSpacing.Value;

            // what the next shape of this kind starts out with. Only the last stretch's answer is worth remembering,
            // since it is the only one the setting is about.
            if (Selector.Current.Value == stretches.Count - 1)
                snapSpacingRemembered.Value = SnapSpacing.Value;

            stretches[Selector.Current.Value] = new StreamConversion.Stretch(NoteCount.Value)
            {
                Acceleration = acceleration.Value,
                Easing = easing.Value,
                Zigzag = zigzag.Value,
                ZigzagPeriod = zigzagPeriod.Value,
                ZigzagShape = zigzagShape.Value,
            };

            Scheduler.AddOnce(rebuild);
        }

        private void rebuild()
        {
            if (!changing || source == null)
                return;

            countTheStretches();

            double beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(source.StartTime).BeatLength;
            double spacing = beatLength / Divisor.Value;

            var plan = StreamConversion.PlanStretches(source, spacing, stretches,
                drawnCurve.Value ? StreamCurve.Build(curveEditor.Points) : null);

            planned = plan;
            preview.Update(source, plan, NewComboEvery.Value);

            // the whole run stays selected while the panel is up, so it can be dragged about as one.
            if (!shapeCameFromTheBeatmap)
            {
                editorBeatmap.SelectedHitObjects.Clear();
                editorBeatmap.SelectedHitObjects.AddRange(preview.Circles);
            }
        }

        private void commit()
        {
            if (!changing)
                return;

            Shape.Value = null;
            pathVersion?.UnbindAll();

            changeHandler?.EndChange();
            changing = false;
            source = null;
            preview.Release();

            Hide();
        }

        private void cancel()
        {
            if (!changing)
                return;

            preview.Clear();

            if (source != null && shapeCameFromTheBeatmap)
                editorBeatmap.Add(source);

            Shape.Value = null;
            pathVersion?.UnbindAll();

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

        /// <summary>
        /// Escape closes without taking the change, enter takes it.
        /// </summary>
        /// <remarks>
        /// Handled here rather than left to whatever has focus. Both are what the hand reaches for once the stream
        /// looks right, and reaching for the mouse to press a button that is already decided is a step for nothing.
        /// </remarks>
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Repeat || State.Value != Visibility.Visible)
                return false;

            switch (e.Key)
            {
                case Key.Escape:
                    Hide();
                    return true;

                case Key.Enter:
                case Key.KeypadEnter:
                    commit();
                    return true;
            }

            return base.OnKeyDown(e);
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
