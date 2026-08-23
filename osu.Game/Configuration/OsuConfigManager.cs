// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Configuration.Tracking;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Overlays.Dashboard.Friends;
using osu.Game.Overlays.Mods.Input;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.OnlinePlay.Lounge.Components;
using osu.Game.Screens.Select;
using osu.Game.Screens.Select.Filter;
using osu.Game.Skinning;
using osu.Game.Users;

namespace osu.Game.Configuration
{
    public class OsuConfigManager : IniConfigManager<OsuSetting>, IGameplaySettings
    {
        public OsuConfigManager(Storage storage)
            : base(storage)
        {
        }

        protected override void InitialiseDefaults()
        {
            // UI/selection defaults
            SetDefault(OsuSetting.Ruleset, string.Empty);
            SetDefault(OsuSetting.Skin, SkinInfo.ARGON_SKIN.ToString());

            SetDefault(OsuSetting.BeatmapDetailTab, BeatmapDetailTab.Local);
            SetDefault(OsuSetting.BeatmapLeaderboardSortMode, LeaderboardSortMode.Score);
            SetDefault(OsuSetting.BeatmapDetailModsFilter, false);

            SetDefault(OsuSetting.ShowConvertedBeatmaps, true);
            SetDefault(OsuSetting.DisplayStarsMinimum, 0.0, 0, 10, 0.1);
            SetDefault(OsuSetting.DisplayStarsMaximum, 10.1, 0, 10.1, 0.1);

            SetDefault(OsuSetting.SongSelectGroupMode, GroupMode.None);
            SetDefault(OsuSetting.SongSelectSortingMode, SortMode.Title);
            SetDefault(OsuSetting.SongSelectCollectionFilter, string.Empty);

            SetDefault(OsuSetting.RandomSelectAlgorithm, RandomSelectAlgorithm.RandomPermutation);
            SetDefault(OsuSetting.ModSelectHotkeyStyle, ModSelectHotkeyStyle.Sequential);
            SetDefault(OsuSetting.ModSelectTextSearchStartsActive, true);

            SetDefault(OsuSetting.ChatDisplayHeight, ChatOverlay.DEFAULT_HEIGHT, 0.2f, 1f, 0.01f);

            SetDefault(OsuSetting.BeatmapListingCardSize, BeatmapCardSize.Normal);
            SetDefault(OsuSetting.BeatmapListingFeaturedArtistFilter, true);

            SetDefault(OsuSetting.ProfileCoverExpanded, true);

            SetDefault(OsuSetting.ToolbarClockDisplayMode, ToolbarClockDisplayMode.Full);

            SetDefault(OsuSetting.SongSelectBackgroundBlur, false);

            // Online settings
            SetDefault(OsuSetting.Username, string.Empty);
            SetDefault(OsuSetting.Token, string.Empty);

            SetDefault(OsuSetting.AutomaticallyDownloadMissingBeatmaps, true);

            SetDefault(OsuSetting.SavePassword, true).ValueChanged += enabled =>
            {
                if (enabled.NewValue)
                    SetValue(OsuSetting.SaveUsername, true);
                else
                    GetBindable<string>(OsuSetting.Token).SetDefault();
            };

            SetDefault(OsuSetting.SaveUsername, true).ValueChanged += enabled =>
            {
                if (!enabled.NewValue)
                {
                    GetBindable<string>(OsuSetting.Username).SetDefault();
                    SetValue(OsuSetting.SavePassword, false);
                }
            };

            SetDefault(OsuSetting.ExternalLinkWarning, true);
            SetDefault(OsuSetting.PreferNoVideo, false);

            SetDefault(OsuSetting.ShowOnlineExplicitContent, false);

            SetDefault(OsuSetting.NotifyOnUsernameMentioned, true);
            SetDefault(OsuSetting.NotifyOnPrivateMessage, true);
            SetDefault(OsuSetting.NotifyOnFriendPresenceChange, true);

            // Audio
            SetDefault(OsuSetting.VolumeInactive, 0.25, 0, 1, 0.01);

            SetDefault(OsuSetting.MenuVoice, true);
            SetDefault(OsuSetting.MenuMusic, true);
            SetDefault(OsuSetting.MenuTips, true);

            SetDefault(OsuSetting.AudioOffset, 0, -500.0, 500.0, 1);

            SetDefault(OsuSetting.AutomaticallyAdjustBeatmapOffset, false);

            // Input
            SetDefault(OsuSetting.MenuCursorSize, 1.0f, 0.5f, 2f, 0.01f);
            SetDefault(OsuSetting.GameplayCursorSize, 1.0f, 0.1f, 2f, 0.01f);
            SetDefault(OsuSetting.GameplayCursorDuringTouch, false);
            SetDefault(OsuSetting.AutoCursorSize, false);

            SetDefault(OsuSetting.MouseDisableButtons, false);
            SetDefault(OsuSetting.MouseDisableWheel, false);
            SetDefault(OsuSetting.ConfineMouseMode, OsuConfineMouseMode.DuringGameplay);

            SetDefault(OsuSetting.TouchDisableGameplayTaps, false);

            // Graphics
            SetDefault(OsuSetting.ShowFpsDisplay, false);

            SetDefault(OsuSetting.ShowStoryboard, true);
            SetDefault(OsuSetting.BeatmapSkins, true);
            SetDefault(OsuSetting.BeatmapColours, true);
            SetDefault(OsuSetting.BeatmapHitsounds, true);

            SetDefault(OsuSetting.CursorRotation, true);

#pragma warning disable CS0612 // Type or member is obsolete (setting default value to avoid risk of any future crashes)
            SetDefault(OsuSetting.MenuParallax, true);
#pragma warning restore CS0612 // Type or member is obsolete
            SetDefault(OsuSetting.MenuParallaxScale, 1.0f, 0.0f, 2.0f, 0.1f);

            // See https://stackoverflow.com/a/63307411 for default sourcing.
            SetDefault(OsuSetting.Prefer24HourTime, !CultureInfoHelper.SystemCulture.DateTimeFormat.ShortTimePattern.Contains(@"tt"));

            // Gameplay
            SetDefault(OsuSetting.PositionalHitsoundsLevel, 0.2f, 0, 1, 0.01f);
            SetDefault(OsuSetting.DimLevel, 0.7, 0, 1, 0.01);
            SetDefault(OsuSetting.BlurLevel, 0, 0, 1, 0.01);
            SetDefault(OsuSetting.LightenDuringBreaks, true);

            SetDefault(OsuSetting.HitLighting, true);
            SetDefault(OsuSetting.StarFountains, true);

            SetDefault(OsuSetting.HUDVisibilityMode, HUDVisibilityMode.Always);
            SetDefault(OsuSetting.ShowHealthDisplayWhenCantFail, true);
            SetDefault(OsuSetting.FadePlayfieldWhenHealthLow, true);
            SetDefault(OsuSetting.KeyOverlay, false);
            SetDefault(OsuSetting.ReplaySettingsOverlay, true);
            SetDefault(OsuSetting.ReplayPlaybackControlsExpanded, true);
            SetDefault(OsuSetting.GameplayLeaderboard, true);
            SetDefault(OsuSetting.AlwaysPlayFirstComboBreak, true);

            SetDefault(OsuSetting.FloatingComments, false);

            SetDefault(OsuSetting.ScoreDisplayMode, ScoringMode.Standardised);

            SetDefault(OsuSetting.IncreaseFirstObjectVisibility, true);
            SetDefault(OsuSetting.GameplayDisableWinKey, true);

            // Update
            SetDefault(OsuSetting.ReleaseStream, ReleaseStream.Lazer);

            SetDefault(OsuSetting.Version, string.Empty);

            SetDefault(OsuSetting.ShowFirstRunSetup, true);
            SetDefault(OsuSetting.ShowMobileDisclaimer, RuntimeInfo.IsMobile);

            SetDefault(OsuSetting.ScreenshotFormat, ScreenshotFormat.Jpg);
            SetDefault(OsuSetting.ScreenshotCaptureMenuCursor, false);

            SetDefault(OsuSetting.Scaling, ScalingMode.Off);
            SetDefault(OsuSetting.SafeAreaConsiderations, true);
            SetDefault(OsuSetting.ScalingBackgroundDim, 0.9f, 0.5f, 1f, 0.01f);

            SetDefault(OsuSetting.ScalingSizeX, 0.8f, 0.2f, 1f, 0.01f);
            SetDefault(OsuSetting.ScalingSizeY, 0.8f, 0.2f, 1f, 0.01f);

            SetDefault(OsuSetting.ScalingPositionX, 0.5f, 0f, 1f, 0.01f);
            SetDefault(OsuSetting.ScalingPositionY, 0.5f, 0f, 1f, 0.01f);

            if (RuntimeInfo.IsMobile)
                SetDefault(OsuSetting.UIScale, 1f, 0.8f, 1.1f, 0.01f);
            else
                SetDefault(OsuSetting.UIScale, 1f, 0.8f, 1.6f, 0.01f);

            SetDefault(OsuSetting.UIHoldActivationDelay, 200.0, 0.0, 500.0, 50.0);

            SetDefault(OsuSetting.IntroSequence, IntroSequence.Triangles);

            SetDefault(OsuSetting.MenuBackgroundSource, BackgroundSource.Skin);
            SetDefault(OsuSetting.SeasonalBackgroundMode, SeasonalBackgroundMode.Sometimes);

            SetDefault(OsuSetting.DiscordRichPresence, DiscordRichPresenceMode.Full);

            SetDefault(OsuSetting.EditorDim, 0.25f, 0f, 1f, 0.25f);
            SetDefault(OsuSetting.EditorWaveformOpacity, 0.25f, 0f, 1f, 0.25f);
            SetDefault(OsuSetting.EditorShowHitMarkers, true);
            SetDefault(OsuSetting.EditorAutoSeekOnPlacement, true);
            SetDefault(OsuSetting.EditorLimitedDistanceSnap, false);
            SetDefault(OsuSetting.EditorShowSpeedChanges, false);
            SetDefault(OsuSetting.EditorScaleOrigin, EditorOrigin.GridCentre);
            SetDefault(OsuSetting.EditorRotationOrigin, EditorOrigin.GridCentre);
            SetDefault(OsuSetting.EditorAdjustExistingObjectsOnTimingChanges, true);

            SetDefault(OsuSetting.HideCountryFlags, false);

            SetDefault(OsuSetting.MultiplayerRoomFilter, RoomPermissionsFilter.All);
            SetDefault(OsuSetting.MultiplayerShowInProgressFilter, true);
            SetDefault(OsuSetting.MultiplayerShowFullFilter, false);

            SetDefault(OsuSetting.LastProcessedMetadataId, -1);

            SetDefault(OsuSetting.ComboColourNormalisationAmount, 0.2f, 0f, 1f, 0.01f);
            SetDefault(OsuSetting.UserOnlineStatus, UserStatus.Online);

            SetDefault(OsuSetting.EditorTimelineShowTimingChanges, true);
            SetDefault(OsuSetting.EditorTimelineShowBreaks, true);
            SetDefault(OsuSetting.EditorTimelineShowTicks, true);

            SetDefault(OsuSetting.EditorContractSidebars, false);

            SetDefault(OsuSetting.EditorTestPlayMods, string.Empty);
            SetDefault(OsuSetting.EditorApplyTestPlayModsToComposer, false);
            SetDefault(OsuSetting.EditorLiveModPreview, true);
            SetDefault(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement, false);
            SetDefault(OsuSetting.EditorFreehandSliderPlacement, true);
            SetDefault(OsuSetting.EditorAlwaysExpandTimelinePointLabels, false);
            SetDefault(OsuSetting.EditorSelectionColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorSelectionThickness, 1f, 0.25f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorSelectionOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorColourScheme, OverlayColourScheme.Aquamarine);
            SetDefault(OsuSetting.EditorSeekAnimationDuration, 300.0, 0.0, 300.0, 50.0);
            SetDefault(OsuSetting.EditorAlwaysExpandLeftToolbox, false);
            SetDefault(OsuSetting.RemoveDragThreshold, false);
            SetDefault(OsuSetting.EditorShowPlayAreaBorder, true);
            SetDefault(OsuSetting.EditorDetectOffscreen, true);
            SetDefault(OsuSetting.EditorNotifyOnBackup, true);
            SetDefault(OsuSetting.EditorToolButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorToggleButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorSampleBankButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorGridButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorActionButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorSliderVelocityPresetSquareness, 0f, 0f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorStatusShowStarRating, true);
            SetDefault(OsuSetting.EditorStatusShowSliderVelocity, true);
            SetDefault(OsuSetting.EditorStatusShowNormalBank, true);
            SetDefault(OsuSetting.EditorStatusShowAdditionBank, true);
            SetDefault(OsuSetting.EditorStatusShowSampleVolume, true);
            SetDefault(OsuSetting.EditorStatusShowKiai, true);
            SetDefault(OsuSetting.EditorStatusShowMods, true);
            // status is the one group a mapper reads rather than operates, so it starts held where it can be read.
            SetDefault(OsuSetting.EditorPinnedToolboxGroups, @"status");
            SetDefault(OsuSetting.EditorHideSelectionButtons, false);
            SetDefault(OsuSetting.EditorHideRotationHandles, false);
            SetDefault(OsuSetting.EditorDefaultAdditionBank, EditorAdditionBank.Auto);
            SetDefault(OsuSetting.EditorHideGridFromPointsTool, false);
            SetDefault(OsuSetting.EditorCarryOverSongSelectMods, false);
            SetDefault(OsuSetting.EditorHideKiaiFlash, false);
            SetDefault(OsuSetting.EditorSelectionHandleColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorSelectionHandleScale, 1f, 0.5f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorSliderAnchorScale, 1f, 0.5f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorShowTimeline, true);
            SetDefault(OsuSetting.EditorCtrlClickCyclesCurveType, false);
            SetDefault(OsuSetting.SwapRightAndMiddleMouse, false);
            SetDefault(OsuSetting.EditorSelectableAfterEndTime, 400.0, 0.0, 2000.0, 50.0);
            SetDefault(OsuSetting.EditorArcOnAnchorAdd, true);
            SetDefault(OsuSetting.EditorShowDifficultyGraph, true);
            SetDefault(OsuSetting.EditorDifficultyGraphColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorDifficultyGraphOpacity, 0.5f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorLoopPlayback, false);
            SetDefault(OsuSetting.EditorAutoBackup, true);
            SetDefault(OsuSetting.EditorAutoBackupInterval, 5.0, 1.0, 30.0, 1.0);
            SetDefault(OsuSetting.EditorAutoBackupKeepCount, 20, 5, 200);
            SetDefault(OsuSetting.EditorHideTimelinePoints, false);
            SetDefault(OsuSetting.EditorTimelineDragAreaScale, 0.5f, 0.5f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorTimelineRepeatDragThreshold, 0.8f, 0.5f, 0.8f, 0.01f);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerScale, 1f, 0.5f, 2f, 0.01f);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorAutoGenerateBreaks, true);
            SetDefault(OsuSetting.EditorCollapsedToolboxGroups, string.Empty);
            SetDefault(OsuSetting.EditorToolboxGroupOrder, string.Empty);

