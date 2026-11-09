// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Configuration
{
    /// <summary>
    /// A set of values for Lazer-Kai's own settings, applied all at once: the recommended one, or one a mapper saved.
    /// </summary>
    /// <remarks>
    /// Only Lazer-Kai's settings, and not all of them (2026-09-25). Always left out: stable compatibility, which is
    /// experimental and only on for somebody who chose it; sending error reports, which is theirs to decide; and the
    /// Mapping Tools path, which is where it is on one machine. The recommended one leaves the colours and the skin
    /// out as well (<see cref="LOOKS"/>).
    /// </remarks>
    public class LazerKaiSettingsPreset
    {
        /// <summary>
        /// The recommended settings: the developer's own, as they stood on 2026-09-25.
        /// </summary>
        public static readonly IReadOnlyDictionary<OsuSetting, string> RECOMMENDED = new Dictionary<OsuSetting, string>
        {
            [OsuSetting.BeatmapExportFormat] = @"Stable",
            [OsuSetting.EditorActionButtonSquareness] = @"0.0",
            [OsuSetting.EditorAdvanceByPreviousSpacingOnPlacement] = @"True",
            [OsuSetting.EditorAllowOutsidePlayArea] = @"False",
            [OsuSetting.EditorAlwaysExpandLeftToolbox] = @"False",
            [OsuSetting.EditorAlwaysExpandTimelinePointLabels] = @"True",
            [OsuSetting.EditorApplyTestPlayModsToComposer] = @"False",
            [OsuSetting.EditorAutoBackup] = @"True",
            [OsuSetting.EditorAutoBackupInterval] = @"15.0",
            [OsuSetting.EditorAutoBackupKeepCount] = @"100",
            [OsuSetting.EditorAutoGenerateBreaks] = @"False",
            [OsuSetting.EditorAutoNewCombo] = @"True",
            [OsuSetting.EditorAutoNewComboInterval] = @"OneBar",
            [OsuSetting.EditorCarryOverSongSelectMods] = @"True",
            [OsuSetting.EditorDefaultAdditionBank] = @"Auto",
            [OsuSetting.EditorDetectOffscreen] = @"True",
            [OsuSetting.EditorDifficultyGraphDetail] = @"1.0",
            [OsuSetting.EditorDifficultyGraphOpacity] = @"0.1",
            [OsuSetting.EditorFreehandSliderPlacement] = @"False",
            [OsuSetting.EditorGridButtonSquareness] = @"0.0",
            [OsuSetting.EditorGridDefaultRotation] = @"0.0",
            [OsuSetting.EditorGridDefaultSpacing] = @"256.0",
            [OsuSetting.EditorGridDefaultType] = @"Square",
            [OsuSetting.EditorGridDefaultX] = @"256.0",
            [OsuSetting.EditorGridDefaultY] = @"192.0",
            [OsuSetting.EditorHideGridFromPointsTool] = @"True",
            [OsuSetting.EditorHideKiaiFlash] = @"True",
            [OsuSetting.EditorHideRotationHandles] = @"True",
            [OsuSetting.EditorHideSelectionButtons] = @"True",
            [OsuSetting.EditorHideTimelinePoints] = @"False",
            [OsuSetting.EditorHideTimelineVolume] = @"True",
            [OsuSetting.EditorLeftToolboxWidth] = @"150.0",
            [OsuSetting.EditorLiveModPreview] = @"False",
            [OsuSetting.EditorLoopPlayback] = @"True",
            [OsuSetting.EditorNotifyOnBackup] = @"True",
            [OsuSetting.EditorObjectOutlineOpacity] = @"0.75",
            [OsuSetting.EditorObjectOutlineThickness] = @"0.25",
            [OsuSetting.EditorReferenceDifficultyOpacity] = @"1.0",
            [OsuSetting.EditorRightToolboxWidth] = @"175.0",
            [OsuSetting.EditorSampleBankButtonSquareness] = @"0.0",
            [OsuSetting.EditorSeekAnimationDuration] = @"0.0",
            [OsuSetting.EditorSelectableAfterEndTime] = @"200.0",
            [OsuSetting.EditorSelectionHandleScale] = @"0.6",
            [OsuSetting.EditorSelectionOpacity] = @"0.5",
            [OsuSetting.EditorSelectionThickness] = @"0.5",
            [OsuSetting.EditorShowDifficultyGraph] = @"True",
            [OsuSetting.EditorShowPlayAreaBorder] = @"True",
            [OsuSetting.EditorShowUnsnapMarkers] = @"True",
            [OsuSetting.EditorShowVolumeGraph] = @"True",
            [OsuSetting.EditorSliderAnchorScale] = @"0.75",
            [OsuSetting.EditorSliderAnchorType] = @"PerfectCurve",
            [OsuSetting.EditorSliderAnchorTypeForPlacement] = @"PerfectCurve",
            [OsuSetting.EditorSliderEndDragMarkerOpacity] = @"0.5",
            [OsuSetting.EditorSliderEndDragMarkerScale] = @"0.5",
            [OsuSetting.EditorSliderVelocityPresetSquareness] = @"0.0",
            [OsuSetting.EditorStableSliderControls] = @"True",
            [OsuSetting.EditorStatusShowAdditionBank] = @"True",
            [OsuSetting.EditorStatusShowCombo] = @"True",
            [OsuSetting.EditorStatusShowDifficultySettings] = @"True",
            [OsuSetting.EditorStatusShowDrainTime] = @"True",
            [OsuSetting.EditorStatusShowKiai] = @"True",
            [OsuSetting.EditorStatusShowLegacyScore] = @"False",
            [OsuSetting.EditorStatusShowMaxPerformance] = @"True",
            [OsuSetting.EditorStatusShowMods] = @"True",
            [OsuSetting.EditorStatusShowNormalBank] = @"True",
            [OsuSetting.EditorStatusShowObjectCount] = @"False",
            [OsuSetting.EditorStatusShowSampleVolume] = @"True",
            [OsuSetting.EditorStatusShowSaveFormat] = @"True",
            [OsuSetting.EditorStatusShowScore] = @"False",
            [OsuSetting.EditorStatusShowSliderVelocity] = @"True",
            [OsuSetting.EditorStatusShowStarRating] = @"True",
            [OsuSetting.EditorTimelineDragAreaScale] = @"0.75",
            [OsuSetting.EditorTimelineDragToSeek] = @"False",
            [OsuSetting.EditorTimelineInspector] = @"True",
            [OsuSetting.EditorTimelineRepeatDragThreshold] = @"0.5",
            [OsuSetting.EditorTimelineShowBookmarks] = @"True",
            [OsuSetting.EditorTimelineShowSavedLines] = @"True",
            [OsuSetting.EditorTimelineStyle] = @"LazerKai",
            [OsuSetting.EditorToggleButtonSquareness] = @"0.0",
            [OsuSetting.EditorToolButtonSquareness] = @"0.15",
            [OsuSetting.EditorVolumeGraphIgnoreFivePercent] = @"True",
            [OsuSetting.EditorVolumeGraphOpacity] = @"0.5",
            [OsuSetting.EditorVolumeGraphThickness] = @"2.5",
            [OsuSetting.RemoveDragThreshold] = @"True",
            [OsuSetting.SwapRightAndMiddleMouse] = @"True",
        };

        /// <summary>
        /// The colours and the skin: kept in a preset a mapper saves, since those are theirs, but not in the recommended
        /// one, which would otherwise put somebody else's taste over theirs (2026-09-25).
        /// </summary>
        public static readonly OsuSetting[] LOOKS =
        {
            OsuSetting.EditorColourScheme,
            OsuSetting.EditorDifficultyGraphColour,
            OsuSetting.EditorObjectOutlineColour,
            OsuSetting.EditorReferenceDifficultyColour,
            OsuSetting.EditorSelectionColour,
            OsuSetting.EditorSelectionHandleColour,
            OsuSetting.EditorSliderEndDragMarkerColour,
            OsuSetting.EditorVolumeGraphColour,
            OsuSetting.UiTheme,
            OsuSetting.EditorTimelineUseSkin,
            OsuSetting.EditorBeatmapColours,
            OsuSetting.EditorBeatmapHitsounds,
        };

        /// <summary>
        /// The settings a preset can hold: the recommended one's, and the colours and the skin. A preset only sets what
        /// it holds, so the recommended one leaves those as they are.
        /// </summary>
        public static IEnumerable<OsuSetting> Settings => RECOMMENDED.Keys.Concat(LOOKS);

        /// <summary>
        /// The values, as the settings file writes them.
        /// </summary>
        public readonly IReadOnlyDictionary<OsuSetting, string> Values;

        public LazerKaiSettingsPreset(IReadOnlyDictionary<OsuSetting, string> values)
        {
            Values = values;
        }

        /// <summary>
        /// The settings as they are now.
        /// </summary>
        public static LazerKaiSettingsPreset Capture(OsuConfigManager config) =>
            new LazerKaiSettingsPreset(Settings.ToDictionary(s => s, config.GetText));

        /// <summary>
        /// Sets every setting the preset has a value for, and only those: a preset saved by an older version says
        /// nothing of a setting added since, which is left as it is.
        /// </summary>
        public void ApplyTo(OsuConfigManager config)
        {
            foreach (var (setting, value) in Values)
            {
                if (Settings.Contains(setting))
                    config.SetText(setting, value);
            }
        }
    }
}
