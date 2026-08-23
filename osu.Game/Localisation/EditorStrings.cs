// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class EditorStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Editor";

        /// <summary>
        /// "Beatmap editor"
        /// </summary>
        public static LocalisableString BeatmapEditor => new TranslatableString(getKey(@"beatmap_editor"), @"Beatmap editor");

        /// <summary>
        /// "Waveform opacity"
        /// </summary>
        public static LocalisableString WaveformOpacity => new TranslatableString(getKey(@"waveform_opacity"), @"Waveform opacity");

        /// <summary>
        /// "Show storyboard"
        /// </summary>
        public static LocalisableString ShowStoryboard => new TranslatableString(getKey(@"show_storyboard"), @"Show storyboard");

        /// <summary>
        /// "Show hit markers"
        /// </summary>
        public static LocalisableString ShowHitMarkers => new TranslatableString(getKey(@"show_hit_markers"), @"Show hit markers");

        /// <summary>
        /// "Automatically seek after placing objects"
        /// </summary>
        public static LocalisableString AutoSeekOnPlacement => new TranslatableString(getKey(@"auto_seek_on_placement"), @"Automatically seek after placing objects");

        /// <summary>
        /// "Timing"
        /// </summary>
        public static LocalisableString Timing => new TranslatableString(getKey(@"timing"), @"Timing");

        /// <summary>
        /// "Set preview point to current time"
        /// </summary>
        public static LocalisableString SetPreviewPointToCurrent => new TranslatableString(getKey(@"set_preview_point_to_current"), @"Set preview point to current time");

        /// <summary>
        /// "Snap all notes to current snap divisor..."
        /// </summary>
        public static LocalisableString SnapAllNotesToCurrentSnapDivisor => new TranslatableString(getKey(@"snap_all_notes_to_current_snap_divisor"), @"Snap all notes to current snap divisor...");

        /// <summary>
        /// "Move already placed objects when changing timing"
        /// </summary>
        public static LocalisableString AdjustExistingObjectsOnTimingChanges =>
            new TranslatableString(getKey(@"adjust_existing_objects_on_timing_changes"), @"Move already placed objects when changing timing");

        /// <summary>
        /// "For editing (.olz)"
        /// </summary>
        public static LocalisableString ExportForEditing => new TranslatableString(getKey(@"export_for_editing"), @"For editing (.olz)");

        /// <summary>
        /// "For compatibility (.osz)"
        /// </summary>
        public static LocalisableString ExportForCompatibility => new TranslatableString(getKey(@"export_for_compatibility"), @"For compatibility (.osz)");

        /// <summary>
        /// "Guest difficulty (.osu)"
        /// </summary>
        public static LocalisableString ExportGuestDifficulty => new TranslatableString(getKey(@"export_guest_difficulty"), @"Guest difficulty (.osu)");

        /// <summary>
        /// "Create new difficulty"
        /// </summary>
        public static LocalisableString CreateNewDifficulty => new TranslatableString(getKey(@"create_new_difficulty"), @"Create new difficulty");

        /// <summary>
        /// "Change difficulty"
        /// </summary>
        public static LocalisableString ChangeDifficulty => new TranslatableString(getKey(@"change_difficulty"), @"Change difficulty");

        /// <summary>
        /// "Delete difficulty..."
        /// </summary>
        public static LocalisableString DeleteDifficulty => new TranslatableString(getKey(@"delete_difficulty"), @"Delete difficulty...");

        /// <summary>
        /// "Edit externally..."
        /// </summary>
        public static LocalisableString EditExternally => new TranslatableString(getKey(@"edit_externally"), @"Edit externally...");

        /// <summary>
        /// "Submit beatmap..."
        /// </summary>
        public static LocalisableString SubmitBeatmap => new TranslatableString(getKey(@"submit_beatmap"), @"Submit beatmap...");

        /// <summary>
        /// "SV {0:0.00}x"
        /// </summary>
        public static LocalisableString SliderVelocityAdjusted(double velocity) => new TranslatableString(getKey(@"slider_velocity_adjusted"), @"SV {0:0.00}x", velocity);

        /// <summary>
        /// "This build does not upload beatmaps."
        /// </summary>
        public static LocalisableString BeatmapUploadUnavailable => new TranslatableString(getKey(@"beatmap_upload_unavailable"), @"This build does not upload beatmaps.");

        /// <summary>
        /// "setup"
        /// </summary>
        public static LocalisableString SetupScreen => new TranslatableString(getKey(@"setup_screen"), @"setup");

        /// <summary>
        /// "compose"
        /// </summary>
        public static LocalisableString ComposeScreen => new TranslatableString(getKey(@"compose_screen"), @"compose");

        /// <summary>
        /// "design"
        /// </summary>
        public static LocalisableString DesignScreen => new TranslatableString(getKey(@"design_screen"), @"design");

        /// <summary>
        /// "timing"
        /// </summary>
        public static LocalisableString TimingScreen => new TranslatableString(getKey(@"timing_screen"), @"timing");

        /// <summary>
        /// "verify"
        /// </summary>
        public static LocalisableString VerifyScreen => new TranslatableString(getKey(@"verify_screen"), @"verify");

        /// <summary>
        /// "Playback speed"
        /// </summary>
        public static LocalisableString PlaybackSpeed => new TranslatableString(getKey(@"playback_speed"), @"Playback speed");

        /// <summary>
        /// "Test!"
        /// </summary>
        public static LocalisableString TestBeatmap => new TranslatableString(getKey(@"test_beatmap"), @"Test!");

        /// <summary>
        /// "{0:0}&#176;"
        /// </summary>
        public static LocalisableString RotationUnsnapped(float newRotation) => new TranslatableString(getKey(@"rotation_unsnapped"), @"{0:0}°", newRotation);

        /// <summary>
        /// "{0:0}&#176; (snapped)"
        /// </summary>
        public static LocalisableString RotationSnapped(float newRotation) => new TranslatableString(getKey(@"rotation_snapped"), @"{0:0}° (snapped)", newRotation);

        /// <summary>
        /// "Limit placement to current time"
        /// </summary>
        public static LocalisableString LimitedDistanceSnap => new TranslatableString(getKey(@"limit_placement_to_current_time"), @"Limit placement to current time");

        /// <summary>
        /// "Contract sidebars when not hovered"
        /// </summary>
        public static LocalisableString ContractSidebars => new TranslatableString(getKey(@"contract_sidebars"), @"Contract sidebars when not hovered");

        /// <summary>
        /// "Must be in edit mode to handle editor links"
        /// </summary>
        public static LocalisableString MustBeInEditorToHandleLinks => new TranslatableString(getKey(@"must_be_in_editor_to_handle_links"), @"Must be in edit mode to handle editor links");

        /// <summary>
        /// "Failed to parse editor link"
        /// </summary>
        public static LocalisableString FailedToParseEditorLink => new TranslatableString(getKey(@"failed_to_parse_edtior_link"), @"Failed to parse editor link");

        /// <summary>
        /// "Timeline"
        /// </summary>
        public static LocalisableString Timeline => new TranslatableString(getKey(@"timeline"), @"Timeline");

        /// <summary>
        /// "Show timing changes"
        /// </summary>
        public static LocalisableString TimelineShowTimingChanges => new TranslatableString(getKey(@"timeline_show_timing_changes"), @"Show timing changes");

        /// <summary>
        /// "Finish editing and import changes"
        /// </summary>
        public static LocalisableString FinishEditingExternally => new TranslatableString(getKey(@"Finish editing and import changes"), @"Finish editing and import changes");

        /// <summary>
        /// "Show breaks"
        /// </summary>
        public static LocalisableString TimelineShowBreaks => new TranslatableString(getKey(@"timeline_show_breaks"), @"Show breaks");

        /// <summary>
        /// "Show ticks"
        /// </summary>
        public static LocalisableString TimelineShowTicks => new TranslatableString(getKey(@"timeline_show_ticks"), @"Show ticks");

        /// <summary>
        /// "Bookmarks"
        /// </summary>
        public static LocalisableString Bookmarks => new TranslatableString(getKey(@"bookmarks"), @"Bookmarks");

        /// <summary>
        /// "Add bookmark"
        /// </summary>
        public static LocalisableString AddBookmark => new TranslatableString(getKey(@"add_bookmark"), @"Add bookmark");

        /// <summary>
        /// "Remove closest bookmark"
        /// </summary>
        public static LocalisableString RemoveClosestBookmark => new TranslatableString(getKey(@"remove_closest_bookmark"), @"Remove closest bookmark");

        /// <summary>
        /// "Seek to previous bookmark"
        /// </summary>
        public static LocalisableString SeekToPreviousBookmark => new TranslatableString(getKey(@"seek_to_previous_bookmark"), @"Seek to previous bookmark");

        /// <summary>
        /// "Seek to next bookmark"
        /// </summary>
        public static LocalisableString SeekToNextBookmark => new TranslatableString(getKey(@"seek_to_next_bookmark"), @"Seek to next bookmark");

        /// <summary>
        /// "Reset bookmarks..."
        /// </summary>
        public static LocalisableString ResetBookmarks => new TranslatableString(getKey(@"reset_bookmarks..."), @"Reset bookmarks...");

        /// <summary>
        /// "Open beatmap info page"
        /// </summary>
        public static LocalisableString OpenInfoPage => new TranslatableString(getKey(@"open_info_page"), @"Open beatmap info page");

        /// <summary>
        /// "Open beatmap discussion page"
        /// </summary>
        public static LocalisableString OpenDiscussionPage => new TranslatableString(getKey(@"open_discussion_page"), @"Open beatmap discussion page");

        /// <summary>
        /// "Current difficulty"
        /// </summary>
        public static LocalisableString CheckCurrentDifficulty => new TranslatableString(getKey(@"check_current_difficulty"), @"Current difficulty");

        /// <summary>
        /// "Entire beatmap set"
        /// </summary>
        public static LocalisableString CheckEntireBeatmapSet => new TranslatableString(getKey(@"check_entire_beatmap_set"), @"Entire beatmap set");

        /// <summary>
        /// "Saving is not supported for this ruleset yet, sorry!"
        /// </summary>
        public static LocalisableString RulesetNotSupportSaving => new TranslatableString(getKey(@"ruleset_not_support_saving"), @"Saving is not supported for this ruleset yet, sorry!");

        /// <summary>
        /// "Export failed!"
        /// </summary>
        public static LocalisableString ExportFailed => new TranslatableString(getKey(@"export_failed"), @"Export failed!");

        /// <summary>
        /// "Import failed!"
        /// </summary>
        public static LocalisableString ImportFailed => new TranslatableString(getKey(@"import_failed"), @"Import failed!");

        /// <summary>
        /// "Open folder"
        /// </summary>
        public static LocalisableString OpenFolder => new TranslatableString(getKey(@"open_folder"), @"Open folder");

        /// <summary>
        /// "Cleaning up..."
        /// </summary>
        public static LocalisableString ExternalEditCleaningUp => new TranslatableString(getKey(@"external_edit_cleaning_up"), @"Cleaning up...");

        /// <summary>
        /// "Exporting for edit..."
        /// </summary>
        public static LocalisableString ExternalEditExporting => new TranslatableString(getKey(@"external_edit_exporting"), @"Exporting for edit...");

        /// <summary>
        /// "Beatmap is mounted externally"
        /// </summary>
        public static LocalisableString BeatmapMountedExternally => new TranslatableString(getKey(@"beatmap_mounted_externally"), @"Beatmap is mounted externally");

        /// <summary>
        /// "Any changes made to the exported folder will be imported to the game, including file additions, modifications and deletions."
        /// </summary>
        public static LocalisableString ExternalEditMountedExplanation => new TranslatableString(getKey(@"external_edit_mounted_explanation"),
            @"Any changes made to the exported folder will be imported to the game, including file additions, modifications and deletions.");

        /// <summary>
        /// "New combo"
        /// </summary>
        public static LocalisableString NewCombo => new TranslatableString(getKey(@"new_combo"), @"New combo");

        /// <summary>
        /// "Bank"
        /// </summary>
        public static LocalisableString Bank => new TranslatableString(getKey(@"bank"), @"Bank");

        /// <summary>
        /// "Normal bank"
        /// </summary>
        public static LocalisableString NormalBank => new TranslatableString(getKey(@"normal_bank"), @"Normal bank");

        /// <summary>
        /// "Addition bank"
        /// </summary>
        public static LocalisableString AdditionBank => new TranslatableString(getKey(@"addition_bank"), @"Addition bank");

        /// <summary>
        /// "Default addition bank"
        /// </summary>
        public static LocalisableString DefaultAdditionBank => new TranslatableString(getKey(@"default_addition_bank"), @"Default addition bank");

        /// <summary>
        /// "Used when a whistle, finish or clap is added to a note which had none."
        /// </summary>
        public static LocalisableString DefaultAdditionBankDescription => new TranslatableString(getKey(@"default_addition_bank_description"),
            @"Used when a whistle, finish or clap is added to a note which had none.");

        /// <summary>
        /// "Auto"
        /// </summary>
        public static LocalisableString AdditionBankAuto => new TranslatableString(getKey(@"addition_bank_auto"), @"Auto");

        /// <summary>
        /// "Normal"
        /// </summary>
        public static LocalisableString AdditionBankNormal => new TranslatableString(getKey(@"addition_bank_normal"), @"Normal");

        /// <summary>
        /// "Soft"
        /// </summary>
        public static LocalisableString AdditionBankSoft => new TranslatableString(getKey(@"addition_bank_soft"), @"Soft");

        /// <summary>
        /// "Drum"
        /// </summary>
        public static LocalisableString AdditionBankDrum => new TranslatableString(getKey(@"addition_bank_drum"), @"Drum");

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString SampleVolume => new TranslatableString(getKey(@"sample_volume"), @"Volume");

        /// <summary>
        /// "Sample"
        /// </summary>
        public static LocalisableString Sample => new TranslatableString(getKey(@"sample"), @"Sample");

        /// <summary>
        /// "Sample set"
        /// </summary>
        public static LocalisableString SampleSet => new TranslatableString(getKey(@"sample_set"), @"Sample set");

        /// <summary>
        /// "Test"
        /// </summary>
        public static LocalisableString MenuBarTest => new TranslatableString(getKey(@"menu_bar_test"), @"Test");

        /// <summary>
        /// "Mods"
        /// </summary>
        public static LocalisableString TestPlayMods => new TranslatableString(getKey(@"test_play_mods"), @"Mods");

        /// <summary>
        /// "Deselect all mods"
        /// </summary>
        public static LocalisableString TestPlayDeselectAllMods => new TranslatableString(getKey(@"test_play_deselect_all_mods"), @"Deselect all mods");

        /// <summary>
        /// "Test gameplay"
        /// </summary>
        public static LocalisableString TestGameplay => new TranslatableString(getKey(@"test_gameplay"), @"Test gameplay");

        /// <summary>
        /// "Preview mods while composing"
        /// </summary>
        public static LocalisableString ApplyTestPlayModsToComposer => new TranslatableString(getKey(@"apply_test_play_mods_to_composer"), @"Preview mods while composing");

        /// <summary>
        /// "Advance by previous spacing after placing objects"
        /// </summary>
        public static LocalisableString AdvanceByPreviousSpacingOnPlacement =>
            new TranslatableString(getKey(@"advance_by_previous_spacing_on_placement"), @"Advance by previous spacing after placing objects");

        /// <summary>
        /// "Draw sliders freehand by dragging"
        /// </summary>
        public static LocalisableString FreehandSliderPlacement => new TranslatableString(getKey(@"freehand_slider_placement"), @"Draw sliders freehand by dragging");

        /// <summary>
        /// "Keep mod preview up to date while editing"
        /// </summary>
        public static LocalisableString LiveModPreview => new TranslatableString(getKey(@"live_mod_preview"), @"Keep mod preview up to date while editing");

        /// <summary>
        /// "Always expand timeline labels"
        /// </summary>
        public static LocalisableString AlwaysExpandTimelinePointLabels => new TranslatableString(getKey(@"always_expand_timeline_point_labels"), @"Always expand timeline labels");

        /// <summary>
        /// "Selection outline"
        /// </summary>
        public static LocalisableString SelectionAppearance => new TranslatableString(getKey(@"selection_appearance"), @"Selection outline");

        /// <summary>
        /// "Colour"
        /// </summary>
        public static LocalisableString SelectionColour => new TranslatableString(getKey(@"selection_colour"), @"Colour");

        /// <summary>
        /// "Thickness"
        /// </summary>
        public static LocalisableString SelectionThickness => new TranslatableString(getKey(@"selection_thickness"), @"Thickness");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString SelectionOpacity => new TranslatableString(getKey(@"selection_opacity"), @"Opacity");

        /// <summary>
        /// "Editor colour (applies on next open)"
        /// </summary>
        public static LocalisableString EditorColourScheme => new TranslatableString(getKey(@"editor_colour_scheme"), @"Editor colour (applies on next open)");

        /// <summary>
        /// "Hide the shortcut buttons on a selection"
        /// </summary>
        public static LocalisableString HideSelectionButtons => new TranslatableString(getKey(@"hide_selection_buttons"), @"Hide the shortcut buttons on a selection");

        /// <summary>
        /// "Hide the rotation handles on a selection"
        /// </summary>
        public static LocalisableString HideRotationHandles => new TranslatableString(getKey(@"hide_rotation_handles"), @"Hide the rotation handles on a selection");

        /// <summary>
        /// "Ctrl+Shift+wheel turns a selection, and adding Alt turns it in finer steps."
        /// </summary>
        public static LocalisableString HideRotationHandlesDescription => new TranslatableString(getKey(@"hide_rotation_handles_description"),
            @"Ctrl+Shift+wheel turns a selection, and adding Alt turns it in finer steps.");

        /// <summary>
        /// "Selection handle size"
        /// </summary>
        public static LocalisableString SelectionHandleSize => new TranslatableString(getKey(@"selection_handle_size"), @"Selection handle size");

        /// <summary>
        /// "Selection handle colour"
        /// </summary>
        public static LocalisableString SelectionHandleColour => new TranslatableString(getKey(@"selection_handle_colour"), @"Selection handle colour");

        /// <summary>
        /// "Slider anchor size"
        /// </summary>
        public static LocalisableString SliderAnchorSize => new TranslatableString(getKey(@"slider_anchor_size"), @"Slider anchor size");

        /// <summary>
        /// "Timeline"
        /// </summary>
        public static LocalisableString ShowTimeline => new TranslatableString(getKey(@"show_timeline"), @"Timeline");

        /// <summary>
        /// "Hitsounds"
        /// </summary>
        public static LocalisableString Hitsounds => new TranslatableString(getKey(@"hitsounds"), @"Hitsounds");

        /// <summary>
        /// "Music"
        /// </summary>
        public static LocalisableString Music => new TranslatableString(getKey(@"music"), @"Music");

        /// <summary>
        /// "Default"
        /// </summary>
        public static LocalisableString ColourDefault => new TranslatableString(getKey(@"colour_default"), @"Default");

        /// <summary>
        /// "Red"
        /// </summary>
        public static LocalisableString ColourRed => new TranslatableString(getKey(@"colour_red"), @"Red");

        /// <summary>
        /// "Orange"
        /// </summary>
        public static LocalisableString ColourOrange => new TranslatableString(getKey(@"colour_orange"), @"Orange");

        /// <summary>
        /// "Green"
        /// </summary>
        public static LocalisableString ColourGreen => new TranslatableString(getKey(@"colour_green"), @"Green");

        /// <summary>
        /// "Cyan"
        /// </summary>
        public static LocalisableString ColourCyan => new TranslatableString(getKey(@"colour_cyan"), @"Cyan");

        /// <summary>
        /// "Blue"
        /// </summary>
        public static LocalisableString ColourBlue => new TranslatableString(getKey(@"colour_blue"), @"Blue");

        /// <summary>
        /// "Magenta"
        /// </summary>
        public static LocalisableString ColourMagenta => new TranslatableString(getKey(@"colour_magenta"), @"Magenta");

        /// <summary>
        /// "White"
        /// </summary>
        public static LocalisableString ColourWhite => new TranslatableString(getKey(@"colour_white"), @"White");

        /// <summary>
        /// "Export this difficulty as .osu (v14)"
        /// </summary>
        public static LocalisableString ExportBeatmapFile => new TranslatableString(getKey(@"export_beatmap_file"), @"Export this difficulty as .osu (v14)");

        /// <summary>
        /// "Show difficulty over the song"
        /// </summary>
        public static LocalisableString ShowDifficultyGraph => new TranslatableString(getKey(@"show_difficulty_graph"), @"Show difficulty over the song");

        /// <summary>
        /// "Difficulty graph colour"
        /// </summary>
        public static LocalisableString DifficultyGraphColour => new TranslatableString(getKey(@"difficulty_graph_colour"), @"Difficulty graph colour");

        /// <summary>
        /// "Difficulty graph opacity"
        /// </summary>
        public static LocalisableString DifficultyGraphOpacity => new TranslatableString(getKey(@"difficulty_graph_opacity"), @"Difficulty graph opacity");

        /// <summary>
        /// "Loop playback"
        /// </summary>
        public static LocalisableString LoopPlayback => new TranslatableString(getKey(@"loop_playback"), @"Loop playback");

        /// <summary>
        /// "New slider anchors make a perfect curve"
        /// </summary>
        public static LocalisableString ArcOnAnchorAdd => new TranslatableString(getKey(@"arc_on_anchor_add"), @"New slider anchors make a perfect curve");

        /// <summary>
        /// "Closer to how stable handles them."
        /// </summary>
        public static LocalisableString ArcOnAnchorAddDescription => new TranslatableString(getKey(@"arc_on_anchor_add_description"), @"Closer to how stable handles them.");

        /// <summary>
        /// "Automatic backups"
        /// </summary>
        public static LocalisableString AutoBackup => new TranslatableString(getKey(@"auto_backup"), @"Automatic backups");

        /// <summary>
        /// "Back up while editing"
        /// </summary>
        public static LocalisableString AutoBackupEnabled => new TranslatableString(getKey(@"auto_backup_enabled"), @"Back up while editing");

        /// <summary>
        /// "Backup interval"
        /// </summary>
        public static LocalisableString AutoBackupInterval => new TranslatableString(getKey(@"auto_backup_interval"), @"Backup interval");

        /// <summary>
        /// "Backups to keep"
        /// </summary>
        public static LocalisableString AutoBackupKeepCount => new TranslatableString(getKey(@"auto_backup_keep_count"), @"Backups to keep");

        /// <summary>
        /// "Back up now"
        /// </summary>
        public static LocalisableString BackupNow => new TranslatableString(getKey(@"backup_now"), @"Back up now");

        /// <summary>
        /// "Open backup folder"
        /// </summary>
        public static LocalisableString OpenBackupFolder => new TranslatableString(getKey(@"open_backup_folder"), @"Open backup folder");

        /// <summary>
        /// "Keep passed objects selectable for"
        /// </summary>
        public static LocalisableString SelectableAfterEndTime => new TranslatableString(getKey(@"selectable_after_end_time"), @"Keep passed objects selectable for");

        /// <summary>
        /// "Ctrl+click a slider anchor to cycle its curve type"
        /// </summary>
        public static LocalisableString CtrlClickCyclesCurveType => new TranslatableString(getKey(@"ctrl_click_cycles_curve_type"), @"Ctrl+click a slider anchor to cycle its curve type");

        /// <summary>
        /// "Swap right and middle mouse buttons"
        /// </summary>
        public static LocalisableString SwapRightAndMiddleMouse => new TranslatableString(getKey(@"swap_right_and_middle_mouse"), @"Swap right and middle mouse buttons");

        /// <summary>
        /// "Seek animation"
        /// </summary>
        public static LocalisableString SeekAnimationDuration => new TranslatableString(getKey(@"seek_animation_duration"), @"Seek animation");

        /// <summary>
        /// "Keep left toolbox expanded"
        /// </summary>
        public static LocalisableString AlwaysExpandLeftToolbox => new TranslatableString(getKey(@"always_expand_left_toolbox"), @"Keep left toolbox expanded");

        /// <summary>
        /// "Speed adjustment is unavailable while a mod that changes the speed is being previewed."
        /// </summary>
        public static LocalisableString PlaybackSpeedDrivenByMods => new TranslatableString(getKey(@"playback_speed_driven_by_mods"),
            @"Speed adjustment is unavailable while a mod that changes the speed is being previewed.");

        /// <summary>
        /// "Share the mod selection with song select"
        /// </summary>
        public static LocalisableString CarryOverSongSelectMods => new TranslatableString(getKey(@"carry_over_song_select_mods"), @"Share the mod selection with song select");

        /// <summary>
        /// "Test play starts from the mods chosen at song select, and what is chosen here is left selected on leaving."
        /// </summary>
        public static LocalisableString CarryOverSongSelectModsDescription => new TranslatableString(getKey(@"carry_over_song_select_mods_description"),
            @"Test play starts from the mods chosen at song select, and what is chosen here is left selected on leaving.");

        /// <summary>
        /// "Hide the kiai flash"
        /// </summary>
        public static LocalisableString HideKiaiFlash => new TranslatableString(getKey(@"hide_kiai_flash"), @"Hide the kiai flash");

        /// <summary>
        /// "Only inside the editor. Test play flashes as it always did."
        /// </summary>
        public static LocalisableString HideKiaiFlashDescription => new TranslatableString(getKey(@"hide_kiai_flash_description"),
            @"Only inside the editor. Test play flashes as it always did.");

        /// <summary>
        /// "Hide the grid from points tool"
        /// </summary>
        public static LocalisableString HideGridFromPointsTool => new TranslatableString(getKey(@"hide_grid_from_points_tool"), @"Hide the grid from points tool");

        /// <summary>
        /// "Its number key stops selecting it too. The grid can still be set up from the grid section on the right."
        /// </summary>
        public static LocalisableString HideGridFromPointsToolDescription => new TranslatableString(getKey(@"hide_grid_from_points_tool_description"),
            @"Its number key stops selecting it too. The grid can still be set up from the grid section on the right.");

        /// <summary>
        /// "Remove the catch when starting a drag"
        /// </summary>
        public static LocalisableString RemoveDragThreshold => new TranslatableString(getKey(@"remove_drag_threshold"), @"Remove the catch when starting a drag");

        /// <summary>
        /// "Makes small adjustments easier to land. Only takes effect over the playfield and the two timelines."
        /// </summary>
        public static LocalisableString RemoveDragThresholdDescription => new TranslatableString(getKey(@"remove_drag_threshold_description"),
            @"Makes small adjustments easier to land. Only takes effect over the playfield and the two timelines.");

        /// <summary>
        /// "Show the play area border"
        /// </summary>
        public static LocalisableString ShowPlayAreaBorder => new TranslatableString(getKey(@"show_play_area_border"), @"Show the play area border");

        /// <summary>
        /// "Offscreen detection"
        /// </summary>
        public static LocalisableString DetectOffscreen => new TranslatableString(getKey(@"detect_offscreen"), @"Offscreen detection");

        /// <summary>
        /// "Tool button height"
        /// </summary>
        public static LocalisableString ToolButtonSquareness => new TranslatableString(getKey(@"tool_button_squareness"), @"Tool button height");

        /// <summary>
        /// "Toggle button height"
        /// </summary>
        public static LocalisableString ToggleButtonSquareness => new TranslatableString(getKey(@"toggle_button_squareness"), @"Toggle button height");

        /// <summary>
        /// "Sample bank button height"
        /// </summary>
        public static LocalisableString SampleBankButtonSquareness => new TranslatableString(getKey(@"sample_bank_button_squareness"), @"Sample bank button height");

        /// <summary>
        /// "Grid type button height"
        /// </summary>
        public static LocalisableString GridButtonSquareness => new TranslatableString(getKey(@"grid_button_squareness"), @"Grid type button height");

        /// <summary>
        /// "Transform and generate button height"
        /// </summary>
        public static LocalisableString ActionButtonSquareness => new TranslatableString(getKey(@"action_button_squareness"), @"Transform and generate button height");

        /// <summary>
        /// "Velocity preset button height"
        /// </summary>
        public static LocalisableString SliderVelocityPresetSquareness => new TranslatableString(getKey(@"slider_velocity_preset_squareness"), @"Velocity preset button height");

        /// <summary>
        /// "SR"
        /// </summary>
        public static LocalisableString StatusShowStarRating => new TranslatableString(getKey(@"status_show_star_rating"), @"SR");

        /// <summary>
        /// "SV"
        /// </summary>
        public static LocalisableString StatusShowSliderVelocity => new TranslatableString(getKey(@"status_show_slider_velocity"), @"SV");

        /// <summary>
        /// "Normal bank"
        /// </summary>
        public static LocalisableString StatusShowNormalBank => new TranslatableString(getKey(@"status_show_normal_bank"), @"Normal bank");

        /// <summary>
        /// "Addition bank"
        /// </summary>
        public static LocalisableString StatusShowAdditionBank => new TranslatableString(getKey(@"status_show_addition_bank"), @"Addition bank");

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString StatusShowSampleVolume => new TranslatableString(getKey(@"status_show_sample_volume"), @"Volume");

        /// <summary>
        /// "Kiai"
        /// </summary>
        public static LocalisableString StatusShowKiai => new TranslatableString(getKey(@"status_show_kiai"), @"Kiai");

        /// <summary>
        /// "Mods"
        /// </summary>
        public static LocalisableString StatusShowMods => new TranslatableString(getKey(@"status_show_mods"), @"Mods");

        /// <summary>
        /// "Difficulty shown as a ghost"
        /// </summary>
        public static LocalisableString ReferenceDifficulty => new TranslatableString(getKey(@"reference_difficulty"), @"Difficulty shown as a ghost");

        /// <summary>
        /// "None"
        /// </summary>
        public static LocalisableString ReferenceDifficultyNone => new TranslatableString(getKey(@"reference_difficulty_none"), @"None");

        /// <summary>
        /// "Ghost opacity"
        /// </summary>
        public static LocalisableString ReferenceDifficultyOpacity => new TranslatableString(getKey(@"reference_difficulty_opacity"), @"Ghost opacity");

        /// <summary>
        /// "Ghost colour"
        /// </summary>
        public static LocalisableString ReferenceDifficultyColour => new TranslatableString(getKey(@"reference_difficulty_colour"), @"Ghost colour");

        /// <summary>
        /// "Backup written"
        /// </summary>
        public static LocalisableString BackupWritten => new TranslatableString(getKey(@"backup_written"), @"Backup written");

        /// <summary>
        /// "Notify when a backup is written"
        /// </summary>
        public static LocalisableString NotifyOnBackup => new TranslatableString(getKey(@"notify_on_backup"), @"Notify when a backup is written");

        /// <summary>
        /// "Select"
        /// </summary>
        public static LocalisableString SelectTool => new TranslatableString(getKey(@"select_tool"), @"Select");

        /// <summary>
        /// "Synchronise..."
        /// </summary>
        public static LocalisableString Synchronise => new TranslatableString(getKey(@"synchronise"), @"Synchronise...");

        /// <summary>
        /// "Seek backwards"
        /// </summary>
        public static LocalisableString SeekBackwards => new TranslatableString(getKey(@"seek_backwards"), @"Seek backwards");

        /// <summary>
        /// "Seek forwards"
        /// </summary>
        public static LocalisableString SeekForwards => new TranslatableString(getKey(@"seek_forwards"), @"Seek forwards");

        /// <summary>
        /// "Seek to previous timing point"
        /// </summary>
        public static LocalisableString SeekToPreviousTimingPoint => new TranslatableString(getKey(@"seek_to_previous_timing_point"), @"Seek to previous timing point");

        /// <summary>
        /// "Seek to next timing point"
        /// </summary>
        public static LocalisableString SeekToNextTimingPoint => new TranslatableString(getKey(@"seek_to_next_timing_point"), @"Seek to next timing point");

        /// <summary>
        /// "Seek to first object / track start"
        /// </summary>
        public static LocalisableString SeekToStart => new TranslatableString(getKey(@"seek_to_start"), @"Seek to first object / track start");

        /// <summary>
        /// "Play from track start"
        /// </summary>
        public static LocalisableString PlayFromStart => new TranslatableString(getKey(@"play_from_start"), @"Play from track start");

        /// <summary>
        /// "Toggle pause"
        /// </summary>
        public static LocalisableString TogglePause => new TranslatableString(getKey(@"toggle_pause"), @"Toggle pause");

        /// <summary>
        /// "Seek to last object / track end"
        /// </summary>
        public static LocalisableString SeekToEnd => new TranslatableString(getKey(@"seek_to_end"), @"Seek to last object / track end");

        /// <summary>
        /// "Toggle new combo"
        /// </summary>
        public static LocalisableString ToggleNewCombo => new TranslatableString(getKey(@"toggle_new_combo"), @"Toggle new combo");

        /// <summary>
        /// "Toggle whistle sound"
        /// </summary>
        public static LocalisableString ToggleWhistleSound => new TranslatableString(getKey(@"toggle_whistle_sound"), @"Toggle whistle sound");

        /// <summary>
        /// "Toggle finish sound"
        /// </summary>
        public static LocalisableString ToggleFinishSound => new TranslatableString(getKey(@"toggle_finish_sound"), @"Toggle finish sound");

        /// <summary>
        /// "Toggle clap sound"
        /// </summary>
        public static LocalisableString ToggleClapSound => new TranslatableString(getKey(@"toggle_clap_sound"), @"Toggle clap sound");

        /// <summary>
        /// "Set normal sample bank to auto"
        /// </summary>
        public static LocalisableString ToggleNormalAutoBank => new TranslatableString(getKey(@"toggle_normal_auto_bank"), @"Set normal sample bank to auto");

        /// <summary>
        /// "Set normal sample bank to normal"
        /// </summary>
        public static LocalisableString ToggleNormalNormalBank => new TranslatableString(getKey(@"toggle_normal_normal_bank"), @"Set normal sample bank to normal");

        /// <summary>
        /// "Set normal sample bank to soft"
        /// </summary>
        public static LocalisableString ToggleNormalSoftBank => new TranslatableString(getKey(@"toggle_normal_soft_bank"), @"Set normal sample bank to soft");

        /// <summary>
        /// "Set normal sample bank to drum"
        /// </summary>
        public static LocalisableString ToggleNormalDrumBank => new TranslatableString(getKey(@"toggle_normal_drum_bank"), @"Set normal sample bank to drum");

        /// <summary>
        /// "Set addition sample bank to auto"
        /// </summary>
        public static LocalisableString ToggleAdditionAutoBank => new TranslatableString(getKey(@"toggle_addition_auto_bank"), @"Set addition sample bank to auto");

        /// <summary>
        /// "Set addition sample bank to normal"
        /// </summary>
        public static LocalisableString ToggleAdditionNormalBank => new TranslatableString(getKey(@"toggle_addition_normal_bank"), @"Set addition sample bank to normal");

        /// <summary>
        /// "Set addition sample bank to soft"
        /// </summary>
        public static LocalisableString ToggleAdditionSoftBank => new TranslatableString(getKey(@"toggle_addition_soft_bank"), @"Set addition sample bank to soft");

        /// <summary>
        /// "Set addition sample bank to drum"
        /// </summary>
        public static LocalisableString ToggleAdditionDrumBank => new TranslatableString(getKey(@"toggle_addition_drum_bank"), @"Set addition sample bank to drum");

        /// <summary>
        /// "Toggle distance snap grid"
        /// </summary>
        public static LocalisableString ToggleDistanceSnap => new TranslatableString(getKey(@"toggle_distance_snap"), @"Toggle distance snap grid");

        /// <summary>
        /// "Hide the timeline's velocity and volume rows"
        /// </summary>
        public static LocalisableString HideTimelinePoints => new TranslatableString(getKey(@"hide_timeline_points"), @"Hide the timeline's velocity and volume rows");

        /// <summary>
        /// "Objects take the freed space, making them easier to grab. Velocity and volume stay editable from the right sidebar."
        /// </summary>
        public static LocalisableString HideTimelinePointsDescription => new TranslatableString(getKey(@"hide_timeline_points_description"),
            @"Objects take the freed space, making them easier to grab. Velocity and volume stay editable from the right sidebar.");

        /// <summary>
        /// "Timeline drag handle size"
        /// </summary>
        public static LocalisableString TimelineDragAreaScale => new TranslatableString(getKey(@"timeline_drag_area_scale"), @"Timeline drag handle size");

        /// <summary>
        /// "Hide velocity and volume rows"
        /// </summary>
        public static LocalisableString TimelineHidePoints => new TranslatableString(getKey(@"timeline_hide_points"), @"Hide velocity and volume rows");

        /// <summary>
        /// "Timeline velocity and volume rows"
        /// </summary>
        public static LocalisableString TimelinePointRows => new TranslatableString(getKey(@"timeline_point_rows"), @"Timeline velocity and volume rows");

        /// <summary>
        /// "shown"
        /// </summary>
        public static LocalisableString Shown => new TranslatableString(getKey(@"shown"), @"shown");

        /// <summary>
        /// "hidden"
        /// </summary>
        public static LocalisableString Hidden => new TranslatableString(getKey(@"hidden"), @"hidden");

        /// <summary>
        /// "Repeat drag distance"
        /// </summary>
        public static LocalisableString TimelineRepeatDragThreshold => new TranslatableString(getKey(@"timeline_repeat_drag_threshold"), @"Repeat drag distance");

        /// <summary>
        /// "How far towards the next repeat a slider has to be dragged before the repeat is added."
        /// </summary>
        public static LocalisableString TimelineRepeatDragThresholdDescription => new TranslatableString(getKey(@"timeline_repeat_drag_threshold_description"),
            @"How far towards the next repeat a slider has to be dragged before the repeat is added.");

        /// <summary>
        /// "Slider length handle colour"
        /// </summary>
        public static LocalisableString SliderEndDragMarkerColour => new TranslatableString(getKey(@"slider_end_drag_marker_colour"), @"Slider length handle colour");

        /// <summary>
        /// "Slider length handle size"
        /// </summary>
        public static LocalisableString SliderEndDragMarkerScale => new TranslatableString(getKey(@"slider_end_drag_marker_scale"), @"Slider length handle size");

        /// <summary>
        /// "Slider length handle opacity"
        /// </summary>
        public static LocalisableString SliderEndDragMarkerOpacity => new TranslatableString(getKey(@"slider_end_drag_marker_opacity"), @"Slider length handle opacity");

        /// <summary>
        /// "Add breaks automatically"
        /// </summary>
        public static LocalisableString AutoGenerateBreaks => new TranslatableString(getKey(@"auto_generate_breaks"), @"Add breaks automatically");

        /// <summary>
        /// "Breaks appear wherever objects are far enough apart, and are rebuilt whenever objects change. Turn this off to place them yourself from the edit menu."
        /// </summary>
        public static LocalisableString AutoGenerateBreaksDescription => new TranslatableString(getKey(@"auto_generate_breaks_description"),
            @"Breaks appear wherever objects are far enough apart, and are rebuilt whenever objects change. Turn this off to place them yourself from the edit menu.");

        /// <summary>
        /// "Insert break"
        /// </summary>
        public static LocalisableString InsertBreak => new TranslatableString(getKey(@"insert_break"), @"Insert break");

        /// <summary>
        /// "Red"
        /// </summary>
        public static LocalisableString ColourSchemeRed => new TranslatableString(getKey(@"colour_scheme_red"), @"Red");

        /// <summary>
        /// "Yellow"
        /// </summary>
        public static LocalisableString ColourSchemeOrange => new TranslatableString(getKey(@"colour_scheme_orange"), @"Yellow");

        /// <summary>
        /// "Lime"
        /// </summary>
        public static LocalisableString ColourSchemeLime => new TranslatableString(getKey(@"colour_scheme_lime"), @"Lime");

        /// <summary>
        /// "Green"
        /// </summary>
        public static LocalisableString ColourSchemeGreen => new TranslatableString(getKey(@"colour_scheme_green"), @"Green");

        /// <summary>
        /// "Aquamarine"
        /// </summary>
        public static LocalisableString ColourSchemeAquamarine => new TranslatableString(getKey(@"colour_scheme_aquamarine"), @"Aquamarine");

        /// <summary>
        /// "Sky blue"
        /// </summary>
        public static LocalisableString ColourSchemeBlue => new TranslatableString(getKey(@"colour_scheme_blue"), @"Sky blue");

        /// <summary>
        /// "Purple"
        /// </summary>
        public static LocalisableString ColourSchemePurple => new TranslatableString(getKey(@"colour_scheme_purple"), @"Purple");

        /// <summary>
        /// "Magenta"
        /// </summary>
        public static LocalisableString ColourSchemePlum => new TranslatableString(getKey(@"colour_scheme_plum"), @"Magenta");

        /// <summary>
        /// "Pink"
        /// </summary>
        public static LocalisableString ColourSchemePink => new TranslatableString(getKey(@"colour_scheme_pink"), @"Pink");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
