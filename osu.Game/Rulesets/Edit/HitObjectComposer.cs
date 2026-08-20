// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.IEnumerableExtensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Overlays;
using osu.Game.Rulesets.Configuration;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Edit
{
    /// <summary>
    /// Top level container for editor compose mode.
    /// Responsible for providing snapping and generally gluing components together.
    /// </summary>
    /// <typeparam name="TObject">The base type of supported objects.</typeparam>
    /// <typeparam name="TAction">The enumeration type used for ruleset-specific editor key bindings.</typeparam>
    public abstract partial class HitObjectComposer<TObject, TAction> : HitObjectComposer, IPlacementHandler
        where TObject : HitObject
        where TAction : struct, Enum
    {
        /// <summary>
        /// Whether the playfield should be centered horizontally. Should be disabled for playfields which span the full horizontal width.
        /// </summary>
        protected virtual bool ApplyHorizontalCentering => true;

        protected IRulesetConfigManager? Config { get; private set; }

        // Provides `Playfield`
        private DependencyContainer dependencies = null!;

        [Resolved]
        protected EditorClock EditorClock { get; private set; } = null!;

        [Resolved]
        protected EditorBeatmap EditorBeatmap { get; private set; } = null!;

        [Resolved]
        protected IBeatSnapProvider BeatSnapProvider { get; private set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public override ComposeBlueprintContainer BlueprintContainer => blueprintContainer;
        private ComposeBlueprintContainer blueprintContainer = null!;

        protected ExpandingToolboxContainer LeftToolbox { get; private set; } = null!;

        protected ExpandingToolboxContainer RightToolbox { get; private set; } = null!;

        private DrawableEditorRulesetWrapper<TObject> drawableRulesetWrapper = null!;

        protected readonly Container LayerBelowRuleset = new Container { RelativeSizeAxes = Axes.Both };

        protected InputManager InputManager { get; private set; } = null!;

        private Box leftToolboxBackground = null!;
        private Box rightToolboxBackground = null!;

        private EditorRadioButtonCollection toolboxCollection = null!;
        private FillFlowContainer togglesCollection = null!;
        private FillFlowContainer sampleBankTogglesCollection = null!;

        private IBindable<bool> hasTiming = null!;
        private Bindable<bool> autoSeekOnPlacement = null!;
        private Bindable<bool> advanceByPreviousSpacingOnPlacement = null!;
        private Bindable<bool> liveModPreview = null!;
        private Bindable<bool> alwaysExpandLeftToolbox = null!;

        /// <summary>
        /// Mods being previewed in the composer, if the user opted into previewing their gameplay test mods while editing.
        /// </summary>
        private Mod[] previewMods = Array.Empty<Mod>();
        private readonly Bindable<bool> composerFocusMode = new Bindable<bool>();

        private EditorRadioButton? lastTool;

        protected DrawableRuleset<TObject> DrawableRuleset { get; private set; } = null!;

        protected HitObjectComposer(Ruleset ruleset)
            : base(ruleset)
        {
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent) =>
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [BackgroundDependencyLoader(true)]
        private void load(OsuConfigManager config, Editor? editor, ReadableKeyCombinationProvider keyCombinationProvider)
        {
            autoSeekOnPlacement = config.GetBindable<bool>(OsuSetting.EditorAutoSeekOnPlacement);
            advanceByPreviousSpacingOnPlacement = config.GetBindable<bool>(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement);
            liveModPreview = config.GetBindable<bool>(OsuSetting.EditorLiveModPreview);
            alwaysExpandLeftToolbox = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox);

            if (editor != null)
            {
                composerFocusMode.BindTo(editor.ComposerFocusMode);

                if (config.Get<bool>(OsuSetting.EditorApplyTestPlayModsToComposer))
                {
                    // mods which drive the playfield every frame are written for a gameplay screen and reach for parts of
                    // one the composer has no reason to show — the cursor above all — so previewing them would take the
                    // editor down on the very next frame. What a mapper wants to see is the objects themselves anyway.
                    previewMods = editor.TestPlayMods.Mods.Value
                                        .Where(m => m is not IUpdatableByPlayfield)
                                        .Select(m => m.DeepClone()).ToArray();
                }
            }

            // must happen before the drawable ruleset is constructed so that hit objects are already in their modded state.
            applyBeatmapPreviewMods();

            Config = Dependencies.Get<IRulesetConfigCache>().GetConfigFor(Ruleset);

            try
            {
                var autoplay = Ruleset.GetAutoplayMod();

                DrawableRuleset = CreateDrawableRuleset(Ruleset, EditorBeatmap.PlayableBeatmap,
                    autoplay != null ? previewMods.Prepend(autoplay).ToArray() : previewMods);
                drawableRulesetWrapper = new DrawableEditorRulesetWrapper<TObject>(DrawableRuleset)
                {
                    Clock = EditorClock,
                    ProcessCustomClock = false
                };
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not load beatmap successfully!");
                return;
            }

            if (DrawableRuleset is IDrawableScrollingRuleset scrollingRuleset)
                dependencies.CacheAs(scrollingRuleset.ScrollingInfo);

            dependencies.CacheAs(Playfield);

            string shiftDisplay = keyCombinationProvider.GetReadableString(new KeyCombination(InputKey.Shift));
            string altDisplay = keyCombinationProvider.GetReadableString(new KeyCombination(InputKey.Alt));

            config.BindWith(OsuSetting.EditorDefaultAdditionBank, defaultAdditionBank);

            // a change only takes hold once nothing is selected, which is also the moment the user would look for it.
            defaultAdditionBank.BindValueChanged(_ => resetTernaryStates());

            createSelectionStateBindables();

            InternalChild = new DatabasedKeyBindingContainer<TAction>(Ruleset.RulesetInfo, Ruleset.EDITOR_VARIANT, matchingMode: KeyCombinationMatchingMode.Modifiers)
            {
                Children = new[]
                {
                    PlayfieldContentContainer = new Container
                    {
                        Name = "Playfield content",
                        RelativeSizeAxes = Axes.Y,
                        Children = new Drawable[]
                        {
                            // layers below playfield
                            drawableRulesetWrapper.CreatePlayfieldAdjustmentContainer().WithChild(LayerBelowRuleset),
                            drawableRulesetWrapper,
                            // layers above playfield
                            drawableRulesetWrapper.CreatePlayfieldAdjustmentContainer()
                                                  .WithChild(blueprintContainer = CreateBlueprintContainer())
                        }
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Children = new Drawable[]
                        {
                            leftToolboxBackground = new Box
                            {
                                Colour = colourProvider.Background5,
                                RelativeSizeAxes = Axes.Both,
                            },
                            LeftToolbox = new ExpandingToolboxContainer(TOOLBOX_CONTRACTED_SIZE_LEFT, 200)
                            {
                                Children = new Drawable[]
                                {
                                    new EditorToolboxGroup("tools")
                                    {
                                        Child = toolboxCollection = new EditorRadioButtonCollection { RelativeSizeAxes = Axes.X }
                                    },
                                    new EditorToolboxGroup("toggles (Q~P)")
                                    {
                                        Child = togglesCollection = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 5),
                                        },
                                    },
                                    new EditorToolboxGroup($"bank ({shiftDisplay}/{altDisplay}-Q~R)")
                                    {
                                        Child = new FillFlowContainer
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            AutoSizeAxes = Axes.Y,
                                            Direction = FillDirection.Vertical,
                                            Spacing = new Vector2(0, 5),
                                            Children = new Drawable[]
                                            {
                                                new Container
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Children = new Drawable[]
                                                    {
                                                        new ExpandableSpriteText
                                                        {
                                                            Text = "Normal",
                                                            AlwaysPresent = true,
                                                            AllowMultiline = false,
                                                            RelativePositionAxes = Axes.X,
                                                            X = 0.25f,
                                                            Origin = Anchor.TopCentre,
                                                            Anchor = Anchor.TopLeft,
                                                            Font = OsuFont.GetFont(weight: FontWeight.Regular, size: 17),
                                                        },
                                                        new ExpandableSpriteText
                                                        {
                                                            Text = "Addition",
                                                            AlwaysPresent = true,
                                                            AllowMultiline = false,
                                                            RelativePositionAxes = Axes.X,
                                                            X = 0.75f,
                                                            Origin = Anchor.TopCentre,
                                                            Anchor = Anchor.TopLeft,
                                                            Font = OsuFont.GetFont(weight: FontWeight.Regular, size: 17),
                                                        },
                                                    }
                                                },
                                                sampleBankTogglesCollection = new FillFlowContainer
                                                {
                                                    RelativeSizeAxes = Axes.X,
                                                    AutoSizeAxes = Axes.Y,
                                                    Direction = FillDirection.Vertical,
                                                    Spacing = new Vector2(0, 5),
                                                },
                                            }
                                        }
                                    },
                                }
                            },
                        }
                    },
                    new Container
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        RelativeSizeAxes = Axes.Y,
                        AutoSizeAxes = Axes.X,
                        Children = new Drawable[]
                        {
                            rightToolboxBackground = new Box
                            {
                                Colour = colourProvider.Background5,
                                RelativeSizeAxes = Axes.Both,
                            },
                            RightToolbox = new ExpandingToolboxContainer(TOOLBOX_CONTRACTED_SIZE_RIGHT, 250)
                            {
                                Children = new Drawable[]
                                {
                                    new EditorToolboxGroup("status")
                                    {
                                        Child = new ComposeStatusDisplay()
                                    },
                                    new EditorToolboxGroup("inspector")
                                    {
                                        Child = CreateHitObjectInspector()
                                    },
                                }
                            }
                        }
                    },
                }
            };

            toolboxCollection.AddButton(new HitObjectCompositionToolButton<GlobalAction>(new SelectTool(), toolSelected)
            {
                Hotkey = new Hotkey(GlobalAction.EditorSelectTool)
            });

            foreach (var compositionTool in CompositionTools)
            {
                toolboxCollection.AddButton(new HitObjectCompositionToolButton<TAction>(compositionTool, toolSelected)
                {
                    Hotkey = compositionTool.Action != null
                        ? new Hotkey(Ruleset.ShortName, Ruleset.EDITOR_VARIANT, (int)Convert.ChangeType(compositionTool.Action.Value, typeof(int)))
                        : null,
                });
            }

            togglesCollection.AddRange(CreateTernaryButtons().ToArray());

            sampleBankTogglesCollection.AddRange(BlueprintContainer.SampleBankTernaryStates);

            SetSelectTool();

            EditorBeatmap.SelectedHitObjects.CollectionChanged += selectionChanged;

            // bring in updates from selection changes
            EditorBeatmap.HitObjectUpdated += hitObjectUpdated;
        }

        /// <summary>
        /// Houses all content relevant to the playfield.
        /// </summary>
        /// <remarks>
        /// Generally implementations should not be adding to this directly.
        /// Use <see cref="LayerBelowRuleset"/> or <see cref="BlueprintContainer"/> instead.
        /// </remarks>
        protected Container PlayfieldContentContainer { get; private set; } = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            InputManager = GetContainingInputManager()!;

            LeftToolbox.AlwaysExpanded.BindTo(alwaysExpandLeftToolbox);

            if (previewMods.OfType<IApplicableToBeatmap>().Any())
                EditorBeatmap.BeatmapReprocessed += onBeatmapReprocessed;

            hasTiming = EditorBeatmap.HasTiming.GetBoundCopy();
            hasTiming.BindValueChanged(timing =>
            {
                // it's important this is performed before the similar code in EditorRadioButton disables the button.
                if (!timing.NewValue)
                    SetSelectTool();
            });

            EditorBeatmap.HasTiming.BindValueChanged(hasTiming =>
            {
                foreach (var item in toolboxCollection.Items)
                {
                    item.Selected.Disabled = !hasTiming.NewValue;
                }
            }, true);

            composerFocusMode.BindValueChanged(_ =>
            {
                // Transforms should be kept in sync with other usages of composer focus mode.
                if (!composerFocusMode.Value)
                {
                    leftToolboxBackground.FadeIn(750, Easing.OutQuint);
                    rightToolboxBackground.FadeIn(750, Easing.OutQuint);
                }
                else
                {
                    leftToolboxBackground.Delay(600).FadeTo(0.5f, 4000, Easing.OutQuint);
                    rightToolboxBackground.Delay(600).FadeTo(0.5f, 4000, Easing.OutQuint);
                }
            }, true);
        }

        protected override void Update()
        {
            base.Update();

            if (ApplyHorizontalCentering)
            {
                PlayfieldContentContainer.Anchor = Anchor.Centre;
                PlayfieldContentContainer.Origin = Anchor.Centre;

                // Ensure that the playfield is always centered but also doesn't get cut off by toolboxes.
                PlayfieldContentContainer.Width = Math.Max(1024, DrawWidth) - TOOLBOX_CONTRACTED_SIZE_RIGHT * 2;
                PlayfieldContentContainer.X = 0;
            }
            else
            {
                PlayfieldContentContainer.Anchor = Anchor.CentreLeft;
                PlayfieldContentContainer.Origin = Anchor.CentreLeft;

                PlayfieldContentContainer.Width = Math.Max(1024, DrawWidth);
                PlayfieldContentContainer.X = LeftToolbox.DrawWidth;
            }

            composerFocusMode.Value = PlayfieldContentContainer.Contains(InputManager.CurrentState.Mouse.Position)
                                      && !LeftToolbox.Contains(InputManager.CurrentState.Mouse.Position)
                                      && !RightToolbox.Contains(InputManager.CurrentState.Mouse.Position);

            flashSamplesPlayingNow();
        }

        private double? lastFlashedTime;

        /// <summary>
        /// Highlights the sample toolbox buttons for hit sounds passing under the playhead, so the additions in use
        /// can be read off while listening rather than by inspecting each object.
        /// </summary>
        private void flashSamplesPlayingNow()
        {
            // load() bails out early if the beatmap could not be loaded, leaving the blueprint container unset.
            if (blueprintContainer == null)
                return;

            if (!EditorClock.IsRunning)
            {
                lastFlashedTime = null;
                return;
            }

            double currentTime = EditorClock.CurrentTime;
            double previousTime = lastFlashedTime ?? currentTime;

            lastFlashedTime = currentTime;

            // only forward playback is handled; seeking backwards would otherwise flash everything jumped over.
            if (currentTime <= previousTime)
                return;

            foreach (var hitObject in EditorBeatmap.HitObjects)
            {
                if (hitObject.StartTime > currentTime)
                    break;

                // sliders keep sounding past their start time, so an object is only skipped once it has finished
                // before the window. Otherwise a slider's repeats and tail would never be reached.
                if (hitObject.GetEndTime() < previousTime)
                    continue;

                flashSamplesInWindow(hitObject, previousTime, currentTime);
            }
        }

        private void flashSamplesInWindow(HitObject hitObject, double previousTime, double currentTime)
        {
            if (hitObject.StartTime > previousTime && hitObject.StartTime <= currentTime)
                flashSamples(hitObject.Samples);

            // the final node's samples (a slider's tail) are not carried by any nested object: the slider itself sounds
            // them at its end time. See `Slider.UpdateNestedSamples()`.
            if (hitObject is IHasRepeats repeats && repeats.NodeSamples.Count > 0)
            {
                double endTime = hitObject.GetEndTime();

                if (endTime > previousTime && endTime <= currentTime)
                    flashSamples(repeats.NodeSamples[^1]);
            }

            // slider heads, repeats and ticks do carry their own samples.
            foreach (var nested in hitObject.NestedHitObjects)
                flashSamplesInWindow(nested, previousTime, currentTime);
        }

        private void flashSamples(IEnumerable<HitSampleInfo> samples)
        {
            foreach (var sample in samples)
            {
                if (blueprintContainer.SampleTernaryButtons.TryGetValue(sample.Name, out var button))
                    button.Flash();
            }
        }

        public override Playfield Playfield => drawableRulesetWrapper.Playfield;

        public override IEnumerable<DrawableHitObject> HitObjects => drawableRulesetWrapper.Playfield.AllHitObjects;

        public override bool CursorInPlacementArea => drawableRulesetWrapper.Playfield.ReceivePositionalInputAt(InputManager.CurrentState.Mouse.Position);

        /// <summary>
        /// Defines all available composition tools, listed on the left side of the editor screen as button controls.
        /// This should usually define one tool for each <see cref="HitObject"/> type used in the target ruleset.
        /// </summary>
        /// <remarks>
        /// A "select" tool is automatically added as the first tool.
        /// </remarks>
        protected abstract IReadOnlyList<CompositionTool<TAction>> CompositionTools { get; }

        /// <summary>
        /// Create all ternary states required to be displayed to the user.
        /// </summary>
        protected virtual IEnumerable<Drawable> CreateTernaryButtons() => BlueprintContainer.MainTernaryStates;

        /// <summary>
        /// Construct a relevant blueprint container. This will manage hitobject selection/placement input handling and display logic.
        /// </summary>
        protected abstract ComposeBlueprintContainer CreateBlueprintContainer();

        protected virtual Drawable CreateHitObjectInspector() => new HitObjectInspector();

        /// <summary>
        /// Construct a drawable ruleset for the provided ruleset.
        /// </summary>
        /// <remarks>
        /// Can be overridden to add editor-specific logical changes to a <see cref="Ruleset"/>'s standard <see cref="DrawableRuleset{TObject}"/>.
        /// For example, hit animations or judgement logic may be changed to give a better editor user experience.
        /// </remarks>
        /// <param name="ruleset">The ruleset used to construct its drawable counterpart.</param>
        /// <param name="beatmap">The loaded beatmap.</param>
        /// <param name="mods">The mods to be applied.</param>
        /// <returns>An editor-relevant <see cref="DrawableRuleset{TObject}"/>.</returns>
        protected virtual DrawableRuleset<TObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => (DrawableRuleset<TObject>)ruleset.CreateDrawableRulesetWith(beatmap, mods);

        #region Tool selection logic

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.ControlPressed || e.SuperPressed)
                return false;

            if (checkToggleMappingFromKey(e.Key, out int rightIndex))
            {
                if (e.ShiftPressed || e.AltPressed)
                {
                    if (sampleBankTogglesCollection.ElementAtOrDefault(rightIndex) is SampleBankTernaryButton sampleBankTernaryButton)
                    {
                        if (e.ShiftPressed)
                            sampleBankTernaryButton.NormalButton.Toggle();

                        if (e.AltPressed)
                            sampleBankTernaryButton.AdditionsButton.Toggle();

                        return true;
                    }
                }
                else
                {
                    if (togglesCollection.ChildrenOfType<DrawableTernaryButton>().ElementAtOrDefault(rightIndex) is DrawableTernaryButton button)
                    {
                        button.Toggle();
                        return true;
                    }
                }
            }

            return base.OnKeyDown(e);
        }

        private bool checkToggleMappingFromKey(Key key, out int index)
        {
            switch (key)
            {
                case Key.Q:
                    index = 0;
                    break;

                case Key.W:
                    index = 1;
                    break;

                case Key.E:
                    index = 2;
                    break;

                case Key.R:
                    index = 3;
                    break;

                case Key.T:
                    index = 4;
                    break;

                case Key.Y:
                    index = 5;
                    break;

                case Key.U:
                    index = 6;
                    break;

                case Key.I:
                    index = 7;
                    break;

                case Key.O:
                    index = 8;
                    break;

                case Key.P:
                    index = 9;
                    break;

                default:
                    index = -1;
                    break;
            }

            return index >= 0;
        }

        private void selectionChanged(object? sender, NotifyCollectionChangedEventArgs changedArgs)
        {
            if (EditorBeatmap.SelectedHitObjects.Any())
            {
                // ensure in selection mode if a selection is made.
                SetSelectTool();
                Scheduler.AddOnce(UpdateTernaryStates);
            }
            else
                // Reset the ternary states when the selection is cleared.
                Scheduler.AddOnce(resetTernaryStates);
        }

        private void hitObjectUpdated(HitObject _)
        {
            // bring in updates from selection changes
            Scheduler.AddOnce(UpdateTernaryStates);
        }

        public void SetSelectTool() => toolboxCollection.Items.First().Select();

        public void SetLastTool() => (lastTool ?? toolboxCollection.Items.First()).Select();

        /// <summary>
        /// Takes a tool's button out of the toolbox, or puts it back.
        /// </summary>
        /// <remarks>
        /// A hidden tool takes up no room and cannot be reached by its number key either. A tool is hidden precisely
        /// because it is in the way, and a stray keypress switching to it would leave the mapper composing with a tool
        /// they cannot see they are holding.
        /// </remarks>
        protected void SetToolVisibility(CompositionTool tool, bool visible)
        {
            var button = toolboxCollection.Items.OfType<HitObjectCompositionToolButton>().Single(b => b.Tool == tool);

            if (visible)
            {
                button.Show();
                return;
            }

            if (button.Selected.Value)
                SetSelectTool();

            button.Hide();
        }

        private void toolSelected(CompositionTool tool)
        {
            lastTool = toolboxCollection.Items.OfType<HitObjectCompositionToolButton>().FirstOrDefault(i => i.Tool == BlueprintContainer.CurrentTool);

            BlueprintContainer.CurrentTool = tool;

            if (!(tool is SelectTool))
                EditorBeatmap.SelectedHitObjects.Clear();
        }

        #endregion

        #region IPlacementHandler

        public void ShowPlacement(HitObject hitObject)
        {
            EditorBeatmap.PlacementObject.Value = hitObject;
        }

        public void HidePlacement()
        {
            EditorBeatmap.PlacementObject.Value = null;
        }

        public void CommitPlacement(HitObject hitObject)
        {
            EditorBeatmap.PlacementObject.Value = null;

            EditorBeatmap.BeginChange();

            foreach (var h in EditorBeatmap.HitObjects.Where(ho => blueprintContainer.CurrentHitObjectPlacement?.ReplacesExistingObject(ho) == true).ToArray())
                EditorBeatmap.Remove(h);
            EditorBeatmap.Add(hitObject);
            EditorBeatmap.EndChange();

            if (autoSeekOnPlacement.Value && EditorClock.CurrentTime < hitObject.StartTime)
                EditorClock.SeekSmoothlyTo(hitObject.StartTime);

            if (advanceByPreviousSpacingOnPlacement.Value)
                advanceByPreviousSpacing(hitObject);
        }

        // deferred to the next frame both to avoid re-entering `EditorBeatmap`'s own processing and to coalesce bursts of edits.
        private void onBeatmapReprocessed()
        {
            if (liveModPreview.Value)
                Scheduler.AddOnce(applyBeatmapPreviewMods);
        }

        public override void RevertBeatmapPreviewMods()
        {
            if (!previewMods.OfType<IApplicableToBeatmap>().Any())
                return;

            foreach (var hitObject in EditorBeatmap.PlayableBeatmap.HitObjects)
                hitObject.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.PlayableBeatmap.Difficulty);
        }

        /// <summary>
        /// Applies the beatmap-level effects of <see cref="previewMods"/> to the beatmap being edited, so that the composer
        /// shows what gameplay would look like with those mods active.
        /// </summary>
        /// <remarks>
        /// These mods mutate hit object properties in place (freeze frame, for instance, extends each object's preempt time),
        /// so everything is reset to its unmodded state first. Without that, repeated application would compound.
        /// </remarks>
        private void applyBeatmapPreviewMods()
        {
            var beatmapMods = previewMods.OfType<IApplicableToBeatmap>().ToArray();

            if (beatmapMods.Length == 0)
                return;

            foreach (var hitObject in EditorBeatmap.PlayableBeatmap.HitObjects)
                hitObject.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.PlayableBeatmap.Difficulty);

            foreach (var mod in beatmapMods)
                mod.ApplyToBeatmap(EditorBeatmap.PlayableBeatmap);
        }

        /// <summary>
        /// Seeks forward by the spacing between the just-placed object and the one before it, so that the playhead lands
        /// where an object continuing the same rhythm would go. This allows placing a constant rhythm without touching the timeline.
        /// </summary>
        private void advanceByPreviousSpacing(HitObject placed)
        {
            // deliberately compares against start times only: a constant rhythm is defined by when objects begin, not when they end.
            HitObject? precedingObject = null;

            foreach (var h in EditorBeatmap.HitObjects)
            {
                if (h.StartTime >= placed.StartTime || h == placed)
                    continue;

                if (precedingObject == null || h.StartTime > precedingObject.StartTime)
                    precedingObject = h;
            }

            // nothing to infer a spacing from yet.
            if (precedingObject == null)
                return;

            // measured from the end of the preceding object rather than its start, so that a slider's length doesn't
            // get counted as part of the gap (which would overshoot when placing after one).
            double spacing = placed.StartTime - precedingObject.GetEndTime();

            if (spacing <= 0)
                return;

            EditorClock.SeekSmoothlyTo(BeatSnapProvider.SnapTime(placed.GetEndTime() + spacing));
        }

        public void Delete(HitObject hitObject) => EditorBeatmap.Remove(hitObject);

        #endregion

        #region IPositionSnapProvider

        /// <summary>
        /// Retrieve the relevant <see cref="Playfield"/> at a specified screen-space position.
        /// In cases where a ruleset doesn't require custom logic (due to nested playfields, for example)
        /// this will return the ruleset's main playfield.
        /// </summary>
        /// <param name="screenSpacePosition">The screen-space position to query.</param>
        /// <returns>The most relevant <see cref="Playfield"/>.</returns>
        protected virtual Playfield PlayfieldAtScreenSpacePosition(Vector2 screenSpacePosition) => drawableRulesetWrapper.Playfield;

        #endregion

        protected override void Dispose(bool isDisposing)
        {
            if (EditorBeatmap.IsNotNull())
            {
                EditorBeatmap.HitObjectUpdated -= hitObjectUpdated;
                EditorBeatmap.SelectedHitObjects.CollectionChanged -= selectionChanged;
                EditorBeatmap.BeatmapReprocessed -= onBeatmapReprocessed;
            }

            base.Dispose(isDisposing);
        }
    }

    /// <summary>
    /// A non-generic definition of a HitObject composer class.
    /// Generally used to access certain methods without requiring a generic type for <see cref="HitObjectComposer{TObject,TAction}" />.
    /// </summary>
    [Cached]
    public abstract partial class HitObjectComposer : CompositeDrawable
    {
        /// <summary>
        /// Puts the beatmap back the way it was before any mods were previewed on it.
        /// </summary>
        /// <remarks>
        /// Has to be called while this composer is still in use, on the update thread. Leaving it to disposal is not an
        /// option: that happens on a thread of its own, and the drawables still bound to these hit objects react to
        /// being given fresh defaults.
        /// </remarks>
        public virtual void RevertBeatmapPreviewMods()
        {
        }

        public const float TOOLBOX_CONTRACTED_SIZE_LEFT = 60;
        public const float TOOLBOX_CONTRACTED_SIZE_RIGHT = 120;

        public readonly Ruleset Ruleset;

        protected HitObjectComposer(Ruleset ruleset)
        {
            Ruleset = ruleset;
            RelativeSizeAxes = Axes.Both;
        }

        /// <summary>
        /// The target ruleset's playfield.
        /// </summary>
        public abstract Playfield Playfield { get; }

        public abstract ComposeBlueprintContainer BlueprintContainer { get; }

        /// <summary>
        /// All <see cref="DrawableHitObject"/>s in currently loaded beatmap.
        /// </summary>
        public abstract IEnumerable<DrawableHitObject> HitObjects { get; }

        /// <summary>
        /// Whether the user's cursor is currently in an area of the <see cref="HitObjectComposer"/> that is valid for placement.
        /// </summary>
        public abstract bool CursorInPlacementArea { get; }

        /// <summary>
        /// Returns a string representing the current selection.
        /// The inverse method to <see cref="SelectFromTimestamp"/>.
        /// </summary>
        public virtual string ConvertSelectionToString() => string.Empty;

        /// <summary>
        /// Selects objects based on the supplied <paramref name="timestamp"/> and <paramref name="objectDescription"/>.
        /// The inverse method to <see cref="ConvertSelectionToString"/>.
        /// </summary>
        /// <param name="timestamp">The time instant to seek to, in milliseconds.</param>
        /// <param name="objectDescription">The ruleset-specific description of objects to select at the given timestamp.</param>
        public virtual void SelectFromTimestamp(double timestamp, string objectDescription) { }
    }
}
