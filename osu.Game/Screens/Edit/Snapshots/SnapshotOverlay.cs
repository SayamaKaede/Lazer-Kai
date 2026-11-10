// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Threading;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Collab;
using osuTK;

namespace osu.Game.Screens.Edit.Snapshots
{
    /// <summary>
    /// Lists what changed on a day, in this difficulty and in its set as a whole, with the times going there when
    /// clicked (2026-11-10 request).
    /// </summary>
    /// <remarks>
    /// Beside the beatmap rather than over it, as the discussion is: what is listed is gone to and looked at one after
    /// another.
    /// </remarks>
    public partial class SnapshotOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        private const float panel_width = 440;
        private const float header_height = 44;
        private const float bar_height = 52;
        private const float list_height = 420;
        private const float footer_height = 94;

        private const string day_format = @"yyyy-MM-dd";

        /// <summary>
        /// How many changes of one kind are listed before the rest are only counted: a difficulty mapped again from
        /// the start is thousands of them.
        /// </summary>
        public const int MAX_ROWS = 300;

        public OsuDropdown<string> Days { get; private set; } = null!;

        public IEnumerable<SnapshotRow> Rows => list.OfType<SnapshotRow>();

        /// <summary>
        /// Whether snapshots are kept for this set.
        /// </summary>
        public OsuCheckbox Recording { get; private set; } = null!;

        /// <summary>
        /// How many days back this set's snapshots are kept, typed; empty for all of them.
        /// </summary>
        public OsuNumberBox KeepDaysBox { get; private set; } = null!;

        /// <summary>
        /// The same, dragged; all the way down for all of them.
        /// </summary>
        public RoundedSliderBar<int> KeepDaysSlider { get; private set; } = null!;

        private readonly BindableInt keepDays = new BindableInt { MinValue = 0, MaxValue = BeatmapSnapshotStore.MAX_KEEP_DAYS };

        /// <summary>
        /// Whether what the controls hold is being read from the store, and so is not a choice to write back to it.
        /// </summary>
        private bool reading;

        private ScheduledDelegate? keepDaysWrite;

