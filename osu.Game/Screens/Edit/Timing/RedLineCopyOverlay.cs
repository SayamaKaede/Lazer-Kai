// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
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
    /// Asks which way red lines are to be copied between this difficulty and the others of its set, and copies them:
    /// see <see cref="RedLineCopying"/>.
    /// </summary>
    /// <remarks>
    /// In the way of the editor while it is open, as the window the whole beatmap is moved in time from is.
    /// </remarks>
    public partial class RedLineCopyOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>, IKeyBindingHandler<PlatformAction>
    {
        private const float width = 460;

        /// <summary>
        /// How tall the difficulties to copy to may be listed before the list scrolls instead.
        /// </summary>
        private const float max_targets_height = 150;

        /// <summary>
        /// How many milliseconds from a tick something is put on it, until it is said otherwise: a red line is most
        /// often moved by one or two, and what was on a tick is then up to one more than that from it.
        /// </summary>
        public const int DEFAULT_WITHIN = 3;

        public FormEnumDropdown<Direction> DirectionChoice { get; private set; } = null!;

        public FormDropdown<string> Source { get; private set; } = null!;

        public FillFlowContainer<OsuCheckbox> Targets { get; private set; } = null!;

        /// <summary>
        /// Whether everything is moved to where its beat is under the red lines copied: on until it is said otherwise,
        /// since red lines some way off leave nothing near a tick to be put on it.
        /// </summary>
        public FormCheckBox KeepBeats { get; private set; } = null!;

        public FormNumberBox Within { get; private set; } = null!;

        public RoundedButton ApplyButton { get; private set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private SavedTimingLines savedTimingLines { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> working { get; set; } = null!;

        [Resolved]
        private Editor editor { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        /// <summary>
        /// The other difficulties of the set, by what each is called in the lists.
        /// </summary>
        private readonly List<(string name, BeatmapInfo difficulty)> others = new List<(string, BeatmapInfo)>();

        private Container sourcePart = null!;
        private Container targetsPart = null!;
        private OsuScrollContainer targetsScroll = null!;

        protected override bool StartHidden => true;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
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
                                        Text = LazerKaiGreenLineStrings.CopyRedLinesHeader,
                                    },
                                    new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Colour = colourProvider.Content2,
                                        Text = LazerKaiGreenLineStrings.CopyRedLinesBody,
                                    },
                                    DirectionChoice = new FormEnumDropdown<Direction>
                                    {
                                        Caption = LazerKaiGreenLineStrings.CopyRedLinesDirection,
                                    },
                                    sourcePart = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = Source = new FormDropdown<string>
                                        {
                                            Caption = LazerKaiGreenLineStrings.CopyRedLinesSource,
                                        },
                                    },
                                    targetsPart = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Child = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(6),
                                            Children = new Drawable[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Text = LazerKaiGreenLineStrings.CopyRedLinesTargets,
                                                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                                                },
                                                targetsScroll = new OsuScrollContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Child = Targets = new FillFlowContainer<OsuCheckbox>
                                                    {
                                                        RelativeSizeAxes = Axes.X,
                                                        AutoSizeAxes = Axes.Y,
                                                        Direction = FillDirection.Vertical,
                                                        // clear of the scrollbar.
                                                        Padding = new MarginPadding { Right = 14 },
                                                    },
                                                },
                                                new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Colour = colours.Orange1,
                                                    Text = LazerKaiGreenLineStrings.CopyRedLinesToOthersWarning,
                                                },
                                            },
                                        },
                                    },
                                    KeepBeats = new FormCheckBox
                                    {
                                        Caption = LazerKaiGreenLineStrings.CopyRedLinesKeepBeats,
                                        HintText = LazerKaiGreenLineStrings.CopyRedLinesKeepBeatsHint,
                                    },
                                    Within = new FormNumberBox
                                    {
                                        Caption = LazerKaiGreenLineStrings.CopyRedLinesWithin,
                                        HintText = LazerKaiGreenLineStrings.CopyRedLinesWithinHint,
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
                                                ApplyButton = new RoundedButton
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Text = LazerKaiGreenLineStrings.CopyRedLinesApply,
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

            DirectionChoice.Current.BindValueChanged(_ => updateState());
            Within.Current.BindValueChanged(_ => updateState());
        }

        protected override void Update()
        {
            base.Update();

            // a scroll container will not take its height from what it holds, so it is given it here, up to a limit.
            targetsScroll.Height = Math.Min(Targets.DrawHeight, max_targets_height);
        }

        /// <summary>
        /// How many milliseconds were typed, if a number that can be was.
        /// </summary>
        private int? within => int.TryParse(Within.Current.Value?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int typed) ? typed : null;

        private IEnumerable<BeatmapInfo> tickedTargets => Targets.Where(t => t.Current.Value).Select(t => others[Targets.IndexOf(t)].difficulty);

        private void updateState()
        {
            bool fromAnother = DirectionChoice.Current.Value == Direction.FromAnother;

            sourcePart.Alpha = fromAnother ? 1 : 0;
            targetsPart.Alpha = fromAnother ? 0 : 1;

            ApplyButton.Enabled.Value = within != null && (fromAnother ? Source.Current.Value != null : tickedTargets.Any());
        }

        private void apply()
        {
            if (State.Value != Visibility.Visible || !ApplyButton.Enabled.Value || within is not int milliseconds)
                return;

            if (DirectionChoice.Current.Value == Direction.ToOthers)
            {
                var targets = tickedTargets.ToArray();

                Hide();

                // by the editor, which saves them and opens the set again.
                editor.CopyRedLinesToOthers(targets, milliseconds, KeepBeats.Current.Value);
                return;
            }

            var source = others.First(o => o.name == Source.Current.Value).difficulty;
            var redLines = RedLineCopying.Of(beatmaps.GetWorkingBeatmap(source).Beatmap.ControlPointInfo);

            if (redLines.Count == 0)
            {
                notifications?.Post(new SimpleErrorNotification { Text = LazerKaiGreenLineStrings.CopyRedLinesNone });
                return;
            }

            // the lines are the file's to keep only for an osu! beatmap kept in stable's format: see WholeBeatmapOffsetOverlay.
            bool linesAreKept = editor.KeptStableVersion.Value != null && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0;

            var result = RedLineCopying.CopyOnto(editorBeatmap, savedTimingLines, linesAreKept, redLines, milliseconds, KeepBeats.Current.Value);

            notifications?.Post(new SimpleNotification
            {
                Text = LazerKaiGreenLineStrings.CopyRedLinesDone(result.RedLines, result.Objects, result.Lines, result.OffTicks),
            });

            Hide();
        }

        protected override void PopIn()
        {
            this.FadeIn(150, Easing.OutQuint);

            // every time it is opened: the difficulties of the set may be others than they were.
            others.Clear();

            foreach (var difficulty in working.Value.BeatmapSetInfo.Beatmaps.Where(b => !b.Equals(editorBeatmap.BeatmapInfo)))
            {
                string name = $@"{difficulty.DifficultyName} ({difficulty.Ruleset.Name})";

                // told apart where two are called the same, as the difficulties of a set brought into the game may be.
                for (int copy = 2; others.Any(o => o.name == name); copy++)
                    name = $@"{difficulty.DifficultyName} ({difficulty.Ruleset.Name}) #{copy}";

                others.Add((name, difficulty));
            }

            Source.Items = others.Select(o => o.name).ToArray();

            Targets.ChildrenEnumerable = others.Select(o => new OsuCheckbox(nubOnRight: false)
            {
                LabelText = o.name,
                Current = { Value = true },
            });

            foreach (var target in Targets)
                target.Current.BindValueChanged(_ => updateState());

            Within.Current.Value = DEFAULT_WITHIN.ToString(CultureInfo.InvariantCulture);
            KeepBeats.Current.Value = true;

            updateState();
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

        /// <summary>
        /// Which way the red lines go.
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// From another difficulty of the set onto this one, where it is seen at once and undone like anything else.
            /// </summary>
            [LocalisableDescription(typeof(LazerKaiGreenLineStrings), nameof(LazerKaiGreenLineStrings.CopyRedLinesFromAnother))]
            FromAnother,

            /// <summary>
            /// From this difficulty onto others of the set, which are saved with them at once.
            /// </summary>
            [LocalisableDescription(typeof(LazerKaiGreenLineStrings), nameof(LazerKaiGreenLineStrings.CopyRedLinesToOthers))]
            ToOthers,
        }
    }
}
