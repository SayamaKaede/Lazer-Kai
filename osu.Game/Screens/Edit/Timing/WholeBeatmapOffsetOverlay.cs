// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Timing
{
    /// <summary>
    /// Asks how far to move the whole beatmap in time, and moves it: see <see cref="WholeBeatmapOffset"/>.
    /// </summary>
    /// <remarks>
    /// In the way of the editor while it is open. It is opened, a number is typed and it is closed, and what it does is
    /// to all of the beatmap at once: nothing is edited underneath in the meantime, and no key gets past it to try.
    /// </remarks>
    public partial class WholeBeatmapOffsetOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>, IKeyBindingHandler<PlatformAction>
    {
        private const float width = 420;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private SavedTimingLines savedTimingLines { get; set; } = null!;

        // not always present: the editor's components are built in test scenes without a whole editor around them.
        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private IEditorChangeHandler? changeHandler { get; set; }

        /// <summary>
        /// Raised with how far the beatmap went, when it is moved and when a move is undone or made again, for what is
        /// kept beside the beatmap at times of its own and has to go with it.
        /// </summary>
        public event Action<int>? Moved;

        /// <summary>
        /// The moves made, each by the two states of the undo history it lies between: going from one to the other is
        /// that move undone or made again.
        /// </summary>
        private readonly List<(string before, string after, int milliseconds)> moves = new List<(string, string, int)>();

        private string? lastState;

        private string? currentState => (changeHandler as EditorChangeHandler)?.CurrentStateHash;

        private FormTextBox amount = null!;
        private RoundedButton applyButton = null!;

        protected override bool StartHidden => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            Child = new InputBlockingContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f,
                    },
                    new Container
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Width = width,
                        AutoSizeAxes = Axes.Y,
                        Masking = true,
                        CornerRadius = 10,
                        EdgeEffect = new EdgeEffectParameters
                        {
                            Type = EdgeEffectType.Shadow,
                            Colour = Color4.Black.Opacity(0.4f),
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
                                Padding = new MarginPadding(16),
                                Spacing = new Vector2(10),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                                        Text = LazerKaiGreenLineStrings.WholeOffsetHeader,
                                    },
                                    new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Colour = colourProvider.Content2,
                                        Text = LazerKaiGreenLineStrings.WholeOffsetBody,
                                    },
                                    amount = new FormTextBox
                                    {
                                        Caption = LazerKaiGreenLineStrings.WholeOffsetAmount,
                                        HintText = LazerKaiGreenLineStrings.WholeOffsetAmountHint,
                                        SelectAllOnFocus = true,
                                        TabbableContentContainer = this,
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        ColumnDimensions = new[]
                                        {
                                            new Dimension(),
                                            new Dimension(GridSizeMode.Absolute, 10),
                                            new Dimension(),
                                        },
                                        RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                        Content = new[]
                                        {
                                            new[]
                                            {
                                                applyButton = new RoundedButton
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Text = LazerKaiGreenLineStrings.WholeOffsetApply,
                                                    Action = apply,
                                                },
                                                Empty(),
                                                new RoundedButton
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Text = LazerKaiGreenLineStrings.Cancel,
                                                    Action = Hide,
                                                },
                                            },
                                        },
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

            amount.Current.BindValueChanged(text => applyButton.Enabled.Value = tryRead(text.NewValue, out _), true);
            amount.OnCommit += (_, _) => apply();

            lastState = currentState;

            if (changeHandler != null)
                changeHandler.OnStateChange += stateChanged;
        }

        /// <summary>
        /// Says so when the state the editor has gone to is the other side of a move from the one it was in.
        /// </summary>
        private void stateChanged()
        {
            string? now = currentState;

            foreach (var move in moves)
            {
                if (lastState == move.after && now == move.before)
                    Moved?.Invoke(-move.milliseconds);
                else if (lastState == move.before && now == move.after)
                    Moved?.Invoke(move.milliseconds);
            }

            lastState = now;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (changeHandler != null)
                changeHandler.OnStateChange -= stateChanged;
        }

        /// <summary>
        /// The whole number of milliseconds typed, with or without a sign.
        /// </summary>
        /// <remarks>
        /// A box nothing was typed into yet holds no text at all.
        /// </remarks>
        private static bool tryRead(string? text, out int milliseconds) =>
            int.TryParse(text?.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out milliseconds);

        private void apply()
        {
            if (State.Value != Visibility.Visible || !tryRead(amount.Current.Value, out int milliseconds))
                return;

            // the lines are the file's to keep only for an osu! beatmap kept in stable's format, which is when the green
            // line panel edits them.
            bool linesAreKept = editor?.KeptStableVersion.Value != null && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0;

            string? before = currentState;

            var result = WholeBeatmapOffset.Move(editorBeatmap, savedTimingLines, milliseconds, linesAreKept);

            if (milliseconds != 0)
            {
                // remembered by the states either side of it, so that undoing it takes back what follows it as well.
                if (before != null && currentState is string after && after != before)
                    moves.Add((before, after, milliseconds));

                Moved?.Invoke(milliseconds);
            }

            notifications?.Post(new SimpleNotification
            {
                Text = LazerKaiGreenLineStrings.WholeOffsetMoved(milliseconds.ToString(@"+0;-0;0", CultureInfo.InvariantCulture), result.Objects, result.Lines),
            });

            Hide();
        }

        protected override void PopIn()
        {
            this.FadeIn(150, Easing.OutQuint);

            // every time it is opened, with nothing left over from the last.
            amount.Current.Value = @"0";
            ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(amount));
        }

        protected override void PopOut() => this.FadeOut(150, Easing.OutQuint);

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (State.Value != Visibility.Visible)
                return false;

            if (e.Action == GlobalAction.Back)
                Hide();

            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        // undo, redo and save among them: what the number box does not take is not the editor's to take either.
        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e) => State.Value == Visibility.Visible;

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (State.Value != Visibility.Visible)
                return false;

            if (!e.Repeat)
            {
                switch (e.Key)
                {
                    case Key.Escape:
                        Hide();
                        break;

                    // with the number box out of focus as well, where it is the box that takes it otherwise.
                    case Key.Enter:
                    case Key.KeypadEnter:
                        apply();
                        break;
                }
            }

            return true;
        }
    }
}