            SetDefault(OsuSetting.AlwaysShowHoldForMenuButton, false);
            SetDefault(OsuSetting.AlwaysRequireHoldingForPause, false);
            SetDefault(OsuSetting.EditorShowStoryboard, true);

            SetDefault(OsuSetting.EditorSubmissionNotifyOnDiscussionReplies, true);
            SetDefault(OsuSetting.EditorSubmissionLoadInBrowserAfterSubmission, true);

            SetDefault(OsuSetting.WasSupporter, false);

            // intentionally uses `DateTime?` and not `DateTimeOffset?` because the latter fails due to `DateTimeOffset` not implementing `IConvertible`
            SetDefault(OsuSetting.LastOnlineTagsPopulation, (DateTime?)null);

            SetDefault(OsuSetting.DashboardSortMode, UserSortCriteria.LastVisit);
            SetDefault(OsuSetting.DashboardDisplayStyle, OverlayPanelDisplayStyle.Card);
        }

        protected override bool CheckLookupContainsPrivateInformation(OsuSetting lookup)
        {
            switch (lookup)
            {
                case OsuSetting.Token:
                    return true;
            }

            return false;
        }

        public override TrackedSettings CreateTrackedSettings()
        {
            return new TrackedSettings
            {
                new TrackedSetting<bool>(OsuSetting.EditorApplyTestPlayModsToComposer, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.EditorToggleComposerModPreview,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.EditorToggleComposerModPreview))
                ),
                new TrackedSetting<bool>(OsuSetting.EditorShowTimeline, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.EditorToggleTimeline,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.EditorToggleTimeline))
                ),
                new TrackedSetting<bool>(OsuSetting.ShowFpsDisplay, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.ToggleFPSCounter,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleFPSDisplay))
                ),
                new TrackedSetting<bool>(OsuSetting.MouseDisableButtons, disabledState => new SettingDescription(
                    rawValue: !disabledState,
                    name: GlobalActionKeyBindingStrings.ToggleGameplayMouseButtons,
                    value: disabledState ? CommonStrings.Disabled.ToLower() : CommonStrings.Enabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleGameplayMouseButtons))
                ),
                new TrackedSetting<bool>(OsuSetting.GameplayLeaderboard, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.ToggleInGameLeaderboard,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleInGameLeaderboard))
                ),
                new TrackedSetting<HUDVisibilityMode>(OsuSetting.HUDVisibilityMode, visibilityMode => new SettingDescription(
                    rawValue: visibilityMode,
                    name: GameplaySettingsStrings.HUDVisibilityMode,
                    value: visibilityMode.GetLocalisableDescription(),
                    shortcut: new TranslatableString(@"_", @"{0}: {1} {2}: {3}",
                        GlobalActionKeyBindingStrings.ToggleInGameInterface,
                        LookupKeyBindings(GlobalAction.ToggleInGameInterface),
                        GlobalActionKeyBindingStrings.HoldForHUD,
                        LookupKeyBindings(GlobalAction.HoldForHUD)))
                ),
                new TrackedSetting<ScalingMode>(OsuSetting.Scaling, scalingMode => new SettingDescription(
                        rawValue: scalingMode,
                        name: GraphicsSettingsStrings.ScreenScaling,
                        value: scalingMode.GetLocalisableDescription()
                    )
                ),
                new TrackedSetting<string>(OsuSetting.Skin, skin =>
                {
                    string skinName = string.Empty;

                    if (Guid.TryParse(skin, out var id))
                        skinName = LookupSkinName(id);

                    return new SettingDescription(
                        rawValue: skinName,
                        name: SkinSettingsStrings.SkinSectionHeader,
                        value: skinName,
                        shortcut: new TranslatableString(@"_", @"{0}: {1}",
                            GlobalActionKeyBindingStrings.RandomSkin,
                            LookupKeyBindings(GlobalAction.RandomSkin))
                    );
                }),
                new TrackedSetting<float>(OsuSetting.UIScale, scale => new SettingDescription(
                        rawValue: scale,
                        name: GraphicsSettingsStrings.UIScaling,
                        value: $"{scale:N2}x"
                        // TODO: implement lookup for framework platform key bindings
                    )
                ),
            };
        }

        public Func<Guid, string> LookupSkinName { private get; set; } = _ => @"unknown";
        public Func<GlobalAction, LocalisableString> LookupKeyBindings { private get; set; } = _ => @"unknown";

        IBindable<float> IGameplaySettings.ComboColourNormalisationAmount => GetOriginalBindable<float>(OsuSetting.ComboColourNormalisationAmount);
        IBindable<float> IGameplaySettings.PositionalHitsoundsLevel => GetOriginalBindable<float>(OsuSetting.PositionalHitsoundsLevel);
    }

    // IMPORTANT: These are used in user configuration files.
    // The naming of these keys should not be changed once they are deployed in a release, unless migration logic is also added.
    public enum OsuSetting
    {
        Ruleset,
        Token,
        MenuCursorSize,
        GameplayCursorSize,
        AutoCursorSize,
        GameplayCursorDuringTouch,
        DimLevel,
        BlurLevel,
        EditorDim,
        LightenDuringBreaks,
        ShowStoryboard,
        KeyOverlay,
        GameplayLeaderboard,
        PositionalHitsoundsLevel,
        AlwaysPlayFirstComboBreak,
        FloatingComments,
        HUDVisibilityMode,

        ShowHealthDisplayWhenCantFail,
        FadePlayfieldWhenHealthLow,

        /// <summary>
        /// Disables mouse buttons clicks during gameplay.
        /// </summary>
        MouseDisableButtons,
        MouseDisableWheel,
        ConfineMouseMode,

        /// <summary>
        /// Globally applied audio offset.
        /// This is added to the audio track's current time. Higher values will cause gameplay to occur earlier, relative to the audio track.
        /// </summary>
        AudioOffset,

        VolumeInactive,
        MenuMusic,
        MenuVoice,
        MenuTips,
        CursorRotation,

        [Obsolete]
        MenuParallax, // todo: can be removed 20270101

        MenuParallaxScale,
        Prefer24HourTime,
        BeatmapDetailTab,
        BeatmapLeaderboardSortMode,
        BeatmapDetailModsFilter,
        Username,
        ReleaseStream,
        SavePassword,
        SaveUsername,
        DisplayStarsMinimum,
        DisplayStarsMaximum,
        SongSelectGroupMode,
        SongSelectSortingMode,
        SongSelectCollectionFilter,
        RandomSelectAlgorithm,
        ModSelectHotkeyStyle,
        ShowFpsDisplay,
        ChatDisplayHeight,
        BeatmapListingCardSize,
        ToolbarClockDisplayMode,
        SongSelectBackgroundBlur,
        Version,
        ShowFirstRunSetup,
        ShowConvertedBeatmaps,
        Skin,
        ScreenshotFormat,
        ScreenshotCaptureMenuCursor,
        BeatmapSkins,
        BeatmapColours,
        BeatmapHitsounds,
        IncreaseFirstObjectVisibility,
        ScoreDisplayMode,
        ExternalLinkWarning,
        PreferNoVideo,
        Scaling,
        ScalingPositionX,
        ScalingPositionY,
        ScalingSizeX,
        ScalingSizeY,
        ScalingBackgroundDim,
        UIScale,
        IntroSequence,
        NotifyOnUsernameMentioned,
        NotifyOnPrivateMessage,
        NotifyOnFriendPresenceChange,
        UIHoldActivationDelay,
        HitLighting,
        StarFountains,
        MenuBackgroundSource,
        GameplayDisableWinKey,
        SeasonalBackgroundMode,
        EditorWaveformOpacity,
        EditorShowHitMarkers,
        EditorAutoSeekOnPlacement,
        DiscordRichPresence,

        ShowOnlineExplicitContent,
        LastProcessedMetadataId,
        SafeAreaConsiderations,
        ComboColourNormalisationAmount,
        ProfileCoverExpanded,
        EditorLimitedDistanceSnap,
        ReplaySettingsOverlay,
        ReplayPlaybackControlsExpanded,
        AutomaticallyDownloadMissingBeatmaps,
        EditorShowSpeedChanges,
        TouchDisableGameplayTaps,
        ModSelectTextSearchStartsActive,

        /// <summary>
        /// The status for the current user to broadcast to other players.
        /// </summary>
        UserOnlineStatus,

        MultiplayerRoomFilter,
        HideCountryFlags,
        EditorTimelineShowTimingChanges,
        EditorTimelineShowTicks,
        AlwaysShowHoldForMenuButton,
        EditorContractSidebars,
        EditorScaleOrigin,
        EditorRotationOrigin,
        EditorTimelineShowBreaks,
        EditorAdjustExistingObjectsOnTimingChanges,
        AlwaysRequireHoldingForPause,
        MultiplayerShowInProgressFilter,
        MultiplayerShowFullFilter,
        BeatmapListingFeaturedArtistFilter,
        ShowMobileDisclaimer,
        EditorShowStoryboard,
        EditorSubmissionNotifyOnDiscussionReplies,
        EditorSubmissionLoadInBrowserAfterSubmission,

        /// <summary>
        /// Cached state of whether local user is a supporter.
        /// Used to allow early checks (ie for startup samples) to be in the correct state, even if the API authentication process has not completed.
        /// </summary>
        WasSupporter,

        LastOnlineTagsPopulation,

        AutomaticallyAdjustBeatmapOffset,

        DashboardSortMode,
        DashboardDisplayStyle,

        /// <summary>
        /// Mods to apply when entering gameplay test from the editor, serialised as a per-ruleset dictionary of <see cref="Online.API.APIMod"/>s.
        /// </summary>
        EditorTestPlayMods,

        /// <summary>
        /// Whether <see cref="EditorTestPlayMods"/> should also be applied to the editor's compose view, not just gameplay test.
        /// </summary>
        EditorApplyTestPlayModsToComposer,

        /// <summary>
        /// Whether the composer's mod preview keeps up with edits as they happen.
        /// Turning this off limits the (potentially expensive) re-derivation to entering compose mode and saving.
        /// </summary>
        EditorLiveModPreview,

        /// <summary>
        /// Whether placing an object should advance the editor clock by the spacing between the two objects preceding it.
        /// </summary>
        EditorAdvanceByPreviousSpacingOnPlacement,

        /// <summary>
        /// Whether sliders can be drawn freehand by dragging during placement.
        /// </summary>
        EditorFreehandSliderPlacement,

        /// <summary>
        /// Whether the timeline's slider velocity / volume / sample bank labels stay expanded even where objects are dense.
        /// </summary>
        EditorAlwaysExpandTimelinePointLabels,

        /// <summary>
        /// Hex colour overriding the editor's selection outlines. Empty leaves each piece at its default.
        /// </summary>
        EditorSelectionColour,

        /// <summary>
        /// Multiplier applied to the border thickness of the editor's selection outlines.
        /// </summary>
        EditorSelectionThickness,

        /// <summary>
        /// Opacity of the editor's selection outlines.
        /// </summary>
        EditorSelectionOpacity,

        /// <summary>
        /// Colour scheme the editor's own chrome is tinted with.
        /// </summary>
        EditorColourScheme,

        /// <summary>
        /// Duration in milliseconds of the editor's smooth seek animation. Zero seeks instantly.
        /// </summary>
        EditorSeekAnimationDuration,

        /// <summary>
        /// Whether the composer's left toolbox stays expanded rather than expanding on hover.
        /// </summary>
        EditorAlwaysExpandLeftToolbox,

        /// <summary>
        /// Distance in pixels the mouse must travel before a left-button drag begins.
        /// Lowering it makes small nudges possible in the editor, but applies game-wide.
        /// </summary>
        RemoveDragThreshold,

        /// <summary>
        /// Whether the editor outlines the area that stays visible during gameplay.
        /// </summary>
        EditorShowPlayAreaBorder,

        /// <summary>
        /// Whether the editor marks objects which leave the visible play area.
        /// </summary>
        EditorDetectOffscreen,

        /// <summary>
        /// Whether a notification is posted each time a backup is written.
        /// </summary>
        EditorNotifyOnBackup,

        /// <summary>
        /// How far the tool buttons are stretched towards being square, from 0 (stock) to 1 (square).
        /// </summary>
        EditorToolButtonSquareness,

        /// <summary>
        /// How far the toggle buttons are stretched towards being square, from 0 (stock) to 1 (square).
        /// </summary>
        EditorToggleButtonSquareness,

        /// <summary>
        /// Titles of the editor toolbox groups the user pinned to the top, separated by <c>|</c>.
        /// Stored by title because the groups themselves are created fresh per ruleset and editor session.
        /// </summary>
        EditorPinnedToolboxGroups,

        /// <summary>
        /// Whether the rotate, flip and reverse buttons appear above a selection.
        /// The keyboard shortcuts work regardless.
        /// </summary>
        EditorHideSelectionButtons,

        /// <summary>
        /// Hex colour of the selection box's drag handles, or empty to keep the stock colour.
        /// </summary>
        EditorSelectionHandleColour,

        /// <summary>
        /// Multiplier applied to the size of the selection box's drag handles.
        /// </summary>
        EditorSelectionHandleScale,

        /// <summary>
        /// Multiplier applied to the size of slider path anchors.
        /// </summary>
        EditorSliderAnchorScale,

        /// <summary>
        /// Whether the timeline is shown above the editor's main content.
        /// </summary>
        EditorShowTimeline,

        /// <summary>
        /// Whether ctrl+clicking a slider control point cycles its curve type.
        /// While on, ctrl no longer adds control points to the selection.
        /// </summary>
        EditorCtrlClickCyclesCurveType,

        /// <summary>
        /// Whether the right and middle mouse buttons are swapped while the editor is the active screen.
        /// </summary>
        SwapRightAndMiddleMouse,

        /// <summary>
        /// How long in milliseconds an object stays selectable after its time has passed.
        /// </summary>
        EditorSelectableAfterEndTime,

        /// <summary>
        /// Whether adding a slider anchor turns the segment it lands in into a perfect curve, where that segment
        /// ends up with the three points a perfect curve needs.
        /// </summary>
        EditorArcOnAnchorAdd,

        /// <summary>
        /// Whether the summary timeline shows where the beatmap's difficulty sits over its length.
        /// </summary>
        EditorShowDifficultyGraph,

        /// <summary>
        /// Colour of the difficulty graph drawn behind the summary timeline.
        /// </summary>
        EditorDifficultyGraphColour,

        /// <summary>
        /// Opacity of the difficulty graph.
        /// </summary>
        EditorDifficultyGraphOpacity,

        /// <summary>
        /// Whether editor playback returns to the start of the beatmap on reaching the end rather than stopping.
        /// </summary>
        EditorLoopPlayback,

        /// <summary>
        /// Whether the editor periodically writes a copy of the beatmap being edited to the backups directory.
        /// </summary>
        EditorAutoBackup,

        /// <summary>
        /// Minutes between automatic backups.
        /// </summary>
        EditorAutoBackupInterval,

        /// <summary>
        /// How many backups to keep per difficulty before the oldest are deleted.
        /// </summary>
        EditorAutoBackupKeepCount,

        /// <summary>
        /// Whether the handles for turning a selection by dragging its corners are left off the selection box.
        /// </summary>
        EditorHideRotationHandles,

        /// <summary>
        /// The addition bank a selection starts on, which is the bank handed to a whistle, finish or clap as it is added.
        /// </summary>
        EditorDefaultAdditionBank,

        /// <summary>
        /// Whether the grid-from-points tool is left out of the composer toolbox.
        /// </summary>
        EditorHideGridFromPointsTool,

        /// <summary>
        /// Whether song select's mod selection and the editor's test play mods are one and the same.
        /// </summary>
        EditorCarryOverSongSelectMods,

        /// <summary>
        /// Whether kiai's flashing is left out of the editor.
        /// </summary>
        EditorHideKiaiFlash,

        /// <summary>
        /// Whether the slider velocity and sample point pieces are left out of the timeline, giving the objects the room instead.
        /// </summary>
        EditorHideTimelinePoints,

        /// <summary>
        /// The size the circle at the end of a timeline object rests at, which is dragged to lengthen it.
        /// </summary>
        EditorTimelineDragAreaScale,

        /// <summary>
        /// How far towards the next repeat a slider has to be dragged in the timeline before the repeat is added.
        /// </summary>
        EditorTimelineRepeatDragThreshold,

        /// <summary>
        /// Colour of the marker dragged to lengthen a slider in the composer.
        /// </summary>
        EditorSliderEndDragMarkerColour,

        /// <summary>
        /// Scales the marker dragged to lengthen a slider in the composer.
        /// </summary>
        EditorSliderEndDragMarkerScale,

        /// <summary>
        /// Opacity of the marker dragged to lengthen a slider in the composer.
        /// </summary>
        EditorSliderEndDragMarkerOpacity,

        /// <summary>
        /// Whether breaks are inserted on the editor's own initiative wherever objects are far enough apart.
        /// </summary>
        EditorAutoGenerateBreaks,

        /// <summary>
        /// How square the sample bank buttons are stretched towards.
        /// </summary>
        EditorSampleBankButtonSquareness,

        /// <summary>
        /// How square the grid type buttons are stretched towards.
        /// </summary>
        EditorGridButtonSquareness,

        /// <summary>
        /// How square the transform and generate buttons are stretched towards.
        /// </summary>
        EditorActionButtonSquareness,

        /// <summary>
        /// Titles of the editor toolbox groups the user folded away, separated by <c>|</c>.
        /// </summary>
        EditorCollapsedToolboxGroups,

        /// <summary>
        /// How square the slider velocity preset buttons are stretched towards.
        /// </summary>
        EditorSliderVelocityPresetSquareness,

        /// <summary>
        /// The order the user put the editor toolbox groups in, separated by <c>|</c>.
        /// </summary>
        EditorToolboxGroupOrder,
        /// <summary>
        /// Whether the SR row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowStarRating,

        /// <summary>
        /// Whether the SV row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowSliderVelocity,

        /// <summary>
        /// Whether the Normal bank row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowNormalBank,

        /// <summary>
        /// Whether the Addition bank row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowAdditionBank,

        /// <summary>
        /// Whether the Volume row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowSampleVolume,

        /// <summary>
        /// Whether the Kiai row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowKiai,

        /// <summary>
        /// Whether the Mods row is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowMods,

        /// <summary>
        /// How solid the difficulty drawn underneath the one being edited is.
        /// </summary>
        EditorReferenceDifficultyOpacity,

        /// <summary>
        /// What colour the difficulty drawn underneath the one being edited is tinted.
        /// </summary>
        EditorReferenceDifficultyColour,
    }
}
