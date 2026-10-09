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
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
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

        [Resolved(canBeNull: true)]
        private EditorClipboard? clipboard { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

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

        /// <summary>
        /// The width of a column of buttons, before they are scaled.
        /// </summary>
        private const float button_column_width = 190;

        /// <summary>
        /// The buttons of the compose screen's toolbox, larger, though not so large as to leave the columns either side
        /// too narrow to read.
        /// </summary>
        private const float button_scale = 1.15f;

        private const float column_spacing = 24;

        /// <summary>
        /// The width of the three columns of buttons, which what is over and under them lines up with.
        /// </summary>
        private const float block_width = 3 * button_column_width * button_scale + 2 * column_spacing;

        private Box background = null!;
        private OsuSpriteText targetText = null!;
        private TruncatingSpriteText statusText = null!;
        private OsuSpriteText lineHeading = null!;
        private FillFlowContainer lineRow = null!;

        /// <summary>
        /// What the last copy or paste did, as shown over the buttons: left there until the next one, so that what was
        /// copied can be read before it is pasted. Empty until one is made.
        /// </summary>
        public LocalisableString Status => statusText.Text;

        /// <summary>
        /// Whether what <see cref="Status"/> says goes with the selection it made, and goes once that is let go of
        /// (2026-10-10).
        /// </summary>
        private bool statusFollowsSelection;

        /// <param name="status">What was done.</param>
        /// <param name="followsSelection">Whether it selected what it was done to, and is said only while that is selected.</param>
        private void setStatus(LocalisableString status, bool followsSelection = false)
        {
            statusText.Text = status;
            statusFollowsSelection = followsSelection;

            // done again with the same result, the text alone would not show it was.
            statusText.FlashColour(Colour4.White, 600, Easing.OutQuint);
        }

        private ActionButton sameAsPreviousButton = null!;

        private readonly Drawable[] placingButtons = new Drawable[2];
        private ActionButton placeCircleButton = null!;
        private ActionButton placeCircleWithPreviousButton = null!;
        private ActionButton pasteOnlyButton = null!;

        /// <summary>
        /// Whether the ruleset has a circle to put in.
        /// </summary>
        private bool canPlace;

        [Resolved(canBeNull: true)]
        private HitObjectComposer? composer { get; set; }

        private FormDropdown<string> copySource = null!;
        private RoundedButton copyFromButton = null!;

        private Drawable nearLinesHeading = null!;
        private Drawable nearLinesScroll = null!;
        private FillFlowContainer nearLinesFlow = null!;

        /// <summary>
        /// Whether the lines close to a sound are to be looked for again: the lines or the objects changed.
        /// </summary>
        private bool nearLinesStale = true;
        /// <summary>
        /// Plays what was changed as it sounds now. Not a pausable sound: the editor holds those back while its clock is
        /// stopped, which is when changes are made, and nothing was ever heard (2026-10-08).
        /// </summary>
        private SkinnableSound preview = null!;

        private Sample? menuOpenSample;
        private Sample? menuCloseSample;

        private readonly Bindable<HitsoundChangeSound> changeSound = new Bindable<HitsoundChangeSound>();

        private readonly Bindable<bool> advanceByPreviousSpacing = new Bindable<bool>();

        /// <summary>
        /// Whether the lines a few milliseconds off a sound that the sound takes all the same are listed too: they change
        /// nothing heard, and by default only those that do are (2026-10-10).
        /// </summary>
        private readonly Bindable<bool> showHarmlessUnsnaps = new Bindable<bool>();

        /// <summary>
        /// The sampleset the next circle placed is given, auto for what its line says: chosen with the buttons while
        /// nothing is selected and nothing sounds at the playhead, as the compose screen's are for the next object
        /// (2026-10-10). The additions' bank is <see cref="additionBank"/>.
        /// </summary>
        private string placementBank = HitObjectComposer.HIT_BANK_AUTO;

        /// <summary>
        /// The additions the next circle placed is given.
        /// </summary>
        private readonly HashSet<string> placementAdditions = new HashSet<string>();

        // whether each column keeps what was chosen in it for the next circle once one is placed (2026-10-10).
        private readonly BindableBool keepBank = new BindableBool();
        private readonly BindableBool keepAdditionBank = new BindableBool();
        private readonly BindableBool keepAdditions = new BindableBool();

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
            config.BindWith(OsuSetting.EditorHitsoundAdvanceByPreviousSpacing, advanceByPreviousSpacing);
            config.BindWith(OsuSetting.EditorHitsoundShowHarmlessUnsnaps, showHarmlessUnsnaps);
            showHarmlessUnsnaps.BindValueChanged(_ => nearLinesStale = true);

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
                            // what is looked at while working at the top, with what room the settings leave it; the settings,
                            // set once, at the bottom (2026-10-10). Under the other side's they did not fit at 1080p.
                            side(new Drawable[]
                            {
                                nearLinesHeading = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 8),
                                    Children = new[]
                                    {
                                        heading(LazerKaiHitsoundStrings.NearLinesHeader),
                                        new FormCheckBox
                                        {
                                            Caption = LazerKaiHitsoundStrings.ShowHarmlessUnsnaps,
                                            Current = showHarmlessUnsnaps,
                                        },
                                    },
                                },
                            }, nearLinesScroll = new OsuScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = nearLinesFlow = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 2),
                                },
                            }, new Drawable[]
                            {
                                heading(LazerKaiHitsoundStrings.DisplayHeader),
                                new FormCheckBox
                                {
                                    Caption = LazerKaiHitsoundStrings.AdvanceByPreviousSpacing,
                                    Current = advanceByPreviousSpacing,
                                },
                                // the lists first: they open downwards, over what is under them.
                                new FormEnumDropdown<EditorSelectionColour>
                                {
                                    Caption = LazerKaiHitsoundStrings.OutlineColour,
                                    Current = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorHitsoundOutlineColour),
                                },
                                new FormEnumDropdown<HitsoundChangeSound>
                                {
                                    Caption = LazerKaiHitsoundStrings.ChangeSound,
                                    Current = changeSound,
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
                                new FormSliderBar<float>
                                {
                                    Caption = LazerKaiHitsoundStrings.PanelOpacity,
                                    Current = opacity,
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
                                // kept clear of the columns either side, whose text otherwise ran up against it.
                                Margin = new MarginPadding { Top = 30, Horizontal = 28 },
                                Children = new Drawable[]
                                {
                                    // where the mapper's eyes are while using it: just under the timeline. What the buttons
                                    // are for on the left, over the first of them, and what was last done on the right, to
                                    // see that it was (2026-10-10).
                                    new Container
                                    {
                                        Width = block_width,
                                        AutoSizeAxes = Axes.Y,
                                        Children = new Drawable[]
                                        {
                                            targetText = new OsuSpriteText
                                            {
                                                Font = OsuFont.GetFont(size: 20, weight: FontWeight.SemiBold),
                                            },
                                            statusText = new TruncatingSpriteText
                                            {
                                                Anchor = Anchor.CentreRight,
                                                Origin = Anchor.CentreRight,
                                                Font = OsuFont.GetFont(size: 16, weight: FontWeight.SemiBold),
                                                Colour = colourProvider.Content2,
                                                MaxWidth = block_width * 0.6f,
                                            },
                                        }
                                    },
                                    // the line the sounds at the playhead follow, changed as stable's timing panel changes
                                    // one: where the playhead is (2026-10-08). Named over it, since a change here goes on to
                                    // the next line rather than to what the buttons are for.
                                    lineRow = new FillFlowContainer
                                    {
                                        Width = block_width,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Spacing = new Vector2(0, 6),
                                        Alpha = 0,
                                        Children = new Drawable[]
                                        {
                                            lineHeading = new OsuSpriteText
                                            {
                                                Font = OsuFont.GetFont(size: 15, weight: FontWeight.SemiBold),
                                                Colour = colourProvider.Content2,
                                            },
                                            new FillFlowContainer
                                            {
                                                AutoSizeAxes = Axes.Both,
                                                Direction = FillDirection.Horizontal,
                                                Spacing = new Vector2(10, 0),
                                                Children = new Drawable[]
                                                {
                                                    ofWidth(200, new FormEnumDropdown<GreenLineSampleSet>
                                                    {
                                                        Caption = LazerKaiGreenLineStrings.SampleSet,
                                                        Current = lineSampleSet,
                                                    }),
                                                    // typed rather than dragged: a custom index is a number to say, not an amount.
                                                    ofWidth(165, lineIndexBox = new FormNumberBox
                                                    {
                                                        Caption = LazerKaiGreenLineStrings.CustomIndex,
                                                    }, new PresetStrip(indexPresets = presetFlow())),
                                                    ofWidth(block_width - 200 - 165 - 20, new FormSliderBar<int>
                                                    {
                                                        Caption = LazerKaiGreenLineStrings.Volume,
                                                        Current = lineVolume,
                                                        TransferValueOnCommit = true,
                                                    }, new PresetStrip(volumePresets = presetFlow())),
                                                }
                                            },
                                        }
                                    },
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(column_spacing, 0),
                                        Children = new[]
                                        {
                                            column(LazerKaiHitsoundStrings.Sampleset, banks.Select((bankName, i) => bankButtons[bankName] = new DrawableTernaryButton
                                            {
                                                Current = bankStates[bankName],
                                                Description = bankName.Titleize(),
                                                CreateIcon = () => iconFor(bankName),
                                                Hotkey = new Hotkey(normal_bank_actions[i]),
                                            }), keepBank),
                                            column(LazerKaiHitsoundStrings.AdditionBank, banks.Select((bankName, i) => additionBankButtons[bankName] = new DrawableTernaryButton
                                            {
                                                Current = additionBankStates[bankName],
                                                Description = bankName.Titleize(),
                                                CreateIcon = () => iconFor(bankName),
                                                Hotkey = new Hotkey(new KeyCombination(InputKey.Control, bank_input_keys[i])),
                                            }), keepAdditionBank),
                                            column(LazerKaiHitsoundStrings.Additions, HitSampleInfo.ALL_ADDITIONS.Select(sampleName => additionButtons[sampleName] = new DrawableTernaryButton
                                            {
                                                Current = additionStates[sampleName],
                                                Description = sampleName.Replace(@"hit", string.Empty).Titleize(),
                                                CreateIcon = () => HitObjectComposer.GetIconForSample(sampleName),
                                                Hotkey = new Hotkey(HitObjectComposer.GetActionForSample(sampleName)),
                                            }), keepAdditions),
                                        }
                                    },
                                    // what is given from one point to others whole, rather than a button at a time (2026-10-08).
                                    new FillFlowContainer
                                    {
                                        Anchor = Anchor.TopCentre,
                                        Origin = Anchor.TopCentre,
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(column_spacing, 0),
                                        Children = new Drawable[]
                                        {
                                            row(sameAsPreviousButton = new ActionButton(sameAsPrevious)
                                            {
                                                Description = LazerKaiHitsoundStrings.SameAsPrevious,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.History },
                                                Hotkey = new Hotkey(GlobalAction.EditorHitsoundSameAsPrevious),
                                            }),
                                            row(new ActionButton(CopyHitsounds)
                                            {
                                                Description = LazerKaiHitsoundStrings.CopyHitsounds,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Regular.Copy },
                                                Hotkey = new Hotkey(PlatformAction.Copy),
                                            }),
                                            row(new ActionButton(() => PasteHitsounds(true))
                                            {
                                                Description = LazerKaiHitsoundStrings.PasteHitsounds,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Paste },
                                                Hotkey = new Hotkey(PlatformAction.Paste),
                                            }),
                                        }
                                    },
                                    // a circle put in where the playhead is, as a hitsound difficulty is made of, and under the
                                    // paste that puts them in, the one that does not (2026-10-10).
                                    new FillFlowContainer
                                    {
                                        AutoSizeAxes = Axes.Both,
                                        Direction = FillDirection.Horizontal,
                                        Spacing = new Vector2(column_spacing, 0),
                                        Margin = new MarginPadding { Top = -10 },
                                        Children = new Drawable[]
                                        {
                                            placingButtons[0] = row(placeCircleButton = new ActionButton(() => placeCircle(false))
                                            {
                                                Description = LazerKaiHitsoundStrings.PlaceCircle,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Regular.Circle },
                                                Hotkey = new Hotkey(GlobalAction.EditorHitsoundPlaceCircle),
                                            }),
                                            placingButtons[1] = row(placeCircleWithPreviousButton = new ActionButton(() => placeCircle(true))
                                            {
                                                Description = LazerKaiHitsoundStrings.PlaceCircleWithPrevious,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Solid.Circle },
                                                Hotkey = new Hotkey(GlobalAction.EditorHitsoundPlaceCircleWithPrevious),
                                            }),
                                            row(pasteOnlyButton = new ActionButton(() => PasteHitsounds(false))
                                            {
                                                Description = LazerKaiHitsoundStrings.PasteHitsoundsOnly,
                                                CreateIcon = () => new SpriteIcon { Icon = FontAwesome.Regular.Clipboard },
                                                Hotkey = new Hotkey(GlobalAction.EditorHitsoundPasteOnly),
                                            }),
                                        }
                                    },
                                }
                            },
                            side(new Drawable[]
                            {
                                heading(LazerKaiHitsoundStrings.NewDifficultyHeader),
                                newDifficultyButton(LazerKaiHitsoundStrings.CopyIntoNewDifficulty, false),
                                newDifficultyButton(LazerKaiHitsoundStrings.MakeHitsoundDifficulty, true),
                                heading(LazerKaiHitsoundStrings.CopyFromHeader),
                                copySource = new FormDropdown<string>
                                {
                                    Caption = LazerKaiHitsoundStrings.CopyFromSource,
                                },
                                copyFromButton = new RoundedButton
                                {
                                    Text = LazerKaiHitsoundStrings.CopyFromButton,
                                    RelativeSizeAxes = Axes.X,
                                    Height = 40,
                                    Action = copyFromDifficulty,
                                },
                            }),
                        },
                    }
                },
            };

            // only where the ruleset has something to put in, keeping their room so the paste stays under the other.
            canPlace = composer?.CreateObjectToHear(0) != null;

            foreach (var button in placingButtons)
            {
                button.Alpha = canPlace ? 1 : 0;
                button.AlwaysPresent = true;
            }

            placeCircleButton.Enabled.Value = placeCircleWithPreviousButton.Enabled.Value = canPlace;
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

            string[] sources = copySources().Select(b => b.DifficultyName).ToArray();

            if (sources.Length > 0)
            {
                copySource.Items = sources;
                // the one most likely made for its hitsounds, by its name.
                copySource.Current.Value = sources.FirstOrDefault(n => n.Contains(@"hitsound", StringComparison.OrdinalIgnoreCase) || n.Equals(@"HS", StringComparison.OrdinalIgnoreCase))
                                           ?? sources[0];
            }

            if (editor != null)
            {
                keptStableVersion = editor.KeptStableVersion.GetBoundCopy();
                keptStableVersion.BindValueChanged(_ => stale = nearLinesStale = true);
            }

            shown.BindValueChanged(s =>
            {
                stale = nearLinesStale = true;
                this.FadeTo(s.NewValue ? 1 : 0, 200, Easing.OutQuint);

                if (!s.NewValue)
                    statusText.Text = default;
            }, true);
        }

        private IBindable<int?>? keptStableVersion;

        private void markStale() => stale = true;

        private void markStale(HitObject _) => stale = nearLinesStale = true;

        private void selectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            stale = true;

            if (statusFollowsSelection && !hasSelection)
            {
                statusText.Text = default;
                statusFollowsSelection = false;
            }
        }

        private void linesChanged(object? sender, NotifyCollectionChangedEventArgs e) => stale = nearLinesStale = true;

        protected override void Update()
        {
            base.Update();

            if (!shown.Value)
            {
                lastFlashedTime = null;
                return;
            }

            flashWhatIsPlayed();

            if (nearLinesStale)
                refreshNearLines();

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
            bool playing = clock.IsRunning && !hasSelection;
            bool placing = !playing && !hasSelection && Target.Count == 0;
            var lit = playing ? Array.Empty<HitPoint>() : Target;

            refreshing = true;

            foreach (var (sampleName, state) in additionStates)
            {
                state.Value = placing
                    ? placementAdditions.Contains(sampleName) ? TernaryState.True : TernaryState.False
                    : HitsoundEditing.AdditionState(lit, sampleName);
            }

            foreach (var (bankName, state) in bankStates)
            {
                state.Value = placing
                    ? bankName == placementBank ? TernaryState.True : TernaryState.False
                    : HitsoundEditing.BankState(lit, bankName);
            }

            foreach (var (bankName, state) in additionBankStates)
            {
                state.Value = playing
                    ? bankName == (heardAdditionBank ?? additionBank) ? TernaryState.True : TernaryState.False
                    : HitsoundEditing.AdditionBankState(lit, bankName) ?? (bankName == additionBank ? TernaryState.True : TernaryState.False);
            }

            var line = lineAtThePlayhead();

            if (line != null)
            {
                if (Enum.IsDefined(typeof(GreenLineSampleSet), line.Value.SampleSet))
                    lineSampleSet.Value = (GreenLineSampleSet)line.Value.SampleSet;

                lineIndexBox.Current.Value = line.Value.SampleIndex.ToString(CultureInfo.InvariantCulture);
                lineVolume.Value = line.Value.Volume;

                double place = linePlace();
                lineHeading.Text = standingLine(place) is SavedTimingLine standing
                    ? LazerKaiHitsoundStrings.LineAt(standing.Time.ToEditorFormattedString())
                    : LazerKaiHitsoundStrings.LineToAdd(place.ToEditorFormattedString());

                var lines = savedTimingLines!.Lines;

                refreshPresets(indexPresets, lines.Select(l => l.SampleIndex), line.Value.SampleIndex, i => i.ToString(CultureInfo.InvariantCulture),
                    i => changeLine(l => l.WithSampleIndex(i)));
                refreshPresets(volumePresets, lines.Select(l => l.Volume), line.Value.Volume, v => $@"{v}%",
                    v => changeLine(l => l.WithVolume(v)));
            }

            refreshing = false;

            lineRow.Alpha = line != null ? 1 : 0;
            targetText.Text = describe(Target);

            bool canCopyFrom = linesEditable && copySource.Items.Any();
            copyFromButton.Enabled.Value = canCopyFrom;
            copyFromButton.TooltipText = canCopyFrom ? default : LazerKaiHitsoundStrings.CopyFromUnavailable;
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
            var line = standingLine(time) ?? lineEditing!.Add(time);

            if (line == null)
                return;

            lineEditing!.Replace(line.Value, change(line.Value));
            lineEditing.Confirm();

            stale = true;
        }

        /// <summary>
        /// The green line a change goes to: one standing at <paramref name="time"/>, or null where one is put in.
        /// </summary>
        private SavedTimingLine? standingLine(double time) =>
            lineEditing!.PickAt(time) is SavedTimingLine standing && !standing.Uninherited && Math.Abs(standing.Time - time) <= HitPoints.LENIENCY
                ? standing
                : null;

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

            // nothing said where nothing sounds: the buttons are then for the next circle placed.
            return points.Count == 0 ? default : describePoint(points[0]);
        }

        private LocalisableString describePoint(HitPoint point) => LocalisableString.Interpolate($"{point.Time.ToEditorFormattedString()} {kindOf(point)}");

        private LocalisableString kindOf(HitPoint point) => point.Node switch
        {
            null when point.HitObject is IHasRepeats => LazerKaiHitsoundStrings.SliderBody,
            null when durationSoundsAtEnd && point.HitObject is IHasDuration => LazerKaiHitsoundStrings.SpinnerEnd,
            null => LazerKaiHitsoundStrings.Circle,
            0 => LazerKaiHitsoundStrings.SliderHead,
            _ when point.IsTail => LazerKaiHitsoundStrings.SliderTail,
            _ => LazerKaiHitsoundStrings.SliderReverse,
        };

        /// <summary>
        /// Gives what the buttons are for the sounds heard just before the first of them, as the buttons would one at a
        /// time (2026-10-08).
        /// </summary>
        private void sameAsPrevious()
        {
            var points = currentTarget();

            if (points.Count == 0)
            {
                setStatus(LazerKaiHitsoundStrings.StatusNothingToCopyOnto);
                return;
            }

            var previous = HitPoints.LastBefore(editorBeatmap.HitObjects, points.Min(p => p.Time), durationSoundsAtEnd);

            if (previous.Count == 0)
            {
                setStatus(LazerKaiHitsoundStrings.StatusNothingBefore);
                return;
            }

            var hitsound = HitsoundEditing.HitsoundOf(previous);
            change(p => HitsoundEditing.SetHitsound(p, hitsound, lineBankOf));

            var (times, count) = span(points);
            setStatus(LazerKaiHitsoundStrings.StatusCopiedPrevious(previous[0].Time.ToEditorFormattedString(), times, count));
        }

        /// <summary>
        /// Puts in a circle on the beat at the playhead, sounding as the buttons were set for it, or with
        /// <paramref name="withPrevious"/>, as the sounds heard before it do (2026-10-10). It is selected alone, to be
        /// changed further from there.
        /// </summary>
        /// <remarks>
        /// Where something sounds already, it is given the sounds instead: circles stacked at a time were more of the same
        /// rather than anything wanted (2026-10-10).
        /// </remarks>
        private void placeCircle(bool withPrevious)
        {
            double time = editorBeatmap.SnapTime(clock.CurrentTime, null);
            var previous = HitPoints.LastBefore(editorBeatmap.HitObjects, time, durationSoundsAtEnd);

            var hitsound = withPrevious
                ? previous.Count > 0 ? HitsoundEditing.HitsoundOf(previous) : (PointHitsound?)null
                : placementHitsoundAt();

            // what the buttons are for at the playhead first: an end of a slider off the beat would be taken for nothing
            // there by the time snapped to it (2026-10-10).
            var existing = HitPoints.At(editorBeatmap.HitObjects, clock.CurrentTime, durationSoundsAtEnd);

            if (existing.Count == 0)
                existing = HitPoints.At(editorBeatmap.HitObjects, time, durationSoundsAtEnd);

            IReadOnlyList<HitPoint> placed;

            if (existing.Count > 0)
            {
                if (hitsound is PointHitsound given)
                    change(existing, p => HitsoundEditing.SetHitsound(p, given, lineBankOf));

                placed = existing;
            }
            else
            {
                var circle = circleAt(time, hitsound);

                if (circle == null)
                    return;

                editorBeatmap.BeginChange();
                editorBeatmap.Add(circle);
                editorBeatmap.EndChange();

                placed = new[] { new HitPoint(circle, null, time) };
                playChange(placed, true);
            }

            selectPoints(placed);

            if (withPrevious)
            {
                setStatus(previous.Count > 0
                    ? LazerKaiHitsoundStrings.StatusCopiedPrevious(previous[0].Time.ToEditorFormattedString(), time.ToEditorFormattedString(), 1)
                    : LazerKaiHitsoundStrings.StatusNothingBefore, true);
            }
            else
                clearPlacementChoices();

            // as the compose screen's setting does after an object is placed there.
            if (advanceByPreviousSpacing.Value && HitObjectComposer.TimeAfterPreviousSpacing(editorBeatmap.HitObjects, placed[0].HitObject, editorBeatmap) is double next)
                clock.SeekSmoothlyTo(next);

            Refresh();
        }

        /// <summary>
        /// Lets go of what was chosen for the circle just placed, in each column not kept: auto for the banks, and no
        /// additions (2026-10-10).
        /// </summary>
        private void clearPlacementChoices()
        {
            if (!keepBank.Value)
                placementBank = HitObjectComposer.HIT_BANK_AUTO;

            if (!keepAdditionBank.Value)
                additionBank = HitObjectComposer.HIT_BANK_AUTO;

            if (!keepAdditions.Value)
                placementAdditions.Clear();
        }

        /// <summary>
        /// A circle to be heard at <paramref name="time"/>, sounding as its line says and then as
        /// <paramref name="hitsound"/> says, if anything; null for a ruleset that has none to put in.
        /// </summary>
        private HitObject? circleAt(double time, PointHitsound? hitsound)
        {
            var circle = composer?.CreateObjectToHear(time);

            if (circle == null)
                return null;

            IList<HitSampleInfo> samples = new List<HitSampleInfo> { normalSampleAt(time) };

            if (hitsound is PointHitsound given)
                samples = HitsoundEditing.WithHitsound(samples, given, samples[0].Bank);

            circle.Samples = samples;
            return circle;
        }

        /// <summary>
        /// What the buttons were set to for the next circle: an auto sampleset follows the line where it is put in.
        /// </summary>
        private PointHitsound placementHitsoundAt() => new PointHitsound(
            placementBank,
            placementAdditions.Count > 0 ? additionBank : null,
            HitSampleInfo.ALL_ADDITIONS.Where(placementAdditions.Contains).ToArray());

        /// <summary>
        /// The bank of the green line <paramref name="point"/> reads its sounds from, which an auto sampleset follows; null
        /// where the beatmap has no lines of its own.
        /// </summary>
        private string? lineBankOf(HitPoint point)
        {
            if (!linesEditable)
                return null;

            // a slider's body reads its line a millisecond after its head does, as the decoder looks them up.
            double lookup = point.Time + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + (point.Node == null && point.HitObject is IHasRepeats ? 1 : 0);

            return lineEditing!.InEffectAt(lookup) is SavedTimingLine line ? bankOf(line) : null;
        }

        private static string bankOf(SavedTimingLine line) => line.SampleSet switch
        {
            2 => HitSampleInfo.BANK_SOFT,
            3 => HitSampleInfo.BANK_DRUM,
            _ => HitSampleInfo.BANK_NORMAL,
        };

        /// <summary>
        /// The normal sound an object put in at <paramref name="time"/> makes: what its line says, as in stable, or where
        /// the beatmap has no lines of its own, what the object before it makes, as an object placed on the compose screen
        /// does.
        /// </summary>
        private HitSampleInfo normalSampleAt(double time)
        {
            if (linesEditable && lineEditing!.InEffectAt(time + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY) is SavedTimingLine line)
            {
                return new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bankOf(line), line.SampleIndex >= 2 ? line.SampleIndex.ToString(CultureInfo.InvariantCulture) : null,
                    line.Volume, editorAutoBank: false, useBeatmapSamples: line.SampleIndex >= 1);
            }

            var before = HitPoints.LastBefore(editorBeatmap.HitObjects, time, durationSoundsAtEnd).SelectMany(p => p.Samples).FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            return before?.With(newEditorAutoBank: false) ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_NORMAL, editorAutoBank: false);
        }

        /// <summary>
        /// When <paramref name="points"/> are heard, as the status says it — from the first to the last — and at how many
        /// times, those heard together counted once.
        /// </summary>
        private static (string times, int count) span(IEnumerable<HitPoint> points)
        {
            double[] times = points.Select(p => p.Time).OrderBy(t => t).ToArray();

            int count = 0;
            double groupStart = double.NegativeInfinity;

            foreach (double time in times)
            {
                if (time - groupStart < 1)
                    continue;

                count++;
                groupStart = time;
            }

            string first = times[0].ToEditorFormattedString();
            string last = times[^1].ToEditorFormattedString();

            return (first == last ? first : $@"{first} ~ {last}", count);
        }

        /// <summary>
        /// Copies the hitsounds of what the buttons are for, rather than the objects, which is what copying does on this
        /// screen (2026-10-08). Points heard together are copied as one.
        /// </summary>
        public void CopyHitsounds()
        {
            if (clipboard == null)
                return;

            var points = currentTarget();

            // a pattern is pasted onto the points sounds are heard at, which a slider's body is not: with a whole slider
            // selected, its head is what is heard at its start.
            var heard = points.Where(p => p.Node != null || p.HitObject is not IHasRepeats).OrderBy(p => p.Time).ToArray();

            if (heard.Length == 0)
                heard = points.OrderBy(p => p.Time).ToArray();

            if (heard.Length == 0)
            {
                setStatus(LazerKaiHitsoundStrings.StatusNothingToCopy);
                return;
            }

            var groups = new List<List<HitPoint>>();

            foreach (var point in heard)
            {
                if (groups.Count == 0 || point.Time - groups[^1][0].Time >= 1)
                    groups.Add(new List<HitPoint>());

                groups[^1].Add(point);
            }

            double first = groups[0][0].Time;
            clipboard.Hitsounds.Value = groups.Select(g => new CopiedHitsound(g[0].Time - first, HitsoundEditing.HitsoundOf(g))).ToArray();

            var (times, count) = span(heard);
            setStatus(LazerKaiHitsoundStrings.StatusCopied(times, count));
        }

        /// <summary>
        /// Pastes the hitsounds copied: one onto everything the buttons are for, and several as far apart as they were,
        /// from the first point the buttons are for, or from the playhead.
        /// </summary>
        /// <param name="withObjects">Whether a circle is put in to have them where nothing is heard, as a paste of the
        /// objects would bring them along (2026-10-10). Otherwise nothing is pasted there.</param>
        public void PasteHitsounds(bool withObjects)
        {
            var copied = clipboard?.Hitsounds.Value;

            if (copied == null || copied.Count == 0)
            {
                setStatus(LazerKaiHitsoundStrings.StatusNothingCopied);
                return;
            }

            // always from the playhead, as a paste of objects is: what is selected after a paste or a copy would otherwise
            // take the next paste where the last one went (2026-10-10).
            var atPlayhead = HitPoints.At(editorBeatmap.HitObjects, clock.CurrentTime, durationSoundsAtEnd);
            double start = atPlayhead.Count > 0 ? atPlayhead[0].Time : editorBeatmap.SnapTime(clock.CurrentTime, null);

            // where each lands, and what is heard there.
            var landing = copied.Select(c => (time: start + c.Offset, c.Hitsound, points: HitPoints.At(editorBeatmap.HitObjects, start + c.Offset, durationSoundsAtEnd)))
                                .ToArray();

            var pastedOnto = new List<HitPoint>();
            var changed = new HashSet<HitObject>();

            editorBeatmap.BeginChange();

            foreach (var (time, hitsound, points) in landing)
            {
                if (points.Count > 0)
                {
                    foreach (var point in HitsoundEditing.SetHitsound(points, hitsound, lineBankOf))
                        changed.Add(point.HitObject);

                    pastedOnto.AddRange(points);
                    continue;
                }

                if (withObjects && circleAt(time, hitsound) is HitObject circle)
                {
                    editorBeatmap.Add(circle);
                    pastedOnto.Add(new HitPoint(circle, null, time));
                }
            }

            foreach (var hitObject in changed)
                editorBeatmap.Update(hitObject);

            editorBeatmap.EndChange();

            if (pastedOnto.Count == 0)
            {
                setStatus(LazerKaiHitsoundStrings.StatusNothingToPasteOnto);
                Refresh();
                return;
            }

            // a key, or the menu, which has no sound of its own for it either.
            changedByKey = true;
            playChange(pastedOnto, true);
            changedByKey = false;

            // left selected, to see what was pasted onto and to change further.
            selectPoints(pastedOnto);

            var (times, count) = span(pastedOnto);
            setStatus(LazerKaiHitsoundStrings.StatusPasted(times, count), true);

            Refresh();
        }

        private void selectPoints(IEnumerable<HitPoint> points)
        {
            if (samplePoints != null)
                samplePoints.SelectPoints(points.Select(p => (p.HitObject, p.Node ?? SamplePointSelection.Body)));
            else
            {
                editorBeatmap.SelectedHitObjects.Clear();
                editorBeatmap.SelectedHitObjects.AddRange(points.Select(p => p.HitObject).Distinct());
            }
        }

        /// <summary>
        /// The difficulties the hitsounds can be copied from: the set's other osu! ones, easiest first.
        /// </summary>
        private IEnumerable<BeatmapInfo> copySources() =>
            workingBeatmap.Value.BeatmapSetInfo?.Beatmaps
                          .Where(b => b.Ruleset.OnlineID == 0 && !b.Equals(editorBeatmap.BeatmapInfo))
                          .OrderBy(b => b.StarRating)
            ?? Enumerable.Empty<BeatmapInfo>();

        /// <summary>
        /// Copies the chosen difficulty's hitsounds, as it is saved, onto this one, as one change to undo (2026-10-08).
        /// </summary>
        private void copyFromDifficulty()
        {
            if (!linesEditable || editor == null)
                return;

            var info = copySources().FirstOrDefault(b => b.DifficultyName == copySource.Current.Value);

            if (info == null)
                return;

            byte[]? file = editor.ReadStoredFileOf(info);
            var source = file == null ? null : HitsoundCopying.Read(file);

            if (source == null)
            {
                notifications?.Post(new SimpleErrorNotification { Text = LazerKaiHitsoundStrings.CouldNotReadDifficulty });
                return;
            }

            HitsoundCopying.CopyOnto(editorBeatmap, savedTimingLines!, lineEditing!, source.Value);
            stale = true;
        }

        /// <summary>
        /// How far from a sound a green line is listed as close to it.
        /// </summary>
        private const double near_line_distance = 10;

        /// <summary>
        /// Lists the green lines a few milliseconds from a sound, and whether that sound takes them: stable reads a sound's
        /// line a little after it (2026-10-08).
        /// </summary>
        private void refreshNearLines()
        {
            nearLinesStale = false;
            nearLinesFlow.Clear();

            // the lines are the file's, which only a beatmap kept in stable's format has, as for the line over the buttons.
            nearLinesHeading.Alpha = nearLinesScroll.Alpha = linesEditable ? 1 : 0;

            if (!linesEditable)
                return;

            var lines = savedTimingLines!.Lines.ToList();
            var points = editorBeatmap.HitObjects.SelectMany(h => HitPoints.Of(h, durationSoundsAtEnd)).OrderBy(p => p.Time).ToArray();
            double[] times = points.Select(p => wholeTime(p.Time)).ToArray();

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                // a red line is the timing's; one saying what the line before it does changes nothing heard.
                if (line.Uninherited || (i > 0 && sameSounds(lines[i - 1], line)))
                    continue;

                int index = Array.BinarySearch(times, line.Time - near_line_distance);

                if (index < 0)
                    index = ~index;

                for (; index < points.Length && times[index] <= line.Time + near_line_distance; index++)
                {
                    double offset = line.Time - times[index];

                    if (Math.Abs(offset) < 1)
                        continue;

                    var read = lines.Where(l => l.Time <= times[index] + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY).Cast<SavedTimingLine?>().LastOrDefault();
                    bool taken = read == line;

                    // taken all the same, it changes nothing heard.
                    if (taken && !showHarmlessUnsnaps.Value)
                        continue;

                    nearLinesFlow.Add(new NearLine(line.Time, (int)Math.Round(Math.Abs(offset)), offset > 0, taken, kindOf(points[index]))
                    {
                        Action = () => clock.SeekSmoothlyTo(line.Time),
                    });
                }
            }

            if (nearLinesFlow.Count == 0)
            {
                nearLinesFlow.Add(new OsuSpriteText
                {
                    Text = LazerKaiHitsoundStrings.NoNearLines,
                    Font = OsuFont.GetFont(size: 14),
                });
            }

            static bool sameSounds(SavedTimingLine a, SavedTimingLine b) => a.SampleSet == b.SampleSet && a.SampleIndex == b.SampleIndex && a.Volume == b.Volume;

            // as stable keeps a time: a slider's reverses and end are worked out to a fraction and cut to a millisecond.
            static double wholeTime(double time) => Math.Floor(time + 1e-6);
        }

        /// <summary>
        /// A green line close to a sound, which moves the playhead to it when clicked.
        /// </summary>
        public partial class NearLine : OsuClickableContainer
        {
            public readonly double LineTime;

            /// <summary>
            /// How many milliseconds from the sound the line is.
            /// </summary>
            public readonly int Distance;

            /// <summary>
            /// Whether the line is after the sound, rather than before it.
            /// </summary>
            public readonly bool After;

            /// <summary>
            /// Whether the sound takes its sampleset, custom index and volume from this line.
            /// </summary>
            public readonly bool Taken;

            public NearLine(double lineTime, int distance, bool after, bool taken, LocalisableString kind)
            {
                LineTime = lineTime;
                Distance = distance;
                After = after;
                Taken = taken;

                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                LocalisableString where = after
                    ? taken ? LazerKaiHitsoundStrings.LineAfterTaken(kind, distance) : LazerKaiHitsoundStrings.LineAfterNotTaken(kind, distance)
                    : taken ? LazerKaiHitsoundStrings.LineBeforeTaken(kind, distance) : LazerKaiHitsoundStrings.LineBeforeNotTaken(kind, distance);

                Child = new OsuTextFlowContainer(t => t.Font = OsuFont.GetFont(size: 14))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = LocalisableString.Interpolate($"{lineTime.ToEditorFormattedString()}  {where}"),
                };
            }
        }

        private void onAdditionChanged(string sampleName)
        {
            if (refreshing)
                return;

            if (forPlacement)
            {
                bool add = placementAdditions.Add(sampleName);

                if (!add)
                    placementAdditions.Remove(sampleName);

                choseForPlacement(add);
                return;
            }

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

            if (forPlacement)
            {
                placementBank = bankName;
                choseForPlacement(true);
                return;
            }

            change(points => HitsoundEditing.SetBank(points, bankName, lineBankOf));
        }

        private void onAdditionBankChanged(string bankName)
        {
            if (refreshing)
                return;

            // kept for the next addition either way, as the compose screen keeps the one chosen before adding one.
            additionBank = bankName;

            if (forPlacement)
            {
                choseForPlacement(true);
                return;
            }

            change(points => HitsoundEditing.SetAdditionBank(points, bankName));
        }

        /// <summary>
        /// Whether the buttons are for the next circle placed: nothing is selected, and nothing sounds at the playhead.
        /// </summary>
        private bool forPlacement => !hasSelection && currentTarget().Count == 0;

        private void choseForPlacement(bool on)
        {
            if (changeSound.Value == HitsoundChangeSound.Menu && changedByKey)
                (on ? menuOpenSample : menuCloseSample)?.Play();

            Refresh();
        }

        /// <summary>
        /// Changes what the buttons are for, as one change to undo, and lets the change be heard.
        /// </summary>
        /// <param name="changePoints">The change.</param>
        /// <param name="on">Whether it puts a sound on, rather than taking one off.</param>
        private void change(Func<IReadOnlyList<HitPoint>, IReadOnlyList<HitPoint>> changePoints, bool on = true) => change(currentTarget(), changePoints, on);

        /// <param name="points">What is changed, of which the first is heard.</param>
        /// <param name="changePoints">The change.</param>
        /// <param name="on">Whether it puts a sound on, rather than taking one off.</param>
        private void change(IReadOnlyList<HitPoint> points, Func<IReadOnlyList<HitPoint>, IReadOnlyList<HitPoint>> changePoints, bool on = true)
        {
            if (points.Count > 0)
            {
                editorBeatmap.BeginChange();

                foreach (var hitObject in changePoints(points).Select(p => p.HitObject).Distinct())
                    editorBeatmap.Update(hitObject);

                editorBeatmap.EndChange();

                playChange(points, on);
            }

            Refresh();
        }

        /// <summary>
        /// Lets a change to <paramref name="points"/> be heard, as the setting says.
        /// </summary>
        private void playChange(IReadOnlyList<HitPoint> points, bool on)
        {
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

        /// <summary>
        /// The additions' bank of the last sound heard with additions, while playing: lit until the next one rather than
        /// flashed, as it seldom changes from one sound to the next (2026-10-10).
        /// </summary>
        private string? heardAdditionBank;

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
            double? previousTime = lastFlashedTime;

            lastFlashedTime = currentTime;

            // started, or seeked backwards: what was heard last is looked for afresh, and nothing jumped over is flashed.
            if (previousTime == null || currentTime < previousTime)
            {
                var last = editorBeatmap.HitObjects.Where(h => h.StartTime <= currentTime)
                                        .SelectMany(h => HitPoints.Of(h, durationSoundsAtEnd))
                                        .Where(p => p.Time <= currentTime && p.Samples.Any(isAddition))
                                        .OrderByDescending(p => p.Time)
                                        .Cast<HitPoint?>()
                                        .FirstOrDefault();

                heardAdditionBank = last is HitPoint point ? bankOf(point.Samples.First(isAddition)) : null;
                return;
            }

            foreach (var point in editorBeatmap.HitObjects.Where(h => h.StartTime <= currentTime && h.GetEndTime() >= previousTime)
                                               .SelectMany(h => HitPoints.Of(h, durationSoundsAtEnd))
                                               .Where(p => p.Time > previousTime && p.Time <= currentTime))
            {
                foreach (var sample in point.Samples)
                {
                    if (sample.Name == HitSampleInfo.HIT_NORMAL)
                    {
                        bankButtons.GetValueOrDefault(HitsoundEditing.FollowsLine(sample) ? HitObjectComposer.HIT_BANK_AUTO : sample.Bank)?.Flash();
                        continue;
                    }

                    additionButtons.GetValueOrDefault(sample.Name)?.Flash();
                    heardAdditionBank = bankOf(sample);
                }
            }

            static bool isAddition(HitSampleInfo sample) => sample.Name != HitSampleInfo.HIT_NORMAL;

            static string bankOf(HitSampleInfo addition) => addition.EditorAutoBank ? HitObjectComposer.HIT_BANK_AUTO : addition.Bank;
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

                case GlobalAction.EditorHitsoundSameAsPrevious:
                    return pressByKey(sameAsPreviousButton);

                case GlobalAction.EditorHitsoundPlaceCircle:
                    return canPlace && pressByKey(placeCircleButton);

                case GlobalAction.EditorHitsoundPlaceCircleWithPrevious:
                    return canPlace && pressByKey(placeCircleWithPreviousButton);

                case GlobalAction.EditorHitsoundPasteOnly:
                    return pressByKey(pasteOnlyButton);
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
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(5),
        };

        /// <summary>
        /// Presets in one row, scrolled sideways once there are more than fit. Wrapped, how many a beatmap's lines use
        /// moved the buttons under them up or down from one beatmap to the next (2026-10-10).
        /// </summary>
        private partial class PresetStrip : OsuScrollContainer
        {
            private readonly Drawable presets;

            public PresetStrip(Drawable presets)
                : base(Direction.Horizontal)
            {
                this.presets = presets;

                RelativeSizeAxes = Axes.X;
                ScrollbarVisible = false;
                Child = presets;
            }

            protected override void Update()
            {
                base.Update();

                // as tall as the buttons, which the squareness setting stretches.
                Height = presets.DrawHeight;
            }
        }

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
        /// <remarks>
        /// As see-through as the middle: made opaque, the playfield could no longer be seen while working, and the
        /// middle looked squeezed between them (2026-10-10).
        /// </remarks>
        /// <param name="top">What goes in it, from the top.</param>
        /// <param name="filling">What takes the height left under those, if anything does.</param>
        /// <param name="bottom">What goes at the bottom, under the filling.</param>
        private static Drawable side(IEnumerable<Drawable> top, Drawable? filling = null, IEnumerable<Drawable>? bottom = null)
        {
            Drawable content = flowOf(top);

            if (filling != null)
            {
                content = new GridContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    RowDimensions = new[]
                    {
                        new Dimension(GridSizeMode.AutoSize),
                        new Dimension(GridSizeMode.Absolute, 8),
                        new Dimension(),
                        new Dimension(GridSizeMode.Absolute, 8),
                        new Dimension(GridSizeMode.AutoSize),
                    },
                    Content = new[]
                    {
                        new[] { content },
                        new[] { Empty() },
                        new[] { filling },
                        new[] { Empty() },
                        new[] { bottom == null ? Empty() : flowOf(bottom) },
                    },
                };
            }

            return new Container
            {
                RelativeSizeAxes = Axes.Both,
                Padding = new MarginPadding { Horizontal = 20, Top = 30, Bottom = 10 },
                Child = content,
            };

            static Drawable flowOf(IEnumerable<Drawable> children) => new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Children = children.ToArray(),
            };
        }

        /// <summary>
        /// A button as wide as a column of them, to stand under it.
        /// </summary>
        private static Drawable row(DrawableTernaryButton button) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Child = new Container
            {
                Width = button_column_width,
                AutoSizeAxes = Axes.Y,
                Scale = new Vector2(button_scale),
                Child = button,
            },
        };

        /// <summary>
        /// A button that looks like the others and does something once, rather than being on or off.
        /// </summary>
        private partial class ActionButton : DrawableTernaryButton
        {
            private readonly Action act;

            public ActionButton(Action act)
            {
                this.act = act;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // in place of the toggling the others do, which would light it.
                Action = () =>
                {
                    if (Enabled.Value)
                        act();
                };
            }
        }

        /// <param name="title">The column's title.</param>
        /// <param name="buttons">Its buttons.</param>
        /// <param name="keep">Whether what is chosen in it for the next circle is kept once one is placed, switched by a
        /// toggle beside the title.</param>
        private static Drawable column(LocalisableString title, IEnumerable<Drawable> buttons, BindableBool keep) => new Container
        {
            AutoSizeAxes = Axes.Both,
            Child = new FillFlowContainer
            {
                Width = button_column_width,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 5),
                Scale = new Vector2(button_scale),
                Children = buttons.Prepend(new Container
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Margin = new MarginPadding { Bottom = 5 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = title,
                            Font = OsuFont.GetFont(size: 17, weight: FontWeight.SemiBold),
                        },
                        new KeepToggle(keep)
                        {
                            Anchor = Anchor.CentreRight,
                            Origin = Anchor.CentreRight,
                        },
                    },
                }).ToArray(),
            },
        };

        /// <summary>
        /// Keeps what is chosen in a column for the next circle once one is placed, which otherwise goes back to auto, or
        /// to nothing (2026-10-10).
        /// </summary>
        public partial class KeepToggle : OsuClickableContainer
        {
            private readonly BindableBool keep;

            private Box background = null!;
            private OsuSpriteText text = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public KeepToggle(BindableBool keep)
            {
                this.keep = keep;

                AutoSizeAxes = Axes.Both;
                Masking = true;
                CornerRadius = 4;
                TooltipText = LazerKaiHitsoundStrings.KeepTooltip;
                Action = keep.Toggle;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    background = new Box { RelativeSizeAxes = Axes.Both },
                    text = new OsuSpriteText
                    {
                        Text = LazerKaiHitsoundStrings.Keep,
                        Font = OsuFont.GetFont(size: 12, weight: FontWeight.SemiBold),
                        Margin = new MarginPadding { Horizontal = 7, Vertical = 2 },
                    },
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                keep.BindValueChanged(k =>
                {
                    background.Colour = k.NewValue ? colourProvider.Highlight1 : colourProvider.Background3;
                    text.Colour = k.NewValue ? colourProvider.Background5 : colourProvider.Content2;
                }, true);
            }
        }

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
