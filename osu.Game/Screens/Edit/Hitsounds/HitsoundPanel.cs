// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Humanizer;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Audio;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.GreenLines;
using osu.Game.Skinning;
using osuTK;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// The hitsound screen's buttons: the sampleset, the additions' bank, and whistle, finish and clap, for the points
    /// selected or, with nothing selected, for what sounds at the playhead. What those already have is lit.
    /// </summary>
    /// <remarks>
    /// The keys are stable's: W, E and R for the additions, shift with Q, W, E and R for the sampleset and ctrl with them
    /// for the additions' bank. Alt does the same as ctrl, as it does on the compose screen, where ctrl with R and E turns
    /// and stretches the selection instead.
    ///
    /// It lies over the playfield, which shows through it and goes on running underneath for the timeline and the
    /// selection to use.
    /// </remarks>
    public partial class HitsoundPanel : CompositeDrawable, IKeyBindingHandler<GlobalAction>
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock clock { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        [Resolved(canBeNull: true)]
        private SavedTimingLines? savedTimingLines { get; set; }

        private static readonly string[] banks = HitSampleInfo.ALL_BANKS.Prepend(HitObjectComposer.HIT_BANK_AUTO).ToArray();

        private static readonly Key[] bank_keys = { Key.Q, Key.W, Key.E, Key.R };

        private static readonly InputKey[] bank_input_keys = { InputKey.Q, InputKey.W, InputKey.E, InputKey.R };

        private static readonly GlobalAction[] normal_bank_actions =
        {
            GlobalAction.EditorToggleNormalAutoBank, GlobalAction.EditorToggleNormalNormalBank, GlobalAction.EditorToggleNormalSoftBank, GlobalAction.EditorToggleNormalDrumBank,
        };

        private static readonly GlobalAction[] addition_bank_actions =
        {
            GlobalAction.EditorToggleAdditionAutoBank, GlobalAction.EditorToggleAdditionNormalBank, GlobalAction.EditorToggleAdditionSoftBank, GlobalAction.EditorToggleAdditionDrumBank,
        };

        private readonly Dictionary<string, Bindable<TernaryState>> additionStates = new Dictionary<string, Bindable<TernaryState>>();
        private readonly Dictionary<string, Bindable<TernaryState>> bankStates = new Dictionary<string, Bindable<TernaryState>>();
        private readonly Dictionary<string, Bindable<TernaryState>> additionBankStates = new Dictionary<string, Bindable<TernaryState>>();

        private readonly Dictionary<string, DrawableTernaryButton> additionButtons = new Dictionary<string, DrawableTernaryButton>();
        private readonly Dictionary<string, DrawableTernaryButton> bankButtons = new Dictionary<string, DrawableTernaryButton>();
        private readonly Dictionary<string, DrawableTernaryButton> additionBankButtons = new Dictionary<string, DrawableTernaryButton>();

        /// <summary>
        /// The bank an addition goes on with, where nothing it is put on has another addition to follow — chosen with the
        /// additions' buttons while what is lit has none.
        /// </summary>
        private string additionBank = HitObjectComposer.HIT_BANK_AUTO;

        private Box background = null!;
        private OsuSpriteText targetText = null!;
        private FillFlowContainer lineRow = null!;
        /// <summary>
        /// Plays what was changed as it sounds now. Not a pausable sound: the editor holds those back while its clock is
        /// stopped, which is when changes are made, and nothing was ever heard (2026-10-08).
        /// </summary>
        private SkinnableSound preview = null!;

        private Sample? menuOpenSample;
        private Sample? menuCloseSample;

        private readonly Bindable<HitsoundChangeSound> changeSound = new Bindable<HitsoundChangeSound>();

        /// <summary>
        /// Whether the change being made comes from a key, which unlike a button makes no sound of its own.
        /// </summary>
        private bool changedByKey;

        private readonly Bindable<GreenLineSampleSet> lineSampleSet = new Bindable<GreenLineSampleSet>();

        private FormNumberBox lineIndexBox = null!;
        private FillFlowContainer<PresetButton> indexPresets = null!;
        private FillFlowContainer<PresetButton> volumePresets = null!;

        /// <summary>
        /// From 5, the quietest a line can be in stable.
        /// </summary>
        private readonly BindableInt lineVolume = new BindableInt(100) { MinValue = 5, MaxValue = 100 };

        /// <summary>
        /// Changes the lines as the green line panel does. Null where the beatmap has no lines of its own.
        /// </summary>
        private GreenLineEditing? lineEditing;

        /// <summary>
        /// The volume of the line the sounds at the playhead follow, as shown.
        /// </summary>
        public IBindable<int> LineVolume => lineVolume;

        private readonly BindableFloat opacity = new BindableFloat();

        /// <summary>
        /// What the buttons read and change, as of the last time they were brought up to date.
        /// </summary>
        public IReadOnlyList<HitPoint> Target { get; private set; } = Array.Empty<HitPoint>();

        private SamplePointSelection? samplePoints;

        /// <summary>
        /// The timeline's selection of sound points, which can be part of an object: a slider's tail or a reverse on its
        /// own. Set once the timeline is there.
        /// </summary>
        public SamplePointSelection? SamplePoints
        {
            get => samplePoints;
            set
            {
                if (samplePoints != null)
                    samplePoints.Changed -= markStale;

                samplePoints = value;

                if (samplePoints != null)
                    samplePoints.Changed += markStale;

                stale = true;
            }
        }

        private bool refreshing;
        private bool stale = true;
        private double lastTime = double.NaN;

        /// <summary>
        /// Where the playhead was when the buttons last flashed for what it passed, while playing.
        /// </summary>
        private double? lastFlashedTime;

        private readonly IBindable<bool> shown;

        /// <param name="shown">Whether the hitsound screen is showing, which this is shown and hidden with.</param>
        public HitsoundPanel(IBindable<bool> shown)
        {
            this.shown = shown.GetBoundCopy();

            // loaded and ready while hidden, so that it is there the moment the screen is switched to.
            Alpha = 0;
            AlwaysPresent = true;
        }

        // while hidden, the compose screen's own buttons have the same keys.
        public override bool PropagatePositionalInputSubTree => shown.Value && base.PropagatePositionalInputSubTree;

        public override bool PropagateNonPositionalInputSubTree => shown.Value && base.PropagateNonPositionalInputSubTree;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider, OsuConfigManager config, AudioManager audio)
        {
            // the top menu's own: a sound put on opens, a sound taken off closes.
            menuOpenSample = audio.Samples.Get(@"UI/menu-open");
            menuCloseSample = audio.Samples.Get(@"UI/menu-close");

            config.BindWith(OsuSetting.EditorHitsoundChangeSound, changeSound);

            RelativeSizeAxes = Axes.Both;

            additionBank = config.Get<EditorAdditionBank>(OsuSetting.EditorDefaultAdditionBank).ToBankName();
            config.BindWith(OsuSetting.EditorHitsoundPanelOpacity, opacity);

            foreach (string sampleName in HitSampleInfo.ALL_ADDITIONS)
            {
                var state = additionStates[sampleName] = new Bindable<TernaryState>();
                state.ValueChanged += _ => onAdditionChanged(sampleName);
            }

            foreach (string bankName in banks)
            {
                var state = bankStates[bankName] = new Bindable<TernaryState>();
                state.ValueChanged += _ => onBankChanged(bankName);

                var additionState = additionBankStates[bankName] = new Bindable<TernaryState>();
                additionState.ValueChanged += _ => onAdditionBankChanged(bankName);
            }

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background4,
                },
                preview = new SkinnableSound(),
                new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    ColumnDimensions = new[]
                    {
                        new Dimension(),
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(),
                    },
                    Content = new[]
                    {
                        new[]
                        {
                            side(new Drawable[]
                            {
                                heading(LazerKaiHitsoundStrings.OutlineHeader),
                                new FormEnumDropdown<EditorSelectionColour>
                                {
                                    Caption = LazerKaiHitsoundStrings.OutlineColour,
                                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorHitsoundOutlineColour),
                                },
                                new FormSliderBar<float>
                                {
                                    Caption = LazerKaiHitsoundStrings.OutlineThickness,
                                    Current = config.GetBindable<float>(OsuSetting.EditorHitsoundOutlineThickness),
                                    DisplayAsPercentage = true,
                                },
                                new FormSliderBar<float>
                                {
                                    Caption = LazerKaiHitsoundStrings.OutlineOpacity,
                                    Current = config.GetBindable<float>(OsuSetting.EditorHitsoundOutlineOpacity),
                                    DisplayAsPercentage = true,
                                },
                            }),
                            new FillFlowContainer
                            {
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(0, 20),
                                Margin = new MarginPadding { Top = 30 },
                                Children = new Drawable[]
                                {
                                    // where the mapper's eyes are while using it: just under the timeline.
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 4),
                                        Children = new Drawable[]
                                        {
                                            targetText = new OsuSpriteText
                                            {
                                                Anchor = Anchor.TopCentre,
                                                Origin = Anchor.TopCentre,
                                                Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold),
                                            },
                                        }
                                    },
                                    // the line the sounds at the playhead follow, changed as stable's timing panel changes
                                    // one: where the playhead is (2026-10-08).
                                    lineRow = new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(10, 0),
                                        Alpha = 0,
                                        Children = new Drawable[]
                                        {
                                            ofWidth(200, new FormEnumDropdown<GreenLineSampleSet>
                                            {
                                                Caption = LazerKaiGreenLineStrings.SampleSet,
                                                Current = lineSampleSet,
                                            }),
                                            // typed rather than dragged: a custom index is a number to say, not an amount.
                                            ofWidth(170, lineIndexBox = new FormNumberBox
                                            {
                                                Caption = LazerKaiGreenLineStrings.CustomIndex,
                                            }, indexPresets = presetFlow()),
                                            ofWidth(320, new FormSliderBar<int>
                                            {
                                                Caption = LazerKaiGreenLineStrings.Volume,
                                                Current = lineVolume,
                                                TransferValueOnCommit = true,
                                            }, volumePresets = presetFlow()),
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(24, 0),
                                        Children = new[]
                                        {
                                            column(LazerKaiHitsoundStrings.Sampleset, banks.Select((bankName, i) => bankButtons[bankName] = new DrawableTernaryButton
                                            {
                                                Current = bankStates[bankName],
                                                Description = bankName.Titleize(),
                                                CreateIcon = () => iconFor(bankName),
                                                Hotkey = new Hotkey(normal_bank_actions[i]),
                                                TooltipText = bankName == HitObjectComposer.HIT_BANK_AUTO ? LazerKaiHitsoundStrings.AutoSamplesetUnavailable : default,
                                            })),
                                            column(LazerKaiHitsoundStrings.AdditionBank, banks.Select((bankName, i) => additionBankButtons[bankName] = new DrawableTernaryButton
                                            {
                                                Current = additionBankStates[bankName],
                                                Description = bankName.Titleize(),
                                                CreateIcon = () => iconFor(bankName),
                                                Hotkey = new Hotkey(new KeyCombination(InputKey.Control, bank_input_keys[i])),
                                            })),
                                            column(LazerKaiHitsoundStrings.Additions, HitSampleInfo.ALL_ADDITIONS.Select(sampleName => additionButtons[sampleName] = new DrawableTernaryButton
                                            {
                                                Current = additionStates[sampleName],
                                                Description = sampleName.Replace(@"hit", string.Empty).Titleize(),
                                                CreateIcon = () => HitObjectComposer.GetIconForSample(sampleName),
                                                Hotkey = new Hotkey(HitObjectComposer.GetActionForSample(sampleName)),
                                            })),
                                        }
                                    },
                                }
                            },
                            side(new Drawable[]
                            {
                                heading(LazerKaiHitsoundStrings.NewDifficultyHeader),
                                newDifficultyButton(LazerKaiHitsoundStrings.CopyIntoNewDifficulty, false),
                                newDifficultyButton(LazerKaiHitsoundStrings.MakeHitsoundDifficulty, true),
                                heading(LazerKaiHitsoundStrings.DisplayHeader),
                                new FormSliderBar<float>
                                {
                                    Caption = LazerKaiHitsoundStrings.PanelOpacity,
                                    Current = opacity,
                                    DisplayAsPercentage = true,
                                },
                                new FormEnumDropdown<HitsoundChangeSound>
                                {
                                    Caption = LazerKaiHitsoundStrings.ChangeSound,
                                    Current = changeSound,
                                },
                            }),
                        },
                    }
                },
            };

            // only placing an object can use auto, as on the compose screen.
            bankButtons[HitObjectComposer.HIT_BANK_AUTO].Enabled.Value = false;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.HitObjectAdded += markStale;
            editorBeatmap.HitObjectRemoved += markStale;
            editorBeatmap.HitObjectUpdated += markStale;
            editorBeatmap.SelectedHitObjects.CollectionChanged += selectionChanged;

            if (savedTimingLines != null)
            {
                savedTimingLines.Lines.CollectionChanged += linesChanged;
                lineEditing = new GreenLineEditing(editorBeatmap, savedTimingLines);
            }

            lineSampleSet.BindValueChanged(s => changeLine(line => line.WithSampleSet((int)s.NewValue)));
            lineVolume.BindValueChanged(v => changeLine(line => line.WithVolume(v.NewValue)));

            lineIndexBox.OnCommit += (_, _) =>
            {
                if (int.TryParse(lineIndexBox.Current.Value, out int index) && index >= 0)
                    changeLine(line => line.WithSampleIndex(index));

                // what was typed and not taken goes back to what the line says.
                stale = true;
            };

            opacity.BindValueChanged(o => background.Alpha = o.NewValue, true);

            shown.BindValueChanged(s =>
            {
                stale = true;
                this.FadeTo(s.NewValue ? 1 : 0, 200, Easing.OutQuint);
            }, true);
        }

        private void markStale() => stale = true;

        private void markStale(HitObject _) => stale = true;

        private void selectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => stale = true;

        private void linesChanged(object? sender, NotifyCollectionChangedEventArgs e) => stale = true;

        protected override void Update()
        {
            base.Update();

            if (!shown.Value)
            {
                lastFlashedTime = null;
                return;
            }

            flashWhatIsPlayed();

            if (!stale && clock.CurrentTime == lastTime)
                return;

            Refresh();
        }

        /// <summary>
        /// Brings the buttons and the lines over them up to date with what they are for.
        /// </summary>
        public void Refresh()
        {
            stale = false;
            lastTime = clock.CurrentTime;

            Target = currentTarget();

            // while playing, what has just been heard is flashed instead, as the compose screen's toolbox does.
            var lit = clock.IsRunning && !hasSelection ? Array.Empty<HitPoint>() : Target;

            refreshing = true;

            foreach (var (sampleName, state) in additionStates)
                state.Value = HitsoundEditing.AdditionState(lit, sampleName);

            foreach (var (bankName, state) in bankStates)
                state.Value = HitsoundEditing.BankState(lit, bankName);

            foreach (var (bankName, state) in additionBankStates)
                state.Value = HitsoundEditing.AdditionBankState(lit, bankName) ?? (bankName == additionBank ? TernaryState.True : TernaryState.False);

            var line = lineAtThePlayhead();

            if (line != null)
            {
                if (Enum.IsDefined(typeof(GreenLineSampleSet), line.Value.SampleSet))
                    lineSampleSet.Value = (GreenLineSampleSet)line.Value.SampleSet;

                lineIndexBox.Current.Value = line.Value.SampleIndex.ToString(CultureInfo.InvariantCulture);
                lineVolume.Value = line.Value.Volume;

                var lines = savedTimingLines!.Lines;

                refreshPresets(indexPresets, lines.Select(l => l.SampleIndex), line.Value.SampleIndex, i => i.ToString(CultureInfo.InvariantCulture),
                    i => changeLine(l => l.WithSampleIndex(i)));
                refreshPresets(volumePresets, lines.Select(l => l.Volume), line.Value.Volume, v => $@"{v}%",
                    v => changeLine(l => l.WithVolume(v)));
            }

            refreshing = false;

            lineRow.Alpha = line != null ? 1 : 0;
            targetText.Text = describe(Target);
        }

        /// <summary>
        /// Whether the beatmap's lines are its own to change here, as in the green line panel: one kept in stable's format.
        /// </summary>
        private bool linesEditable => lineEditing != null && editor?.KeptStableVersion.Value != null && durationSoundsAtEnd;

        /// <summary>
        /// The line the sounds at the playhead follow: the last one at or a little before them, as stable looks it up.
        /// </summary>
        private SavedTimingLine? lineAtThePlayhead() => linesEditable ? lineEditing!.InEffectAt(linePlace() + HitPoints.LENIENCY) : null;

        /// <summary>
        /// Where a line put in for the playhead goes: on the millisecond of what sounds there, which is what it is for, and
        /// otherwise the playhead's nearest one.
        /// </summary>
        private double linePlace()
        {
            var sounding = HitPoints.At(editorBeatmap.HitObjects, clock.CurrentTime, durationSoundsAtEnd);
            return sounding.Count > 0 ? Math.Floor(sounding[0].Time + 1e-6) : Math.Round(clock.CurrentTime);
        }

        /// <summary>
        /// Changes the green line at the playhead, putting one in there first if there is none, the way stable's timing
        /// panel does. What it changes is carried over to the objects that follow it, up to the next line.
        /// </summary>
        private void changeLine(Func<SavedTimingLine, SavedTimingLine> change)
        {
            if (refreshing || !linesEditable)
                return;

            var inEffect = lineAtThePlayhead();

            if (inEffect == null || change(inEffect.Value) == inEffect.Value)
                return;

            double time = linePlace();

            var picked = lineEditing!.PickAt(time);
            var line = picked is SavedTimingLine standing && !standing.Uninherited && Math.Abs(standing.Time - time) <= HitPoints.LENIENCY
                ? standing
                : lineEditing.Add(time);

            if (line == null)
                return;

            lineEditing.Replace(line.Value, change(line.Value));
            lineEditing.Confirm();

            stale = true;
        }

        private bool durationSoundsAtEnd => editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0;

        private bool hasSelection => editorBeatmap.SelectedHitObjects.Count > 0;

        private IReadOnlyList<HitPoint> currentTarget()
        {
            if (hasSelection)
            {
                if (samplePoints == null)
                    return HitPoints.OfSelection(editorBeatmap.SelectedHitObjects).ToArray();

                return samplePoints.All
                                   .Select(p => new HitPoint(p.hitObject, p.point == SamplePointSelection.Body ? null : p.point,
                                       SamplePointSelection.TimeOf(p.hitObject, p.point, editorBeatmap)))
                                   .ToArray();
            }

            return clock.IsRunning
                ? HitPoints.LastAtOrBefore(editorBeatmap.HitObjects, clock.CurrentTime, durationSoundsAtEnd)
                : HitPoints.At(editorBeatmap.HitObjects, clock.CurrentTime, durationSoundsAtEnd);
        }

        private LocalisableString describe(IReadOnlyList<HitPoint> points)
        {
            if (hasSelection)
            {
                return points.Count == 1
                    ? LocalisableString.Interpolate($"{describePoint(points[0])} {LazerKaiHitsoundStrings.SelectedSuffix}")
                    : LazerKaiHitsoundStrings.Selected(editorBeatmap.SelectedHitObjects.Count);
            }

            return points.Count == 0 ? LazerKaiHitsoundStrings.NothingAtThePlayhead : describePoint(points[0]);
        }

        private LocalisableString describePoint(HitPoint point)
        {
            LocalisableString kind = point.Node switch
            {
                null when point.HitObject is IHasRepeats => LazerKaiHitsoundStrings.SliderBody,
                null when durationSoundsAtEnd && point.HitObject is IHasDuration => LazerKaiHitsoundStrings.SpinnerEnd,
                null => LazerKaiHitsoundStrings.Circle,
                0 => LazerKaiHitsoundStrings.SliderHead,
                _ when point.IsTail => LazerKaiHitsoundStrings.SliderTail,
                _ => LazerKaiHitsoundStrings.SliderReverse,
            };

            return LocalisableString.Interpolate($"{point.Time.ToEditorFormattedString()} {kind}");
        }

        private void onAdditionChanged(string sampleName)
        {
            if (refreshing)
                return;

            // what is lit is not always what is there (nothing is, while playing), so what the points have decides.
            bool on = HitsoundEditing.AdditionState(currentTarget(), sampleName) != TernaryState.True;
            change(points => HitsoundEditing.SetAddition(points, sampleName, on, additionBank), on);
        }

        /// <remarks>
        /// A bank is chosen, never taken away: pressing a lit one chooses it again, as radio buttons do.
        /// </remarks>
        private void onBankChanged(string bankName)
        {
            if (refreshing)
                return;

            change(points => HitsoundEditing.SetBank(points, bankName));
        }

        private void onAdditionBankChanged(string bankName)
        {
            if (refreshing)
                return;

            // kept for the next addition either way, as the compose screen keeps the one chosen before adding one.
            additionBank = bankName;
            change(points => HitsoundEditing.SetAdditionBank(points, bankName));
        }

        /// <summary>
        /// Changes what the buttons are for, as one change to undo, and lets the change be heard.
        /// </summary>
        /// <param name="changePoints">The change.</param>
        /// <param name="on">Whether it puts a sound on, rather than taking one off.</param>
        private void change(Func<IReadOnlyList<HitPoint>, IReadOnlyList<HitPoint>> changePoints, bool on = true)
        {
            var points = currentTarget();

            if (points.Count > 0)
            {
                editorBeatmap.BeginChange();

                foreach (var hitObject in changePoints(points).Select(p => p.HitObject).Distinct())
                    editorBeatmap.Update(hitObject);

                editorBeatmap.EndChange();

                switch (changeSound.Value)
                {
                    case HitsoundChangeSound.Hitsound:
                        preview.Samples = points[0].Samples.Cast<ISampleInfo>().ToArray();
                        preview.Play();
                        break;

                    // a button makes its own sound as it is pressed; a key makes none, which left a change by key unheard.
                    case HitsoundChangeSound.Menu when changedByKey:
                        (on ? menuOpenSample : menuCloseSample)?.Play();
                        break;
                }
            }

            Refresh();
        }

        /// <summary>
        /// Flashes the buttons for the sounds the playhead has passed since the last frame, while playing.
        /// </summary>
        private void flashWhatIsPlayed()
        {
            if (!clock.IsRunning)
            {
                lastFlashedTime = null;
                return;
            }

            double currentTime = clock.CurrentTime;
            double previousTime = lastFlashedTime ?? currentTime;

            lastFlashedTime = currentTime;

            // only forward playback is handled; seeking backwards would otherwise flash everything jumped over.
            if (currentTime <= previousTime)
                return;

            foreach (var point in editorBeatmap.HitObjects.Where(h => h.StartTime <= currentTime && h.GetEndTime() >= previousTime)
                                               .SelectMany(h => HitPoints.Of(h, durationSoundsAtEnd))
                                               .Where(p => p.Time > previousTime && p.Time <= currentTime))
            {
                foreach (var sample in point.Samples)
                {
                    if (sample.Name == HitSampleInfo.HIT_NORMAL)
                    {
                        bankButtons.GetValueOrDefault(sample.Bank)?.Flash();
                        continue;
                    }

                    additionButtons.GetValueOrDefault(sample.Name)?.Flash();
                    additionBankButtons.GetValueOrDefault(sample.EditorAutoBank ? HitObjectComposer.HIT_BANK_AUTO : sample.Bank)?.Flash();
                }
            }
        }

        /// <summary>
        /// Moves the playhead to the next point a sound is played in <paramref name="direction"/>: the end of a slider and
        /// each reverse are stopped at as well.
        /// </summary>
        private void seekToNextPoint(int direction)
        {
            double current = clock.CurrentTime;

            var times = editorBeatmap.HitObjects.SelectMany(h => HitPoints.Of(h, durationSoundsAtEnd)).Select(p => p.Time);

            double? next = direction > 0
                ? times.Where(t => t > current + 1).DefaultIfEmpty(double.NaN).Min()
                : times.Where(t => t < current - 1).DefaultIfEmpty(double.NaN).Max();

            if (next is double time && !double.IsNaN(time))
                clock.SeekSmoothlyTo(time);
        }

        protected override bool OnClick(ClickEvent e)
        {
            // what is around the buttons is where nothing is: clicking there lets go of what is selected, as clicking an
            // empty part of the playfield does.
            editorBeatmap.SelectedHitObjects.Clear();
            return true;
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.EditorSeekBackwards:
                    seekToNextPoint(-1);
                    return true;

                case GlobalAction.EditorSeekForwards:
                    seekToNextPoint(1);
                    return true;
            }

            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.EditorToggleWhistleSound:
                    return pressByKey(additionButtons[HitSampleInfo.HIT_WHISTLE]);

                case GlobalAction.EditorToggleFinishSound:
                    return pressByKey(additionButtons[HitSampleInfo.HIT_FINISH]);

                case GlobalAction.EditorToggleClapSound:
                    return pressByKey(additionButtons[HitSampleInfo.HIT_CLAP]);
            }

            int normal = Array.IndexOf(normal_bank_actions, e.Action);

            if (normal >= 0)
            {
                pressByKey(bankButtons[banks[normal]]);
                return true;
            }

            int addition = Array.IndexOf(addition_bank_actions, e.Action);

            if (addition >= 0)
            {
                pressByKey(additionBankButtons[banks[addition]]);
                return true;
            }

            return false;
        }

        private bool pressByKey(DrawableTernaryButton button)
        {
            changedByKey = true;

            try
            {
                return button.TriggerClick();
            }
            finally
            {
                changedByKey = false;
            }
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Repeat || !e.ControlPressed || e.ShiftPressed || e.AltPressed)
                return base.OnKeyDown(e);

            int index = Array.IndexOf(bank_keys, e.Key);

            if (index < 0)
                return base.OnKeyDown(e);

            pressByKey(additionBankButtons[banks[index]]);
            return true;
        }

        /// <summary>
        /// A form control, which fills the width it is given, given one of its own to sit in a row, with whatever goes
        /// under it.
        /// </summary>
        private static Drawable ofWidth(float width, Drawable control, Drawable? under = null) => new FillFlowContainer
        {
            Width = width,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 5),
            Children = under == null ? new[] { control } : new[] { control, under },
        };

        private static FillFlowContainer<PresetButton> presetFlow() => new FillFlowContainer<PresetButton>
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Full,
            Spacing = new Vector2(5),
        };

        /// <summary>
        /// The values the beatmap's lines use, a button each, as the slider velocity presets are: what has been used once
        /// is there to be used again (2026-10-08).
        /// </summary>
        private void refreshPresets(FillFlowContainer<PresetButton> flow, IEnumerable<int> used, int? current, Func<int, string> label, Action<int> apply)
        {
            int[] values = used.Distinct().OrderBy(v => v).ToArray();

            if (!flow.Select(b => b.Value).SequenceEqual(values))
            {
                flow.Clear();

                foreach (int value in values)
                {
                    var button = new PresetButton(value, label(value));
                    button.Current.BindValueChanged(state =>
                    {
                        if (refreshing)
                            return;

                        if (state.NewValue == TernaryState.True)
                            apply(value);
                        else
                            stale = true;
                    });

                    flow.Add(button);
                }
            }

            foreach (var button in flow)
                button.Current.Value = button.Value == current ? TernaryState.True : TernaryState.False;
        }

        public partial class PresetButton : DrawableTernaryButton
        {
            public readonly int Value;

            protected override OsuSetting SquarenessSetting => OsuSetting.EditorSliderVelocityPresetSquareness;

            protected override float StockHeight => 25;

            [SetsRequiredMembers]
            public PresetButton(int value, string label)
            {
                Value = value;
                Description = default;
                CreateIcon = () => new Container
                {
                    Child = new OsuSpriteText
                    {
                        Text = label,
                        Font = OsuFont.Style.Body.With(weight: FontWeight.Bold),
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                    }
                };

                RelativeSizeAxes = Axes.None;
                Width = 50;
                Height = 25;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Icon.Position = Vector2.Zero;
                Icon.RelativeSizeAxes = Axes.Both;
                Icon.Size = new Vector2(1);
            }
        }

        private static Drawable heading(LocalisableString text) => new OsuSpriteText
        {
            Text = text,
            Font = OsuFont.GetFont(size: 17, weight: FontWeight.SemiBold),
            Margin = new MarginPadding { Top = 10 },
        };

        /// <summary>
        /// One of the two columns either side of the buttons, filling the room left there.
        /// </summary>
        private static Drawable side(IEnumerable<Drawable> children) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Padding = new MarginPadding { Horizontal = 24, Top = 30 },
            Child = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Children = children.ToArray(),
            },
        };

        private static Drawable column(LocalisableString title, IEnumerable<Drawable> buttons) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Child = new FillFlowContainer
            {
                Width = 190,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                // the buttons of the compose screen's toolbox, larger, though not so large as to leave the settings either
                // side too narrow to read.
                Scale = new Vector2(1.15f),
                Children = buttons.Prepend(new OsuSpriteText
                {
                    Text = title,
                    Font = OsuFont.GetFont(size: 17, weight: FontWeight.SemiBold),
                    Margin = new MarginPadding { Bottom = 5 },
                }).ToArray(),
            },
        };

        private static Drawable iconFor(string bankName) => new SpriteIcon
        {
            Size = new Vector2(20),
            Icon = bankName switch
            {
                HitObjectComposer.HIT_BANK_AUTO => OsuIcon.EditorBankAuto,
                HitSampleInfo.BANK_NORMAL => OsuIcon.EditorBankNormal,
                HitSampleInfo.BANK_SOFT => OsuIcon.EditorBankSoft,
                _ => OsuIcon.EditorBankDrum,
            },
        };

        private Drawable newDifficultyButton(LocalisableString text, bool asHitCircles)
        {
            var button = new RoundedButton
            {
                Text = text,
                RelativeSizeAxes = Axes.X,
                Height = 40,
                Action = () => editor?.MakeHitsoundDifficulty(asHitCircles),
            };

            if (editor != null)
            {
                var can = editor.CanMakeHitsoundDifficulty.GetBoundCopy();

                can.BindValueChanged(c =>
                {
                    button.Enabled.Value = c.NewValue;
                    button.TooltipText = c.NewValue ? default : LazerKaiHitsoundStrings.NewDifficultyUnavailable;
                }, true);

                canMakeHitsoundDifficulty.Add(can);
            }
            else
                button.Enabled.Value = false;

            return button;
        }

        /// <summary>
        /// Held for as long as the buttons that follow them.
        /// </summary>
        private readonly List<IBindable<bool>> canMakeHitsoundDifficulty = new List<IBindable<bool>>();

        /// <summary>
        /// The samplesets a line can say, by the number it is written with.
        /// </summary>
        public enum GreenLineSampleSet
        {
            Normal = 1,
            Soft = 2,
            Drum = 3,
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
            {
                editorBeatmap.HitObjectAdded -= markStale;
                editorBeatmap.HitObjectRemoved -= markStale;
                editorBeatmap.HitObjectUpdated -= markStale;
                editorBeatmap.SelectedHitObjects.CollectionChanged -= selectionChanged;
            }

            if (savedTimingLines.IsNotNull())
                savedTimingLines.Lines.CollectionChanged -= linesChanged;

            if (samplePoints != null)
                samplePoints.Changed -= markStale;
        }
    }
}
