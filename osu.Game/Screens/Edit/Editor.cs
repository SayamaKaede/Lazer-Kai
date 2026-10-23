// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Bindables;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Framework.Timing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Timing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.Multiplayer;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.OSD;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Screens.Edit.ComboColours;
using osu.Game.Screens.Edit.Presets;
using osu.Game.Screens.Edit.DifficultyGraph;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.Discussion;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Screens.Edit.Compose;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.Design;
using osu.Game.Screens.Edit.GameplayTest;
using osu.Game.Screens.Edit.Setup;
using osu.Game.Screens.Edit.Submission;
using osu.Game.Screens.Edit.GreenLines;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Screens.Edit.Timing;
using osu.Game.Screens.Edit.Verify;
using osu.Game.Screens.OnlinePlay;
using osu.Game.Users;
using osu.Game.Utils;
using WebCommonStrings = osu.Game.Resources.Localisation.Web.CommonStrings;

namespace osu.Game.Screens.Edit
{
    [Cached(typeof(IBeatSnapProvider))]
    [Cached]
    public partial class Editor : OsuScreen, IKeyBindingHandler<GlobalAction>, IKeyBindingHandler<PlatformAction>, IBeatSnapProvider, ISamplePlaybackDisabler, IBeatSyncProvider
    {
        /// <summary>
        /// An offset applied to waveform visuals to align them with expectations.
        /// </summary>
        /// <remarks>
        /// Historically, osu! beatmaps have an assumption of full system latency baked in.
        /// This comes from a culmination of stable's platform offset, average hardware playback
        /// latency, and users having their universal offsets tweaked to previous beatmaps.
        ///
        /// Coming to this value involved running various tests with existing users / beatmaps.
        /// This included both visual and audible comparisons. Ballpark confidence is ≈2 ms.
        /// </remarks>
        public const float WAVEFORM_VISUAL_OFFSET = 20;

        public override float BackgroundParallaxAmount => 0.1f;

        public override bool HideOverlaysOnEnter => true;

        public override bool DisallowExternalBeatmapRulesetChanges => true;

        public override bool? ApplyModTrackAdjustments => false;

        protected override bool PlayExitSound => !ExitConfirmed && !switchingDifficulty;

        protected bool HasUnsavedChanges
        {
            get
            {
                if (!canSave)
                    return false;

                return lastSavedHash != changeHandler?.CurrentStateHash;
            }
        }

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        [Resolved]
        private RulesetStore rulesets { get; set; }

        [Resolved(canBeNull: true)]
        private IDialogOverlay dialogOverlay { get; set; }

        [Resolved(CanBeNull = true)]
        private IOverlayManager overlayManager { get; set; }

        [Resolved(CanBeNull = true)]
        private TestPlayModCarryOver modCarryOver { get; set; }

        [Resolved(canBeNull: true)]
        private INotificationOverlay notifications { get; set; }

        [Resolved(canBeNull: true)]
        [CanBeNull]
        private VolumeOverlay volumeOverlay { get; set; }

        [Resolved(canBeNull: true)]
        [CanBeNull]
        private LoginOverlay loginOverlay { get; set; }

        [Resolved]
        private RealmAccess realm { get; set; }

        public readonly Bindable<EditorScreenMode> Mode = new Bindable<EditorScreenMode>();

        public IBindable<bool> SamplePlaybackDisabled => samplePlaybackDisabled;

        /// <summary>
        /// Ensure all asynchronously loading pieces of the editor are in a good state.
        /// This exists here for convenience for tests, not for actual use.
        /// Eventually we'd probably want a better way to signal this.
        /// </summary>
        public bool ReadyForUse
        {
            get
            {
                if (!workingBeatmapUpdated)
                    return false;

                if (currentScreen?.IsLoaded != true)
                    return false;

                if (currentScreen is EditorScreenWithTimeline)
                    return currentScreen.ChildrenOfType<TimelineArea>().FirstOrDefault()?.IsLoaded == true;

                return true;
            }
        }

        private bool workingBeatmapUpdated;

        private readonly Bindable<bool> samplePlaybackDisabled = new Bindable<bool>();

        /// <summary>
        /// Whether hitsounds and the music are heard. Deliberately not persisted: silence is something a mapper turns on
        /// to get through a passage, not a state they should find waiting for them next time.
        /// </summary>
        private readonly BindableBool hitsoundsAudible = new BindableBool(true);

        private readonly BindableBool musicAudible = new BindableBool(true);

        /// <summary>
        /// Laid over the track's volume rather than replacing it, so the volume the user set is never touched and comes
        /// back exactly as it was — including any change made while the music was off.
        /// </summary>
        private readonly BindableDouble trackMute = new BindableDouble();

        /// <summary>
        /// The track <see cref="trackMute"/> is currently applied to, if any.
        /// </summary>
        private Track mutedTrack;

        /// <summary>
        /// Whether the editor currently owns the track. False while a gameplay test is in front, which is why muting
        /// the music here does not follow the mapper into the test.
        /// </summary>
        private bool trackHeld;

        private bool canSave;
        private readonly List<MenuItem> saveRelatedMenuItems = new List<MenuItem>();

        /// <summary>
        /// Holds on to the file of a beatmap opened in stable's format, while stable compatibility is on and the
        /// beatmap can still be saved in that format.
        /// </summary>
        [CanBeNull]
        private StableFileKeeper stableFileKeeper;

        /// <summary>
        /// The format version the next save keeps the beatmap in, while it is kept in stable's; null while the save
        /// is in lazer's.
        /// </summary>
        public IBindable<int?> KeptStableVersion => keptStableVersion;

        private readonly Bindable<int?> keptStableVersion = new Bindable<int?>();

        private readonly SavedTimingLines savedTimingLines = new SavedTimingLines();

        /// <summary>
        /// Tracks ongoing mutually-exclusive operations related to changing the beatmap
        /// (e.g. save, export).
        /// </summary>
        public OngoingOperationTracker MutationTracker { get; } = new OngoingOperationTracker();

        protected bool ExitConfirmed { get; private set; }

        private bool switchingDifficulty;

        private string lastSavedHash;
        private EditorMenuItem discardChangesMenuItem;

        private TestPlayModsController testPlayModsController;

        private EditorModSelectOverlay modSelect;

        /// <summary>
        /// The mod screen is not part of the editor's own hierarchy: it is handed to the game so it lands in the layer
        /// that carries the footer its buttons live on. This releases it again.
        /// </summary>
        private IDisposable modSelectRegistration;

        /// <summary>
        /// Mods which will be applied when entering gameplay test.
        /// </summary>
        public TestPlayModsController TestPlayMods => testPlayModsController;

        private ScreenContainer screenContainer;

        [CanBeNull]
        private readonly EditorLoader loader;

        private EditorScreen currentScreen;

        /// <summary>
        /// The composer the screen currently being shown has, if it has one.
        /// </summary>
        /// <remarks>
        /// The composer is cached inside the compose screen rather than here, so anything sitting alongside the
        /// screens — the panels the menus open — has to reach it this way.
        /// </remarks>
        internal HitObjectComposer CurrentComposer => currentScreen?.ChildrenOfType<HitObjectComposer>().FirstOrDefault();

        private readonly BindableBeatDivisor beatDivisor = new BindableBeatDivisor();
        private EditorClock clock;

        private IBeatmap playableBeatmap;
        private EditorBeatmap editorBeatmap;
        private EditorReferenceDifficulty referenceDifficulty;

        private BottomBar bottomBar;
        private ComboColourStudioOverlay comboColourStudio;

        private CollabOverlay collab;

        private DiscussionOverlay discussion;

        private PlacementPresetOverlay placementPresets;

        private EditorMenuItem placementPresetsMenuItem;

        /// <summary>
        /// Whether the ruleset being edited can place presets, once its composer has loaded and been asked.
        /// </summary>
        /// <remarks>
        /// Null until then. The ruleset does not change while the editor is open, so this is worked out once and the
        /// menu item left as it was set.
        /// </remarks>
        private bool? placementPresetsSupported;

        private DifficultyGraphDetailOverlay difficultyGraphDetail;

        private GreenLinePanel greenLinePanel;

        [Resolved]
        private OsuConfigManager osuConfig { get; set; }

        /// <summary>
        /// The time and volume of every red and green line as the editor has them, for test play.
        /// </summary>
        internal IEnumerable<(double time, int volume)> LineVolumes => savedTimingLines.Lines.Select(l => (l.Time, l.Volume)).ToArray();

        /// <summary>
        /// Opens or closes the green line panel. The key that shows what is playing everywhere else does this here,
        /// the way it opens the timing panel in stable's editor.
        /// </summary>
        public void ToggleGreenLinePanel() => greenLinePanel?.ToggleVisibility();

        [CanBeNull] // Should be non-null once it can support custom rulesets.
        private EditorChangeHandler changeHandler;

        private EditorAsmr asmr;

        private DependencyContainer dependencies;

        private bool isNewBeatmap;

        protected override UserActivity InitialActivity => getCurrentUserActivity();

        private UserActivity getCurrentUserActivity()
        {
            if (Beatmap.Value.Metadata.Author.OnlineID == api.LocalUser.Value.OnlineID)
                return new UserActivity.EditingBeatmap(Beatmap.Value.BeatmapInfo);

            return new UserActivity.ModdingBeatmap(Beatmap.Value.BeatmapInfo);
        }

        protected override bool InitialBackButtonVisibility => false;

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
            => dependencies = new DependencyContainer(base.CreateChildDependencies(parent));

        [Resolved]
        private IAPIProvider api { get; set; }

        [Cached]
        public readonly EditorClipboard Clipboard = new EditorClipboard();

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Aquamarine);

        private Bindable<OverlayColourScheme> editorColourScheme;

        private Bindable<bool> carryOverMods;

        [Resolved(canBeNull: true)]
        private OnScreenDisplay onScreenDisplay { get; set; }

        private Bindable<float> editorBackgroundDim;
        private Bindable<bool> editorShowStoryboard;
        private Bindable<bool> editorHitMarkers;
        private Bindable<bool> editorAutoSeekOnPlacement;
        private Bindable<bool> editorLimitedDistanceSnap;
        private Bindable<bool> editorTimelineShowTimingChanges;
        private Bindable<bool> editorTimelineShowBreaks;
        private Bindable<bool> editorTimelineShowTicks;
        private Bindable<bool> editorContractSidebars;
        private Bindable<bool> editorAdvanceByPreviousSpacing;
        private Bindable<bool> editorFreehandSliderPlacement;
        private Bindable<bool> editorApplyTestPlayModsToComposer;
        private Bindable<bool> editorLiveModPreview;
        private Bindable<bool> editorAlwaysExpandTimelinePointLabels;
        private Bindable<bool> editorHideTimelinePoints;
        private Bindable<EditorSelectionColour> editorSelectionColour;
        private Bindable<float> editorSelectionThickness;
        private Bindable<float> editorSelectionOpacity;
        private Bindable<EditorSelectionColour> editorObjectOutlineColour;
        private Bindable<float> editorObjectOutlineThickness;
        private Bindable<float> editorObjectOutlineOpacity;
        private Bindable<float> editorSelectionHandleScale;
        private Bindable<EditorSelectionColour> editorSelectionHandleColour;
        private Bindable<float> editorSliderAnchorScale;
        private Bindable<double> editorSeekAnimationDuration;
        private Bindable<bool> editorAlwaysExpandLeftToolbox;
        private Bindable<SliderPlacementAnchorType> editorPlacementAnchorType;
        private Bindable<bool> editorShowPlayAreaBorder;
        private Bindable<bool> editorShowAutoCursor;
        private Bindable<bool> editorDetectOffscreen;
        private Bindable<bool> editorShowTimeline;
        private Bindable<bool> editorShowDifficultyGraph;
        private Bindable<bool> editorAutoBackup;
        private Bindable<bool> editorShowVolumeGraph;

        private Bindable<bool> editorHideTimelineVolume;

        private EditorAutoBackup autoBackup;

        [Resolved]
        private osu.Framework.Platform.Storage storage { get; set; }

        private SliderVelocityPresetStore sliderVelocityPresetStore => new SliderVelocityPresetStore(storage);

        private MappingTimeRecorder mappingTime;
        private RunningTimeRecorder openTime;

        /// <summary>
        /// This controls the opacity of components like the timelines, sidebars, etc.
        /// In "composer focus" mode the opacity of the aforementioned components is reduced so that the user can focus on the composer better.
        /// </summary>
        /// <remarks>
        /// The state of this bindable is controlled by <see cref="HitObjectComposer"/> when in <see cref="EditorScreenMode.Compose"/> mode.
        /// </remarks>
        public Bindable<bool> ComposerFocusMode { get; } = new Bindable<bool>();

        [CanBeNull]
        public event Action<double> ShowSampleEditPopoverRequested;

