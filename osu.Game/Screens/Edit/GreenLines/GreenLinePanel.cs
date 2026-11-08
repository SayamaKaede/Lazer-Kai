// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.Timing;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.GreenLines
{
    /// <summary>
    /// Stable's timing panel for the green lines: every line in a list to jump to, and what the line at the playhead
    /// says, to change.
    /// </summary>
    /// <remarks>
    /// As in stable, a change goes to the selected line: picked where the panel is opened (see
    /// <see cref="GreenLineEditing.PickAt"/>), clicked in the list, or just put in. Moving the playhead does not move the
    /// selection, so the line is marked on the timeline too, where one far from the playhead can be seen to be the one
    /// changing. A red line takes changes to its sounds; a velocity at one goes on a green line beside it.
    ///
    /// Changes show everywhere as they are made, but stay pending until confirmed, which makes them one step to undo.
    /// Cancelling, closing the panel, or pressing its key again puts them back.
    ///
    /// A window rather than a popover, like the stream panel. It opens under the timeline, where the lines it lists can
    /// be seen as they change, and is dragged by its title.
    /// </remarks>
    public partial class GreenLinePanel : VisibilityContainer, IKeyBindingHandler<GlobalAction>, IKeyBindingHandler<PlatformAction>
    {
        private const float list_width = 330;
        private const float controls_width = 320;
        private const float list_height = 380;
        private const float row_height = 22;

        public Bindable<double> SliderVelocity => velocityControl.Current;

        public readonly Bindable<GreenLineSampleSet> SampleSet = new Bindable<GreenLineSampleSet>();

        public readonly BindableInt SampleIndex = new BindableInt { MinValue = 0, MaxValue = 99 };

        public readonly BindableInt Volume = new BindableInt(100) { MinValue = 5, MaxValue = 100 };

        /// <summary>
        /// Whether the selected line turns kiai on, as each of stable's lines says.
        /// </summary>
        public readonly BindableBool Kiai = new BindableBool();

        /// <summary>
        /// The line being edited, whose values are shown: the last one clicked among <see cref="SelectedLines"/>.
        /// </summary>
        public SavedTimingLine? Selected { get; private set; }

        /// <summary>
        /// Every line a change goes to, picked in the list the way files are in stable's timing panel: ctrl+click adds
        /// or takes out one, shift+click takes every line between, and select all takes what the list shows.
        /// </summary>
        /// <remarks>
        /// A change goes to each of them as that one thing changed, so the rest of what each line says stays its own.
        /// </remarks>
        public IReadOnlyList<SavedTimingLine> SelectedLines => selection;

        private readonly List<SavedTimingLine> selection = new List<SavedTimingLine>();

        /// <summary>
        /// Where a shift+click takes the lines from: the line last clicked on its own or with ctrl.
        /// </summary>
        private SavedTimingLine? rangeStart;

        /// <summary>
        /// Which lines the list shows, as stable's timing panel's tabs do.
        /// </summary>
        public readonly Bindable<GreenLineListFilter> Filter = new Bindable<GreenLineListFilter>();

        /// <summary>
        /// Whether lines can be edited here, which needs a beatmap kept in stable's format.
        /// </summary>
        public bool Editable => editor?.KeptStableVersion.Value != null && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0;

        /// <summary>
        /// Whether there are changes waiting to be confirmed or cancelled.
        /// </summary>
        public bool Pending => editing.Pending;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private SavedTimingLines savedTimingLines { get; set; } = null!;

        [Resolved]
        private Editor? editor { get; set; }

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private GreenLineEditing editing = null!;

        private Container body = null!;
        private Box header = null!;
        private OsuTextFlowContainer notEditableText = null!;
        private GridContainer content = null!;
        private OsuScrollContainer listScroll = null!;
        private FillFlowContainer<LineRow> list = null!;
        private OsuSpriteText lineText = null!;
        private SliderVelocityAdjustmentControl velocityControl = null!;
        private Container velocityContainer = null!;
        private FillFlowContainer<SampleSetButton> sampleSetButtons = null!;
        private FormNumberBox sampleIndexBox = null!;
        private FillFlowContainer usedIndexButtons = null!;
        private RoundedButton addButton = null!;
        private RoundedButton removeButton = null!;
        private RoundedButton useCurrentTimeButton = null!;
        private RoundedButton pasteButton = null!;
        private RoundedButton cancelButton = null!;
        private RoundedButton confirmButton = null!;

        private Sample? openSample;

        /// <summary>
        /// Set while the controls are being made to show a line, so that doing so is not taken as an edit to it.
        /// </summary>
        private bool showing;

        /// <summary>
        /// Whether the list needs building again, which it does whenever the lines change.
        /// </summary>
        private bool listOutdated = true;


        /// <summary>
        /// Whether the window has been put anywhere yet: it opens under the timeline the first time, and wherever it
        /// was left after that.
        /// </summary>
        private bool placed;

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            editing = new GreenLineEditing(editorBeatmap, savedTimingLines);

            RelativeSizeAxes = Axes.Both;

            openSample = audio.Samples.Get(@"UI/overlay-pop-in");

            InternalChild = body = new Container
            {
                AutoSizeAxes = Axes.Both,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
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
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Children = new Drawable[]
                        {
                            new Container
                            {
                                Width = list_width + controls_width + 30,
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
                                        Text = LazerKaiGreenLineStrings.Title,
                                    },
                                    new IconButton
                                    {
                                        Anchor = Anchor.CentreRight,
                                        Origin = Anchor.CentreRight,
                                        Margin = new MarginPadding { Right = 4 },
                                        Scale = new Vector2(0.6f),
                                        Icon = FontAwesome.Solid.Times,
                                        Action = Hide,
                                    },
                                },
                            },
                            notEditableText = new OsuTextFlowContainer(s => s.Font = OsuFont.Default.With(size: 13))
                            {
                                Width = list_width + controls_width + 30,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding(10),
                                Text = LazerKaiGreenLineStrings.NotEditable,
                            },
                            content = new GridContainer
                            {
                                Width = list_width + controls_width + 30,
                                AutoSizeAxes = Axes.Y,
                                Padding = new MarginPadding(10),
                                ColumnDimensions = new[]
                                {
                                    new Dimension(GridSizeMode.Absolute, list_width),
                                    new Dimension(GridSizeMode.Absolute, 10),
                                    new Dimension(),
                                },
                                RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                                Content = new[]
                                {
                                    new Drawable?[]
                                    {
                                        createList(),
                                        null,
                                        createControls(),
                                    }
                                }
                            },
                        },
                    },
                },
            };

            SliderVelocity.BindValueChanged(v => change(l => l.WithSliderVelocity(v.NewValue), forVelocity: true));
            SampleSet.BindValueChanged(v => change(l => l.WithSampleSet((int)v.NewValue)));
            SampleIndex.BindValueChanged(v => change(l => l.WithSampleIndex(v.NewValue)));
            Volume.BindValueChanged(v => change(l => l.WithVolume(v.NewValue)));
            Kiai.BindValueChanged(v => change(l => l.WithKiai(v.NewValue)));

            savedTimingLines.Lines.BindCollectionChanged((_, _) => listOutdated = true);
            savedTimingLines.Shown.BindCollectionChanged((_, _) => listOutdated = true);
            Filter.BindValueChanged(_ => listOutdated = true);
        }

        private Drawable createList() => new FillFlowContainer
        {
            Width = list_width,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(4),
            Children = new Drawable[]
            {
                new OsuTabControl<GreenLineListFilter>
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 24,
                    Current = Filter,
                },
                createRows(),
                // under the list, whose selection it copies.
                pasteButton = new RoundedButton
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 30,
                    Text = LazerKaiGreenLineStrings.PasteAtCurrentTime,
                    Action = PasteAtCurrentTime,
                },
            }
        };

        private Drawable createRows() => new Container
        {
            RelativeSizeAxes = Axes.X,
            Height = list_height,
            Masking = true,
            CornerRadius = 5,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                new LineRow.Heading(),
                // padded from outside rather than within: a scroll container masks its own bounds, padding and all, so
                // one padded itself lets its rows scroll up over the heading.
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = row_height },
                    Child = listScroll = new OsuScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Child = list = new FillFlowContainer<LineRow>
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                        },
                    },
                },
            }
        };

        private Drawable createControls() => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(5),
            Children = new Drawable[]
            {
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[] { new Dimension(), new Dimension(GridSizeMode.AutoSize) },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            lineText = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                            },
                            useCurrentTimeButton = new RoundedButton
                            {
                                Width = 140,
                                Height = 26,
                                Text = LazerKaiGreenLineStrings.UseCurrentTime,
                                Action = UseCurrentTime,
                            },
                        }
                    }
                },
                // the same control as the velocity toolbox's, presets and all, so that a velocity is set the same way
                // wherever it is set. The space stays when a red line hides it, so the rest does not jump about.
                velocityContainer = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Child = velocityControl = new SliderVelocityAdjustmentControl(),
                },
                // on this line alone, from it to the next line, as in stable.
                new FormCheckBox
                {
                    Caption = @"Kiai",
                    Current = Kiai,
                },
                new FormFieldCaption { Caption = LazerKaiGreenLineStrings.SampleSet },
                sampleSetButtons = new FillFlowContainer<SampleSetButton>
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(4),
                    ChildrenEnumerable = Enum.GetValues<GreenLineSampleSet>().Select(set => new SampleSetButton(set)
                    {
                        Width = (controls_width - 12) / 4,
                        Action = () => SampleSet.Value = set,
                    }),
                },
                sampleIndexBox = new FormNumberBox
                {
                    Caption = LazerKaiGreenLineStrings.CustomIndex,
                },
                usedIndexButtons = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Full,
                    Spacing = new Vector2(4),
                },
                // set the way the velocity is, stepping out from the middle of the bar.
                new FormDiscreteAdjustmentControl<int>(1, new[] { 1, 5, 10, 25 })
                {
                    Caption = LazerKaiGreenLineStrings.Volume,
                    Current = Volume,
                    LabelFormat = v => FormattableString.Invariant($@"{v}%"),
                    TabbableContentContainer = this,
                },
                new GridContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    ColumnDimensions = new[] { new Dimension(), new Dimension(), new Dimension(), new Dimension() },
                    RowDimensions = new[] { new Dimension(GridSizeMode.AutoSize) },
                    Content = new[]
                    {
                        new Drawable[]
                        {
                            addButton = button(LazerKaiGreenLineStrings.Add, AddLine),
                            removeButton = button(LazerKaiGreenLineStrings.Remove, RemoveLine),
                            cancelButton = button(LazerKaiGreenLineStrings.Cancel, Cancel),
                            confirmButton = button(LazerKaiGreenLineStrings.Confirm, Confirm),
                        }
                    }
                },
            }
        };

        private static RoundedButton button(LocalisableString text, Action action) => new RoundedButton
        {
            RelativeSizeAxes = Axes.X,
            Height = 30,
            Padding = new MarginPadding { Horizontal = 2 },
            Text = text,
            Action = action,
        };

        private InputManager inputManager = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager()!;

            sampleIndexBox.OnCommit += (_, _) =>
            {
                if (int.TryParse(sampleIndexBox.Current.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                    SampleIndex.Value = Math.Clamp(index, SampleIndex.MinValue, SampleIndex.MaxValue);

                // whatever was typed, the box goes back to saying what the line does.
                sampleIndexBox.Current.Value = SampleIndex.Value.ToString(CultureInfo.InvariantCulture);
            };
        }

        protected override void Update()
        {
            base.Update();

            if (State.Value != Visibility.Visible)
                return;

            bool editable = Editable;

            notEditableText.Alpha = editable ? 0 : 1;
            content.Alpha = editable ? 1 : 0;

            if (!editable)
                return;

            bool refresh = false;

            // not while something outside the panel is being dragged. A slider with a velocity of its own dragged along
            // the timeline moves the line a save will put in for it on every frame, and the list was built again on each
            // of them, every object's sounds counted and the list scrolled back to the selection as it went. Its own
            // controls are left alone, so what they change shows as it changes (2026-11-09).
            bool draggedElsewhere = inputManager.DraggedDrawable is Drawable dragged && !dragged.IsRootedAt(this);

            if (listOutdated && !draggedElsewhere)
            {
                rebuildList();
                keepSelectionToWhatIsListed();
                listOutdated = false;
                refresh = true;
            }

            // a line that is gone — cancelled, or undone — leaves the panel on the one it would pick where it is. One a
            // save will put in is still there while it is shown.
            if (Selected is not SavedTimingLine selected || !isListed(selected) || !(savedTimingLines.Lines.Contains(selected) || savedTimingLines.Shown.Contains(selected)))
                select(pickListedAt(editorClock.CurrentTime));
            else if (refresh)
                show();

            cancelButton.Enabled.Value = confirmButton.Enabled.Value = editing.Pending;

            // before the first red line there is nothing for a new line to carry on from. Pressing it there did nothing
            // and said nothing (2026-11-09).
            bool canAdd = editing.InEffectAt(timeToAddAt()) != null;
            addButton.Enabled.Value = canAdd;
            addButton.TooltipText = canAdd ? default : LazerKaiGreenLineStrings.NothingToCarryOnFrom;
        }

        // stable's lines stand on whole milliseconds, and one just before the object is still read by it.
        private double timeToAddAt() => Math.Floor(editorBeatmap.SnapTime(editorClock.CurrentTime, null));

        /// <summary>
        /// Puts in a green line at the playhead, on the beat divisor, saying what the line before it does, and selects
        /// it. Where a green line stands there already, that one is selected.
        /// </summary>
        public void AddLine()
        {
            if (!Editable)
                return;

            var line = editing.Add(timeToAddAt());

            if (line != null)
                select(line);
        }

        /// <summary>
        /// Takes out the selected lines that are green ones, and selects the one in effect in their place.
        /// </summary>
        public void RemoveLine()
        {
            if (!Editable || Selected is not SavedTimingLine line)
                return;

            if (selection.Count > 1)
            {
                if (selection.All(l => l.Uninherited))
                    return;

                takeShownIfAny();
                editing.RemoveAll(selection);
                select(editing.InEffectAt(line.Time));
                return;
            }

            if (line.Uninherited || !takeIfShown(line))
                return;

            editing.Remove(line);
            select(editing.InEffectAt(line.Time));
        }

        /// <summary>
        /// Stable's "use current time": moves the selected green line to the playhead, on the beat divisor, and the
        /// objects follow what is in effect where they are afterwards. Where a green line stands there already, nothing
        /// is moved, rather than leave two on one time.
        /// </summary>
        /// <remarks>
        /// A red line goes to the playhead's millisecond rather than onto the beat divisor, since the beats are counted
        /// from it, and the objects stay where they are, as in stable. Where a red line stands already, nothing is moved.
        /// </remarks>
        public void UseCurrentTime()
        {
            // one line at a time: several lines have no one time to go to.
            if (!Editable || selection.Count > 1 || Selected is not SavedTimingLine line || !takeIfShown(line))
                return;

            if (line.Uninherited)
            {
                var movedRed = editing.MoveRedLine(line, Math.Floor(editorClock.CurrentTime));

                if (movedRed != null)
                    select(movedRed);

                return;
            }

            double time = Math.Floor(editorBeatmap.SnapTime(editorClock.CurrentTime, null));

            if (time == line.Time || savedTimingLines.Lines.Any(l => l.Time == time && !l.Uninherited))
                return;

            var moved = line.WithTime(time);

            editing.Replace(line, moved);
            select(moved);
        }

        /// <summary>
        /// Copies the selected lines to the playhead, on the beat divisor, as far apart as they are, and selects the copies,
        /// so that the next time the part comes round they can be copied there in turn (see <see cref="GreenLineEditing.Paste"/>).
        /// </summary>
        public void PasteAtCurrentTime()
        {
            if (!Editable || selection.Count == 0)
                return;

            takeShownIfAny();

            // stable's lines stand on whole milliseconds, and one just before the object is still read by it.
            var copies = editing.Paste(selection, Math.Floor(editorBeatmap.SnapTime(editorClock.CurrentTime, null)));

            if (copies.Count == 0)
                return;

            selection.Clear();
            selection.AddRange(copies);
            Selected = rangeStart = copies[0];

            savedTimingLines.Selected.Value = Selected;
            show();
        }

        public void Confirm()
        {
            editing.Confirm();
            show();
        }

        public void Cancel() => editing.Cancel();

        /// <summary>
        /// Changes the selected line. A velocity cannot go on a red line, so for one of those a green line is put in
        /// beside it, the way stable pairs them, and that is changed and selected instead.
        /// </summary>
        private void change(Func<SavedTimingLine, SavedTimingLine> changeLine, bool forVelocity = false)
        {
            if (showing || !Editable || Selected is not SavedTimingLine line)
                return;

            if (selection.Count > 1)
            {
                changeSelected(changeLine, forVelocity);
                return;
            }

            if (!takeIfShown(line))
                return;

            if (forVelocity && line.Uninherited)
            {
                var beside = editing.Add(line.Time);

                if (beside == null)
                    return;

                line = beside.Value;
            }

            var changed = changeLine(line);

            editing.Replace(line, changed);
            select(changed);
        }

        /// <summary>
        /// Changes every selected line, each by the one thing changed. A velocity goes on the green ones alone: with
        /// several lines there is no one red line to put a green line in beside.
        /// </summary>
        private void changeSelected(Func<SavedTimingLine, SavedTimingLine> changeLine, bool forVelocity)
        {
            takeShownIfAny();

            var replacements = selection.Where(l => savedTimingLines.Lines.Contains(l) && !(forVelocity && l.Uninherited))
                                        .Select(l => (line: l, changed: changeLine(l)))
                                        .ToList();

            editing.ReplaceAll(replacements);

            var changedTo = new Dictionary<SavedTimingLine, SavedTimingLine>();

            foreach (var (line, changed) in replacements)
                changedTo.TryAdd(line, changed);

            for (int i = 0; i < selection.Count; i++)
                selection[i] = changedTo.GetValueOrDefault(selection[i], selection[i]);

            if (Selected is SavedTimingLine selected)
                Selected = changedTo.GetValueOrDefault(selected, selected);

            if (rangeStart is SavedTimingLine start)
                rangeStart = changedTo.GetValueOrDefault(start, start);

            savedTimingLines.Selected.Value = Selected;
            show();
        }

        /// <summary>
        /// Makes the lines a save will put in into lines like the others, if any of the selected lines is one of them.
        /// </summary>
        private void takeShownIfAny()
        {
            if (selection.Any(l => !savedTimingLines.Lines.Contains(l)))
                editing.TakeShownLines();
        }

        /// <summary>
        /// Readies a line to be changed: one a save will put in becomes a line like the others first, along with every
        /// other such line (see <see cref="GreenLineEditing.TakeShownLines"/>).
        /// </summary>
        /// <returns>Whether the line is there to be changed.</returns>
        private bool takeIfShown(SavedTimingLine line)
        {
            if (savedTimingLines.Lines.Contains(line))
                return true;

            editing.TakeShownLines();
            return savedTimingLines.Lines.Contains(line);
        }

        /// <summary>
        /// Selects one line alone.
        /// </summary>
        private void select(SavedTimingLine? line)
        {
            Selected = line;
            rangeStart = line;

            selection.Clear();

            if (line is SavedTimingLine selected)
                selection.Add(selected);

            savedTimingLines.Selected.Value = line;
            show();
        }

        /// <summary>
        /// A click on a line in the list: alone, with ctrl to add or take out, or with shift for every line between.
        /// </summary>
        private void clicked(SavedTimingLine line)
        {
            var keyboard = inputManager.CurrentState.Keyboard;

            if (keyboard.ControlPressed)
            {
                if (selection.Contains(line))
                {
                    // the last one stays: a change always has a line to go to.
                    if (selection.Count == 1)
                        return;

                    selection.Remove(line);

                    if (Selected == line)
                        Selected = selection[^1];

                    savedTimingLines.Selected.Value = Selected;
                    show();
                    return;
                }

                selection.Add(line);
                Selected = rangeStart = line;
            }
            else if (keyboard.ShiftPressed && rangeStart is SavedTimingLine start)
            {
                var listed = list.Select(r => r.Line).ToList();
                int from = listed.IndexOf(start);
                int to = listed.IndexOf(line);

                if (from < 0)
                    from = to;

                selection.Clear();
                selection.AddRange(listed.GetRange(Math.Min(from, to), Math.Abs(to - from) + 1));
                Selected = line;
            }
            else
            {
                select(line);
                editorClock.Seek(line.Time);
                return;
            }

            savedTimingLines.Selected.Value = Selected;
            show();
            editorClock.Seek(line.Time);
        }

        /// <summary>
        /// Selects every line the list shows, the one being edited staying so where it is among them.
        /// </summary>
        private void selectAll()
        {
            var listed = list.Select(r => r.Line).ToList();

            if (listed.Count == 0)
                return;

            selection.Clear();
            selection.AddRange(listed);

            if (Selected is not SavedTimingLine selected || !listed.Contains(selected))
                Selected = listed[0];

            rangeStart = Selected;
            savedTimingLines.Selected.Value = Selected;
            show();
        }

        private bool isListed(SavedTimingLine line)
        {
            switch (Filter.Value)
            {
                case GreenLineListFilter.RedLines:
                    return line.Uninherited;

                case GreenLineListFilter.GreenLines:
                    return !line.Uninherited;

                default:
                    return true;
            }
        }

        /// <summary>
        /// The line to select at a time among those the list shows: the one stable's panel picks, if the list shows it,
        /// else the last one listed before the time, else the first one listed.
        /// </summary>
        private SavedTimingLine? pickListedAt(double time)
        {
            if (editing.PickAt(time) is SavedTimingLine picked && isListed(picked))
                return picked;

            var listed = savedTimingLines.Shown.Where(isListed).ToList();

            return listed.Where(l => l.Time <= time).Cast<SavedTimingLine?>().LastOrDefault()
                   ?? listed.Cast<SavedTimingLine?>().FirstOrDefault();
        }

        /// <summary>
        /// Takes out of the selection what the list no longer shows: lines gone, or ones another tab lists.
        /// </summary>
        private void keepSelectionToWhatIsListed()
        {
            var listed = list.Select(r => r.Line).ToHashSet();

            selection.RemoveAll(l => !listed.Contains(l));

            if (Selected is SavedTimingLine selected && !selection.Contains(selected))
                Selected = selection.Count > 0 ? selection[^1] : null;

            if (rangeStart is SavedTimingLine start && !listed.Contains(start))
                rangeStart = Selected;
        }

        private void show()
        {
            var line = Selected;
            bool green = line?.Uninherited == false;

            showing = true;

            SliderVelocity.Value = line?.ClampedSliderVelocity ?? 1;
            SampleSet.Value = (GreenLineSampleSet)Math.Clamp(line?.SampleSet ?? 0, 0, 3);
            SampleIndex.Value = Math.Clamp(line?.SampleIndex ?? 0, SampleIndex.MinValue, SampleIndex.MaxValue);
            Volume.Value = Math.Clamp(line?.Volume ?? 100, Volume.MinValue, Volume.MaxValue);
            Kiai.Value = line?.Kiai ?? false;
            sampleIndexBox.Current.Value = SampleIndex.Value.ToString(CultureInfo.InvariantCulture);

            showing = false;

            foreach (var button in sampleSetButtons)
                button.Selected = button.Set == SampleSet.Value;

            // a red line's velocity is always 1: a velocity set there goes on a green line put in beside it.
            velocityContainer.Alpha = line?.Uninherited == true ? 0.5f : 1;
            removeButton.Enabled.Value = green || selection.Any(l => !l.Uninherited);

            useCurrentTimeButton.Enabled.Value = line != null && selection.Count <= 1;
            pasteButton.Enabled.Value = selection.Count > 0;

            if (line == null)
                lineText.Text = default;
            else
            {
                lineText.Text = LocalisableString.Interpolate(
                    $@"{(green ? LazerKaiGreenLineStrings.GreenLine : LazerKaiGreenLineStrings.RedLine)} {line.Value.Time.ToEditorFormattedString()}");
                lineText.Colour = green ? colours.Lime1 : colours.Red1;
            }

            var confirmed = editing.Confirmed.ToHashSet();

            var inFile = savedTimingLines.Lines.ToHashSet();
            var selected = selection.ToHashSet();

            foreach (var row in list)
            {
                row.Current = selected.Contains(row.Line);
                row.ToBePutIn = !inFile.Contains(row.Line);
                row.Unconfirmed = !row.ToBePutIn && !confirmed.Contains(row.Line);
            }

            // once the rows are laid out: a list just built has them all at the top until then.
            scrollToSelected = true;
        }

        /// <summary>
        /// Whether the list is to be scrolled to the selected line once it is laid out.
        /// </summary>
        private bool scrollToSelected;

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            if (!scrollToSelected)
                return;

            scrollToSelected = false;

            var current = list.FirstOrDefault(r => r.Line == Selected);

            // to the middle rather than just into view, which leaves it against an edge where it is easily missed.
            if (current != null)
                listScroll.ScrollTo(listScroll.GetChildPosInContent(current) - (listScroll.DrawHeight - current.DrawHeight) / 2);
        }

        private void rebuildList()
        {
            list.Clear();

            // with the lines a save will put in, as the timeline shows them (2026-09-25).
            list.AddRange(savedTimingLines.Shown.Where(isListed).Select(l => new LineRow(l)
            {
                Action = () => clicked(l),
            }));

            usedIndexButtons.Clear();
            usedIndexButtons.AddRange(usedIndices().Select(i => new RoundedButton
            {
                Width = 40,
                Height = 25,
                Text = i.ToString(CultureInfo.InvariantCulture),
                Action = () => SampleIndex.Value = i,
            }));
        }

        /// <summary>
        /// Every custom index the beatmap uses, on its lines or its objects.
        /// </summary>
        private IEnumerable<int> usedIndices()
        {
            var indices = savedTimingLines.Lines.Select(l => l.SampleIndex).ToHashSet();

            foreach (var hitObject in editorBeatmap.HitObjects)
            {
                foreach (var sample in hitObject.Samples.Concat((hitObject as IHasRepeats)?.NodeSamples.SelectMany(n => n) ?? Enumerable.Empty<HitSampleInfo>()))
                {
                    if (sample.Suffix != null && int.TryParse(sample.Suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                        indices.Add(index);
                }
            }

            return indices.Where(i => i >= 0 && i <= SampleIndex.MaxValue).OrderBy(i => i);
        }

        protected override void PopIn()
        {
            if (!placed)
                placeUnderTimeline();

            listOutdated = true;
            select(pickListedAt(editorClock.CurrentTime));
            openSample?.Play();
            this.FadeIn(150, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            editing.Cancel();
            Selected = rangeStart = null;
            selection.Clear();
            savedTimingLines.Selected.Value = null;
            this.FadeOut(150, Easing.OutQuint);
        }

        private void placeUnderTimeline()
        {
            var timeline = editor?.ChildrenOfType<TimelineArea>().FirstOrDefault();

            if (timeline == null || !timeline.IsPresent)
            {
                body.Y = 150;
                return;
            }

            body.Y = ToLocalSpace(timeline.ScreenSpaceDrawQuad.BottomLeft).Y + 5;
            placed = true;
        }

        /// <summary>
        /// Only the window itself takes the mouse, so the rest of the editor can still be used with it open.
        /// </summary>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            body.ScreenSpaceDrawQuad.Contains(screenSpacePos);

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        /// <summary>
        /// Closes on the back action, putting back anything not confirmed, which also stops it reaching the editor
        /// behind.
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
        /// Select all and delete go to the lines while the mouse is over the window, and to the objects everywhere else,
        /// as they did before, since the editor stays in use with the window open. Not while the index is being typed,
        /// where they are the text box's.
        /// </summary>
        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            if (State.Value != Visibility.Visible || !Editable || !ReceivePositionalInputAt(e.ScreenSpaceMousePosition)
                || sampleIndexBox.ChildrenOfType<TextBox>().Any(t => t.HasFocus))
                return false;

            switch (e.Action)
            {
                case PlatformAction.SelectAll:
                    selectAll();
                    return true;

                case PlatformAction.Delete:
                    RemoveLine();
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        // dragging is only offered by the title: the controls below it are all things that take a drag of their own.
        protected override bool OnDragStart(DragStartEvent e) => header.ScreenSpaceDrawQuad.Contains(e.ScreenSpaceMouseDownPosition);

        protected override void OnDrag(DragEvent e)
        {
            body.Position += e.Delta;
            placed = true;
        }

        private partial class SampleSetButton : RoundedButton
        {
            public readonly GreenLineSampleSet Set;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SampleSetButton(GreenLineSampleSet set)
            {
                Set = set;
                Height = 30;
                Text = set.ToString();
            }

            private bool selected;

            public bool Selected
            {
                set
                {
                    selected = value;

                    if (IsLoaded)
                        updateColour();
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                updateColour();
            }

            private void updateColour() => BackgroundColour = selected ? colourProvider.Highlight1 : colourProvider.Background3;
        }

        /// <summary>
        /// One line in the list, which goes to its time when clicked.
        /// </summary>
        public partial class LineRow : OsuClickableContainer
        {
            public readonly SavedTimingLine Line;

            private Box background = null!;
            private FillFlowContainer texts = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private OsuColour colours { get; set; } = null!;

            private bool current;

            /// <summary>
            /// Whether this line is selected, so that changes go to it.
            /// </summary>
            public bool Current
            {
                set
                {
                    current = value;

                    if (IsLoaded)
                        updateState();
                }
            }

            private bool unconfirmed;

            /// <summary>
            /// Whether this line is new or changed and not yet confirmed.
            /// </summary>
            public bool Unconfirmed
            {
                set
                {
                    unconfirmed = value;

                    if (IsLoaded)
                        updateState();
                }
            }

            private bool toBePutIn;

            /// <summary>
            /// Whether this is a line a save will put in, not yet among the others: drawn fainter, as on the timeline.
            /// </summary>
            public bool ToBePutIn
            {
                get => toBePutIn;
                set
                {
                    toBePutIn = value;

                    if (IsLoaded)
                        updateState();
                }
            }

            public LineRow(SavedTimingLine line)
            {
                Line = line;
                RelativeSizeAxes = Axes.X;
                Height = row_height;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                string set = ((GreenLineSampleSet)Math.Clamp(Line.SampleSet, 0, 3)).ToString();

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                    },
                    texts = columns(
                        Line.Time.ToEditorFormattedString(),
                        Line.Uninherited ? FormattableString.Invariant($@"{Line.BeatsPerMinute:0.##}bpm") : FormattableString.Invariant($@"x{Line.ClampedSliderVelocity:0.##}"),
                        set,
                        FormattableString.Invariant($@"#{Line.SampleIndex}"),
                        FormattableString.Invariant($@"{Line.Volume}%"),
                        // a star, as in stable's timing panel.
                        Line.Kiai ? @"☆" : string.Empty),
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                updateState();
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

            private void updateState()
            {
                background.Colour = current ? colourProvider.Highlight1.Opacity(0.3f) : colourProvider.Background3;
                background.Alpha = current || IsHovered ? 1 : 0;
                texts.Colour = unconfirmed ? colours.Yellow : Line.Uninherited ? colours.Red1 : colours.Lime1;
                texts.Alpha = toBePutIn ? 0.5f : 1;
            }

            private static FillFlowContainer columns(params string[] values)
            {
                float[] widths = { 90, 70, 55, 35, 45, 35 };

                return new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Padding = new MarginPadding { Horizontal = 6 },
                    Children = values.Select((v, i) => new TruncatingSpriteText
                    {
                        Width = widths[i],
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Font = OsuFont.Default.With(size: 12, fixedWidth: i == 0),
                        Text = v,
                    }).ToArray(),
                };
            }

            /// <summary>
            /// The names of the columns, over the list.
            /// </summary>
            public partial class Heading : CompositeDrawable
            {
                [BackgroundDependencyLoader]
                private void load(OverlayColourProvider colourProvider)
                {
                    RelativeSizeAxes = Axes.X;
                    Height = row_height;

                    InternalChildren = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = colourProvider.Background3,
                        },
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Padding = new MarginPadding { Horizontal = 6 },
                            Children = new[]
                            {
                                caption(LazerKaiGreenLineStrings.Time, 90),
                                caption(LazerKaiGreenLineStrings.SliderVelocity, 70),
                                caption(LazerKaiGreenLineStrings.SampleSet, 55),
                                caption(@"#", 35),
                                caption(LazerKaiGreenLineStrings.Volume, 45),
                                caption(@"Kiai", 35),
                            },
                        },
                    };
                }

                private static TruncatingSpriteText caption(LocalisableString text, float width) => new TruncatingSpriteText
                {
                    Width = width,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Font = OsuFont.Default.With(size: 11, weight: FontWeight.SemiBold),
                    Text = text,
                };
            }
        }
    }

    /// <summary>
    /// Which lines the green line panel lists.
    /// </summary>
    public enum GreenLineListFilter
    {
        [LocalisableDescription(typeof(LazerKaiGreenLineStrings), nameof(LazerKaiGreenLineStrings.FilterAll))]
        All,

        [LocalisableDescription(typeof(LazerKaiGreenLineStrings), nameof(LazerKaiGreenLineStrings.FilterRedLines))]
        RedLines,

        [LocalisableDescription(typeof(LazerKaiGreenLineStrings), nameof(LazerKaiGreenLineStrings.FilterGreenLines))]
        GreenLines,
    }

    /// <summary>
    /// The sample sets a line can say, numbered the way stable writes them.
    /// </summary>
    public enum GreenLineSampleSet
    {
        Auto = 0,
        Normal = 1,
        Soft = 2,
        Drum = 3,
    }
}
