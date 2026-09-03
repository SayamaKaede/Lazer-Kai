// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// One stretch of the beatmap, and who has it.
    /// </summary>
    /// <remarks>
    /// The name is picked from a list rather than stepped through: a collaboration between five people would take four
    /// clicks to reach the fifth, and the number of people is exactly what grows. The list opens as a popover rather
    /// than a dropdown because the panel is masked, and a dropdown's menu would be cut off inside it.
    ///
    /// Choosing somebody also moves the editor to where their stretch begins, so the split is heard as it is decided
    /// rather than agreed blind and checked afterwards.
    /// </remarks>
    public partial class CollabBlockRow : CompositeDrawable
    {
        /// <summary>What the list shows for a stretch nobody has yet.</summary>
        public const string NOBODY = "—";

        /// <summary>Whether this stretch is being pointed at, from this list or from the timeline at the bottom.</summary>
        public bool Lit { get; private set; }

        private readonly CollabBlock block;
        private readonly CollabProject project;
        private readonly OverlayColourProvider colourProvider;
        private readonly Action onAssigned;

        private Box background = null!;
        private Box playhead = null!;
        private OsuSpriteText assignedText = null!;

        private Color4 assignedColour = Color4.White;

        private readonly IBindable<double?> pointedAt = new Bindable<double?>();

        private bool playing;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private CollabHighlight highlight { get; set; } = null!;

        public CollabBlockRow(CollabBlock block, CollabProject project, OverlayColourProvider colourProvider, Action onAssigned)
        {
            this.block = block;
            this.project = project;
            this.colourProvider = colourProvider;
            this.onAssigned = onAssigned;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background3,
                },
                // says which stretch is being listened to, without taking the brightening the pointer uses: the two
                // answer different questions and are read at the same time.
                playhead = new Box
                {
                    RelativeSizeAxes = Axes.Y,
                    Width = 4,
                    Alpha = 0,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Horizontal = 8, Vertical = 4 },
                    ColumnDimensions = new[]
                    {
                        new Dimension(GridSizeMode.Absolute, 200),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 130),
                    },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            new QuietButton
                            {
                                RelativeSizeAxes = Axes.Both,
                                Action = seek,
                                Child = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Margin = new MarginPadding { Left = 6 },
                                    Text = $"{CollabClipboard.Timestamp(block.Start)} – {CollabClipboard.Timestamp(block.End)}",
                                    Font = OsuFont.Default.With(size: 14),
                                },
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = CollabStrings.BlockSummary(ComposeStatusDisplay.DrainTimeReadout(block.DrainTime), block.ObjectCount),
                                Font = OsuFont.Default.With(size: 14),
                                Colour = colourProvider.Foreground1,
                            },
                            new AssignButton(project, choose)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = assignedText = new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    Margin = new MarginPadding { Right = 8 },
                                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                                },
                            },
                        }
                    }
                },
            };

            updateAssigned();
        }

        private void choose(string? name)
        {
            if (name == null)
                project.Assignment.Remove(block.Key);
            else
                project.Assignment[block.Key] = name;

            updateAssigned();
            seek();
            onAssigned();
        }

        private void seek() => editorClock.SeekSmoothlyTo(block.Start);

        protected override void LoadComplete()
        {
            base.LoadComplete();

            pointedAt.BindTo(highlight.PointedAt);
            pointedAt.BindValueChanged(_ => updateBackground(), true);
        }

        /// <summary>
        /// Pointing at a row lights its stretch on the timeline along the bottom, which is the only place the split
        /// can be seen against the song rather than as a list of times.
        /// </summary>
        protected override bool OnHover(HoverEvent e)
        {
            highlight.Show(this, new[] { block }, assignedColour);
            updateBackground();
            return false;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            highlight.Hide(this);
            updateBackground();
        }

        /// <summary>
        /// Follows the playhead, so that the stretch being listened to says whose it is without being looked up.
        /// </summary>
        protected override void Update()
        {
            base.Update();

            bool nowPlaying = holds(editorClock.CurrentTime);

            if (nowPlaying == playing)
                return;

            playing = nowPlaying;
            playhead.FadeTo(nowPlaying ? 1 : 0, 150, Easing.OutQuint);
        }

        /// <summary>
        /// Whether a moment of the song falls in this stretch. Blocks meet end to end, so the closing moment belongs
        /// to the block that opens on it and no moment lights two rows.
        /// </summary>
        private bool holds(double time) => time >= block.Start && time < block.End;

        private void updateAssigned()
        {
            bool taken = project.Assignment.TryGetValue(block.Key, out string? name);

            assignedColour = taken ? CollabColours.For(project.Names.IndexOf(name!)) : colourProvider.Foreground1;

            assignedText.Text = taken ? name! : NOBODY;
            assignedText.Colour = assignedColour;
            playhead.Colour = assignedColour;

            if (IsHovered)
                highlight.Show(this, new[] { block }, assignedColour);

            updateBackground();
        }

        /// <summary>
        /// Brightens while the stretch is being pointed at, from this list or from the timeline along the bottom.
        /// </summary>
        private void updateBackground()
        {
            bool taken = project.Assignment.ContainsKey(block.Key);

            Lit = IsHovered || (pointedAt.Value is double time && holds(time));

            background.FadeColour(Lit ? colourProvider.Background1
                : taken ? colourProvider.Background2
                : colourProvider.Background3, 100, Easing.OutQuint);
        }

        /// <summary>
        /// A button that answers a click without a sound.
        /// </summary>
        /// <remarks>
        /// Moving the playhead is not a decision, and the list is scrubbed through a stretch at a time. Every one of
        /// them clicking turns reading the split into a rattle.
        /// </remarks>
        private partial class QuietButton : OsuAnimatedButton
        {
            protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverSounds(sampleSet);
        }

        /// <summary>
        /// The control the list is opened from, and what the list is measured against.
        /// </summary>
        /// <remarks>
        /// The list hangs off the button rather than off the row so that it opens where it was asked for. Anchored
        /// to a row spanning the whole panel, it came up half a panel away from the pointer, and every choice cost
        /// the trip out and back.
        /// </remarks>
        private partial class AssignButton : OsuAnimatedButton, IHasPopover
        {
            /// <summary>
            /// Opens a list, which is what the menu bar does, so it says so the way the menu bar says it. The names
            /// inside are left on the ordinary click, which is what a menu's own items use.
            /// </summary>
            protected override HoverSounds CreateHoverSounds(HoverSampleSet sampleSet) => new HoverClickSounds(HoverSampleSet.MenuOpen);

            private readonly CollabProject project;
            private readonly Action<string?> choose;

            public AssignButton(CollabProject project, Action<string?> choose)
            {
                this.project = project;
                this.choose = choose;

                Action = this.ShowPopover;
            }

            public Popover GetPopover() => new NamePopover(project, chosen =>
            {
                this.HidePopover();
                choose(chosen);
            });
        }

        /// <summary>
        /// The list of collaborators to hand a stretch to, with an entry for taking it back off everyone.
        /// </summary>
        private partial class NamePopover : OsuPopover
        {
            /// <summary>
            /// Opening and closing are silent here, so that the button opening the list and the name being picked in
            /// it are each one sound rather than two.
            /// </summary>
            /// <remarks>
            /// A lookup that finds nothing leaves the sample unset, which is how a popover with nothing to play is
            /// said. The two controls involved are buttons and speak for themselves.
            /// </remarks>
            protected override string PopInSampleName => string.Empty;

            protected override string PopOutSampleName => string.Empty;

            private readonly CollabProject project;
            private readonly Action<string?> choose;

            public NamePopover(CollabProject project, Action<string?> choose)
            {
                this.project = project;
                this.choose = choose;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                // straight under the control, or straight over it where the row is too near the bottom of the
                // screen. Never off to one side, which is the trip the button anchoring was meant to save.
                AllowableAnchors = new[] { Anchor.TopCentre, Anchor.BottomCentre };

                var items = new List<Drawable> { entry(NOBODY, null, -1) };

                for (int i = 0; i < project.Names.Count; i++)
                    items.Add(entry(project.Names[i], project.Names[i], i));

                // the list is drawn outside the panel, so what lands on it has to be stopped here instead.
                Child = new CollabInputBlocker
                {
                    Width = 160,
                    AutoSizeAxes = Axes.Y,
                    Child = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 2),
                        ChildrenEnumerable = items,
                    },
                };
            }

            private Drawable entry(string label, string? name, int index) => new OsuAnimatedButton
            {
                RelativeSizeAxes = Axes.X,
                Height = 30,
                Action = () => choose(name),
                Child = new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 10 },
                    Text = label,
                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                    Colour = CollabColours.For(index),
                },
            };
        }
    }
}