        public Editor(EditorLoader loader = null)
        {
            this.loader = loader;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            editorColourScheme = config.GetBindable<OverlayColourScheme>(OsuSetting.EditorColourScheme);
            carryOverMods = config.GetBindable<bool>(OsuSetting.EditorCarryOverSongSelectMods);

            // `OverlayColourProvider` does not signal drawables when its hue changes, so this is only applied on load.
            // A change to the setting therefore takes effect the next time the editor is opened.
            colourProvider.ChangeColourScheme(editorColourScheme.Value);

            var loadableBeatmap = Beatmap.Value;

            if (loadableBeatmap is DummyWorkingBeatmap)
            {
                Logger.Log("Editor was loaded without a valid beatmap; creating a new beatmap.");

                isNewBeatmap = true;

                loadableBeatmap = beatmapManager.CreateNew(Ruleset.Value, api.LocalUser.Value);

                // required so we can get the track length in EditorClock.
                // this is ONLY safe because the track being provided is a `TrackVirtual` which we don't really care about disposing.
                loadableBeatmap.LoadTrack();

                // this is a bit haphazard, but guards against setting the lease Beatmap bindable if
                // the editor has already been exited.
                if (!ValidForPush)
                {
                    beatmapManager.Delete(loadableBeatmap.BeatmapSetInfo);
                    return;
                }
            }

            try
            {
                playableBeatmap = loadableBeatmap.GetPlayableBeatmap(loadableBeatmap.BeatmapInfo.Ruleset);

                // clone these locally for now to avoid incurring overhead on GetPlayableBeatmap usages.
                // eventually we will want to improve how/where this is done as there are issues with *not* cloning it in all cases.
                playableBeatmap.ControlPointInfo = playableBeatmap.ControlPointInfo.DeepClone();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not load beatmap successfully!");
                // couldn't load, hard abort!
                this.Exit();
                return;
            }

            // Todo: should probably be done at a DrawableRuleset level to share logic with Player.
            clock = new EditorClock(playableBeatmap, beatDivisor);
            clock.ChangeSource(loadableBeatmap.Track);

            dependencies.CacheAs(clock);
            AddInternal(clock);

            clock.SeekingOrStopped.BindValueChanged(_ => updateSampleDisabledState());

            // todo: remove caching of this and consume via editorBeatmap?
            dependencies.Cache(beatDivisor);

            AddInternal(editorBeatmap = new EditorBeatmap(playableBeatmap, loadableBeatmap.GetSkin(), loadableBeatmap.Storyboard, loadableBeatmap.BeatmapInfo));
            dependencies.CacheAs(editorBeatmap);

            AddInternal(referenceDifficulty = new EditorReferenceDifficulty());
            dependencies.CacheAs(referenceDifficulty);

            editorBeatmap.UpdateInProgress.BindValueChanged(_ => updateSampleDisabledState());

            canSave = editorBeatmap.BeatmapInfo.Ruleset.CreateInstance() is ILegacyRuleset;

            byte[] savedFile = readSavedFile(loadableBeatmap);

            AddInternal(savedTimingLines);
            dependencies.Cache(savedTimingLines);
            dependencies.CacheAs<ILineVolumes>(savedTimingLines);

            // a difficulty made to hitsound in, whose lanes are its sounds: one that has lanes kept for it. One hitsounded
            // in stable has something kept for it too once it has been turned into a hitsound difficulty, and is not one.
            if (canSave && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == mania_ruleset_id
                        && new LaneSoundProjectStore(storage).Load(editorBeatmap.BeatmapInfo) is LaneSoundProject lanes && !lanes.NotesSoundAsThemselves)
            {
                AddInternal(laneSounds = new LaneSounds(lanes));
                dependencies.Cache(laneSounds);
            }

            // before the first state is taken, which the lines are a part of.
            if (savedFile != null)
                savedTimingLines.Update(savedFile);

            savedInStableFormat.Value = savedFile != null && StableFileKeeper.IsStableFile(savedFile);

            // not osu!taiko or osu!catch (2026-10-23): stable compatibility is on from the start, and there an edit to an object
            // cannot stay in stable's format, so every first save of a session asked about lazer's instead, test play
            // included, for nothing kept in exchange. osu!mania keeps it: a difficulty hitsounded in stable is turned into
            // a hitsound difficulty from the file as stable wrote it.
            bool keepingStableFile = canSave && savedInStableFormat.Value && config.Get<bool>(OsuSetting.StableCompatibility)
                                     && editorBeatmap.BeatmapInfo.Ruleset.OnlineID is 0 or mania_ruleset_id;

            // the presets kept beside the file, where they are newer than the file's own: always, for a file kept in
            // stable's format, which has no line for them. Put back before the first state is taken, so that having them
            // is not an edit.
            if (canSave && sliderVelocityPresetStore.Load(editorBeatmap.BeatmapInfo) is double[] keptPresets
                        && (keepingStableFile || editorBeatmap.BeatmapInfo.LastLocalUpdate == null
                                              || sliderVelocityPresetStore.LastWritten(editorBeatmap.BeatmapInfo) > editorBeatmap.BeatmapInfo.LastLocalUpdate))
            {
                editorBeatmap.SliderVelocityPresets.Clear();
                editorBeatmap.SliderVelocityPresets.AddRange(keptPresets);
            }

            // kept as they change rather than on saving: they are how the mapper places sliders, as much as a part of the
            // beatmap.
            if (canSave)
            {
                editorBeatmap.SliderVelocityPresets.BindCollectionChanged((_, _) =>
                    sliderVelocityPresetStore.Save(editorBeatmap.BeatmapInfo, editorBeatmap.SliderVelocityPresets.ToArray()));
            }

            if (canSave)
            {
                changeHandler = new BeatmapEditorChangeHandler(editorBeatmap, savedTimingLines);
                dependencies.CacheAs<IEditorChangeHandler>(changeHandler);
            }

            beatDivisor.SetArbitraryDivisor(editorBeatmap.BeatmapInfo.BeatDivisor);
            beatDivisor.BindValueChanged(divisor => editorBeatmap.BeatmapInfo.BeatDivisor = divisor.NewValue);

            updateLastSavedHash();

            if (keepingStableFile)
            {
                stableFileKeeper = new StableFileKeeper(savedFile, editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard);
                keptStableVersion.Value = stableFileKeeper.Version;

                // the lines a save puts in for the velocities, shown as they come about. Only an osu! beatmap's objects
                // are written into the file.
                if (editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0)
                    savedTimingLines.ShowVelocityLinesOf(editorBeatmap);
            }

            Schedule(() =>
            {
                // we need to avoid changing the beatmap from an asynchronous load thread. it can potentially cause weirdness including crashes.
                // this assumes that nothing during the rest of this load() method is accessing Beatmap.Value (loadableBeatmap should be preferred).
                // generally this is quite safe, as the actual load of editor content comes after menuBar.Mode.ValueChanged is fired in its own LoadComplete.
                Beatmap.Value = loadableBeatmap;
                workingBeatmapUpdated = true;
            });

            var bookmarkController = new BookmarkController();
            AddInternal(bookmarkController);

            testPlayModsController = new TestPlayModsController(config, editorBeatmap.BeatmapInfo.Ruleset);

            LoadComponent(modSelect = new EditorModSelectOverlay(editorColourScheme.Value));

            var difficultyTracker = new EditorDifficultyTracker();
            AddInternal(difficultyTracker);
            dependencies.Cache(difficultyTracker);

            var kiaiFlashSuppression = new KiaiFlashSuppression();
            config.BindWith(OsuSetting.EditorHideKiaiFlash, kiaiFlashSuppression);
            dependencies.Cache(kiaiFlashSuppression);

            var modRatePreview = new EditorModRatePreview();
            AddInternal(modRatePreview);
            dependencies.Cache(modRatePreview);

            AddInternal(asmr = new EditorAsmr());
            dependencies.Cache(asmr);

            dependencies.Cache(new CollabHighlight());
            dependencies.Cache(new DiscussionMarks());

            OsuMenuItem undoMenuItem;
            OsuMenuItem redoMenuItem;

            editorBackgroundDim = config.GetBindable<float>(OsuSetting.EditorDim);
            editorShowStoryboard = config.GetBindable<bool>(OsuSetting.EditorShowStoryboard);
            editorHitMarkers = config.GetBindable<bool>(OsuSetting.EditorShowHitMarkers);
            editorAutoSeekOnPlacement = config.GetBindable<bool>(OsuSetting.EditorAutoSeekOnPlacement);
            editorLimitedDistanceSnap = config.GetBindable<bool>(OsuSetting.EditorLimitedDistanceSnap);
            editorTimelineShowTimingChanges = config.GetBindable<bool>(OsuSetting.EditorTimelineShowTimingChanges);
            editorTimelineShowBreaks = config.GetBindable<bool>(OsuSetting.EditorTimelineShowBreaks);
            editorTimelineShowTicks = config.GetBindable<bool>(OsuSetting.EditorTimelineShowTicks);
            editorContractSidebars = config.GetBindable<bool>(OsuSetting.EditorContractSidebars);
            // the one of the ruleset being edited, which is the one the menu turns on and off.
            editorAdvanceByPreviousSpacing = config.GetBindable<bool>(HitObjectComposer.AdvanceByPreviousSpacingSettingFor(Beatmap.Value.BeatmapInfo.Ruleset));
            editorFreehandSliderPlacement = config.GetBindable<bool>(OsuSetting.EditorFreehandSliderPlacement);
            editorApplyTestPlayModsToComposer = config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer);
            editorLiveModPreview = config.GetBindable<bool>(OsuSetting.EditorLiveModPreview);
            editorAlwaysExpandTimelinePointLabels = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandTimelinePointLabels);
            editorHideTimelinePoints = config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints);
            editorSelectionColour = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionColour);
            editorSelectionThickness = config.GetBindable<float>(OsuSetting.EditorSelectionThickness);
            editorSelectionOpacity = config.GetBindable<float>(OsuSetting.EditorSelectionOpacity);
            editorObjectOutlineColour = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorObjectOutlineColour);
            editorObjectOutlineThickness = config.GetBindable<float>(OsuSetting.EditorObjectOutlineThickness);
            editorObjectOutlineOpacity = config.GetBindable<float>(OsuSetting.EditorObjectOutlineOpacity);
            editorSelectionHandleScale = config.GetBindable<float>(OsuSetting.EditorSelectionHandleScale);
            editorSelectionHandleColour = config.GetBindable<EditorSelectionColour>(OsuSetting.EditorSelectionHandleColour);
            editorSliderAnchorScale = config.GetBindable<float>(OsuSetting.EditorSliderAnchorScale);
            editorSeekAnimationDuration = config.GetBindable<double>(OsuSetting.EditorSeekAnimationDuration);
            editorAlwaysExpandLeftToolbox = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox);
            editorPlacementAnchorType = config.GetBindable<SliderPlacementAnchorType>(OsuSetting.EditorSliderAnchorTypeForPlacement);
            editorShowPlayAreaBorder = config.GetBindable<bool>(OsuSetting.EditorShowPlayAreaBorder);
            editorShowAutoCursor = config.GetBindable<bool>(OsuSetting.EditorShowAutoCursor);
            editorDetectOffscreen = config.GetBindable<bool>(OsuSetting.EditorDetectOffscreen);
            editorShowTimeline = config.GetBindable<bool>(OsuSetting.EditorShowTimeline);
            editorShowDifficultyGraph = config.GetBindable<bool>(OsuSetting.EditorShowDifficultyGraph);
            editorAutoBackup = config.GetBindable<bool>(OsuSetting.EditorAutoBackup);
            editorShowVolumeGraph = config.GetBindable<bool>(OsuSetting.EditorShowVolumeGraph);
            editorHideTimelineVolume = config.GetBindable<bool>(OsuSetting.EditorHideTimelineVolume);

            // These two settings don't work together. Make them mutually exclusive to let the user know.
            editorAutoSeekOnPlacement.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                    editorLimitedDistanceSnap.Value = false;
            });
            editorLimitedDistanceSnap.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                    editorAutoSeekOnPlacement.Value = false;
            });

            AddInternal(new OsuContextMenuContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Container
                    {
                        Name = "Screen container",
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 40, Bottom = 50 },
                        Child = screenContainer = new ScreenContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                        }
                    },
                    bottomBar = new BottomBar(),
                    // what the composer's preview mods draw over the playfield goes here, above the timeline and the
                    // bottom bar so that flashlight reaches those too — they show the beatmap as plainly as the
                    // playfield does. The menu bar is left out, so there is always somewhere to reach the menus, and
                    // comes after it, so that a menu opened down over the rest is not hidden under it (2026-09-26).
                    ModOverlays = new Container
                    {
                        Name = "Mod overlays",
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 40 },
                    },
                    new Container
                    {
                        Name = "Top bar",
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Children = new Drawable[]
                        {
                            new EditorMenuBar
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                RelativeSizeAxes = Axes.Both,
                                MaxHeight = 600,
                                Items = new[]
                                {
                                    new MenuItem(CommonStrings.MenuBarFile)
                                    {
                                        Items = createFileMenuItems().ToList()
                                    },
                                    new MenuItem(CommonStrings.MenuBarEdit)
                                    {
                                        Items = new MenuItem[]
                                        {
                                            undoMenuItem = new EditorMenuItem(CommonStrings.Undo, MenuItemType.Standard, Undo) { Hotkey = new Hotkey(PlatformAction.Undo) },
                                            redoMenuItem = new EditorMenuItem(CommonStrings.Redo, MenuItemType.Standard, Redo) { Hotkey = new Hotkey(PlatformAction.Redo) },
                                            new OsuMenuItemSpacer(),
                                            cutMenuItem = new EditorMenuItem(CommonStrings.Cut, MenuItemType.Standard, Cut) { Hotkey = new Hotkey(PlatformAction.Cut) },
                                            copyMenuItem = new EditorMenuItem(CommonStrings.Copy, MenuItemType.Standard, Copy) { Hotkey = new Hotkey(PlatformAction.Copy) },
                                            pasteMenuItem = new EditorMenuItem(CommonStrings.Paste, MenuItemType.Standard, Paste) { Hotkey = new Hotkey(PlatformAction.Paste) },
                                            cloneMenuItem = new EditorMenuItem(CommonStrings.Clone, MenuItemType.Standard, Clone) { Hotkey = new Hotkey(GlobalAction.EditorCloneSelection) },
                                            new OsuMenuItemSpacer(),
                                            insertBreakMenuItem = new EditorMenuItem(EditorStrings.InsertBreak, MenuItemType.Standard, insertBreak),
                                            new OsuMenuItemSpacer(),
                                            new EditorMenuItem(ComboColourStudioStrings.MenuItem, MenuItemType.Standard, () => comboColourStudio?.ToggleVisibility()),
                                            new EditorMenuItem(CollabStrings.MenuItem, MenuItemType.Standard, () => collab?.ToggleVisibility()),
                                            new EditorMenuItem(DiscussionStrings.MenuItem, MenuItemType.Standard, () => discussion?.ToggleVisibility())
                                            {
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleDiscussion)
                                            },
                                            placementPresetsMenuItem = new EditorMenuItem(PlacementPresetStrings.MenuItem, MenuItemType.Standard, () => placementPresets?.ToggleVisibility())
                                            {
                                                Hotkey = new Hotkey(GlobalAction.EditorTogglePlacementPresets)
                                            },
                                            new OsuMenuItemSpacer(),
                                            new MenuItem(EditorStrings.PlacementAnchorType)
                                            {
                                                Items = placementAnchorTypeMenuItems()
                                            },
                                            new OsuMenuItemSpacer(),
                                            removeUnneededGreenLinesMenuItem = new EditorMenuItem(LazerKaiGreenLineStrings.RemoveUnneeded, MenuItemType.Destructive,
                                                ConfirmRemoveUnneededGreenLines),
                                        }
                                    },
                                    new MenuItem(CommonStrings.MenuBarView)
                                    {
                                        Items = new[]
                                        {
                                            new MenuItem(EditorStrings.Timeline)
                                            {
                                                Items =
                                                [
                                                    new WaveformOpacityMenuItem(config.GetBindable<float>(OsuSetting.EditorWaveformOpacity)),
                                                    new ToggleMenuItem(EditorStrings.TimelineShowTimingChanges)
                                                    {
                                                        State = { BindTarget = editorTimelineShowTimingChanges }
                                                    },
                                                    new ToggleMenuItem(EditorStrings.TimelineShowTicks)
                                                    {
                                                        State = { BindTarget = editorTimelineShowTicks }
                                                    },
                                                    new ToggleMenuItem(EditorStrings.TimelineShowBreaks)
                                                    {
                                                        State = { BindTarget = editorTimelineShowBreaks }
                                                    },
                                                    new ToggleMenuItem(EditorStrings.TimelineHidePoints)
                                                    {
                                                        State = { BindTarget = editorHideTimelinePoints },
                                                        Hotkey = new Hotkey(GlobalAction.EditorToggleTimelinePoints),
                                                    },
                                                    new ToggleMenuItem(EditorStrings.AlwaysExpandTimelinePointLabels)
                                                    {
                                                        State = { BindTarget = editorAlwaysExpandTimelinePointLabels },
                                                        Hotkey = new Hotkey(GlobalAction.EditorToggleTimelinePointLabels),
                                                    },
                                                ]
                                            },
                                            createReferenceDifficultyMenu(),
                                            new SelectionAppearanceMenuItem(editorSelectionColour, editorSelectionThickness, editorSelectionOpacity,
                                                editorObjectOutlineColour, editorObjectOutlineThickness, editorObjectOutlineOpacity,
                                                editorSelectionHandleScale, editorSelectionHandleColour, editorSliderAnchorScale),
                                            new ChoiceMenuItem<OverlayColourScheme>(EditorStrings.EditorColourScheme, editorColourScheme,
                                                Enum.GetValues<OverlayColourScheme>().Select(scheme => (scheme.GetLocalisableDescription(), scheme))),
                                            new ChoiceMenuItem<double>(EditorStrings.SeekAnimationDuration, editorSeekAnimationDuration, new[]
                                            {
                                                ((LocalisableString)@"Instant", 0d),
                                                ((LocalisableString)@"100ms", 100d),
                                                ((LocalisableString)@"150ms", 150d),
                                                ((LocalisableString)@"300ms", 300d),
                                            }),
                                            new ToggleMenuItem(EditorStrings.AlwaysExpandLeftToolbox)
                                            {
                                                State = { BindTarget = editorAlwaysExpandLeftToolbox },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleLeftToolboxExpansion),
                                            },
                                            new ToggleMenuItem(EditorStrings.ShowDifficultyGraph)
                                            {
                                                State = { BindTarget = editorShowDifficultyGraph },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleDifficultyGraph),
                                            },
                                            new ToggleMenuItem(EditorStrings.ShowVolumeGraph)
                                            {
                                                State = { BindTarget = editorShowVolumeGraph },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleVolumeGraph),
                                            },
                                            new ToggleMenuItem(EditorStrings.ShowPlayAreaBorder)
                                            {
                                                State = { BindTarget = editorShowPlayAreaBorder },
                                                Hotkey = new Hotkey(GlobalAction.EditorTogglePlayAreaBorder),
                                            },
                                            new ToggleMenuItem(EditorStrings.ShowAutoCursor)
                                            {
                                                State = { BindTarget = editorShowAutoCursor },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleAutoCursor),
                                            },
                                            new ToggleMenuItem(EditorStrings.DetectOffscreen)
                                            {
                                                State = { BindTarget = editorDetectOffscreen },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleOffscreenDetection),
                                            },
                                            new OsuMenuItemSpacer(),
                                            new BackgroundDimMenuItem(editorBackgroundDim),
                                            new ToggleMenuItem(EditorStrings.ShowStoryboard)
                                            {
                                                State = { BindTarget = editorShowStoryboard },
                                            },
                                            new OsuMenuItemSpacer(),
                                            new ToggleMenuItem(EditorStrings.ShowHitMarkers)
                                            {
                                                State = { BindTarget = editorHitMarkers },
                                            },
                                            new ToggleMenuItem(EditorStrings.AutoSeekOnPlacement)
                                            {
                                                State = { BindTarget = editorAutoSeekOnPlacement },
                                            },
                                            new ToggleMenuItem(EditorStrings.LimitedDistanceSnap)
                                            {
                                                State = { BindTarget = editorLimitedDistanceSnap },
                                            },
                                            new ToggleMenuItem(EditorStrings.AdvanceByPreviousSpacingOnPlacement)
                                            {
                                                State = { BindTarget = editorAdvanceByPreviousSpacing },
                                            },
                                            new ToggleMenuItem(EditorStrings.FreehandSliderPlacement)
                                            {
                                                State = { BindTarget = editorFreehandSliderPlacement },
                                            },
                                            new ToggleMenuItem(EditorStrings.ContractSidebars)
                                            {
                                                State = { BindTarget = editorContractSidebars }
                                            },
                                            new OsuMenuItemSpacer(),
                                            new EditorMenuItem(EditorStrings.DifficultyGraphDetailScreen, MenuItemType.Standard, () => difficultyGraphDetail?.Show()),
                                        }
                                    },
                                    new MenuItem(EditorStrings.Timing)
                                    {
                                        Items = new MenuItem[]
                                        {
                                            new EditorMenuItem(EditorStrings.SetPreviewPointToCurrent, MenuItemType.Standard, SetPreviewPointToCurrentTime),
                                            new EditorMenuItem(EditorStrings.SnapAllNotesToCurrentSnapDivisor, MenuItemType.Destructive, confirmSnapAllHitObjectsToCurrentDivisor),
                                            new EditorMenuItem(LazerKaiGreenLineStrings.WholeOffset, MenuItemType.Standard, AdjustWholeBeatmapOffset),
                                            new EditorMenuItem(LazerKaiGreenLineStrings.CopyRedLines, MenuItemType.Standard, CopyRedLines)
                                            {
                                                Action = { Disabled = loadableBeatmap.BeatmapSetInfo.Beatmaps.Count < 2 }
                                            },
                                            new EditorMenuItem(EditorStrings.Synchronise, MenuItemType.Destructive, confirmSyncTimingAttributes)
                                            {
                                                Action = { Disabled = loadableBeatmap.BeatmapSetInfo.Beatmaps.Count < 2 }
                                            },
                                            bookmarkController.Menu,
                                        }
                                    },
                                    new MenuItem(EditorStrings.MenuBarTest)
                                    {
                                        Items = new MenuItem[]
                                        {
                                            new EditorMenuItem(EditorStrings.TestGameplay, MenuItemType.Standard, TestGameplay)
                                            {
                                                Hotkey = new Hotkey(GlobalAction.EditorTestGameplay)
                                            },
                                            new OsuMenuItemSpacer(),
                                            new EditorMenuItem(EditorStrings.TestPlayMods, MenuItemType.Standard, () => modSelect.ToggleVisibility()),
                                            new EditorMenuItem(EditorStrings.TestPlayDeselectAllMods, MenuItemType.Destructive, testPlayModsController.DeselectAll),
                                            new OsuMenuItemSpacer(),
                                            new ToggleMenuItem(EditorStrings.ApplyTestPlayModsToComposer)
                                            {
                                                State = { BindTarget = editorApplyTestPlayModsToComposer },
                                                Hotkey = new Hotkey(GlobalAction.EditorToggleComposerModPreview),
                                            },
                                            new ToggleMenuItem(EditorStrings.LiveModPreview)
                                            {
                                                State = { BindTarget = editorLiveModPreview },
                                            },
                                        }
                                    },
                                    // leaf items, so they act as buttons sitting immediately right of the Test menu.
                                    new ToggleMenuItem(EditorStrings.ShowTimeline)
                                    {
                                        State = { BindTarget = editorShowTimeline },
                                        Hotkey = new Hotkey(GlobalAction.EditorToggleTimeline),
                                    },
                                    new ToggleMenuItem(EditorStrings.Hitsounds)
                                    {
                                        State = { BindTarget = hitsoundsAudible },
                                        Hotkey = new Hotkey(GlobalAction.EditorToggleHitsounds),
                                    },
                                    new ToggleMenuItem(EditorStrings.Music)
                                    {
                                        State = { BindTarget = musicAudible },
                                        Hotkey = new Hotkey(GlobalAction.EditorToggleMusic),
                                    },
                                }
                            },
                            screenSwitcher = new EditorScreenSwitcherControl
                            {
                                Anchor = Anchor.BottomRight,
                                Origin = Anchor.BottomRight,
                                X = -10,
                                Current = Mode,
                            },
                        },
                    },
                    // sits above the screens so it stays readable, but takes no input away from them: the layout is
                    // built while listening to the song and watching the playfield.
                    comboColourStudio = new ComboColourStudioOverlay { State = { Value = Visibility.Hidden } },
                    // spans the screen, unlike the studio above it, so that it can tell a click meant for the beatmap
                    // from one meant for itself. Only its own panel takes input.
                    collab = new CollabOverlay { State = { Value = Visibility.Hidden } },
                    // the same kind of panel as the one above, read alongside the beatmap it is about.
                    discussion = new DiscussionOverlay { State = { Value = Visibility.Hidden } },
                    // above the screens so that the digits it answers to reach it before the composer's tool
                    // shortcuts underneath, which are the same keys.
                    placementPresets = new PlacementPresetOverlay { State = { Value = Visibility.Hidden } },
                    // modal, unlike the studio above it: this one is opened, read, and closed.
                    difficultyGraphDetail = new DifficultyGraphDetailOverlay { State = { Value = Visibility.Hidden } },
                    // above the screens, so the lines can be edited from whichever one is open, as in stable.
                    greenLinePanel = new GreenLinePanel { State = { Value = Visibility.Hidden } },
                    // above everything else of the editor's, which it stands in the way of while it is open.
                    wholeBeatmapOffset = new WholeBeatmapOffsetOverlay { State = { Value = Visibility.Hidden } },
                    redLineCopy = new RedLineCopyOverlay { State = { Value = Visibility.Hidden } },
                    MutationTracker,
                    autoBackup = new EditorAutoBackup
                    {
                        // whatever a save in stable's format does to the file, what came in can be put back exactly.
                        OpenedFile = canSave && config.Get<bool>(OsuSetting.StableCompatibility) ? savedFile : null,
                    },
                    new EditorPlaybackLoop(),
                }
            });

            // the colour layout is kept beside the beatmap at times of its own, and goes where the beatmap does.
            wholeBeatmapOffset.Moved += milliseconds => comboColourStudio.MoveInTime(milliseconds);

            // until the composer has loaded and said whether it can place them, which the update loop asks once.
            placementPresetsMenuItem.Action.Disabled = true;

            changeHandler?.CanUndo.BindValueChanged(v => undoMenuItem.Action.Disabled = !v.NewValue, true);

            // only where the lines are the file's to keep: in lazer's format they are made up again on every save.
            keptStableVersion.BindValueChanged(v =>
                removeUnneededGreenLinesMenuItem.Action.Disabled = v.NewValue == null || editorBeatmap.BeatmapInfo.Ruleset.OnlineID != 0, true);

            stableCompatibility = config.GetBindable<bool>(OsuSetting.StableCompatibility);
            stableCompatibility.BindValueChanged(_ => updateConvertToStableFormat());
            keptStableVersion.BindValueChanged(_ => updateConvertToStableFormat());
            MutationTracker.InProgress.BindValueChanged(_ => updateConvertToStableFormat(), true);

            stableCompatibility.BindValueChanged(_ => updateCanMakeHitsoundDifficulty());
            keptStableVersion.BindValueChanged(_ => updateCanMakeHitsoundDifficulty());
            MutationTracker.InProgress.BindValueChanged(_ => updateCanMakeHitsoundDifficulty(), true);

            stableCompatibility.BindValueChanged(_ => updateStableFormatNeeds());
            savedInStableFormat.BindValueChanged(_ => updateStableFormatNeeds(), true);

            changeHandler?.CanRedo.BindValueChanged(v => redoMenuItem.Action.Disabled = !v.NewValue, true);
        }

        [Resolved]
        private MusicController musicController { get; set; }

        protected override BackgroundScreen CreateBackground() => new EditorBackgroundScreen(editorBeatmap);

        /// <summary>
        /// Where the composer puts whatever its preview mods draw over the playfield.
        /// </summary>
        /// <remarks>
        /// A ruleset only ever fills the space its playfield was given, so anything it overlays stops at the edge of
        /// the composer. Flashlight has to reach further than that to be worth practising on, so it is drawn from
        /// here instead — above everything the editor shows of the beatmap.
        /// </remarks>
        public Container ModOverlays { get; private set; }

        private readonly BindableBool flashlightPreviewed = new BindableBool();

        /// <summary>
        /// Whether the composer is currently showing flashlight.
        /// </summary>
        /// <remarks>
        /// Flashlight is drawn by the ruleset, so it only ever covers the composer. Everything else the editor
        /// shows of the beatmap has to be dealt with on its own, and this is what those parts read.
        /// </remarks>
        public IBindable<bool> FlashlightPreviewed => flashlightPreviewed;

        private void updateFlashlightPreviewed() =>
            flashlightPreviewed.Value = editorApplyTestPlayModsToComposer.Value
                                        && testPlayModsController.Mods.Value.Any(m => m is ModFlashlight);

        protected override void LoadComplete()
        {
            base.LoadComplete();
            setUpClipboardActionAvailability();

            // only what changes the beatmap counts as time spent mapping, so opening one just to look at it never does.
            // undo and redo count too: they are part of working on it. Subscribed only now, after the state the
            // beatmap was opened in has been taken.
            mappingTime = new MappingTimeRecorder(new MappingTimeStore(storage));

            if (changeHandler != null)
                changeHandler.OnStateChange += () => mappingTime.Changed(editorBeatmap.BeatmapInfo, DateTimeOffset.Now);

            // and the whole time it is open beside that, so how much of it went on changing it can be read off.
            openTime = RunningTimeRecorder.ForBeatmap(new MappingTimeStore(storage), editorBeatmap.BeatmapInfo, DateTimeOffset.Now);
            Scheduler.AddDelayed(() => openTime.Tick(DateTimeOffset.Now), MappingTimeRecorder.WRITE_INTERVAL.TotalMilliseconds, true);

            // handing the mod screen over has to wait for the update thread: it is moved into the game's own layer.
            modSelect.Beatmap.BindTo(Beatmap);
            modSelect.Ruleset.BindTo(Ruleset);
            modSelect.SelectedMods.BindTo(testPlayModsController.Mods);
            modSelectRegistration = overlayManager?.RegisterBlockingOverlay(modSelect);

            // taken from the loader rather than from `Mods`, which it empties on the way in so that the editor itself
            // runs unmodded. Cloned rather than shared: the mods here get their settings edited, and those edits must
            // not reach back into the selection song select is still holding.
            if (carryOverMods.Value && loader != null)
                testPlayModsController.Mods.Value = loader.ModsOnEntry.Select(m => m.DeepClone()).ToArray();

            Mode.Value = isNewBeatmap ? EditorScreenMode.SongSetup : EditorScreenMode.Compose;
            Mode.BindValueChanged(onModeChanged, true);

            hitsoundsAudible.BindValueChanged(audible =>
            {
                updateSampleDisabledState();
                announceEnabled(EditorStrings.Hitsounds, audible.NewValue);
            });

            musicAudible.BindValueChanged(audible =>
            {
                updateTrackMute();
                announceEnabled(EditorStrings.Music, audible.NewValue);
            });

            // reported as what the rows are doing rather than as the setting being on, which reads as a double
            // negative for a setting whose name is already "hide".
            editorHideTimelinePoints.BindValueChanged(hidden =>
                announceState(EditorStrings.TimelinePointRows, hidden.NewValue ? EditorStrings.Hidden : EditorStrings.Shown));

            // named rather than described: which difficulty is being ghosted is the whole question, and stepping
            // through them from the keyboard leaves the menu closed.
            referenceDifficulty.Selected.BindValueChanged(_ =>
                announceState(EditorStrings.ReferenceDifficulty, referenceDifficultyName()));

            // reported as what is on screen for the two that show something, and as the switch being on for the three
            // that change how the editor behaves.
            editorShowDifficultyGraph.BindValueChanged(shown =>
                announceState(EditorStrings.ShowDifficultyGraph, shown.NewValue ? EditorStrings.Shown : EditorStrings.Hidden));

            editorShowVolumeGraph.BindValueChanged(shown =>
                announceState(EditorStrings.ShowVolumeGraph, shown.NewValue ? EditorStrings.Shown : EditorStrings.Hidden));

            // as the timeline's rows are: what the row is doing, the setting itself being named "hide".
            editorHideTimelineVolume.BindValueChanged(hidden =>
                announceState(EditorStrings.TimelineVolumeRow, hidden.NewValue ? EditorStrings.Hidden : EditorStrings.Shown));

            editorShowPlayAreaBorder.BindValueChanged(shown =>
                announceState(EditorStrings.ShowPlayAreaBorder, shown.NewValue ? EditorStrings.Shown : EditorStrings.Hidden));

            editorShowAutoCursor.BindValueChanged(shown =>
                announceState(EditorStrings.ShowAutoCursor, shown.NewValue ? EditorStrings.Shown : EditorStrings.Hidden));

            editorDetectOffscreen.BindValueChanged(on => announceEnabled(EditorStrings.DetectOffscreen, on.NewValue));

            editorAlwaysExpandLeftToolbox.BindValueChanged(on => announceEnabled(EditorStrings.AlwaysExpandLeftToolbox, on.NewValue));

            editorPlacementAnchorType.BindValueChanged(type =>
                announceState(EditorStrings.PlacementAnchorType, type.NewValue.GetLocalisableDescription()));

            editorAlwaysExpandTimelinePointLabels.BindValueChanged(on =>
                announceEnabled(EditorStrings.AlwaysExpandTimelinePointLabels, on.NewValue));

            // the composer reads the mod preview settings when it is constructed, so it has to be rebuilt for a change to take effect.
            editorApplyTestPlayModsToComposer.BindValueChanged(_ =>
            {
                reloadComposeScreen();
                updateFlashlightPreviewed();
            });
            testPlayModsController.SelectionChanged += onTestPlayModsChanged;

            updateFlashlightPreviewed();

            MutationTracker.InProgress.BindValueChanged(_ =>
            {
                foreach (var item in saveRelatedMenuItems)
                    item.Action.Disabled = MutationTracker.InProgress.Value;
            }, true);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            // redundant (should have happened via a `resetTrack()` call in `OnExiting()`), but done for safety
            musicController.TrackChanged -= onTrackChanged;

            modSelectRegistration?.Dispose();

            // an editor disposed before it finished loading never got as far as building this.
            if (testPlayModsController != null)
            {
                testPlayModsController.SelectionChanged -= onTestPlayModsChanged;
                testPlayModsController.Dispose();
            }
        }

        private void onTrackChanged(WorkingBeatmap working, TrackChangeDirection direction)
        {
            clock.ChangeSource(working.Track);

            // the mute belongs to whichever track is playing now, not the one it was put on.
            updateTrackMute();
        }

        /// <summary>
        /// Creates an <see cref="EditorState"/> instance representing the current state of the editor.
        /// </summary>
        /// <param name="nextRuleset">
        /// The ruleset of the next beatmap to be shown, in the case of difficulty switch.
        /// <see langword="null"/> indicates that the beatmap will not be changing.
        /// </param>
        public EditorState GetState([CanBeNull] RulesetInfo nextRuleset = null) => new EditorState
        {
            Mode = Mode.Value,
            Time = clock.CurrentTimeAccurate,
            ClipboardContent = nextRuleset == null || editorBeatmap.BeatmapInfo.Ruleset.ShortName == nextRuleset.ShortName ? Clipboard.Content.Value : string.Empty,
            HitsoundClipboard = Clipboard.Hitsounds.Value,
        };

        /// <summary>
        /// Restore the editor to a provided state.
        /// </summary>
        /// <param name="state">The state to restore.</param>
        public void RestoreState([NotNull] EditorState state) => Schedule(() =>
        {
            Mode.Value = state.Mode;
            clock.Seek(state.Time);
            Clipboard.Content.Value = state.ClipboardContent;
            Clipboard.Hitsounds.Value = state.HitsoundClipboard;
        });

        public void TestGameplay()
        {
            clock.Stop();

            if (HasUnsavedChanges)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptMutationOperation(() =>
                {
                    if (!Save()) return false;

                    pushEditorPlayer();
                    return true;
                })));
            }
            else
            {
                pushEditorPlayer();
            }

            void pushEditorPlayer()
            {
                // applied to the editor's own (leased) bindable so that the pushed player screen picks them up.
                // the lease reverts on editor exit, so the user's song select mod selection is left untouched.
                Mods.Value = testPlayModsController.Mods.Value.Select(m => m.DeepClone()).ToArray();

                this.Push(new EditorPlayerLoader(this));
            }
        }

        private bool attemptMutationOperation(Func<bool> mutationOperation)
        {
            if (MutationTracker.InProgress.Value)
                return false;

            using (MutationTracker.BeginOperation())
                return mutationOperation.Invoke();
        }

        private bool attemptAsyncMutationOperation(Func<Task> mutationTask)
        {
            if (MutationTracker.InProgress.Value)
                return false;

            var operation = MutationTracker.BeginOperation();
            var task = mutationTask.Invoke();
            task.FireAndForget(operation.Dispose, _ => operation.Dispose());
            return true;
        }

        [CanBeNull]
        internal event Action Saved;

        /// <summary>
        /// Saves the currently edited beatmap.
        /// </summary>
        /// <returns>Whether the save was successful.</returns>
        internal bool Save()
        {
            if (!canSave)
            {
                notifications?.Post(new SimpleErrorNotification { Text = EditorStrings.RulesetNotSupportSaving });
                return false;
            }

            try
            {
                if (stableFileKeeper != null)
                {
                    if (!saveKeepingStableFile())
                        return false;
                }
                else
                    saveInLazerFormat();
            }
            catch (Exception ex)
            {
                // can fail e.g. due to duplicated difficulty names.
                Logger.Error(ex, ex.Message);
                return false;
            }

            // no longer new after first user-triggered save.
            isNewBeatmap = false;
            updateLastSavedHash();
            onScreenDisplay?.Display(new EditorToast(ToastStrings.BeatmapSaved, editorBeatmap.BeatmapInfo.GetDisplayTitle()));
            Saved?.Invoke();

            // a save is a deliberate stopping point, so the graph is brought up to date without waiting.
            bottomBar?.ChildrenOfType<DifficultyGraphPart>().SingleOrDefault()?.RecalculateNow();

            // This triggers an update to the window title post-save (ie if the difficulty name changed).
            Activity.Value = getCurrentUserActivity();
            return true;
        }

        /// <summary>
        /// The beatmap's file as it is saved, or null for a beatmap that has none yet.
        /// </summary>
        [CanBeNull]
        private static byte[] readSavedFile(WorkingBeatmap working)
        {
            string path = working.BeatmapInfo.Path == null ? null : working.BeatmapSetInfo?.GetPathForFile(working.BeatmapInfo.Path);

            if (path == null)
                return null;

            using (var stream = working.GetStream(path))
                return stream?.ReadAllRemainingBytesToArray();
        }

        /// <summary>
        /// Whether a save that cannot stay in stable's format goes ahead in lazer's, having been asked.
        /// </summary>
        private bool savingInLazerFormatAllowed;

        /// <returns>Whether it saved: a save that cannot stay in stable's format asks first, and saves nothing yet.</returns>
        private bool saveKeepingStableFile()
        {
            var lines = savedTimingLines.Edited ? savedTimingLines.Lines.Select(l => l.Raw).ToArray() : null;
            byte[] file = stableFileKeeper.Write(editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard, lines, out string current);

            if (file != null)
            {
                beatmapManager.SaveFile(editorBeatmap.BeatmapInfo, editorBeatmap.PlayableBeatmap, file);
                stableFileKeeper.Saved(file, current, editorBeatmap.PlayableBeatmap);
                savedTimingLines.Update(file);
                return true;
            }

            // reported as well as logged: what cannot be written yet is what stable compatibility has left to do. The
            // reason is the report's message and the beatmap only its description, so the reports gather by reason.
            Logger.Error(new SentryOnlyDiagnosticsException(stableFileKeeper.LastFailure ?? @"no reason given"),
                $@"{editorBeatmap.BeatmapInfo.GetDisplayTitle()} could not be kept in stable's format");

            // the file does not come back from lazer's format, so that is asked for first.
            if (!savingInLazerFormatAllowed)
            {
                dialogOverlay.Push(new CannotStayStableDialog(saveInLazerFormatAfterAll, () => (Game as OsuGame)?.ExportLogsForSending()));
                return false;
            }

            byte[] stable = readStoredFile();

            if (stable != null)
                autoBackup.KeepBeforeLazerSave(stable);

            beatmapManager.Save(editorBeatmap.BeatmapInfo, editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard);
            savedTimingLines.Update(Encoding.UTF8.GetBytes(current));

            // the file is lazer's from here on, so there is nothing left to keep.
            stableFileKeeper = null;
            savedTimingLines.StopShowingVelocityLines();
            keptStableVersion.Value = null;
            savedInStableFormat.Value = false;
            notifications?.Post(new SimpleNotification { Text = LazerKaiStableCompatibilityStrings.SavedInLazerFormat });
            return true;
        }

        private void saveInLazerFormatAfterAll()
        {
            savingInLazerFormatAllowed = true;

            try
            {
                attemptMutationOperation(Save);
            }
            finally
            {
                savingInLazerFormatAllowed = false;
            }
        }

        /// <summary>
        /// The difficulty's file as it is stored now, or null if it has none.
        /// </summary>
        [CanBeNull]
        private byte[] readStoredFile() => ReadStoredFileOf(editorBeatmap.BeatmapInfo);

        /// <summary>
        /// A difficulty's file as it is stored now, or null if it has none: this one's, or another of the set's.
        /// </summary>
        [CanBeNull]
        public byte[] ReadStoredFileOf(BeatmapInfo beatmapInfo)
        {
            var info = beatmapManager.QueryBeatmap(b => b.ID == beatmapInfo.ID);
            return info == null ? null : readSavedFile(beatmapManager.GetWorkingBeatmap(info, true));
        }

        private void saveInLazerFormat()
        {
            beatmapManager.Save(editorBeatmap.BeatmapInfo, editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard);

            // what was just written, encoded again the same way, since the manager does not hand the file back.
            savedTimingLines.Update(Encoding.UTF8.GetBytes(StableFileKeeper.Encode(editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard)));
            savedInStableFormat.Value = false;
        }

        protected override void Update()
        {
            base.Update();
            clock.ProcessFrame();

            discardChangesMenuItem.Action.Disabled = !HasUnsavedChanges;

            // asked once, on whichever of the first frames the composer has finished loading on, and then never
            // again: walking for it every frame to answer a question whose answer cannot change is not worth it.
            if (placementPresetsSupported == null && CurrentComposer != null)
            {
                placementPresetsSupported = CurrentComposer.SupportsPlacementPresets;
                placementPresetsMenuItem.Action.Disabled = !placementPresetsSupported.Value;
            }

            // depends on where the playhead is, so there is nothing to hang a notification off; the scan stops at the
            // first object past it and costs nothing next to the rest of a frame here.
            insertBreakMenuItem.Action.Disabled = !tryFitBreakAtCurrentTime(out _, out _);
        }

        public bool OnPressed(KeyBindingPressEvent<PlatformAction> e)
        {
            switch (e.Action)
            {
                case PlatformAction.Cut:
                    Cut();
                    return true;

                case PlatformAction.Copy:
                    Copy();
                    return true;

                case PlatformAction.Paste:
                    Paste();
                    return true;

                case PlatformAction.Undo:
                    Undo();
                    return true;

                case PlatformAction.Redo:
                    Redo();
                    return true;

                case PlatformAction.Save:
                    if (e.Repeat)
                        return false;

                    return attemptMutationOperation(Save);
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<PlatformAction> e)
        {
        }

        private double scrollAccumulation;

        protected override bool OnScroll(ScrollEvent e)
        {
            // alt and the wheel change the volume, as on every other screen: there a wheel nothing took goes to the volume
            // (`GlobalScrollAdjustsVolume`), which the editor cannot have, seeking with the wheel as it does. Over the
            // timeline they are taken before this, to zoom it (2026-10-11).
            if (e.AltPressed && !e.ControlPressed && !e.SuperPressed && !e.ShiftPressed && e.ScrollDelta.Y != 0)
                return volumeOverlay?.Adjust(GlobalAction.IncreaseVolume, e.ScrollDelta.Y, e.IsPrecise) ?? false;

            if (e.ControlPressed || e.AltPressed || e.SuperPressed)
                return false;

            const double precision = 1;

            double scrollComponent = e.ScrollDelta.X + e.ScrollDelta.Y;

            double scrollDirection = Math.Sign(scrollComponent);

            // this is a special case to handle the "pivot" scenario.
            // if we are precise scrolling in one direction then change our mind and scroll backwards,
            // the existing accumulation should be applied in the inverse direction to maintain responsiveness.
            if (scrollAccumulation != 0 && Math.Sign(scrollAccumulation) != scrollDirection)
                scrollAccumulation = scrollDirection * (precision - Math.Abs(scrollAccumulation));

            scrollAccumulation += scrollComponent;

            // because we are doing snapped seeking, we need to add up precise scrolls until they accumulate to an arbitrary cut-off.
            while (Math.Abs(scrollAccumulation) >= precision)
            {
                if (scrollAccumulation > 0)
                    seek(e, -1);
                else
                    seek(e, 1);

                scrollAccumulation = scrollAccumulation < 0 ? Math.Min(0, scrollAccumulation + precision) : Math.Max(0, scrollAccumulation - precision);
            }

            return true;
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            // Repeatable actions
            switch (e.Action)
            {
                case GlobalAction.EditorSeekBackwards:
                    seek(e, -1);
                    return true;

                case GlobalAction.EditorSeekForwards:
                    seek(e, 1);
                    return true;

                case GlobalAction.EditorSeekToPreviousTimingPoint:
                    seekControlPoint(-1);
                    return true;

                case GlobalAction.EditorSeekToNextTimingPoint:
                    seekControlPoint(1);
                    return true;

                case GlobalAction.EditorSeekToPreviousHitObject:
                    if (editorBeatmap.SelectedHitObjects.Any())
                        return false;

                    seekHitObject(-1, e.Repeat);
                    return true;

                case GlobalAction.EditorSeekToNextHitObject:
                    if (editorBeatmap.SelectedHitObjects.Any())
                        return false;

                    seekHitObject(1, e.Repeat);
                    return true;

                case GlobalAction.EditorSeekToPreviousSamplePoint:
                    seekSamplePoint(-1);
                    return true;

                case GlobalAction.EditorSeekToNextSamplePoint:
                    seekSamplePoint(1);
                    return true;
            }

            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.EditorCloneSelection:
                    Clone();
                    return true;

                case GlobalAction.EditorComposeMode:
                    // mods are only previewed in the composer and only reachable from it, so once already there the
                    // key is free to do what it does at song select.
                    if (Mode.Value == EditorScreenMode.Compose)
                        modSelect.ToggleVisibility();
                    else
                        screenSwitcher.SelectItem(EditorScreenMode.Compose);

                    return true;

                case GlobalAction.EditorDesignMode:
                    screenSwitcher.SelectItem(EditorScreenMode.Design);
                    return true;

                case GlobalAction.EditorTimingMode:
                    screenSwitcher.SelectItem(EditorScreenMode.Timing);
                    return true;

                case GlobalAction.EditorSetupMode:
                    screenSwitcher.SelectItem(EditorScreenMode.SongSetup);
                    return true;

                case GlobalAction.EditorVerifyMode:
                    screenSwitcher.SelectItem(EditorScreenMode.Verify);
                    return true;

                case GlobalAction.EditorHitsoundMode:
                    screenSwitcher.SelectItem(EditorScreenMode.Hitsounds);
                    return true;

                case GlobalAction.EditorTestGameplay:
                    bottomBar.TestGameplayButton.TriggerClick();
                    return true;

                case GlobalAction.EditorToggleComposerModPreview:
                    editorApplyTestPlayModsToComposer.Value = !editorApplyTestPlayModsToComposer.Value;
                    return true;

                case GlobalAction.EditorToggleTimeline:
                    editorShowTimeline.Value = !editorShowTimeline.Value;
                    return true;

                case GlobalAction.EditorBackupNow:
                    autoBackup.Backup(asked: true);
                    return true;

                case GlobalAction.EditorToggleHitsounds:
                    hitsoundsAudible.Toggle();
                    return true;

                case GlobalAction.EditorToggleMusic:
                    musicAudible.Toggle();
                    return true;

                case GlobalAction.EditorToggleTimelinePoints:
                    editorHideTimelinePoints.Value = !editorHideTimelinePoints.Value;
                    return true;

                case GlobalAction.EditorToggleReferenceDifficulty:
                    referenceDifficulty.Toggle();
                    announceNothingToGhost();
                    return true;

                case GlobalAction.EditorNextReferenceDifficulty:
                    referenceDifficulty.Cycle(1);
                    announceNothingToGhost();
                    return true;

                case GlobalAction.EditorPreviousReferenceDifficulty:
                    referenceDifficulty.Cycle(-1);
                    announceNothingToGhost();
                    return true;

                case GlobalAction.EditorToggleDifficultyGraph:
                    editorShowDifficultyGraph.Value = !editorShowDifficultyGraph.Value;
                    return true;

                case GlobalAction.EditorToggleVolumeGraph:
                    editorShowVolumeGraph.Value = !editorShowVolumeGraph.Value;
                    return true;

                case GlobalAction.EditorToggleTimelineVolume:
                    editorHideTimelineVolume.Value = !editorHideTimelineVolume.Value;
                    return true;

                case GlobalAction.EditorIncreasePlaybackSpeed:
                    changePlaybackSpeed(1);
                    return true;

                case GlobalAction.EditorDecreasePlaybackSpeed:
                    changePlaybackSpeed(-1);
                    return true;

                case GlobalAction.EditorTogglePlayAreaBorder:
                    editorShowPlayAreaBorder.Value = !editorShowPlayAreaBorder.Value;
                    return true;

                case GlobalAction.EditorToggleOffscreenDetection:
                    editorDetectOffscreen.Value = !editorDetectOffscreen.Value;
                    return true;

                case GlobalAction.EditorToggleAutoCursor:
                    editorShowAutoCursor.Value = !editorShowAutoCursor.Value;
                    return true;

                case GlobalAction.EditorToggleLeftToolboxExpansion:
                    editorAlwaysExpandLeftToolbox.Value = !editorAlwaysExpandLeftToolbox.Value;
                    return true;

                case GlobalAction.EditorToggleTimelinePointLabels:
                    editorAlwaysExpandTimelinePointLabels.Value = !editorAlwaysExpandTimelinePointLabels.Value;
                    return true;

                case GlobalAction.EditorTogglePlacementPresets:
                    if (placementPresetsMenuItem.Action.Disabled)
                        return false;

                    placementPresets?.ToggleVisibility();
                    return true;

                case GlobalAction.EditorToggleDiscussion:
                    discussion?.ToggleVisibility();
                    return true;

                case GlobalAction.EditorDiscardUnsavedChanges:
                    DiscardUnsavedChanges();
                    return true;

                case GlobalAction.EditorEditExternally:
                    editExternally();
                    return true;

                case GlobalAction.EditorSubmitBeatmap:
                    submitBeatmap();
                    return true;

                // Track traversal keys.
                // Matching osu-stable implementations.
                case GlobalAction.EditorSeekToStart:
                    if (e.Repeat)
                        return false;

                    // Seek to first object time, or track start if already there.
                    double? firstObjectTime = editorBeatmap.HitObjects.FirstOrDefault()?.StartTime;

                    if (firstObjectTime == null || clock.CurrentTime == firstObjectTime)
                        clock.Seek(0);
                    else
                        clock.Seek(firstObjectTime.Value);
                    return true;

                case GlobalAction.EditorPlayFromStart:
                    if (e.Repeat)
                        return false;

                    // Restart playback from beginning of track.
                    clock.Seek(0);
                    clock.Start();
                    return true;

                case GlobalAction.EditorTogglePause:
                    if (e.Repeat)
                        return false;

                    // Pause or resume.
                    if (clock.IsRunning)
                        clock.Stop();
                    else
                        clock.Start();
                    return true;

                case GlobalAction.EditorSeekToEnd:
                    if (e.Repeat)
                        return false;

                    // Seek to last object time, or track end if already there.
                    // Note that in osu-stable subsequent presses when at track end won't return to last object.
                    // This has intentionally been changed to make it more useful.
                    if (!editorBeatmap.HitObjects.Any())
                    {
                        clock.Seek(clock.TrackLength);
                        return true;
                    }

                    double lastObjectTime = editorBeatmap.GetLastObjectTime();
                    clock.Seek(clock.CurrentTime == lastObjectTime ? clock.TrackLength : lastObjectTime);
                    return true;
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);
            setUpTrack(seekToStart: true);
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);
            setUpTrack();
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            currentScreen?.OnExiting(e);

            if (!ExitConfirmed)
            {
                // dialog overlay may not be available in visual tests.
                if (dialogOverlay == null)
                {
                    confirmExit();
                    return true;
                }

                // if the dialog is already displayed, block exiting until the user explicitly makes a decision.
                if (dialogOverlay.CurrentDialog is PromptForSaveDialog saveDialog)
                {
                    saveDialog.Flash();
                    return true;
                }

                if (isNewBeatmap || HasUnsavedChanges)
                {
                    updateSampleDisabledState();
                    dialogOverlay?.Push(new PromptForSaveDialog(confirmExit, confirmExitWithSave, cancelExit));
                    return true;
                }
            }

            // here rather than on suspending: time spent test playing after a change is still part of mapping.
            mappingTime?.Stop(DateTimeOffset.Now);
            openTime?.Stop(DateTimeOffset.Now);

            realm.Write(r =>
            {
                var beatmap = r.Find<BeatmapInfo>(editorBeatmap.BeatmapInfo.ID);
                beatmap?.EditorTimestamp = clock.CurrentTime;
            });

            // leave the beatmap unmodded for whatever picks it up next, while there is still an update thread to do it on.
            currentScreen?.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.RevertBeatmapPreviewMods();

            // `resetTrack()` MUST happen before `refetchBeatmap()`, because along other things, `refetchBeatmap()` causes a global working beatmap change,
            // which would cause `EditorClock` to reload the track and automatically reapply adjustments to it if not preceded by `resetTrack()`.
            resetTrack();
            refetchBeatmap();

            // the lease on `Mods` reverts as this screen goes away, so the handover cannot be done from here directly.
            if (carryOverMods.Value)
                modCarryOver?.HandOver(testPlayModsController.Mods.Value.Select(m => m.DeepClone()).ToArray());

            return base.OnExiting(e);
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            // `resetTrack()` MUST happen before `refetchBeatmap()`, because along other things, `refetchBeatmap()` causes a global working beatmap change,
            // which would cause `EditorClock` to reload the track and automatically reapply adjustments to it if not preceded by `resetTrack()`.
            resetTrack();
            refetchBeatmap();
        }

        private void refetchBeatmap()
        {
            // To update the game-wide beatmap with any changes, perform a re-fetch on exit/suspend.
            // This is required as the editor makes its local changes via EditorBeatmap
            // (which are not propagated outwards to a potentially cached WorkingBeatmap).
            var refetchedBeatmap = beatmapManager.GetWorkingBeatmap(Beatmap.Value.BeatmapInfo, true);

            if (!(refetchedBeatmap is DummyWorkingBeatmap))
            {
                Logger.Log(@"Editor providing re-fetched beatmap post edit session");
                Beatmap.Value = refetchedBeatmap;
            }
        }

        private void confirmExitWithSave()
        {
            if (!attemptMutationOperation(Save))
                return;

            ExitConfirmed = true;
            this.Exit();
        }

        private void confirmExit()
        {
            // stop the track if playing to allow the parent screen to choose a suitable playback mode.
            Beatmap.Value.Track.Stop();

            if (isNewBeatmap)
            {
                // confirming exit without save means we should delete the new beatmap completely.
                if (playableBeatmap.BeatmapInfo.BeatmapSet != null)
                    beatmapManager.Delete(playableBeatmap.BeatmapInfo.BeatmapSet);

                // eagerly clear contents before restoring default beatmap to prevent value change callbacks from firing.
                ClearInternal();

                // in theory this shouldn't be required but due to EF core not sharing instance states 100%
                // MusicController is unaware of the changed DeletePending state.
                Beatmap.SetDefault();
            }

            ExitConfirmed = true;
            this.Exit();
        }

        #region Clipboard support

        /// <summary>
        /// Lists the other difficulties in this set, so one can be drawn underneath the one being edited.
        /// </summary>
        private MenuItem createReferenceDifficultyMenu()
        {
            var choices = new List<(LocalisableString, Guid)> { (EditorStrings.ReferenceDifficultyNone, Guid.Empty) };

            choices.AddRange(referenceDifficulty.Available.Select(b => ((LocalisableString)b.DifficultyName, b.ID)));

            return new ChoiceMenuItem<Guid>(EditorStrings.ReferenceDifficulty, referenceDifficulty.Selected, choices)
            {
                Hotkey = new Hotkey(GlobalAction.EditorToggleReferenceDifficulty)
            };
        }

        private EditorMenuItem insertBreakMenuItem;

        /// <summary>
        /// Fills the gap in objects the playhead is sitting in with a break.
        /// </summary>
        /// <remarks>
        /// Placed as a manual break, so it survives whether or not the editor is generating breaks on its own.
        /// </remarks>
        private void insertBreak()
        {
            if (!tryFitBreakAtCurrentTime(out double startTime, out double endTime))
                return;

            changeHandler?.BeginChange();
            editorBeatmap.Breaks.Add(new ManualBreakPeriod(startTime, endTime));
            changeHandler?.EndChange();
        }

        /// <summary>
        /// The break the gap around the playhead has room for.
        /// </summary>
        /// <returns><c>false</c> when there is no gap to fill, or it already holds a break.</returns>
        private bool tryFitBreakAtCurrentTime(out double startTime, out double endTime)
        {
            startTime = endTime = 0;

            if (editorBeatmap == null)
                return false;

            double time = clock.CurrentTime;
            double lastEndTime = double.MinValue;
            HitObject nextObject = null;

            foreach (var hitObject in editorBeatmap.HitObjects)
            {
                if (hitObject.StartTime > time)
                {
                    nextObject = hitObject;
                    break;
                }

                // objects can overlap, so the gap starts at whichever of them runs longest rather than the last one.
                lastEndTime = Math.Max(lastEndTime, hitObject.GetEndTime());
            }

            // there is no gap before the first object or after the last one, only the space outside the map.
            if (nextObject == null || lastEndTime == double.MinValue)
                return false;

            if (!EditorBeatmapProcessor.TryFitBreak(editorBeatmap, lastEndTime, nextObject, out startTime, out endTime))
                return false;

            var candidate = new BreakPeriod(startTime, endTime);
            return !editorBeatmap.Breaks.Any(b => b.Intersects(candidate));
        }

        private EditorMenuItem cutMenuItem;
        private EditorMenuItem copyMenuItem;
        private EditorMenuItem cloneMenuItem;
        private EditorMenuItem pasteMenuItem;

        private readonly BindableWithCurrent<bool> canCut = new BindableWithCurrent<bool>();
        private readonly BindableWithCurrent<bool> canCopy = new BindableWithCurrent<bool>();
        private readonly BindableWithCurrent<bool> canPaste = new BindableWithCurrent<bool>();

        private void setUpClipboardActionAvailability()
        {
            canCut.Current.BindValueChanged(cut => cutMenuItem.Action.Disabled = !cut.NewValue, true);
            canCopy.Current.BindValueChanged(copy =>
            {
                copyMenuItem.Action.Disabled = !copy.NewValue;
                cloneMenuItem.Action.Disabled = !copy.NewValue;
            }, true);
            canPaste.Current.BindValueChanged(paste => pasteMenuItem.Action.Disabled = !paste.NewValue, true);
        }

        private void rebindClipboardBindables()
        {
            canCut.Current = currentScreen.CanCut;
            canCopy.Current = currentScreen.CanCopy;
            canPaste.Current = currentScreen.CanPaste;
        }

        protected void Cut() => currentScreen?.Cut();

        protected void Copy() => currentScreen?.Copy();

        protected void Clone()
        {
            // Avoid attempting to clone if copying is not available (as it may result in pasting something unexpected).
            if (!canCopy.Value)
                return;

            // This is an initial implementation just to get an idea of how people used this function.
            // There are a couple of differences from osu!stable's implementation which will require more work to match:
            // - The "clipboard" is not populated during the duplication process.
            // - The duplicated hitobjects are inserted after the original pattern (add one beat_length and then quantize using beat snap).
            // - The duplicated hitobjects are selected (but this is also applied for all paste operations so should be changed there).
            Copy();
            Paste();
        }

        protected void Paste() => currentScreen?.Paste();

        #endregion

        protected void Undo()
        {
            if (changeHandler?.CanUndo.Value == true)
                asmr.Play(AsmrSound.Undo);

            changeHandler?.RestoreState(-1);
        }

        protected void Redo()
        {
            if (changeHandler?.CanRedo.Value == true)
                asmr.Play(AsmrSound.Redo);

            changeHandler?.RestoreState(1);
        }

        protected void DiscardUnsavedChanges()
        {
            if (!HasUnsavedChanges)
                return;

            // we're not doing this via `changeHandler` because `changeHandler` has limited number of undo actions
            // and therefore there's no guarantee that it even *has* the beatmap's last saved state in its history still.
            dialogOverlay.Push(new DiscardUnsavedChangesDialog(() =>
            {
                updateLastSavedHash(); // without this a second dialog will show (the standard "save unsaved changes" one that shows on exit).
                SwitchToDifficulty(editorBeatmap.BeatmapInfo);
            }));
        }

        protected void SetPreviewPointToCurrentTime()
        {
            editorBeatmap.PreviewTime.Value = (int)clock.CurrentTime;
        }

        private void confirmSyncTimingAttributes()
        {
            dialogOverlay.Push(new SyncTimingConfirmationDialog((syncBookmarks, syncPreviewPoint) =>
            {
                if (Beatmap.Value.BeatmapSetInfo.Beatmaps.Count <= 1)
                    return;

                if (!syncBookmarks && !syncPreviewPoint)
                    return;

                int[] sourceBookmarks = editorBeatmap.Bookmarks.ToArray();
                int sourcePreviewTime = editorBeatmap.BeatmapInfo.Metadata.PreviewTime;
                var leftAsTheyWere = new List<string>();

                foreach (var beatmapInfo in Beatmap.Value.BeatmapSetInfo.Beatmaps)
                {
                    if (beatmapInfo.Equals(editorBeatmap.BeatmapInfo))
                        continue;

                    try
                    {
                        var targetWorking = beatmapManager.GetWorkingBeatmap(beatmapInfo);
                        int previewTimeBefore = beatmapInfo.Metadata.PreviewTime;

                        // a difficulty still in stable's format stays in it, with only the lines that change written.
                        bool saved = StableFileSaving.Save(beatmapManager, targetWorking, osuConfig.Get<bool>(OsuSetting.StableCompatibility), playable =>
                        {
                            if (syncBookmarks)
                                playable.Bookmarks = sourceBookmarks;

                            if (syncPreviewPoint)
                                beatmapInfo.Metadata.PreviewTime = sourcePreviewTime;
                        });

                        // back to what its file says, in the set held here as well, which the save below writes to the
                        // database whole.
                        if (!saved)
                        {
                            beatmapInfo.Metadata.PreviewTime = previewTimeBefore;
                            leftAsTheyWere.Add(beatmapInfo.DifficultyName);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.Error(e, $@"Failed to sync bookmarks/preview to {beatmapInfo.GetDisplayTitle()}");
                        return;
                    }
                }

                if (leftAsTheyWere.Count > 0)
                    notifications?.Post(new SimpleNotification { Text = LazerKaiStableCompatibilityStrings.OtherDifficultiesLeftAsTheyWere(string.Join(@", ", leftAsTheyWere)) });

                SaveAndReload(withDialog: false);
            }));
        }

        private WholeBeatmapOffsetOverlay wholeBeatmapOffset;

        /// <summary>
        /// Opens the window that moves the whole beatmap in time.
        /// </summary>
        internal void AdjustWholeBeatmapOffset()
        {
            // what the green line panel has pending is held open as one step to undo, which this would become a part of
            // and be put back with.
            if (greenLinePanel?.Pending == true)
            {
                notifications?.Post(new SimpleNotification { Text = LazerKaiGreenLineStrings.WholeOffsetPending });
                return;
            }

            wholeBeatmapOffset.Show();
        }

        private RedLineCopyOverlay redLineCopy;

        /// <summary>
        /// Opens the window red lines are copied between this difficulty and the others of the set from.
        /// </summary>
        internal void CopyRedLines()
        {
            // as moving the whole beatmap is: what the green line panel has pending would become a part of the copy.
            if (greenLinePanel?.Pending == true)
            {
                notifications?.Post(new SimpleNotification { Text = LazerKaiGreenLineStrings.WholeOffsetPending });
                return;
            }

            redLineCopy.Show();
        }

        /// <summary>
        /// Puts the red lines of this difficulty in the place of those of others of the set, which are saved with them
        /// at once, and opens the set again as copying the bookmarks to them does.
        /// </summary>
        /// <param name="targets">The difficulties to copy to.</param>
        /// <param name="within">How many milliseconds from a tick something stands and is still put on it.</param>
        /// <param name="keepBeats">Whether everything of them is first moved to where its beat is under the red lines.</param>
        internal void CopyRedLinesToOthers(IReadOnlyList<BeatmapInfo> targets, int within, bool keepBeats)
        {
            var redLines = RedLineCopying.Of(editorBeatmap.ControlPointInfo);
            bool keepStableFiles = osuConfig.Get<bool>(OsuSetting.StableCompatibility);

            int copied = 0;
            int objects = 0;
            int lines = 0;

            var offTicks = new List<string>();
            var notKept = new List<string>();
            var failed = new List<string>();

            foreach (var beatmapInfo in targets)
            {
                try
                {
                    // loaded as the beatmap in the editor is, for it to be changed the same way.
                    var result = RedLineCopying.CopyTo(beatmapManager, beatmapManager.GetWorkingBeatmap(beatmapInfo), keepStableFiles, redLines, within, keepBeats, LoadComponent);

                    if (result == null)
                    {
                        notKept.Add(beatmapInfo.DifficultyName);
                        continue;
                    }

                    copied++;
                    objects += result.Value.Objects;
                    lines += result.Value.Lines;

                    if (result.Value.OffTicks > 0)
                        offTicks.Add($@"{beatmapInfo.DifficultyName} ({result.Value.OffTicks})");
                }
                catch (Exception e)
                {
                    Logger.Error(e, $@"Failed to copy red lines to {beatmapInfo.GetDisplayTitle()}");
                    failed.Add(beatmapInfo.DifficultyName);
                }
            }

            notifications?.Post(new SimpleNotification { Text = LazerKaiGreenLineStrings.CopyRedLinesDoneToOthers(copied, objects, lines) });

            if (offTicks.Count > 0)
                notifications?.Post(new SimpleNotification { Text = LazerKaiGreenLineStrings.CopyRedLinesOffTicksIn(string.Join(@", ", offTicks)) });

            if (notKept.Count > 0)
                notifications?.Post(new SimpleErrorNotification { Text = LazerKaiGreenLineStrings.CopyRedLinesNotKept(string.Join(@", ", notKept)) });

            if (failed.Count > 0)
                notifications?.Post(new SimpleErrorNotification { Text = LazerKaiGreenLineStrings.CopyRedLinesFailed(string.Join(@", ", failed)) });

            // the set is other than the editor has it: saving this difficulty with it as it was would put the others back.
            if (copied > 0)
                SaveAndReload(withDialog: false);
        }

        private EditorMenuItem removeUnneededGreenLinesMenuItem;

        /// <summary>
        /// Asks whether to take out the green lines that change nothing, and does, saying how many went.
        /// </summary>
        internal void ConfirmRemoveUnneededGreenLines()
        {
            dialogOverlay.Push(new RemoveUnneededGreenLinesDialog(() =>
            {
                int removed = GreenLineCleanup.Remove(editorBeatmap, savedTimingLines);

                notifications?.Post(new SimpleNotification
                {
                    Text = removed > 0 ? LazerKaiGreenLineStrings.Removed(removed) : LazerKaiGreenLineStrings.NoneRemoved,
                });
            }));
        }

        internal EditorMenuItem ConvertToStableFormatMenuItem { get; private set; }

        private Bindable<bool> stableCompatibility;

        /// <summary>
        /// Offered for an osu! beatmap in lazer's format, while stable compatibility is on: with it off, the next save
        /// would write lazer's format again.
        /// </summary>
        private void updateConvertToStableFormat() =>
            ConvertToStableFormatMenuItem.Action.Disabled = !canSave || !stableCompatibility.Value || keptStableVersion.Value != null
                                                            || editorBeatmap.BeatmapInfo.Ruleset.OnlineID != 0 || MutationTracker.InProgress.Value;

        /// <summary>
        /// Asks whether to convert the difficulty to stable's format, and does: it is saved the way exporting for stable
        /// writes it, and opened again, to be kept in stable's format from then on.
        /// </summary>
        internal void ConfirmConvertToStableFormat()
        {
            dialogOverlay.Push(new ConvertToStableFormatDialog(() => attemptMutationOperation(convertToStableFormat)));
        }

        private bool convertToStableFormat()
        {
            // saved first as it always is, so that a beatmap never saved has a file, and unsaved changes are in it.
            if (!Save())
                return false;

            var info = beatmapManager.QueryBeatmap(b => b.ID == editorBeatmap.BeatmapInfo.ID);
            byte[] saved = info == null ? null : readSavedFile(beatmapManager.GetWorkingBeatmap(info, true));

            if (saved == null)
                return false;

            using (var converted = LegacyBeatmapExporter.EncodeForStable(() => new MemoryStream(saved), info, LegacyBeatmapExporter.ConvertForStable))
            {
                if (converted == null)
                    return false;

                beatmapManager.SaveFile(editorBeatmap.BeatmapInfo, editorBeatmap.PlayableBeatmap, converted.ToArray());
            }

            // opened again, the file is read as one from stable is, and what the conversion changed shows.
            SwitchToDifficulty(editorBeatmap.BeatmapInfo);
            return true;
        }

        /// <summary>
        /// Whether the difficulty's file is in stable's format, as it was opened or last saved, whether or not the option
        /// to keep it so is on.
        /// </summary>
        private readonly Bindable<bool> savedInStableFormat = new Bindable<bool>();

        /// <summary>
        /// What the difficulty is missing before it is kept in stable's format, as the hitsound screen asks for it. Only
        /// an osu! difficulty is converted; one of another mode is asked only for the option.
        /// </summary>
        public IBindable<StableFormatNeeds> StableFormatNeeds => stableFormatNeeds;

        private readonly Bindable<StableFormatNeeds> stableFormatNeeds = new Bindable<StableFormatNeeds>();

        private void updateStableFormatNeeds()
        {
            var needs = Edit.StableFormatNeeds.None;

            if (canSave)
            {
                if (!stableCompatibility.Value)
                    needs |= Edit.StableFormatNeeds.Compatibility;

                if (!savedInStableFormat.Value && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0)
                    needs |= Edit.StableFormatNeeds.Conversion;
            }

            stableFormatNeeds.Value = needs;
        }

        /// <summary>
        /// Asks whether to do what <see cref="StableFormatNeeds"/> says is missing, and does it all at once: the option is
        /// turned on, the difficulty converted where it is not in stable's format, and opened again to be kept in it.
        /// </summary>
        public void ConfirmUseStableFormat()
        {
            var needs = stableFormatNeeds.Value;

            if (needs == Edit.StableFormatNeeds.None)
                return;

            // a file already in stable's format with nothing unsaved only needs opening again, which keeps it from then
            // on. An unsaved change cannot go into it without the option having been on as it was opened, so it is saved
            // and converted along with the rest, and the dialog says so.
            var done = HasUnsavedChanges ? needs | Edit.StableFormatNeeds.Conversion : needs;

            dialogOverlay.Push(new UseStableFormatDialog(done, () => attemptMutationOperation(() =>
            {
                stableCompatibility.Value = true;

                if (!done.HasFlag(Edit.StableFormatNeeds.Conversion))
                {
                    SwitchToDifficulty(editorBeatmap.BeatmapInfo);
                    return true;
                }

                return convertToStableFormat();
            })));
        }

        /// <summary>
        /// Whether <see cref="MakeHitsoundDifficulty"/> can be done: for an osu! difficulty kept in stable's format, whose
        /// green lines are its own.
        /// </summary>
        public IBindable<bool> CanMakeHitsoundDifficulty => canMakeHitsoundDifficulty;

        private readonly Bindable<bool> canMakeHitsoundDifficulty = new Bindable<bool>();

        private void updateCanMakeHitsoundDifficulty()
        {
            canMakeHitsoundDifficulty.Value = canSave && stableCompatibility.Value && keptStableVersion.Value != null
                                              && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0 && !MutationTracker.InProgress.Value;

            canMakeManiaHitsoundDifficulty.Value = canSave && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0 && !MutationTracker.InProgress.Value
                                                   && rulesets.GetRuleset(mania_ruleset_id) != null;

            canConvertManiaToHitsoundDifficulty.Value = canSave && stableCompatibility.Value && keptStableVersion.Value != null
                                                        && editorBeatmap.BeatmapInfo.Ruleset.OnlineID == mania_ruleset_id && laneSounds == null
                                                        && !MutationTracker.InProgress.Value;
        }

        /// <summary>
        /// Whether <see cref="ConvertManiaToHitsoundDifficulty"/> can be done: for an osu!mania difficulty kept in stable's
        /// format, whose notes say what they sound as and whose green lines are its own. One made to hitsound in here,
        /// whose lanes are its sounds, is turned into one from its own screen (see <see cref="LaneSounds"/>).
        /// </summary>
        public IBindable<bool> CanConvertManiaToHitsoundDifficulty => canConvertManiaToHitsoundDifficulty;

        private readonly Bindable<bool> canConvertManiaToHitsoundDifficulty = new Bindable<bool>();

        /// <summary>
        /// What is kept for an osu!mania difficulty hitsounded in stable: what it was last turned into, and whether its
        /// volumes went along.
        /// </summary>
        private LaneSoundProject notesOwnSounds() =>
            new LaneSoundProjectStore(storage).Load(editorBeatmap.BeatmapInfo) ?? new LaneSoundProject { NotesSoundAsThemselves = true };

        /// <summary>
        /// Whether the volumes of this difficulty went into the hitsound difficulty the last time it was turned into one.
        /// </summary>
        public bool ManiaVolumesAreConverted => notesOwnSounds().ConvertVolumes;

        /// <summary>
        /// Whether sliders were made silent as they are followed the last time this difficulty was turned into a
        /// hitsound difficulty.
        /// </summary>
        public bool ManiaSliderBodiesAreMuted => notesOwnSounds().MuteSliderBodies;

        /// <summary>
        /// Turns this osu!mania difficulty, hitsounded in stable, into an osu! hitsound difficulty of the same beatmap
        /// set, as it is: its notes sound as they say, and it is not changed. Done again, the new one is put in place of
        /// the one made before.
        /// </summary>
        /// <remarks>
        /// What Mapping Tools' Hitsound Studio does with such a difficulty, which is how the user has made hitsound
        /// difficulties until now (see <see cref="StableManiaHitsounds"/> and <see cref="HitsoundDifficultyConverter"/>).
        /// </remarks>
        /// <param name="withVolumes">Whether the volumes of this difficulty's lines go into the new one. Left out, the
        /// volumes of the one made before are kept.</param>
        /// <param name="muteSliderBodies">Whether a silent sound is written for sliders to make as they are followed,
        /// for every sample set and custom index the lines of the new one say.</param>
        public void ConvertManiaToHitsoundDifficulty(bool withVolumes, bool muteSliderBodies = false)
        {
            if (!canConvertManiaToHitsoundDifficulty.Value)
                return;

            attemptMutationOperation(() =>
            {
                // saved first, so that the file read is the difficulty as it is seen.
                if (!Save())
                    return false;

                var working = Beatmap.Value;
                byte[] saved = readSavedFile(working);

                if (saved == null)
                    return false;

                string file = Encoding.UTF8.GetString(saved);

                var store = new LaneSoundProjectStore(storage);
                var project = notesOwnSounds();

                project.ConvertVolumes = withVolumes;
                project.MuteSliderBodies = muteSliderBodies;

                var read = StableManiaHitsounds.From(file, name => HitsoundDifficultyConverter.FileOf(working.BeatmapSetInfo, name));

                // its notes sound as files of the beatmap, where stable has them.
                new HitsoundDifficultyConverter(beatmapManager, working, project, () => store.Save(editorBeatmap.BeatmapInfo, project), notifications, dialogOverlay,
                        HitsoundDifficultyConverter.FromSet(working))
                    .Convert(read.Notes, read.Sounds, read.Fallback, () => file, read.Volumes);

                return true;
            });
        }

        private const int mania_ruleset_id = 3;

        /// <summary>
        /// How many lanes a difficulty made to hitsound in has: two stages of nine, which is as many as osu!mania has,
        /// since each lane is a sound and there is no knowing how many will be wanted (2026-10-12).
        /// </summary>
        public const int HITSOUND_CANVAS_LANES = 18;

        /// <summary>
        /// The sounds of this difficulty's lanes, if it is one made to hitsound in.
        /// </summary>
        [CanBeNull]
        private LaneSounds laneSounds;

        /// <summary>
        /// Whether <see cref="MakeManiaHitsoundDifficulty"/> can be done: for an osu! difficulty, whatever format it is
        /// kept in, since nothing of its file is carried over but its red lines.
        /// </summary>
        public IBindable<bool> CanMakeManiaHitsoundDifficulty => canMakeManiaHitsoundDifficulty;

        private readonly Bindable<bool> canMakeManiaHitsoundDifficulty = new Bindable<bool>();

        /// <summary>
        /// Adds an empty osu!mania difficulty to hitsound in, with this one's red lines, and switches to it.
        /// </summary>
        /// <remarks>
        /// Hitsounds for osu! are worked out in osu!mania, where any number of sounds can be played at one time, and
        /// turned into a hitsound difficulty afterwards. Each lane is a sound there (see <see cref="LaneSounds"/>).
        /// </remarks>
        public void MakeManiaHitsoundDifficulty()
        {
            if (!canMakeManiaHitsoundDifficulty.Value)
                return;

            attemptMutationOperation(() =>
            {
                // saved first, so that red lines not yet saved are in the new difficulty.
                if (!Save())
                    return false;

                var info = beatmapManager.QueryBeatmap(b => b.ID == editorBeatmap.BeatmapInfo.ID);
                var mania = rulesets.GetRuleset(mania_ruleset_id);

                if (info?.BeatmapSet == null || mania == null)
                    return false;

                string name = NamingUtils.GetNextBestName(info.BeatmapSet.Beatmaps.Select(b => b.DifficultyName), @"Hitsounds (mania)");

                var created = beatmapManager.CreateHitsoundCanvas(info.BeatmapSet, beatmapManager.GetWorkingBeatmap(info, true), mania, name, HITSOUND_CANVAS_LANES);

                // having lanes kept for it is what makes it a difficulty to hitsound in.
                var lanes = new LaneSoundProject();
                lanes.EnsureLanes(HITSOUND_CANVAS_LANES);
                new LaneSoundProjectStore(storage).Save(created.BeatmapInfo, lanes);

                SwitchToDifficulty(created.BeatmapInfo);
                return true;
            });
        }

        /// <summary>
        /// Adds a difficulty to hitsound in, written from this one's file as stable has it, and switches to it.
        /// </summary>
        /// <param name="asHitCircles">Whether every point a sound is played becomes a circle of its own, as in a hitsound
        /// difficulty, rather than the objects being copied as they are.</param>
        public void MakeHitsoundDifficulty(bool asHitCircles)
        {
            if (!canMakeHitsoundDifficulty.Value)
                return;

            attemptMutationOperation(() =>
            {
                // saved first, so that unsaved changes are in the file the new difficulty is written from.
                if (!Save())
                    return false;

                var info = beatmapManager.QueryBeatmap(b => b.ID == editorBeatmap.BeatmapInfo.ID);
                byte[] saved = info == null ? null : readSavedFile(beatmapManager.GetWorkingBeatmap(info, true));

                if (info?.BeatmapSet == null || saved == null)
                    return false;

                string name = NamingUtils.GetNextBestName(info.BeatmapSet.Beatmaps.Select(b => b.DifficultyName),
                    asHitCircles ? @"Hitsounds" : $@"{info.DifficultyName} (copy)");

                string file = Encoding.UTF8.GetString(saved);
                string written = asHitCircles ? HitsoundDifficultyWriter.AsHitCircles(file, name) : HitsoundDifficultyWriter.CopyAs(file, name);

                if (written == null)
                {
                    notifications?.Post(new SimpleErrorNotification { Text = LazerKaiHitsoundStrings.CouldNotReadObjects });
                    return false;
                }

                var created = beatmapManager.AddDifficultyFromFile(info.BeatmapSet, beatmapManager.GetWorkingBeatmap(info, true), Encoding.UTF8.GetBytes(written));

                SwitchToDifficulty(created.BeatmapInfo);
                return true;
            });
        }

        private void confirmSnapAllHitObjectsToCurrentDivisor()
        {
            dialogOverlay.Push(new SnapAllNotesConfirmationDialog(SnapAllHitObjectsToCurrentDivisor));
        }

        protected void SnapAllHitObjectsToCurrentDivisor() => editorBeatmap.SnapAllHitObjectsToCurrentDivisor();

        private void setUpTrack(bool seekToStart = false)
        {
            clock.Stop();

            if (seekToStart)
            {
                double targetTime = 0;

                if (editorBeatmap.BeatmapInfo.EditorTimestamp != null)
                {
                    targetTime = editorBeatmap.BeatmapInfo.EditorTimestamp.Value;
                }
                else if (Beatmap.Value.Beatmap.HitObjects.Count > 0)
                {
                    // seek to one beat length before the first hitobject
                    targetTime = Beatmap.Value.Beatmap.HitObjects[0].StartTime;
                    targetTime -= Beatmap.Value.Beatmap.ControlPointInfo.TimingPointAt(targetTime).BeatLength;
                }

                clock.Seek(Math.Max(0, targetTime));
            }

            clock.BindAdjustments();
            musicController.TrackChanged += onTrackChanged;

            trackHeld = true;
            updateTrackMute();
        }

        private void resetTrack()
        {
            clock.Stop();
            clock.UnbindAdjustments();
            musicController.TrackChanged -= onTrackChanged;

            trackHeld = false;
            updateTrackMute();
        }

        /// <summary>
        /// Calls out a switch the same way every other editor shortcut does, since the menu bar may not be where the
        /// user is looking when a key binding flips it.
        /// </summary>
        private void announceEnabled(LocalisableString what, bool on)
            => announceState(what, on ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower());

        /// <summary>
        /// The shapes a slider can be placed with, as one menu item each, ticked to show which is in use.
        /// </summary>
        private MenuItem[] placementAnchorTypeMenuItems()
        {
            var items = new List<MenuItem>();

            foreach (var shape in Enum.GetValues<SliderPlacementAnchorType>())
            {
                var item = new ToggleMenuItem(shape.GetLocalisableDescription());

                item.State.Value = editorPlacementAnchorType.Value == shape;

                // only one shape can be in use, so picking one is what turns the rest off. Unticking the one in use
                // asks for the same shape again, which is why the item is not allowed to turn itself off.
                item.State.BindValueChanged(on =>
                {
                    if (on.NewValue)
                        editorPlacementAnchorType.Value = shape;
                    else if (editorPlacementAnchorType.Value == shape)
                        item.State.Value = true;
                });

                editorPlacementAnchorType.BindValueChanged(chosen => item.State.Value = chosen.NewValue == shape);

                items.Add(item);
            }

            return items.ToArray();
        }

        /// <summary>
        /// Reports something the editor just did, for actions whose key binding has no other visible result.
        /// </summary>
        private void announceState(LocalisableString what, LocalisableString state)
            => onScreenDisplay?.Display(new EditorToast(what, state));

        /// <summary>
        /// Steps the playback speed, and says what it is now: also at either end, where the key leaves it as it was.
        /// Where speed cannot be adjusted the bottom bar's own tooltip says why, so the key does nothing.
        /// </summary>
        private void changePlaybackSpeed(int direction)
        {
            if (bottomBar.PlaybackControl.ChangeSpeed(direction) is double speed)
                announceState(EditorStrings.PlaybackSpeed, $"{speed:0%}");
        }

        /// <summary>
        /// The name of the difficulty being ghosted, or that there is none.
        /// </summary>
        private LocalisableString referenceDifficultyName()
        {
            var shown = referenceDifficulty.Available.FirstOrDefault(b => b.ID == referenceDifficulty.Selected.Value);

            return shown == null ? EditorStrings.ReferenceDifficultyNone : (LocalisableString)shown.DifficultyName;
        }

        /// <summary>
        /// Says so when there is no other difficulty to ghost.
        /// </summary>
        /// <remarks>
        /// The toast otherwise rides on the selection changing, and in a set of one nothing changes — leaving a key
        /// that reads as broken rather than as having nothing to do.
        /// </remarks>
        private void announceNothingToGhost()
        {
            if (referenceDifficulty.Available.Length == 0)
                announceState(EditorStrings.ReferenceDifficulty, EditorStrings.ReferenceDifficultyNone);
        }

        /// <summary>
        /// Puts the mute on the track the editor is playing, or takes it off.
        /// </summary>
        private void updateTrackMute()
        {
            var target = trackHeld && !musicAudible.Value ? Beatmap.Value.Track : null;

            if (ReferenceEquals(mutedTrack, target))
                return;

            mutedTrack?.RemoveAdjustment(AdjustableProperty.Volume, trackMute);
            mutedTrack = target;
            mutedTrack?.AddAdjustment(AdjustableProperty.Volume, trackMute);
        }

        /// <summary>
        /// Discards the cached compose screen so it is rebuilt from scratch on next display.
        /// Needed for settings which the composer only reads while being constructed, such as the mods it previews.
        /// </summary>
        private ScheduledDelegate composerReloadDebounce;

        private void onTestPlayModsChanged()
        {
            updateFlashlightPreviewed();

            if (!editorApplyTestPlayModsToComposer.Value)
                return;

            RefreshComposer();
        }

        /// <summary>
        /// Rebuilds the composer once changes stop coming, for what it only reads while being built: the mods it
        /// previews, or the copy of the beatmap it shows while <see cref="HitObjectComposer.EditingLocked"/>.
        /// </summary>
        public void RefreshComposer()
        {
            // dragging a mod's setting changes it every frame, and rebuilding the composer is far from free.
            composerReloadDebounce?.Cancel();
            composerReloadDebounce = Scheduler.AddDelayed(reloadComposeScreen, 200);
        }

        private void reloadComposeScreen()
        {
            // the composer hands these over on load and cannot take them back on disposal, which does not run on the
            // update thread. Clearing here, where the composer that put them there is about to go, is what does it.
            ModOverlays.Clear();

            var existing = screenContainer.SingleOrDefault(s => s.Type == EditorScreenMode.Compose);

            if (existing == null)
                return;

            // before the screen goes away rather than as it is disposed: see `RevertBeatmapPreviewMods`.
            existing.ChildrenOfType<HitObjectComposer>().SingleOrDefault()?.RevertBeatmapPreviewMods();

            bool wasCurrent = currentScreen == existing;

            if (wasCurrent)
                currentScreen = null;

            screenContainer.Remove(existing, true);

            if (wasCurrent)
                onModeChanged(new ValueChangedEvent<EditorScreenMode>(Mode.Value, Mode.Value));
        }

        private void onModeChanged(ValueChangedEvent<EditorScreenMode> e)
        {
            // the overlays belong to the composer, which is kept around rather than thrown away when another screen is
            // opened. Without this, flashlight would go on covering the design and timing screens too.
            ModOverlays.Alpha = e.NewValue == EditorScreenMode.Compose ? 1 : 0;

            // the panels the menus open are read against the beatmap being composed, and they sit above whatever
            // screen is showing while blocking the clicks that land on them. Left open, they cover the design and
            // timing screens and take the controls underneath with them.
            if (e.NewValue != EditorScreenMode.Compose)
            {
                placementPresets?.Hide();
                comboColourStudio?.Hide();
                collab?.Hide();
                discussion?.Hide();
                difficultyGraphDetail?.Hide();
            }

            var lastScreen = currentScreen;

            // the hitsound screen is the compose screen with the playfield given over to the hitsounds, so that there is
            // one composer to keep the timeline, the selection and the previewed mods.
            var screenType = e.NewValue == EditorScreenMode.Hitsounds ? EditorScreenMode.Compose : e.NewValue;

            try
            {
                if ((currentScreen = screenContainer.SingleOrDefault(s => s.Type == screenType)) != null)
                {
                    if (lastScreen != currentScreen)
                    {
                        lastScreen?.Hide();
                        screenContainer.ChangeChildDepth(currentScreen, lastScreen?.Depth + 1 ?? 0);
                    }

                    currentScreen.Show();
                    return;
                }

                lastScreen?.Hide();

                switch (screenType)
                {
                    case EditorScreenMode.SongSetup:
                        currentScreen = new SetupScreen();
                        break;

                    case EditorScreenMode.Compose:
                        currentScreen = new ComposeScreen();
                        break;

                    case EditorScreenMode.Design:
                        currentScreen = new DesignScreen();
                        break;

                    case EditorScreenMode.Timing:
                        currentScreen = new TimingScreen();
                        break;

                    case EditorScreenMode.Verify:
                        currentScreen = new VerifyScreen();
                        break;

                    default:
                        throw new InvalidOperationException("Editor menu bar switched to an unsupported mode");
                }

                screenContainer.LoadComponentAsync(currentScreen, newScreen =>
                {
                    if (newScreen == currentScreen)
                    {
                        screenContainer.Add(newScreen);
                        newScreen.Show();
                    }
                });
            }
            finally
            {
                if (Mode.Value != EditorScreenMode.Compose)
                    ComposerFocusMode.Value = false;

                updateSampleDisabledState();
                rebindClipboardBindables();
            }
        }

        /// <summary>
        /// Forces a reload of the compose screen after significant configuration changes.
        /// </summary>
        public void ReloadComposeScreen()
        {
            ModOverlays.Clear();

            screenContainer.SingleOrDefault(s => s.Type == EditorScreenMode.Compose)?.RemoveAndDisposeImmediately();

            // If not currently on compose screen, the reload will happen on next mode change.
            // That said, control points *can* change on compose screen (e.g. via undo), so we have to handle that case too.
            if (Mode.Value is EditorScreenMode.Compose or EditorScreenMode.Hitsounds)
                Mode.TriggerChange();
        }

        [CanBeNull]
        private ScheduledDelegate playbackDisabledDebounce;

        private EditorScreenSwitcherControl screenSwitcher;

        private void updateSampleDisabledState()
        {
            bool shouldDisableSamples = !hitsoundsAudible.Value
                                        || clock.SeekingOrStopped.Value
                                        || currentScreen is not ComposeScreen
                                        || editorBeatmap.UpdateInProgress.Value
                                        || dialogOverlay?.CurrentDialog != null;

            playbackDisabledDebounce?.Cancel();

            if (shouldDisableSamples)
            {
                samplePlaybackDisabled.Value = true;
            }
            else
            {
                // Debounce re-enabling arbitrarily high enough to avoid flip-flopping during beatmap updates
                // or rapid user seeks.
                playbackDisabledDebounce = Scheduler.AddDelayed(() => samplePlaybackDisabled.Value = false, 50);
            }
        }

        private void seekControlPoint(int direction)
        {
            // In the case of a backwards seek while playing, it can be hard to jump before a timing point.
            // Adding some lenience here makes it more user-friendly.
            double seekLenience = clock.IsRunning ? 1000 * ((IAdjustableClock)clock).Rate : 0;

            ControlPoint found = direction < 1
                ? editorBeatmap.ControlPointInfo.AllControlPoints.LastOrDefault(p => p.Time < clock.CurrentTime - seekLenience)
                : editorBeatmap.ControlPointInfo.AllControlPoints.FirstOrDefault(p => p.Time > clock.CurrentTime);

            if (found != null)
                clock.Seek(found.Time);
        }

        /// <param name="direction">Backwards below 1, forwards otherwise.</param>
        /// <param name="repeat">Whether this is a repeat of a key held.</param>
        private void seekHitObject(int direction, bool repeat)
        {
            double current = clock.CurrentTimeAccurate;

            // held while playing, it goes back an object a repeat rather than to the one object the playhead has just
            // gone by again, as the left key alone does with white ticks.
            if (direction < 1)
                current = heldSeekBack.From(current, clock.IsRunning, repeat);
            else
                heldSeekBack.Reset();

            var found = direction < 1
                ? editorBeatmap.HitObjects.LastOrDefault(p => p.StartTime < current)
                : editorBeatmap.HitObjects.FirstOrDefault(p => p.StartTime > current);

            if (found == null)
                return;

            if (direction < 1)
                heldSeekBack.WentTo(found.StartTime);

            clock.SeekSmoothlyTo(found.StartTime);
        }

        private void seekSamplePoint(int direction)
        {
            double currentTime = clock.CurrentTimeAccurate;

            // Check if we are currently inside a hit object with node samples, if so seek to the next node sample point
            var current = direction < 1
                ? editorBeatmap.HitObjects.LastOrDefault(p => p is IHasRepeats r && p.StartTime < currentTime && r.EndTime >= currentTime)
                : editorBeatmap.HitObjects.LastOrDefault(p => p is IHasRepeats r && p.StartTime <= currentTime && r.EndTime > currentTime);

            if (current != null)
            {
                // Find the next node sample point
                var r = (IHasRepeats)current;
                double[] nodeSamplePointTimes = new double[r.RepeatCount + 3];

                nodeSamplePointTimes[0] = current.StartTime;
                // The sample point for the main samples is sandwiched between the head and the first repeat
                nodeSamplePointTimes[1] = current.StartTime + r.Duration / r.SpanCount() / 2;

                for (int i = 0; i < r.SpanCount(); i++)
                {
                    nodeSamplePointTimes[i + 2] = current.StartTime + r.Duration * (i + 1) / r.SpanCount();
                }

                double found = direction < 1
                    ? nodeSamplePointTimes.Last(p => p < currentTime)
                    : nodeSamplePointTimes.First(p => p > currentTime);

                clock.SeekSmoothlyTo(found);
            }
            else
            {
                if (direction < 1)
                {
                    current = editorBeatmap.HitObjects.LastOrDefault(p => lastSoundOf(p) < currentTime);
                    if (current != null)
                        clock.SeekSmoothlyTo(lastSoundOf(current));
                }
                else
                {
                    current = editorBeatmap.HitObjects.FirstOrDefault(p => soundOf(p) > currentTime);
                    if (current != null)
                        clock.SeekSmoothlyTo(soundOf(current));
                }

                // a spinner's sound is at its end, where the timeline puts it.
                double soundOf(HitObject h) => SamplePointPiece.SoundsAtItsEnd(h, editorBeatmap) ? h.GetEndTime() : h.StartTime;

                double lastSoundOf(HitObject h) => h is IHasRepeats r ? r.EndTime : soundOf(h);
            }

            // Show the sample edit popover at the current time
            ShowSampleEditPopoverRequested?.Invoke(clock.CurrentTimeAccurate);
        }

        /// <summary>
        /// Where the last seek back made while playing went to, for a held key to go on from.
        /// </summary>
        private readonly HeldSeekBack heldSeekBack = new HeldSeekBack();

        /// <summary>
        /// Seeks as the wheel and the arrow keys do: a tick while stopped, and a white tick while playing.
        /// </summary>
        /// <remarks>
        /// While playing it is from white tick to white tick, as stable's editor has it: back to the one before, and
        /// forward to the next (2026-10-11, see <see cref="WhiteTickSeeking"/>). Lazer took it half a second whatever the
        /// tempo, a beat and a half at 180 BPM, to wherever that came to: nowhere a tick is, and with the left key held,
        /// little further back than had been played since its last repeat.
        /// </remarks>
        /// <param name="e">What asked for it, for the keys held with it.</param>
        /// <param name="direction">Backwards below 1, forwards otherwise.</param>
        private void seek(UIEvent e, int direction)
        {
            double amount = e.ShiftPressed ? 4 : 1;

            if (!clock.IsRunning)
            {
                heldSeekBack.Reset();

                if (direction < 1)
                    clock.SeekBackward(true, amount);
                else
                    clock.SeekForward(true, amount);

                // a little to the side it went, and a little lower going back (2026-10-21).
                asmr.Play(AsmrSound.Seek, direction < 1 ? 200 : 312, direction < 1 ? 0.94 : 1.06);

                return;
            }

            var timing = editorBeatmap.ControlPointInfo;
            double current = clock.CurrentTimeAccurate;

            // as many more at a fast tempo as stable takes, which `EditorClock.seek` has from it.
            int count = (int)amount * (1 + 250 / (int)timing.TimingPointAt(current).BeatLength);

            if (direction >= 1)
            {
                heldSeekBack.Reset();
                clock.SeekSmoothlyTo(WhiteTickSeeking.After(timing, current, count));
                return;
            }

            // a held key goes on from the white tick its last repeat went to rather than from the playhead. Going by the
            // playhead, a repeat finds the first tick after that white tick already gone by wherever ticks are shorter
            // than the time between two repeats, and goes back to the same white tick over and over.
            current = heldSeekBack.From(current, true, e is KeyBindingPressEvent<GlobalAction> { Repeat: true });

            double whiteTick = WhiteTickSeeking.Before(timing, current, beatDivisor.Value, count);

            heldSeekBack.WentTo(whiteTick);
            clock.SeekSmoothlyTo(whiteTick);
        }

        private void updateLastSavedHash()
        {
            lastSavedHash = changeHandler?.CurrentStateHash;
        }

        /// <summary>
        /// Opens the folder holding this difficulty's backups.
        /// </summary>
        /// <remarks>
        /// The whole backup directory holds a folder per difficulty of every beatmap ever opened, so landing there
        /// means hunting for the right one. Opening the folder the difficulty in front of the mapper writes to puts
        /// the file they came looking for on screen. The folder is created if the editor has not written a backup
        /// yet, which also answers where they are going to appear.
        /// </remarks>
        private void openBackupFolder()
        {
            storage.GetStorageForDirectory(EditorAutoBackup.DirectoryFor(editorBeatmap.BeatmapInfo)).PresentExternally();
        }

        private IEnumerable<MenuItem> createFileMenuItems()
        {
            yield return createDifficultyCreationMenu();
            yield return createDifficultySwitchMenu();
            yield return new OsuMenuItemSpacer();
            yield return new EditorMenuItem(EditorStrings.DeleteDifficulty, MenuItemType.Destructive, deleteDifficulty) { Action = { Disabled = Beatmap.Value.BeatmapSetInfo.Beatmaps.Count < 2 } };
            yield return new OsuMenuItemSpacer();

            var save = new EditorMenuItem(WebCommonStrings.ButtonsSave, MenuItemType.Standard, () => attemptMutationOperation(Save)) { Hotkey = new Hotkey(PlatformAction.Save) };
            saveRelatedMenuItems.Add(save);
            yield return save;

            yield return discardChangesMenuItem = new EditorMenuItem(GlobalActionKeyBindingStrings.EditorDiscardUnsavedChanges, MenuItemType.Destructive, DiscardUnsavedChanges)
            {
                Hotkey = new Hotkey(GlobalAction.EditorDiscardUnsavedChanges)
            };

            var export = createExportMenu();
            saveRelatedMenuItems.AddRange(export.Items);
            yield return export;

            yield return new EditorMenuItem(LazerKaiStableCompatibilityStrings.Convert)
            {
                Items = new MenuItem[]
                {
                    ConvertToStableFormatMenuItem = new EditorMenuItem(LazerKaiStableCompatibilityStrings.ConvertToStableFormat, MenuItemType.Destructive,
                        ConfirmConvertToStableFormat)
                }
            };

            yield return new MenuItem(EditorStrings.AutoBackup)
            {
                Items = new MenuItem[]
                {
                    new ToggleMenuItem(EditorStrings.AutoBackupEnabled)
                    {
                        State = { BindTarget = editorAutoBackup },
                    },
                    new OsuMenuItemSpacer(),
                    new EditorMenuItem(EditorStrings.BackupNow, MenuItemType.Standard, () => autoBackup.Backup(asked: true))
                    {
                        Hotkey = new Hotkey(GlobalAction.EditorBackupNow)
                    },
                    new EditorMenuItem(EditorStrings.OpenBackupFolder, MenuItemType.Standard, openBackupFolder),
                }
            };

            if (RuntimeInfo.IsDesktop)
            {
                var externalEdit = new EditorMenuItem(EditorStrings.EditExternally, MenuItemType.Standard, editExternally)
                {
                    Hotkey = new Hotkey(GlobalAction.EditorEditExternally)
                };
                saveRelatedMenuItems.Add(externalEdit);
                yield return externalEdit;
            }

            // one for each thing the folder was opened to do, rather than one that leads to both.
            var importFromAnotherSet = new EditorMenuItem(LazerKaiBeatmapFilesStrings.ImportFromAnotherSet, MenuItemType.Standard, OpenImportFromAnotherSet);
            saveRelatedMenuItems.Add(importFromAnotherSet);
            yield return importFromAnotherSet;

            var editFileNames = new EditorMenuItem(LazerKaiBeatmapFilesStrings.EditFileNames, MenuItemType.Standard, OpenSoundFileNames);
            saveRelatedMenuItems.Add(editFileNames);
            yield return editFileNames;

            bool isSetMadeOfLegacyRulesetBeatmaps = (isNewBeatmap && Ruleset.Value.IsLegacyRuleset())
                                                    || (!isNewBeatmap && Beatmap.Value.BeatmapSetInfo.Beatmaps.All(b => b.Ruleset.IsLegacyRuleset()));
            bool submissionAvailable = api.Endpoints.BeatmapSubmissionServiceUrl != null;

            if (isSetMadeOfLegacyRulesetBeatmaps && submissionAvailable && beatmapUploadEnabled)
            {
                var upload = new EditorMenuItem(EditorStrings.SubmitBeatmap, MenuItemType.Standard, submitBeatmap)
                {
                    Hotkey = new Hotkey(GlobalAction.EditorSubmitBeatmap)
                };
                saveRelatedMenuItems.Add(upload);
                yield return upload;
            }

            if (editorBeatmap.BeatmapInfo.OnlineID > 0)
            {
                yield return new OsuMenuItemSpacer();
                yield return new EditorMenuItem(EditorStrings.OpenInfoPage, MenuItemType.Standard,
                    () => (Game as OsuGame)?.OpenUrlExternally(editorBeatmap.BeatmapInfo.GetOnlineURL(api, editorBeatmap.BeatmapInfo.Ruleset)));
                yield return new EditorMenuItem(EditorStrings.OpenDiscussionPage, MenuItemType.Standard,
                    () => (Game as OsuGame)?.OpenUrlExternally(
                        $@"{api.Endpoints.WebsiteUrl}/beatmapsets/{editorBeatmap.BeatmapInfo.BeatmapSet!.OnlineID}/discussion/{editorBeatmap.BeatmapInfo.OnlineID}"));
            }

            yield return new OsuMenuItemSpacer();
            yield return new EditorMenuItem(CommonStrings.Exit, MenuItemType.Standard, this.Exit);
        }

        private EditorMenuItem createExportMenu()
        {
            var exportItems = new List<MenuItem>
            {
                new EditorMenuItem(EditorStrings.ExportForEditing, MenuItemType.Standard, () => runExport(manager => manager.Export(Beatmap.Value.BeatmapSetInfo))),
                new EditorMenuItem(EditorStrings.ExportForCompatibility, MenuItemType.Standard, () => runExport(manager => manager.ExportLegacy(Beatmap.Value.BeatmapSetInfo))),
                new EditorMenuItem(EditorStrings.ExportGuestDifficulty, MenuItemType.Standard, () => runExport(manager => manager.ExportLegacy(Beatmap.Value.BeatmapInfo))),
                new EditorMenuItem(EditorStrings.ExportBeatmapFile, MenuItemType.Standard, () => runExport(manager =>
                {
                    manager.ExportLegacyBeatmapFile(Beatmap.Value.BeatmapInfo);
                    return Task.CompletedTask;
                })),
            };

            return new EditorMenuItem(CommonStrings.Export) { Items = exportItems };
        }

        private void editExternally()
        {
            if (HasUnsavedChanges)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptMutationOperation(() =>
                {
                    if (!Save())
                        return false;

                    startEdit();
                    return true;
                })));
            }
            else
            {
                startEdit();
            }

            void startEdit()
            {
                this.Push(new ExternalEditScreen());
            }
        }

        /// <summary>
        /// Opens the screen difficulties and sounds of another beatmap set are brought into this one on.
        /// </summary>
        public void OpenImportFromAnotherSet() => openBeatmapFiles(() => new ImportFromAnotherSetScreen());

        /// <summary>
        /// Opens the screen the sounds of the beatmap set are given the names they are played as hitsounds by on.
        /// </summary>
        public void OpenSoundFileNames() => openBeatmapFiles(() => new SoundFileNamesScreen());

        /// <summary>
        /// Opens a screen the files of the beatmap set are seen to on, saving first as before its folder is opened:
        /// what is done there is written to the set at once.
        /// </summary>
        private void openBeatmapFiles(Func<BeatmapFilesScreen> screen)
        {
            if (HasUnsavedChanges)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptMutationOperation(() =>
                {
                    if (!Save())
                        return false;

                    this.Push(screen());
                    return true;
                })));
            }
            else
            {
                this.Push(screen());
            }
        }

        /// <summary>
        /// Whether this build may upload beatmaps. It may not: its encoder deliberately writes control points the
        /// official one drops, so what it would upload is not what osu! would have written. The requests are refused
        /// outright in <see cref="UnsendableRequests"/>; this keeps the menu entry away and gives the key binding
        /// something to say. Flip it to restore upstream behaviour.
        /// </summary>
        private readonly bool beatmapUploadEnabled = false;

        private void submitBeatmap()
        {
            if (!beatmapUploadEnabled)
            {
                notifications?.Post(new SimpleNotification { Text = EditorStrings.BeatmapUploadUnavailable });
                return;
            }

            if (api.State.Value != APIState.Online)
            {
                loginOverlay?.Show();
                return;
            }

            if (!editorBeatmap.HitObjects.Any())
            {
                notifications?.Post(new SimpleNotification
                {
                    Text = BeatmapSubmissionStrings.EmptyBeatmapsCannotBeSubmitted,
                });
                return;
            }

            if (HasUnsavedChanges)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptMutationOperation(() =>
                {
                    if (!Save())
                        return false;

                    startSubmission();
                    return true;
                })));
            }
            else
            {
                startSubmission();
            }

            void startSubmission() => this.Push(new BeatmapSubmissionScreen());
        }

        private void runExport(Func<BeatmapManager, Task> exportAction)
        {
            if (HasUnsavedChanges)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptAsyncMutationOperation(() =>
                {
                    if (!Save())
                        return Task.CompletedTask;

                    return exportAction.Invoke(beatmapManager);
                })));
            }
            else
            {
                attemptAsyncMutationOperation(() => exportAction(beatmapManager));
            }
        }

        /// <summary>
        /// Beatmaps of the currently edited set, grouped by ruleset and ordered by difficulty.
        /// </summary>
        private IOrderedEnumerable<IGrouping<RulesetInfo, BeatmapInfo>> groupedOrderedBeatmaps => Beatmap.Value.BeatmapSetInfo.Beatmaps
                                                                                                         .OrderBy(b => b.StarRating)
                                                                                                         .GroupBy(b => b.Ruleset)
                                                                                                         .OrderBy(group => group.Key);

        private void deleteDifficulty()
        {
            if (dialogOverlay == null)
                delete();
            else
                dialogOverlay.Push(new DeleteDifficultyConfirmationDialog(playableBeatmap.BeatmapInfo.DifficultyName, editorBeatmap.HitObjects.Count, delete));

            void delete()
            {
                BeatmapInfo difficultyToDelete = playableBeatmap.BeatmapInfo;

                var difficultiesBeforeDeletion = groupedOrderedBeatmaps.SelectMany(g => g).ToList();

                // if the difficulty being currently deleted has unsaved changes,
                // the editor exit flow would prompt for save *after* this method has done its thing.
                // this is generally undesirable and also ends up leaving the user in a broken state.
                // therefore, just update the last saved hash to make the exit flow think the deleted beatmap is not dirty,
                // so that it will not show the save dialog on exit.
                updateLastSavedHash();

                beatmapManager.DeleteDifficultyImmediately(difficultyToDelete);

                int deletedIndex = difficultiesBeforeDeletion.IndexOf(difficultyToDelete);
                // of note, we're still working with the cloned version, so indices are all prior to deletion.
                BeatmapInfo nextToShow = difficultiesBeforeDeletion[deletedIndex == 0 ? 1 : deletedIndex - 1];

                Beatmap.Value = beatmapManager.GetWorkingBeatmap(nextToShow);

                SwitchToDifficulty(nextToShow);
            }
        }

        private EditorMenuItem createDifficultyCreationMenu()
        {
            var rulesetItems = new List<MenuItem>();

            foreach (var ruleset in rulesets.AvailableRulesets)
                rulesetItems.Add(new EditorMenuItem(ruleset.Name, MenuItemType.Standard, () => CreateNewDifficulty(ruleset)));

            saveRelatedMenuItems.AddRange(rulesetItems);

            return new EditorMenuItem(EditorStrings.CreateNewDifficulty) { Items = rulesetItems };
        }

        protected internal void CreateNewDifficulty(RulesetInfo rulesetInfo)
        {
            if (isNewBeatmap)
            {
                dialogOverlay.Push(new SaveRequiredPopupDialog(() => attemptMutationOperation(() =>
                {
                    if (!Save())
                        return false;

                    CreateNewDifficulty(rulesetInfo);
                    return true;
                })));

                return;
            }

            if (!rulesetInfo.Equals(editorBeatmap.BeatmapInfo.Ruleset))
            {
                switchToNewDifficulty(rulesetInfo, false);
                return;
            }

            dialogOverlay.Push(new CreateNewDifficultyDialog(createCopy =>
            {
                // kept in stable's format, a copy is written from the file as it is kept. Copied as objects, it was
                // saved in lazer's format, with its green lines put where its objects are (2026-10-08).
                if (createCopy && canMakeHitsoundDifficulty.Value)
                    MakeHitsoundDifficulty(false);
                else
                    switchToNewDifficulty(rulesetInfo, createCopy);
            }));
        }

        private void switchToNewDifficulty(RulesetInfo rulesetInfo, bool createCopy)
        {
            switchingDifficulty = true;
            loader?.ScheduleSwitchToNewDifficulty(editorBeatmap.BeatmapInfo, rulesetInfo, createCopy, GetState(rulesetInfo));
        }

        private EditorMenuItem createDifficultySwitchMenu()
        {
            var difficultyItems = new List<MenuItem>();

            foreach (var rulesetBeatmaps in groupedOrderedBeatmaps)
            {
                if (difficultyItems.Count > 0)
                    difficultyItems.Add(new OsuMenuItemSpacer());

                foreach (var beatmap in rulesetBeatmaps)
                {
                    bool isCurrentDifficulty = playableBeatmap.BeatmapInfo.Equals(beatmap);
                    var difficultyMenuItem = new DifficultyMenuItem(beatmap, isCurrentDifficulty, SwitchToDifficulty);
                    difficultyItems.Add(difficultyMenuItem);
                }
            }

            // Ensure difficulty names are updated when modified in the editor.
            // Maybe we could trigger less often but this seems to work well enough.
            editorBeatmap.SaveStateTriggered += () =>
            {
                foreach (var beatmapInfo in Beatmap.Value.BeatmapSetInfo.Beatmaps)
                {
                    var menuItem = difficultyItems.OfType<DifficultyMenuItem>().FirstOrDefault(i => i.BeatmapInfo.Equals(beatmapInfo));
                    menuItem?.Text.Value = string.IsNullOrEmpty(beatmapInfo.DifficultyName) ? "(unnamed)" : beatmapInfo.DifficultyName;
                }
            };

            return new EditorMenuItem(EditorStrings.ChangeDifficulty) { Items = difficultyItems };
        }

        public void SwitchToDifficulty(BeatmapInfo nextBeatmap)
        {
            switchingDifficulty = true;
            loader?.ScheduleSwitchToExistingDifficulty(nextBeatmap, GetState(nextBeatmap.Ruleset));
        }

        private void cancelExit()
        {
            updateSampleDisabledState();
            loader?.CancelPendingDifficultySwitch();
        }

        public Task<bool> SaveAndReload(bool withDialog = true)
        {
            var tcs = new TaskCompletionSource<bool>();

            void performReload()
            {
                bool reloadedSuccessfully = attemptMutationOperation(() =>
                {
                    if (!Save()) return false;

                    SwitchToDifficulty(editorBeatmap.BeatmapInfo);
                    return true;
                });
                tcs.SetResult(reloadedSuccessfully);
            }

            if (withDialog)
            {
                dialogOverlay.Push(new SaveAndReloadEditorDialog(
                    reload: performReload,
                    cancel: () => tcs.SetResult(false)));
            }
            else
            {
                performReload();
            }

            return tcs.Task;
        }

        public bool HandleTimestamp(string timestamp, bool notifyOnError = false)
        {
            if (!EditorTimestampParser.TryParse(timestamp, out var timeSpan, out string selection))
            {
                if (notifyOnError)
                {
                    Schedule(() => notifications?.Post(new SimpleErrorNotification
                    {
                        Icon = FontAwesome.Solid.ExclamationTriangle,
                        Text = EditorStrings.FailedToParseEditorLink
                    }));
                }

                return false;
            }

            editorBeatmap.SelectedHitObjects.Clear();

            if (clock.IsRunning)
                clock.Stop();

            double position = timeSpan.Value.TotalMilliseconds;

            if (string.IsNullOrEmpty(selection))
            {
                clock.SeekSmoothlyTo(position);
                return true;
            }

            // Seek to the next closest HitObject instead
            HitObject nextObject = editorBeatmap.HitObjects.FirstOrDefault(x => x.StartTime >= position);

            if (nextObject != null)
                position = nextObject.StartTime;

            clock.SeekSmoothlyTo(position);

            Mode.Value = EditorScreenMode.Compose;

            // Delegate handling the selection to the ruleset.
            currentScreen.Dependencies.Get<HitObjectComposer>().SelectFromTimestamp(position, selection);
            return true;
        }

        public double SnapTime(double time, double? referenceTime) => editorBeatmap.SnapTime(time, referenceTime);

        public double GetBeatLengthAtTime(double referenceTime) => editorBeatmap.GetBeatLengthAtTime(referenceTime);

        public int BeatDivisor => beatDivisor.Value;

        ControlPointInfo IBeatSyncProvider.ControlPoints => editorBeatmap.ControlPointInfo;
        IClock IBeatSyncProvider.Clock => clock;
        ChannelAmplitudes IHasAmplitudes.CurrentAmplitudes => Beatmap.Value.TrackLoaded ? Beatmap.Value.Track.CurrentAmplitudes : ChannelAmplitudes.Empty;

        private partial class ScreenContainer : Container<EditorScreen>
        {
            public new Task LoadComponentAsync<TLoadable>([NotNull] TLoadable component, Action<TLoadable> onLoaded = null, CancellationToken cancellation = default, Scheduler scheduler = null)
                where TLoadable : Drawable
                => base.LoadComponentAsync(component, onLoaded, cancellation, scheduler);
        }
    }
}