        // not always present: the editor's components are built in test scenes without a whole editor around them.
        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private BeatmapSnapshotStore store { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private Container panel = null!;
        private FillFlowContainer list = null!;
        private OsuTextFlowContainer status = null!;
        private Container dayBar = null!;

        protected override bool StartHidden => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;

            // the same blocker as the collab panel's: a click meant for the panel does not reach the beatmap behind.
            InternalChild = panel = new CollabInputBlocker
            {
                Anchor = Anchor.CentreRight,
                Origin = Anchor.CentreRight,
                X = -20,
                Width = panel_width,
                Height = header_height + bar_height + list_height + footer_height,
                Masking = true,
                CornerRadius = 10,
                EdgeEffect = new EdgeEffectParameters
                {
                    Type = EdgeEffectType.Shadow,
                    Colour = Color4Extensions.FromHex("000").Opacity(0.4f),
                    Radius = 10,
                },
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Y = header_height + bar_height,
                        Height = list_height,
                        Children = new Drawable[]
                        {
                            status = new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding { Horizontal = 30 },
                                TextAnchor = Anchor.TopCentre,
                                Colour = colourProvider.Foreground1,
                            },
                            new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = list = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    // the right one clear of the scrollbar.
                                    Padding = new MarginPadding { Left = 14, Right = 18, Vertical = 10 },
                                    Spacing = new Vector2(0, 4),
                                }
                            },
                        }
                    },
                    // what is chosen for this set: whether it is recorded at all, and for how long.
                    new Container
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = footer_height,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background3,
                            },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding { Horizontal = 14, Top = 12 },
                                Spacing = new Vector2(0, 10),
                                Children = new Drawable[]
                                {
                                    Recording = new OsuCheckbox(nubOnRight: false)
                                    {
                                        LabelText = LazerKaiSnapshotStrings.Record,
                                    },
                                    new GridContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 30,
                                        ColumnDimensions = new[]
                                        {
                                            new Dimension(GridSizeMode.AutoSize),
                                            new Dimension(GridSizeMode.Absolute, 10),
                                            new Dimension(GridSizeMode.Absolute, 60),
                                            new Dimension(GridSizeMode.Absolute, 12),
                                            new Dimension(),
                                        },
                                        Content = new[]
                                        {
                                            new[]
                                            {
                                                new OsuSpriteText
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    Text = LazerKaiSnapshotStrings.KeepDays,
                                                    Font = OsuFont.Default.With(size: 13),
                                                },
                                                Empty(),
                                                KeepDaysBox = new OsuNumberBox
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    Height = 30,
                                                    LengthLimit = 3,
                                                },
                                                Empty(),
                                                KeepDaysSlider = new RoundedSliderBar<int>
                                                {
                                                    Anchor = Anchor.CentreLeft,
                                                    Origin = Anchor.CentreLeft,
                                                    RelativeSizeAxes = Axes.X,
                                                    Current = keepDays,
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                    // after the list, so that the days opening downwards are drawn over it.
                    dayBar = new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Y = header_height,
                        Height = bar_height,
                        Padding = new MarginPadding { Horizontal = 14, Top = 8 },
                        Child = Days = new OsuDropdown<string>
                        {
                            RelativeSizeAxes = Axes.X,
                        },
                    },
                    new HeaderBar(() => panel)
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = header_height,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background3,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Margin = new MarginPadding { Left = 14 },
                                Text = LazerKaiSnapshotStrings.Header,
                                Font = OsuFont.Default.With(size: 16, weight: FontWeight.SemiBold),
                            },
                            new IconButton
                            {
                                Anchor = Anchor.CentreRight,
                                Origin = Anchor.CentreRight,
                                Margin = new MarginPadding { Right = 6 },
                                Icon = FontAwesome.Solid.Times,
                                Scale = new Vector2(0.7f),
                                Action = Hide,
                            },
                        }
                    },
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Days.Current.BindValueChanged(_ => showDay());

            Recording.Current.BindValueChanged(on =>
            {
                if (reading)
                    return;

                store.SetEnabled(editorBeatmap.BeatmapInfo, on.NewValue);

                // from now rather than from the next save: what is there now is what the first change is told from.
                if (on.NewValue)
                    editor?.RecordSnapshotAsOpened();

                Refresh();
            });

            keepDays.BindValueChanged(days =>
            {
                string text = days.NewValue == 0 ? string.Empty : days.NewValue.ToString(CultureInfo.InvariantCulture);

                if (KeepDaysBox.Current.Value != text)
                    KeepDaysBox.Current.Value = text;

                if (reading)
                    return;

                // once the slider has come to rest, rather than at every day it is dragged across.
                keepDaysWrite?.Cancel();
                keepDaysWrite = Scheduler.AddDelayed(() => store.SetKeepDays(editorBeatmap.BeatmapInfo, keepDays.Value), 300);
            });

            KeepDaysBox.Current.BindValueChanged(text =>
            {
                if (string.IsNullOrWhiteSpace(text.NewValue))
                    keepDays.Value = 0;
                else if (int.TryParse(text.NewValue, NumberStyles.None, CultureInfo.InvariantCulture, out int typed))
                    keepDays.Value = Math.Clamp(typed, 0, BeatmapSnapshotStore.MAX_KEEP_DAYS);
            });
        }

        /// <summary>
        /// Reads the days again, as after a save, which is what adds to them. The day being looked at stays the one
        /// looked at.
        /// </summary>
        public void Refresh()
        {
            if (State.Value != Visibility.Visible)
                return;

            var settings = store.SettingsOf(editorBeatmap.BeatmapInfo);

            reading = true;
            Recording.Current.Value = settings.Enabled == true;
            keepDays.Value = settings.KeepDays;
            reading = false;

            string? looking = Days.Current.Value;
            string[] days = store.DaysOf(editorBeatmap.BeatmapInfo).Select(d => d.ToString(day_format, CultureInfo.InvariantCulture)).ToArray();

            Days.Items = days;
            dayBar.Alpha = days.Length > 0 ? 1 : 0;

            if (days.Length > 0)
                Days.Current.Value = looking != null && days.Contains(looking) ? looking : days[0];

            // the day is the same one as before where only what it holds has changed, and nothing was told of it.
            showDay();
        }

        private void showDay()
        {
            list.Clear();
            status.Text = string.Empty;

            if (!Days.Items.Any() || Days.Current.Value == null
                                  || !DateTime.TryParseExact(Days.Current.Value, day_format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
            {
                status.Text = Recording.Current.Value ? LazerKaiSnapshotStrings.NoSnapshots : LazerKaiSnapshotStrings.NotRecording;
                return;
            }

            var beatmap = editorBeatmap.BeatmapInfo;

            list.Add(createHeading(LocalisableString.Format(@"{0} [{1}]", LazerKaiSnapshotStrings.ThisDifficulty, beatmap.DifficultyName)));

            if (store.DifficultyOn(beatmap, day) is not (var before, var after))
                list.Add(createNote(LazerKaiSnapshotStrings.NotSaved));
            else if (before == null)
                list.Add(createNote(LazerKaiSnapshotStrings.FirstRecord));
            else
                addRows(BeatmapSnapshotDiff.Compare(before, after));

            list.Add(createHeading(LazerKaiSnapshotStrings.WholeSet));

            if (store.SetOn(beatmap, day) is not (var filesBefore, var filesAfter))
                list.Add(createNote(LazerKaiSnapshotStrings.NotSaved));
            else if (filesBefore == null)
                list.Add(createNote(LazerKaiSnapshotStrings.FirstRecord));
            else
                addRows(BeatmapSnapshotDiff.CompareFiles(filesBefore, filesAfter));
        }

        private void addRows(IReadOnlyList<SnapshotChange> changes)
        {
            if (changes.Count == 0)
            {
                list.Add(createNote(LazerKaiSnapshotStrings.NoChanges));
                return;
            }

            foreach (var change in changes.Take(MAX_ROWS))
                list.Add(new SnapshotRow(change, time => editorClock.SeekSmoothlyTo(time)));

            if (changes.Count > MAX_ROWS)
                list.Add(createNote(LazerKaiSnapshotStrings.More(changes.Count - MAX_ROWS)));
        }

        private Drawable createHeading(LocalisableString text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
            Margin = new MarginPadding { Top = 6, Bottom = 2 },
        };

        private Drawable createNote(LocalisableString text) => new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 13))
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Colour = colourProvider.Content2,
            Text = text,
        };

        /// <summary>
        /// What a change is listed as.
        /// </summary>
        public static LocalisableString Describe(SnapshotChange change)
        {
            switch (change.Subject)
            {
                case SnapshotSubject.Setting:
                    switch (change.Kind)
                    {
                        case SnapshotChangeKind.Added:
                            return LazerKaiSnapshotStrings.Added($@"{change.Name}: {change.After}");

                        case SnapshotChangeKind.Removed:
                            return LazerKaiSnapshotStrings.Removed($@"{change.Name}: {change.Before}");

                        // the row's colour says it was changed, and the two values say the rest.
                        default:
                            return $@"{change.Name}: {change.Before} → {change.After}";
                    }

                case SnapshotSubject.Event:
                    return withKind(change.Kind, LazerKaiSnapshotStrings.Event(change.Name ?? string.Empty));

                case SnapshotSubject.File:
                    return withKind(change.Kind, LazerKaiSnapshotStrings.File(change.Name ?? string.Empty));

                default:
                    if (change.Kind == SnapshotChangeKind.Changed && change.Aspects != SnapshotAspects.None)
                        return LazerKaiSnapshotStrings.ChangedIn(nameOf(change.Subject), namesOf(change.Aspects));

                    // where it is goes on the same line as what it is, as it is said: a circle at (256, 192).
                    if (position(change) is SnapshotDetail at)
                        return withKind(change.Kind, LocalisableString.Format(@"{0} {1}", nameOf(change.Subject), at.After ?? at.Before ?? string.Empty));

                    return withKind(change.Kind, nameOf(change.Subject));
            }
        }

        /// <summary>
        /// Where an object added or removed is, which is listed on its own line rather than under it.
        /// </summary>
        private static SnapshotDetail? position(SnapshotChange change) =>
            change.Kind == SnapshotChangeKind.Changed ? null : change.Details.FirstOrDefault(d => d.Aspect == SnapshotAspects.Position);

        /// <summary>
        /// The values listed under a change, one to a line: what each aspect was and is.
        /// </summary>
        public static IEnumerable<LocalisableString> DescribeDetails(SnapshotChange change)
        {
            var onItsLine = position(change);

            foreach (var detail in change.Details)
            {
                if (detail == onItsLine)
                    continue;

                var name = aspect_names.First(a => a.aspect == detail.Aspect).name();

                yield return detail.Before != null && detail.After != null
                    ? LocalisableString.Format(@"{0}: {1} → {2}", name, detail.Before, detail.After)
                    : LocalisableString.Format(@"{0}: {1}", name, detail.After ?? detail.Before ?? string.Empty);
            }
        }

        private static LocalisableString withKind(SnapshotChangeKind kind, LocalisableString what)
        {
            switch (kind)
            {
                case SnapshotChangeKind.Added:
                    return LazerKaiSnapshotStrings.Added(what);

                case SnapshotChangeKind.Removed:
                    return LazerKaiSnapshotStrings.Removed(what);

                default:
                    return LazerKaiSnapshotStrings.Changed(what);
            }
        }

        private static LocalisableString nameOf(SnapshotSubject subject)
        {
            switch (subject)
            {
                case SnapshotSubject.Slider:
                    return LazerKaiSnapshotStrings.Slider;

                case SnapshotSubject.Spinner:
                    return LazerKaiSnapshotStrings.Spinner;

                case SnapshotSubject.HoldNote:
                    return LazerKaiSnapshotStrings.HoldNote;

                case SnapshotSubject.RedLine:
                    return LazerKaiSnapshotStrings.RedLine;

                case SnapshotSubject.GreenLine:
                    return LazerKaiSnapshotStrings.GreenLine;

                case SnapshotSubject.Bookmark:
                    return LazerKaiSnapshotStrings.Bookmark;

                case SnapshotSubject.Break:
                    return LazerKaiSnapshotStrings.Break;

                default:
                    return LazerKaiSnapshotStrings.Circle;
            }
        }

        private static readonly (SnapshotAspects aspect, Func<LocalisableString> name)[] aspect_names =
        {
            (SnapshotAspects.Position, () => LazerKaiSnapshotStrings.Position),
            (SnapshotAspects.Shape, () => LazerKaiSnapshotStrings.Shape),
            (SnapshotAspects.Repeats, () => LazerKaiSnapshotStrings.Repeats),
            (SnapshotAspects.Length, () => LazerKaiSnapshotStrings.Length),
            (SnapshotAspects.NewCombo, () => LazerKaiSnapshotStrings.NewCombo),
            (SnapshotAspects.Bpm, () => LazerKaiSnapshotStrings.Bpm),
            (SnapshotAspects.Meter, () => LazerKaiSnapshotStrings.Meter),
            (SnapshotAspects.SliderVelocity, () => LazerKaiSnapshotStrings.SliderVelocity),
            (SnapshotAspects.Hitsound, () => LazerKaiSnapshotStrings.Hitsound),
            (SnapshotAspects.Volume, () => LazerKaiSnapshotStrings.Volume),
            (SnapshotAspects.Kiai, () => LazerKaiSnapshotStrings.Kiai),
        };

        private static LocalisableString namesOf(SnapshotAspects aspects)
        {
            LocalisableString? joined = null;

            foreach (var (aspect, name) in aspect_names)
            {
                if (!aspects.HasFlag(aspect))
                    continue;

                joined = joined == null ? name() : LocalisableString.Format(@"{0}{1}{2}", joined.Value, LazerKaiSnapshotStrings.Separator, name());
            }

            return joined ?? string.Empty;
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);

            // every time it is opened: a save since may have added a day, or to the day being looked at.
            Refresh();
        }

        protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

        /// <summary>
        /// Escape closes the panel rather than leaving the editor.
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
        /// The bar along the top: dragging it moves the panel.
        /// </summary>
        private partial class HeaderBar : Container
        {
            private readonly Func<Container> panel;

            public HeaderBar(Func<Container> panel)
            {
                this.panel = panel;
            }

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override void OnDrag(DragEvent e) => panel().Position += e.Delta;
        }
    }

    /// <summary>
    /// One change: a mark in the colour of what was done, the time where there is one, and what it was.
    /// </summary>
    public partial class SnapshotRow : CompositeDrawable
    {
        private const float time_width = 82;

        public readonly SnapshotChange Change;

        /// <summary>
        /// The time, which goes there when clicked; null for a change that is at no time.
        /// </summary>
        public Drawable? Timestamp { get; private set; }

        private readonly Action<double> goTo;

        public SnapshotRow(SnapshotChange change, Action<double> goTo)
        {
            Change = change;
            this.goTo = goTo;

            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            AddInternal(new Circle
            {
                RelativeSizeAxes = Axes.Y,
                Width = 3,
                Colour = Change.Kind == SnapshotChangeKind.Added ? colours.Lime1
                    : Change.Kind == SnapshotChangeKind.Removed ? colours.Red1
                    : colours.Yellow,
            });

            if (Change.Time is double time)
            {
                AddInternal(Timestamp = new TimestampButton(colourProvider.Light2)
                {
                    X = 10,
                    Action = () => goTo(time),
                    Child = new OsuSpriteText
                    {
                        Text = time.ToEditorFormattedString(),
                        Font = OsuFont.Default.With(size: 13, fixedWidth: true),
                    },
                });
            }

            FillFlowContainer lines;

            AddInternal(lines = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding { Left = 10 + (Change.Time == null ? 0 : time_width) },
                Child = new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 13))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = SnapshotOverlay.Describe(Change),
                },
            });

            // the values under what they are the values of, quieter than it.
            foreach (var detail in SnapshotOverlay.DescribeDetails(Change))
            {
                lines.Add(new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 12))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Colour = colourProvider.Content2,
                    Text = detail,
                });
            }
        }

        private partial class TimestampButton : OsuClickableContainer
        {
            private readonly ColourInfo idle;

            public TimestampButton(ColourInfo idle)
            {
                this.idle = idle;

                AutoSizeAxes = Axes.Both;
                Colour = idle;
            }

            protected override bool OnHover(HoverEvent e)
            {
                this.FadeColour(Colour4.White, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                this.FadeColour(idle, 100);
                base.OnHoverLost(e);
            }
        }
    }
}
