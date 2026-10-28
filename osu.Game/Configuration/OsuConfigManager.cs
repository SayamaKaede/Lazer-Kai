// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.Reflection;
using osu.Framework;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Configuration.Tracking;
using osu.Framework.Extensions;
using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Beatmaps.Drawables.Cards;
using osu.Game.Graphics;
using osu.Game.Input;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.Leaderboards;
using osu.Game.Overlays;
using osu.Game.Overlays.Dashboard.Friends;
using osu.Game.Overlays.Mods.Input;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Hitsounds;
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
            SetDefault(OsuSetting.EditorTimelineShowNightcoreCymbals, false);

            SetDefault(OsuSetting.EditorContractSidebars, false);

            SetDefault(OsuSetting.EditorTestPlayMods, string.Empty);
            SetDefault(OsuSetting.EditorApplyTestPlayModsToComposer, false);
            SetDefault(OsuSetting.EditorLiveModPreview, true);
            SetDefault(OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement, false);
            SetDefault(OsuSetting.EditorManiaAdvanceByPreviousSpacingOnPlacement, false);
            SetDefault(OsuSetting.SongSelectOpensEditor, false);
            SetDefault(OsuSetting.SongSelectDateUpdatedSplitsDifficulties, false);
            SetDefault(OsuSetting.EditorDiscussionOpacity, 1f, 0.2f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorAsmrSounds, false);
            SetDefault(OsuSetting.EditorAsmrVolume, 0.6f, 0f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorFreehandSliderPlacement, true);
            SetDefault(OsuSetting.EditorAlwaysExpandTimelinePointLabels, false);
            SetDefault(OsuSetting.EditorSelectionColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorSelectionThickness, 1f, 0.25f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorSelectionOpacity, 1f, 0.1f, 1f, 0.01f);

            // the same stock values the box uses, so splitting the two changed nothing anyone was looking at.
            SetDefault(OsuSetting.EditorObjectOutlineColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorObjectOutlineThickness, 1f, 0.25f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorObjectOutlineOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.BeatmapExportFormat, BeatmapExportFormat.Stable);
            SetDefault(OsuSetting.EditorColourScheme, OverlayColourScheme.Aquamarine);
            SetDefault(OsuSetting.EditorSeekAnimationDuration, 300.0, 0.0, 300.0, 50.0);
            SetDefault(OsuSetting.EditorAlwaysExpandLeftToolbox, false);
            SetDefault(OsuSetting.EditorLeftToolboxWidth, 200f, 120f, 400f, 1f);
            SetDefault(OsuSetting.EditorRightToolboxWidth, 250f, 150f, 500f, 1f);
            SetDefault(OsuSetting.RemoveDragThreshold, false);
            SetDefault(OsuSetting.EditorShowPlayAreaBorder, true);
            SetDefault(OsuSetting.EditorShowAutoCursor, false);
            SetDefault(OsuSetting.EditorDetectOffscreen, true);
            SetDefault(OsuSetting.EditorNotifyOnBackup, true);
            SetDefault(OsuSetting.EditorToolButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorToggleButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorSampleBankButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorGridButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorActionButtonSquareness, 0f, 0f, 0.3f, 0.01f);
            SetDefault(OsuSetting.EditorSliderVelocityPresetSquareness, 0f, 0f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorStatusShowStarRating, true);
            SetDefault(OsuSetting.EditorStatusShowMaxPerformance, true);
            SetDefault(OsuSetting.EditorStatusShowScore, true);
            SetDefault(OsuSetting.EditorStatusShowLegacyScore, true);
            SetDefault(OsuSetting.EditorStatusShowSliderVelocity, true);
            SetDefault(OsuSetting.EditorStatusShowNormalBank, true);
            SetDefault(OsuSetting.EditorStatusShowAdditionBank, true);
            SetDefault(OsuSetting.EditorStatusShowSampleVolume, true);
            SetDefault(OsuSetting.EditorStatusShowKiai, true);
            SetDefault(OsuSetting.EditorStatusShowMods, true);
            // on, with the rest of the rows that describe the moment the playhead is at.
            SetDefault(OsuSetting.EditorStatusShowCombo, true);
            // off by default: these describe the beatmap as a whole rather than the place being worked on, so they
            // are checked now and then rather than watched, and the panel is a narrow column.
            SetDefault(OsuSetting.EditorStatusShowObjectCount, false);
            SetDefault(OsuSetting.EditorStatusShowDrainTime, false);
            SetDefault(OsuSetting.EditorStatusShowDifficultySettings, false);
            SetDefault(OsuSetting.EditorReferenceDifficultyOpacity, 0.3f, 0.05f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorReferenceDifficultyColour, EditorSelectionColour.Cyan);
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
            SetDefault(OsuSetting.EditorShowUnsnapMarkers, true);
            SetDefault(OsuSetting.EditorGridDefaultX, 256f, 0f, 512f, 0.1f);
            SetDefault(OsuSetting.EditorGridDefaultY, 192f, 0f, 384f, 0.1f);
            SetDefault(OsuSetting.EditorGridDefaultSpacing, 4f, 4f, 256f, 0.1f);
            SetDefault(OsuSetting.EditorGridDefaultRotation, 0f, -180f, 180f, 0.1f);
            SetDefault(OsuSetting.EditorGridDefaultType, EditorGridShape.Square);
            SetDefault(OsuSetting.MappingToolsPath, string.Empty);

            // empty for "not set": that ruleset then gets the skin setting's skin, as every ruleset did before these.
            SetDefault(OsuSetting.RulesetSkinOsu, string.Empty);
            SetDefault(OsuSetting.RulesetSkinTaiko, string.Empty);
            SetDefault(OsuSetting.RulesetSkinCatch, string.Empty);
            SetDefault(OsuSetting.RulesetSkinMania, string.Empty);

            // on by default, which is what the editor did before there was a switch. Somebody who has not gone
            // looking for this should not find the beatmap they opened looking like a different beatmap.
            SetDefault(OsuSetting.EditorBeatmapColours, true);
            SetDefault(OsuSetting.EditorBeatmapHitsounds, true);

            SetDefault(OsuSetting.EditorStableSliderControls, true);
            SetDefault(OsuSetting.SwapRightAndMiddleMouse, false);
            SetDefault(OsuSetting.EditorSelectableAfterEndTime, 400.0, 0.0, 2000.0, 50.0);
            SetDefault(OsuSetting.EditorSliderAnchorType, SliderAnchorType.PerfectCurve);
            SetDefault(OsuSetting.EditorAllowOutsidePlayArea, false);
            SetDefault(OsuSetting.EditorSliderAnchorTypeForPlacement, SliderPlacementAnchorType.Automatic);
            SetDefault(OsuSetting.EditorTimelineInspector, false);
            SetDefault(OsuSetting.EditorIntegerPositions, true);
            SetDefault(OsuSetting.EditorVolumeGraphIgnoreFivePercent, false);
            // on from the start (2026-10-23): without it a stable beatmap opened and saved here has its green lines put
            // where its objects are, and somebody new would only find out once it had happened. Somebody already using
            // Lazer-Kai keeps what they had, since the settings file says it either way.
            SetDefault(OsuSetting.StableCompatibility, true);
            SetDefault(OsuSetting.EditorTimelineShowSavedLines, false);
            SetDefault(OsuSetting.EditorStatusShowSaveFormat, true);
            SetDefault(OsuSetting.ErrorReporting, true);
            SetDefault(OsuSetting.ReplayShowMissTimeline, false);
            SetDefault(OsuSetting.EditorShowDifficultyGraph, true);
            SetDefault(OsuSetting.EditorDifficultyGraphColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorDifficultyGraphOpacity, 0.5f, 0.1f, 1f, 0.01f);

            SetDefault(OsuSetting.EditorShowVolumeGraph, false);
            SetDefault(OsuSetting.EditorVolumeGraphColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorVolumeGraphThickness, 2f, 1f, 6f, 0.5f);
            SetDefault(OsuSetting.EditorVolumeGraphOpacity, 0.8f, 0.1f, 1f, 0.01f);
            // the top of the range is where the column count stops, so every part of the slider does something.
            SetDefault(OsuSetting.EditorDifficultyGraphDetail, DifficultyGraphPart.DEFAULT_DETAIL, 0.25f, 3f, 0.05f);
            // the detail screen is a room rather than a strip, so how finely it is worth drawing there is a separate
            // question. these three are remembered rather than offered in the settings: they belong to the screen.
            SetDefault(OsuSetting.EditorDifficultyGraphOverlayDetail, DifficultyGraphPart.DEFAULT_DETAIL, 0.25f, 3f, 0.05f);
            SetDefault(OsuSetting.EditorDifficultyGraphOverlayShowSkills, true);
            SetDefault(OsuSetting.EditorDifficultyGraphOverlayApplyMods, false);
            // never all the way down: a screen that can be made invisible while still taking the mouse is a screen
            // someone loses.
            SetDefault(OsuSetting.EditorDifficultyGraphOverlayOpacity, 1f, 0.3f, 1f, 0.05f);
            SetDefault(OsuSetting.EditorLoopPlayback, false);
            SetDefault(OsuSetting.EditorAutoBackup, true);
            SetDefault(OsuSetting.EditorAutoBackupInterval, 5.0, 1.0, 30.0, 1.0);
            SetDefault(OsuSetting.EditorAutoBackupKeepCount, 20, 5, 200);
            SetDefault(OsuSetting.EditorHideTimelinePoints, false);

            SetDefault(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer);
            SetDefault(OsuSetting.EditorTimelineDragToSeek, true);
            SetDefault(OsuSetting.EditorTimelineUseSkin, false);
            SetDefault(OsuSetting.EditorHideTimelineVolume, false);

            SetDefault(OsuSetting.CollabOverlayOpacity, 1f, 0.2f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorTimelineDragAreaScale, 0.5f, 0.5f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorTimelineRepeatDragThreshold, 0.8f, 0.5f, 0.8f, 0.01f);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerScale, 1f, 0.5f, 2f, 0.01f);
            SetDefault(OsuSetting.EditorSliderEndDragMarkerOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorDistanceSnapGridThickness, 1f, 0.25f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorDistanceSnapGridOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorOperationLog, false);

            // the hitsound screen's own: what is placed shows faintly through it, and what is selected on the timeline is
            // what it changes, so its outline can be made to stand out there more than everywhere else.
            SetDefault(OsuSetting.EditorHitsoundPanelOpacity, 0.85f, 0.3f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorHitsoundOutlineColour, EditorSelectionColour.Default);
            SetDefault(OsuSetting.EditorHitsoundOutlineThickness, 1f, 0.25f, 3f, 0.01f);
            SetDefault(OsuSetting.EditorHitsoundOutlineOpacity, 1f, 0.1f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorHitsoundChangeSound, HitsoundChangeSound.Menu);
            SetDefault(OsuSetting.EditorHitsoundAdvanceByPreviousSpacing, false);
            SetDefault(OsuSetting.EditorHitsoundShowHarmlessUnsnaps, false);
            SetDefault(OsuSetting.EditorManiaHideKeys, true);
            // as the user set them with the three of them to try (2026-10-12).
            SetDefault(OsuSetting.EditorManiaStageSpacing, 0f, 0f, 300f, 1f);
            SetDefault(OsuSetting.EditorManiaTickThickness, 1f, 1f, 8f, 0.5f);
            SetDefault(OsuSetting.EditorManiaTickOpacity, 0.5f, 0.05f, 1f, 0.01f);
            SetDefault(OsuSetting.EditorStreamSnapSpacingWhole, true);
            SetDefault(OsuSetting.EditorStreamSnapSpacingLastOfSeveral, false);
            SetDefault(OsuSetting.EditorAutoGenerateBreaks, true);
            SetDefault(OsuSetting.EditorAutoNewCombo, false);
            SetDefault(OsuSetting.EditorAutoNewComboInterval, AutoNewComboInterval.OneBar);
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

            SetDefault(OsuSetting.PMFriendsOnly, false);

            // the look the game wears, which is nothing to do with the editor and so is kept away from it.
            SetDefault(OsuSetting.UiTheme, UiPaletteValues.Default.Theme);

        }

        /// <summary>
        /// A setting's value as the settings file writes it, for <see cref="LazerKaiSettingsPreset"/>.
        /// </summary>
        internal string GetText(OsuSetting lookup) => ConfigStore[lookup].ToString(CultureInfo.InvariantCulture);

        /// <summary>
        /// Sets a setting from its value as the settings file writes it.
        /// </summary>
        /// <remarks>
        /// A setting the settings screen has greyed out is set all the same. The screen greys one out by disabling what
        /// it shows, which is the setting itself, and a setting that is disabled throws when it is set: a preset
        /// applied while automatic new combos were off, when their interval is greyed out, took the game down
        /// (DOTNET-J). Greyed out is that the setting does nothing as things are, not that it is not to be changed, so
        /// it is set and left greyed out.
        /// </remarks>
        internal void SetText(OsuSetting lookup, string value)
        {
            var setting = ConfigStore[lookup];

            if (setting is not IParseable parseable)
                return;

            if (!setting.Disabled)
            {
                parseable.Parse(value, CultureInfo.InvariantCulture);
                return;
            }

            // of a bindable of whatever kind the setting is: nothing that all of them are says how to enable one.
            var disabled = setting.GetType().GetProperty(nameof(Bindable<bool>.Disabled));

            if (disabled == null)
                return;

            try
            {
                disabled.SetValue(setting, false);
            }
            catch (TargetInvocationException)
            {
                // somebody has it to themselves, which is another thing than greyed out, and it is theirs to set.
                return;
            }

            try
            {
                parseable.Parse(value, CultureInfo.InvariantCulture);
            }
            finally
            {
                disabled.SetValue(setting, true);
            }
        }

        protected override bool CheckLookupContainsPrivateInformation(OsuSetting lookup)
        {
            switch (lookup)
            {
                case OsuSetting.Token:
                // the configuration goes out with every error report, and the account name is who sent it.
                case OsuSetting.Username:
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
                new TrackedSetting<bool>(OsuSetting.ReplayShowMissTimeline, state => new SettingDescription(
                    rawValue: state,
                    name: GlobalActionKeyBindingStrings.ToggleReplayMissTimeline,
                    value: state ? CommonStrings.Enabled.ToLower() : CommonStrings.Disabled.ToLower(),
                    shortcut: LookupKeyBindings(GlobalAction.ToggleReplayMissTimeline))
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
                // Lazer-Kai: a skin picked while the selected ruleset has one of its own goes into that ruleset's setting
                // rather than the one above, and has to be announced from there too. The skin shortcuts are pressed
                // blind, and this toast is the only way to tell which skin they landed on.
                rulesetSkinToast(OsuSetting.RulesetSkinOsu, @"osu!"),
                rulesetSkinToast(OsuSetting.RulesetSkinTaiko, @"osu!taiko"),
                rulesetSkinToast(OsuSetting.RulesetSkinCatch, @"osu!catch"),
                rulesetSkinToast(OsuSetting.RulesetSkinMania, @"osu!mania"),
                new TrackedSetting<float>(OsuSetting.UIScale, scale => new SettingDescription(
                        rawValue: scale,
                        name: GraphicsSettingsStrings.UIScaling,
                        value: $"{scale:N2}x"
                        // TODO: implement lookup for framework platform key bindings
                    )
                ),
            };
        }

        /// <summary>
        /// The skin toast, for one ruleset's own skin. The ruleset is named in the heading, since the skin setting's own
        /// toast looks the same otherwise and the two mean different things.
        /// </summary>
        private TrackedSetting<string> rulesetSkinToast(OsuSetting setting, string rulesetName) => new TrackedSetting<string>(setting, skin =>
        {
            LocalisableString skinName = Guid.TryParse(skin, out var id)
                ? LookupSkinName(id)
                : LazerKaiSettingsStrings.RulesetSkinNotSet;

            return new SettingDescription(
                rawValue: skin,
                name: new TranslatableString(@"_", @"{0} ({1})", SkinSettingsStrings.SkinSectionHeader, rulesetName),
                value: skinName,
                shortcut: new TranslatableString(@"_", @"{0}: {1}",
                    GlobalActionKeyBindingStrings.RandomSkin,
                    LookupKeyBindings(GlobalAction.RandomSkin))
            );
        });

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
        /// Blocks private messages, multiplayer room invites, and duel requests from people not on the user's friends list.
        /// </summary>
        PMFriendsOnly,

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

        /// <summary>Whether an object placed by hand is given a new combo when it opens a bar.</summary>
        EditorAutoNewCombo,

        /// <summary>How far apart those combos are wanted.</summary>
        EditorAutoNewComboInterval,

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

        /// <summary>Whether the editor draws where autoplay's cursor would be.</summary>
        EditorShowAutoCursor,

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
        /// Whether objects that do not sit on the beat are marked in the timeline as they are placed.
        /// </summary>
        EditorShowUnsnapMarkers,

        /// <summary>
        /// The grid origin's X the editor opens with.
        /// </summary>
        EditorGridDefaultX,

        /// <summary>
        /// The grid origin's Y the editor opens with.
        /// </summary>
        EditorGridDefaultY,

        /// <summary>
        /// The grid spacing the editor opens with, in place of the one the beatmap last saved.
        /// </summary>
        EditorGridDefaultSpacing,

        /// <summary>
        /// The grid rotation the editor opens with.
        /// </summary>
        EditorGridDefaultRotation,

        /// <summary>
        /// The grid shape the editor opens with.
        /// </summary>
        EditorGridDefaultType,

        /// <summary>
        /// Whether sliders are shaped the way stable shapes them: an anchor added with ctrl+click gives its part of
        /// the path a shape without splitting it, and ctrl+clicking an anchor makes a corner of it or changes the shape
        /// between the corners either side of it. While on, ctrl no longer adds control points to the selection.
        /// </summary>
        EditorStableSliderControls,

        /// <summary>
        /// Whether the right and middle mouse buttons are swapped while the editor is the active screen.
        /// </summary>
        SwapRightAndMiddleMouse,

        /// <summary>
        /// How long in milliseconds an object stays selectable after its time has passed.
        /// </summary>
        EditorSelectableAfterEndTime,

        /// <summary>
        /// The shape an anchor added with ctrl+click gives the part of the path it lands in.
        /// </summary>
        EditorSliderAnchorType,

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
        /// How densely the editor's difficulty graph is drawn, as a proportion of its usual detail.
        /// </summary>
        EditorDifficultyGraphDetail,

        /// <summary>
        /// Whether the volume is drawn over the summary timeline.
        /// </summary>
        EditorShowVolumeGraph,

        /// <summary>
        /// Colour of that line.
        /// </summary>
        EditorVolumeGraphColour,

        /// <summary>
        /// How thick it is drawn.
        /// </summary>
        EditorVolumeGraphThickness,

        /// <summary>
        /// How solid it is drawn.
        /// </summary>
        EditorVolumeGraphOpacity,

        /// <summary>
        /// How densely the difficulty graph's detail screen is drawn, as a proportion of its usual detail.
        /// </summary>
        EditorDifficultyGraphOverlayDetail,

        /// <summary>
        /// Whether the difficulty graph's detail screen has its per-skill rows unfolded.
        /// </summary>
        EditorDifficultyGraphOverlayShowSkills,

        /// <summary>
        /// Whether the difficulty graph's detail screen describes the beatmap with the current mods applied.
        /// </summary>
        EditorDifficultyGraphOverlayApplyMods,

        /// <summary>
        /// How solid the difficulty graph's detail screen is drawn.
        /// </summary>
        EditorDifficultyGraphOverlayOpacity,

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
        /// Whether the row for what an SS would be worth is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowMaxPerformance,

        /// <summary>
        /// Whether the row for the highest total on this build's scoring is shown in the composer's status panel.
        /// </summary>
        /// <summary>
        /// How wide the composer's left toolbox opens.
        /// </summary>
        EditorLeftToolboxWidth,

        /// <summary>
        /// How wide the composer's right toolbox opens.
        /// </summary>
        EditorRightToolboxWidth,

        EditorStatusShowScore,

        /// <summary>
        /// Whether the row for the highest total under stable's scoring is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowLegacyScore,

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
        /// Whether the hit object count is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowObjectCount,

        /// <summary>
        /// Whether the drain time is shown in the composer's status panel.
        /// </summary>
        EditorStatusShowDrainTime,

        /// <summary>
        /// Whether the approach rate, circle size, overall difficulty and drain rate are shown in the composer's
        /// status panel.
        /// </summary>
        EditorStatusShowDifficultySettings,

        /// <summary>
        /// How solid the difficulty drawn underneath the one being edited is.
        /// </summary>
        EditorReferenceDifficultyOpacity,

        /// <summary>
        /// What colour the difficulty drawn underneath the one being edited is tinted.
        /// </summary>
        EditorReferenceDifficultyColour,

        /// <summary>
        /// Where Mapping Tools is installed. Empty means the usual install is looked for instead.
        /// </summary>
        MappingToolsPath,

        /// <summary>
        /// Whether the editor uses the beatmap's own combo colours. Off falls back to the selected skin's.
        /// </summary>
        EditorBeatmapColours,

        /// <summary>
        /// Whether the editor uses the beatmap's own hitsounds. Off falls back to the selected skin's.
        /// </summary>
        EditorBeatmapHitsounds,

        /// <summary>
        /// Whether the status panel shows the combo a player would be on at the playhead.
        /// </summary>
        EditorStatusShowCombo,

        /// <summary>
        /// How the timeline lays its objects, ticks and point rows out.
        /// </summary>
        EditorTimelineStyle,

        /// <summary>
        /// Whether the timeline can be dragged to move the playhead.
        /// </summary>
        EditorTimelineDragToSeek,

        /// <summary>How solid the collab manager is drawn, so the beatmap behind it can be checked.</summary>
        CollabOverlayOpacity,

        /// <summary>
        /// Whether the timeline draws its objects with the skin's own circles, ends and reverse arrows.
        /// </summary>
        EditorTimelineUseSkin,

        /// <summary>Whether the timeline's volume row alone gives up its space to the objects.</summary>
        EditorHideTimelineVolume,

        /// <summary>The colour of the outline drawn around a selected object itself.</summary>
        EditorObjectOutlineColour,

        /// <summary>How thick that outline is drawn.</summary>
        EditorObjectOutlineThickness,

        /// <summary>How solid that outline is drawn.</summary>
        EditorObjectOutlineOpacity,

        /// <summary>Which format song select writes a beatmap out in.</summary>
        BeatmapExportFormat,

        /// <summary>Which look the game paints itself in.</summary>
        UiTheme,

        /// <summary>The skin put on while osu! is selected. Empty for the skin setting's.</summary>
        RulesetSkinOsu,

        /// <summary>The skin put on while osu!taiko is selected. Empty for the skin setting's.</summary>
        RulesetSkinTaiko,

        /// <summary>The skin put on while osu!catch is selected. Empty for the skin setting's.</summary>
        RulesetSkinCatch,

        /// <summary>The skin put on while osu!mania is selected. Empty for the skin setting's.</summary>
        RulesetSkinMania,

        /// <summary>Whether the top timeline marks where nightcore's cymbal falls.</summary>
        EditorTimelineShowNightcoreCymbals,

        /// <summary>
        /// What a stream of one stretch is told about snapping its spacing, which is the last thing the panel was
        /// told rather than a setting of its own.
        /// </summary>
        EditorStreamSnapSpacingWhole,

        /// <summary>
        /// The same for the last stretch of a stream that turns a corner, which is remembered apart: a shape with
        /// corners is being shaped by hand, and starts out following the hand rather than the ticks.
        /// </summary>
        EditorStreamSnapSpacingLastOfSeveral,

        /// <summary>
        /// Whether a selection may be moved and scaled past the edges of the play area, instead of being pushed back
        /// inside after every drag.
        /// </summary>
        EditorAllowOutsidePlayArea,

        /// <summary>
        /// The shape a slider is drawn with while it is being placed, or automatic to leave it to the number of
        /// points in the segment.
        /// </summary>
        EditorSliderAnchorTypeForPlacement,

        /// <summary>
        /// Whether what the inspector says about the selection is shown above the beat divisor, in place of the row
        /// of ticks.
        /// </summary>
        EditorTimelineInspector,

        /// <summary>
        /// Whether objects are put on whole coordinates, since a fraction of a pixel survives here and nowhere else.
        /// </summary>
        EditorIntegerPositions,

        /// <summary>
        /// Whether the volume graph passes over a volume of 5% as if none were set there, carrying on at the volume
        /// before it.
        /// </summary>
        EditorVolumeGraphIgnoreFivePercent,

        /// <summary>
        /// Whether a beatmap in stable's format is kept in it: saved with every line that was not edited exactly as it
        /// was read, and exported for stable as it is. Experimental.
        /// </summary>
        StableCompatibility,

        /// <summary>
        /// Whether the timeline shows the red and green lines of the beatmap's saved file, the way stable's has them.
        /// </summary>
        EditorTimelineShowSavedLines,

        /// <summary>
        /// Whether the STATUS panel says which format the next save writes, while stable compatibility is on.
        /// </summary>
        EditorStatusShowSaveFormat,

        /// <summary>
        /// Whether errors, and saves that could not stay in stable's format, are sent to Lazer-Kai's error reports.
        /// </summary>
        ErrorReporting,

        /// <summary>
        /// Whether a timeline of what a replay missed is shown along the bottom as it is watched.
        /// </summary>
        ReplayShowMissTimeline,

        /// <summary>
        /// How opaque the hitsound screen is over the playfield, which shows through it.
        /// </summary>
        EditorHitsoundPanelOpacity,

        /// <summary>
        /// The colour of the outline around what is selected on the timeline, while the hitsound screen is showing.
        /// </summary>
        EditorHitsoundOutlineColour,

        /// <summary>
        /// The thickness of that outline, scaling the stock one.
        /// </summary>
        EditorHitsoundOutlineThickness,

        /// <summary>
        /// The opacity of that outline.
        /// </summary>
        EditorHitsoundOutlineOpacity,

        /// <summary>
        /// What the hitsound screen plays as a change is made.
        /// </summary>
        EditorHitsoundChangeSound,

        /// <summary>
        /// Whether the playhead moves on by the gap before a circle the hitsound screen places, as
        /// <see cref="EditorAdvanceByPreviousSpacingOnPlacement"/> does on the compose screen.
        /// </summary>
        EditorHitsoundAdvanceByPreviousSpacing,

        /// <summary>
        /// Whether the hitsound screen also lists the green lines off a sound that the sound takes all the same.
        /// </summary>
        EditorHitsoundShowHarmlessUnsnaps,

        /// <summary>
        /// Whether the osu!mania editor leaves out the keys under the judgement line, and shows the lanes going on
        /// there with the notes that have gone by, as stable's editor does.
        /// </summary>
        EditorManiaHideKeys,

        /// <summary>
        /// How far apart, in pixels of the playfield, the osu!mania editor puts the stages of a difficulty with more
        /// than one. Left where the game puts them, each is in the middle of its own half, however far that is.
        /// </summary>
        EditorManiaStageSpacing,

        /// <summary>
        /// How thick the osu!mania editor draws the lines of the ticks across the lanes, in pixels.
        /// </summary>
        EditorManiaTickThickness,

        /// <summary>
        /// How opaque the osu!mania editor draws the lines of the ticks across the lanes.
        /// </summary>
        EditorManiaTickOpacity,

        /// <summary>
        /// What <see cref="EditorAdvanceByPreviousSpacingOnPlacement"/> is in the osu!mania editor, which has its own:
        /// notes are put down side by side at one time there, and seeking on after the first of them takes the
        /// playhead away from the rest. Off unless it is turned on (2026-10-14).
        /// </summary>
        EditorManiaAdvanceByPreviousSpacingOnPlacement,

        /// <summary>
        /// Whether song select opens a beatmap in the editor where it would start playing it, and plays it where it
        /// would open it in the editor: for somebody who goes there to map far more often than to play (2026-10-18).
        /// </summary>
        SongSelectOpensEditor,

        /// <summary>
        /// Whether song select, sorted by <see cref="SortMode.DateUpdated"/>, lists each difficulty as a panel of its own
        /// the way stable does, with one set's in a row stacked and the selected set's brought together (2026-10-19).
        /// By any other sort that keeps sets together, the selected set's difficulties are stacked under its panel (2026-10-20).
        /// The name is kept from before then, as it is the key the setting is saved under.
        /// </summary>
        SongSelectDateUpdatedSplitsDifficulties,

        /// <summary>
        /// How opaque the editor's discussion panel is drawn, so the beatmap can be seen through it (2026-10-20).
        /// </summary>
        EditorDiscussionOpacity,

        /// <summary>
        /// Whether the editor makes a soft sound for each thing done to the objects while the track is stopped: the
        /// ASMR mode (2026-10-21).
        /// </summary>
        EditorAsmrSounds,

        /// <summary>
        /// How loud <see cref="EditorAsmrSounds"/> are.
        /// </summary>
        EditorAsmrVolume,

        /// <summary>
        /// The thickness of the rings drawn while distance snap is on, as a share of the stock thickness (2026-10-22).
        /// </summary>
        EditorDistanceSnapGridThickness,

        /// <summary>
        /// How opaque the rings drawn while distance snap is on are (2026-10-22).
        /// </summary>
        EditorDistanceSnapGridOpacity,

        /// <summary>
        /// Whether the editor writes down every change and input to a file in the game's folder, for working on this
        /// game (2026-10-28). Offered only in builds that are not distributed.
        /// </summary>
        EditorOperationLog,
    }
}
