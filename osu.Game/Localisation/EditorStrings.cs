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
        /// "Mods"
        /// </summary>
        public static LocalisableString MenuBarTest => new TranslatableString(getKey(@"menu_bar_test"), @"Mods");

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
        /// "Automatic new combo"
        /// </summary>
        public static LocalisableString AutoNewCombo => new TranslatableString(getKey(@"auto_new_combo"), @"Automatic new combo");

        /// <summary>
        /// "Whatever opens a bar is given a new combo as it is placed. Nothing already in the beatmap is touched."
        /// </summary>
        public static LocalisableString AutoNewComboDescription => new TranslatableString(getKey(@"auto_new_combo_description"),
            @"Whatever opens a bar is given a new combo as it is placed. Nothing already in the beatmap is touched.");

        /// <summary>
        /// "How often"
        /// </summary>
        public static LocalisableString AutoNewComboIntervalLabel => new TranslatableString(getKey(@"auto_new_combo_interval"), @"How often");

        /// <summary>
        /// "Every half bar"
        /// </summary>
        public static LocalisableString AutoNewComboHalfBar => new TranslatableString(getKey(@"auto_new_combo_half_bar"), @"Every half bar");

        /// <summary>
        /// "Every bar"
        /// </summary>
        public static LocalisableString AutoNewComboOneBar => new TranslatableString(getKey(@"auto_new_combo_one_bar"), @"Every bar");

        /// <summary>
        /// "Every two bars"
        /// </summary>
        public static LocalisableString AutoNewComboTwoBars => new TranslatableString(getKey(@"auto_new_combo_two_bars"), @"Every two bars");

        /// <summary>
        /// "Keep mod preview up to date while editing"
        /// </summary>
        public static LocalisableString LiveModPreview => new TranslatableString(getKey(@"live_mod_preview"), @"Keep mod preview up to date while editing");

        /// <summary>
        /// "Always expand the labels"
        /// </summary>
        public static LocalisableString AlwaysExpandTimelinePointLabels => new TranslatableString(getKey(@"always_expand_timeline_point_labels"), @"Always expand the labels");

        /// <summary>
        /// "Selection outline"
        /// </summary>
        public static LocalisableString SelectionAppearance => new TranslatableString(getKey(@"selection_appearance"), @"Selection outline");

        /// <summary>
        /// "Box colour"
        /// </summary>
        public static LocalisableString SelectionColour => new TranslatableString(getKey(@"selection_colour"), @"Box colour");

        /// <summary>
        /// "Box thickness"
        /// </summary>
        public static LocalisableString SelectionThickness => new TranslatableString(getKey(@"selection_thickness"), @"Box thickness");

        /// <summary>
        /// "Box opacity"
        /// </summary>
        public static LocalisableString SelectionOpacity => new TranslatableString(getKey(@"selection_opacity"), @"Box opacity");

        /// <summary>
        /// "Object outline colour"
        /// </summary>
        public static LocalisableString ObjectOutlineColour => new TranslatableString(getKey(@"object_outline_colour"), @"Object outline colour");

        /// <summary>
        /// "Object outline thickness"
        /// </summary>
        public static LocalisableString ObjectOutlineThickness => new TranslatableString(getKey(@"object_outline_thickness"), @"Object outline thickness");

        /// <summary>
        /// "Object outline opacity"
        /// </summary>
        public static LocalisableString ObjectOutlineOpacity => new TranslatableString(getKey(@"object_outline_opacity"), @"Object outline opacity");

        /// <summary>
        /// "Editor colour (applies on next open)"
        /// </summary>
        public static LocalisableString EditorColourScheme => new TranslatableString(getKey(@"editor_colour_scheme"), @"Editor colour (applies on next open)");

        /// <summary>
        /// "Hide the shortcut buttons"
        /// </summary>
        public static LocalisableString HideSelectionButtons => new TranslatableString(getKey(@"hide_selection_buttons"), @"Hide the shortcut buttons");

        /// <summary>
        /// "Hide the rotation handles"
        /// </summary>
        public static LocalisableString HideRotationHandles => new TranslatableString(getKey(@"hide_rotation_handles"), @"Hide the rotation handles");

        /// <summary>
        /// "Hides the handles at a selection's corners that rotate it."
        /// </summary>
        public static LocalisableString HideRotationHandlesDescription => new TranslatableString(getKey(@"hide_rotation_handles_description"),
            @"Hides the handles at a selection's corners that rotate it.");

        /// <summary>
        /// "Corner handle size"
        /// </summary>
        public static LocalisableString SelectionHandleSize => new TranslatableString(getKey(@"selection_handle_size"), @"Corner handle size");

        /// <summary>
        /// "Corner handle colour"
        /// </summary>
        public static LocalisableString SelectionHandleColour => new TranslatableString(getKey(@"selection_handle_colour"), @"Corner handle colour");

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
        /// "Mark objects that are off the beat"
        /// </summary>
        public static LocalisableString ShowUnsnapMarkers => new TranslatableString(getKey(@"show_unsnap_markers"), @"Mark objects that are off the beat");

        /// <summary>
        /// "Convert to stream"
        /// </summary>
        public static LocalisableString ConvertToStream => new TranslatableString(getKey(@"convert_to_stream"), @"Convert to stream");

        /// <summary>
        /// "Count"
        /// </summary>
        public static LocalisableString StreamCount => new TranslatableString(getKey(@"stream_count"), @"Count");

        /// <summary>
        /// "Allow more than 16"
        /// </summary>
        public static LocalisableString StreamExtendedLimits => new TranslatableString(getKey(@"stream_extended_limits"), @"Allow more than 16");

        /// <summary>
        /// "New combo every"
        /// </summary>
        public static LocalisableString StreamNewComboEvery => new TranslatableString(getKey(@"stream_new_combo_every"), @"New combo every");

        /// <summary>
        /// "None"
        /// </summary>
        public static LocalisableString StreamNewComboNone => new TranslatableString(getKey(@"stream_new_combo_none"), @"None");

        /// <summary>
        /// "Acceleration"
        /// </summary>
        public static LocalisableString StreamAcceleration => new TranslatableString(getKey(@"stream_acceleration"), @"Acceleration");

        /// <summary>
        /// "Easing"
        /// </summary>
        public static LocalisableString StreamEasing => new TranslatableString(getKey(@"stream_easing"), @"Easing");

        /// <summary>
        /// "Zigzag"
        /// </summary>
        public static LocalisableString StreamZigzag => new TranslatableString(getKey(@"stream_zigzag"), @"Zigzag");

        /// <summary>
        /// "Circles per side"
        /// </summary>
        public static LocalisableString StreamZigzagPeriod => new TranslatableString(getKey(@"stream_zigzag_period"), @"Circles per side");

        /// <summary>
        /// "Zigzag shape"
        /// </summary>
        public static LocalisableString StreamZigzagShape => new TranslatableString(getKey(@"stream_zigzag_shape"), @"Zigzag shape");

        /// <summary>
        /// "Draw the curve"
        /// </summary>
        public static LocalisableString StreamDrawTheCurve => new TranslatableString(getKey(@"stream_draw_the_curve"), @"Draw the curve");

        /// <summary>
        /// "Grid shape"
        /// </summary>
        public static LocalisableString GridDefaultShape => new TranslatableString(getKey(@"grid_default_shape"), @"Grid shape");

        /// <summary>
        /// "Grid spacing"
        /// </summary>
        public static LocalisableString GridDefaultSpacing => new TranslatableString(getKey(@"grid_default_spacing"), @"Grid spacing");

        /// <summary>
        /// "Grid rotation"
        /// </summary>
        public static LocalisableString GridDefaultRotation => new TranslatableString(getKey(@"grid_default_rotation"), @"Grid rotation");

        /// <summary>
        /// "Grid origin X"
        /// </summary>
        public static LocalisableString GridDefaultX => new TranslatableString(getKey(@"grid_default_x"), @"Grid origin X");

        /// <summary>
        /// "Grid origin Y"
        /// </summary>
        public static LocalisableString GridDefaultY => new TranslatableString(getKey(@"grid_default_y"), @"Grid origin Y");

        /// <summary>
        /// "What the editor's grid opens on. The spacing replaces the one saved in the beatmap."
        /// </summary>
        public static LocalisableString GridDefaultsDescription => new TranslatableString(getKey(@"grid_defaults_description"),
            @"What the editor's grid opens on. The spacing replaces the one saved in the beatmap.");

        /// <summary>
        /// "Only marks an object more than 2ms away from the nearest snap."
        /// </summary>
        public static LocalisableString ShowUnsnapMarkersDescription => new TranslatableString(getKey(@"show_unsnap_markers_description"),
            @"Only marks an object more than 2ms away from the nearest snap.");

        /// <summary>
        /// "Difficulty graph colour"
        /// </summary>
        public static LocalisableString DifficultyGraphColour => new TranslatableString(getKey(@"difficulty_graph_colour"), @"Difficulty graph colour");

        /// <summary>
        /// "Difficulty graph opacity"
        /// </summary>
        public static LocalisableString DifficultyGraphOpacity => new TranslatableString(getKey(@"difficulty_graph_opacity"), @"Difficulty graph opacity");

        /// <summary>
        /// "Difficulty graph detail"
        /// </summary>
        public static LocalisableString DifficultyGraphDetail => new TranslatableString(getKey(@"difficulty_graph_detail"), @"Difficulty graph detail");

        /// <summary>
        /// "Show the volume over the song"
        /// </summary>
        public static LocalisableString ShowVolumeGraph => new TranslatableString(getKey(@"show_volume_graph"), @"Show the volume over the song");

        /// <summary>
        /// "Draws how loud the beatmap is over its length across the bottom timeline."
        /// </summary>
        public static LocalisableString ShowVolumeGraphDescription => new TranslatableString(getKey(@"show_volume_graph_description"),
            @"Draws how loud the beatmap is over its length across the bottom timeline.");

        /// <summary>
        /// "Volume graph colour"
        /// </summary>
        public static LocalisableString VolumeGraphColour => new TranslatableString(getKey(@"volume_graph_colour"), @"Volume graph colour");

        /// <summary>
        /// "Volume graph thickness"
        /// </summary>
        public static LocalisableString VolumeGraphThickness => new TranslatableString(getKey(@"volume_graph_thickness"), @"Volume graph thickness");

        /// <summary>
        /// "Volume graph opacity"
        /// </summary>
        public static LocalisableString VolumeGraphOpacity => new TranslatableString(getKey(@"volume_graph_opacity"), @"Volume graph opacity");

        /// <summary>
        /// "Difficulty graph in detail"
        /// </summary>
        public static LocalisableString DifficultyGraphDetailScreen => new TranslatableString(getKey(@"difficulty_graph_detail_screen"), @"Difficulty graph in detail");

        /// <summary>
        /// "Per skill"
        /// </summary>
        public static LocalisableString DifficultyGraphPerSkill => new TranslatableString(getKey(@"difficulty_graph_per_skill"), @"Per skill");

        /// <summary>
        /// "Total"
        /// </summary>
        public static LocalisableString DifficultyGraphTotal => new TranslatableString(getKey(@"difficulty_graph_total"), @"Total");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString DifficultyGraphWindowOpacity => new TranslatableString(getKey(@"difficulty_graph_window_opacity"), @"Opacity");

        /// <summary>
        /// "Apply the current mods"
        /// </summary>
        public static LocalisableString DifficultyGraphApplyCurrentMods => new TranslatableString(getKey(@"difficulty_graph_apply_current_mods"), @"Apply the current mods");

        /// <summary>
        /// "Raising it too far can make the graph slow to build."
        /// </summary>
        public static LocalisableString DifficultyGraphDetailDescription => new TranslatableString(getKey(@"difficulty_graph_detail_description"),
            @"Raising it too far can make the graph slow to build.");

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
        /// "Open this difficulty's backups"
        /// </summary>
        public static LocalisableString OpenBackupFolder => new TranslatableString(getKey(@"open_backup_folder"), @"Open this difficulty's backups");

        /// <summary>
        /// "How long an object stays selectable after it passes"
        /// </summary>
        public static LocalisableString SelectableAfterEndTime => new TranslatableString(getKey(@"selectable_after_end_time"), @"How long an object stays selectable after it passes");

        /// <summary>
        /// "Ctrl+click a slider anchor to cycle its curve type"
        /// </summary>
        public static LocalisableString CtrlClickCyclesCurveType => new TranslatableString(getKey(@"ctrl_click_cycles_curve_type"), @"Ctrl+click a slider anchor to cycle its curve type");

        /// <summary>
        /// "Swap right click and the wheel button"
        /// </summary>
        public static LocalisableString SwapRightAndMiddleMouse => new TranslatableString(getKey(@"swap_right_and_middle_mouse"), @"Swap right click and the wheel button");

        /// <summary>
        /// "Seek animation"
        /// </summary>
        public static LocalisableString SeekAnimationDuration => new TranslatableString(getKey(@"seek_animation_duration"), @"Seek animation");

        /// <summary>
        /// "Keep left toolbox expanded"
        /// </summary>
        public static LocalisableString AlwaysExpandLeftToolbox => new TranslatableString(getKey(@"always_expand_left_toolbox"), @"Keep left toolbox expanded");

        /// <summary>
        /// "Left width when expanded"
        /// </summary>
        public static LocalisableString LeftToolboxWidth => new TranslatableString(getKey(@"left_toolbox_width"), @"Left width when expanded");

        /// <summary>
        /// "Right width when expanded"
        /// </summary>
        public static LocalisableString RightToolboxWidth => new TranslatableString(getKey(@"right_toolbox_width"), @"Right width when expanded");

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
        /// "Turn off the kiai flash"
        /// </summary>
        public static LocalisableString HideKiaiFlash => new TranslatableString(getKey(@"hide_kiai_flash"), @"Turn off the kiai flash");

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
        /// "Stock lazer ignores a drag that stays within 10px of the click, so small adjustments are easier with this on."
        /// </summary>
        public static LocalisableString RemoveDragThresholdDescription => new TranslatableString(getKey(@"remove_drag_threshold_description"),
            @"Stock lazer ignores a drag that stays within 10px of the click, so small adjustments are easier with this on.");

        /// <summary>
        /// "Show the play area border"
        /// </summary>
        public static LocalisableString ShowPlayAreaBorder => new TranslatableString(getKey(@"show_play_area_border"), @"Show the play area border");

        /// <summary>
        /// "Show autoplay's cursor"
        /// </summary>
        public static LocalisableString ShowAutoCursor => new TranslatableString(getKey(@"show_auto_cursor"), @"Show autoplay's cursor");

        /// <summary>
        /// "Draws where the cursor would be while scrubbing or playing back, moving exactly as autoplay does."
        /// </summary>
        public static LocalisableString ShowAutoCursorDescription => new TranslatableString(getKey(@"show_auto_cursor_description"),
            @"Draws where the cursor would be while scrubbing or playing back, moving exactly as autoplay does.");

        /// <summary>
        /// "Offscreen detection"
        /// </summary>
        public static LocalisableString DetectOffscreen => new TranslatableString(getKey(@"detect_offscreen"), @"Offscreen detection");

        /// <summary>
        /// "(left) Toolbox button height"
        /// </summary>
        public static LocalisableString ToolButtonSquareness => new TranslatableString(getKey(@"tool_button_squareness"), @"(left) Toolbox button height");

        /// <summary>
        /// "(left) Toggles button height"
        /// </summary>
        public static LocalisableString ToggleButtonSquareness => new TranslatableString(getKey(@"toggle_button_squareness"), @"(left) Toggles button height");

        /// <summary>
        /// "(left) Bank button height"
        /// </summary>
        public static LocalisableString SampleBankButtonSquareness => new TranslatableString(getKey(@"sample_bank_button_squareness"), @"(left) Bank button height");

        /// <summary>
        /// "(right) Grid button height"
        /// </summary>
        public static LocalisableString GridButtonSquareness => new TranslatableString(getKey(@"grid_button_squareness"), @"(right) Grid button height");

        /// <summary>
        /// "(right) Transform and Generate button height"
        /// </summary>
        public static LocalisableString ActionButtonSquareness => new TranslatableString(getKey(@"action_button_squareness"), @"(right) Transform and Generate button height");

        /// <summary>
        /// "(right) Slider velocity preset height"
        /// </summary>
        public static LocalisableString SliderVelocityPresetSquareness => new TranslatableString(getKey(@"slider_velocity_preset_squareness"), @"(right) Slider velocity preset height");

        /// <summary>
        /// "SR"
        /// </summary>
        public static LocalisableString StatusShowStarRating => new TranslatableString(getKey(@"status_show_star_rating"), @"SR");

        /// <summary>
        /// "MAX PP"
        /// </summary>
        public static LocalisableString StatusShowMaxPerformance => new TranslatableString(getKey(@"status_show_max_performance"), @"MAX PP");

        /// <summary>
        /// "SCORE (Lazer)"
        /// </summary>
        public static LocalisableString StatusShowScore => new TranslatableString(getKey(@"status_show_score"), @"SCORE (Lazer)");

        /// <summary>
        /// "SCORE V1 (Stable)"
        /// </summary>
        public static LocalisableString StatusShowLegacyScore => new TranslatableString(getKey(@"status_show_legacy_score"), @"SCORE V1 (Stable)");

        /// <summary>
        /// "SV"
        /// </summary>
        public static LocalisableString StatusShowSliderVelocity => new TranslatableString(getKey(@"status_show_slider_velocity"), @"SV");

        /// <summary>
        /// "Normal bank (Sampleset)"
        /// </summary>
        public static LocalisableString StatusShowNormalBank => new TranslatableString(getKey(@"status_show_normal_bank"), @"Normal bank (Sampleset)");

        /// <summary>
        /// "Addition bank (Additions)"
        /// </summary>
        public static LocalisableString StatusShowAdditionBank => new TranslatableString(getKey(@"status_show_addition_bank"), @"Addition bank (Additions)");

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
        /// "Object count"
        /// </summary>
        /// <summary>
        /// "Combo"
        /// </summary>
        public static LocalisableString StatusShowCombo => new TranslatableString(getKey(@"status_show_combo"), @"Combo");

        /// <summary>
        /// "Object count"
        /// </summary>
        public static LocalisableString StatusShowObjectCount => new TranslatableString(getKey(@"status_show_object_count"), @"Object count");

        /// <summary>
        /// "Drain time"
        /// </summary>
        public static LocalisableString StatusShowDrainTime => new TranslatableString(getKey(@"status_show_drain_time"), @"Drain time");

        /// <summary>
        /// "Difficulty settings (AR, CS, OD, HP)"
        /// </summary>
        public static LocalisableString StatusShowDifficultySettings => new TranslatableString(getKey(@"status_show_difficulty_settings"), @"Difficulty settings (AR, CS, OD, HP)");

        /// <summary>
        /// "Difficulty shown as a ghost"
        /// </summary>
        public static LocalisableString ReferenceDifficulty => new TranslatableString(getKey(@"reference_difficulty"), @"Difficulty shown as a ghost");

        /// <summary>
        /// "None"
        /// </summary>
        public static LocalisableString ReferenceDifficultyNone => new TranslatableString(getKey(@"reference_difficulty_none"), @"None");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString ReferenceDifficultyOpacity => new TranslatableString(getKey(@"reference_difficulty_opacity"), @"Opacity");

        /// <summary>
        /// "Colour"
        /// </summary>
        public static LocalisableString ReferenceDifficultyColour => new TranslatableString(getKey(@"reference_difficulty_colour"), @"Colour");

        /// <summary>
        /// "Backup written"
        /// </summary>
        public static LocalisableString BackupWritten => new TranslatableString(getKey(@"backup_written"), @"Backup written");

        /// <summary>
        /// "The backup is already up to date"
        /// </summary>
        public static LocalisableString BackupUpToDate => new TranslatableString(getKey(@"backup_up_to_date"), @"The backup is already up to date");

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
        /// "Hide the velocity and volume labels"
        /// </summary>
        public static LocalisableString HideTimelinePoints => new TranslatableString(getKey(@"hide_timeline_points"), @"Hide the velocity and volume labels");

        /// <summary>
        /// "The objects take the room the labels give up, which makes them easier to hit."
        /// </summary>
        public static LocalisableString HideTimelinePointsDescription => new TranslatableString(getKey(@"hide_timeline_points_description"),
            @"The objects take the room the labels give up, which makes them easier to hit.");

        /// <summary>
        /// "Layout"
        /// </summary>
        public static LocalisableString TimelineStyle => new TranslatableString(getKey(@"timeline_style"), @"Layout");

        /// <summary>
        /// "Lazer"
        /// </summary>
        public static LocalisableString TimelineStyleLazer => new TranslatableString(getKey(@"timeline_style_lazer"), @"Lazer");

        /// <summary>
        /// "Stable"
        /// </summary>
        public static LocalisableString TimelineStyleStable => new TranslatableString(getKey(@"timeline_style_stable"), @"Stable");

        /// <summary>
        /// "Lazer-Kai"
        /// </summary>
        public static LocalisableString TimelineStyleLazerKai => new TranslatableString(getKey(@"timeline_style_lazer_kai"), @"Lazer-Kai");

        /// <summary>
        /// "Stable rests the objects on the bottom edge and keeps the ticks down there with them, with velocity and volume above. Lazer-Kai is the same with a tick growing in from the top edge as well."
        /// </summary>
        public static LocalisableString TimelineStyleDescription => new TranslatableString(getKey(@"timeline_style_description"),
            @"Stable rests the objects on the bottom edge and keeps the ticks down there with them, with velocity and volume above. Lazer-Kai is the same with a tick growing in from the top edge as well.");

        /// <summary>
        /// "Drag the top of it to move the playhead"
        /// </summary>
        public static LocalisableString TimelineDragToSeek => new TranslatableString(getKey(@"timeline_drag_to_seek"), @"Drag the top of it to move the playhead");

        /// <summary>
        /// "The timeline can be pulled along like a hand tool. Turn it off if it keeps happening by accident."
        /// </summary>
        public static LocalisableString TimelineDragToSeekDescription => new TranslatableString(getKey(@"timeline_drag_to_seek_description"),
            @"The timeline can be pulled along like a hand tool. Turn it off if it keeps happening by accident.");

        /// <summary>
        /// "Draw it with the current skin"
        /// </summary>
        public static LocalisableString TimelineUseSkin => new TranslatableString(getKey(@"timeline_use_skin"), @"Draw it with the current skin");

        /// <summary>
        /// "Brings the look and the feel closer to stable's."
        /// </summary>
        public static LocalisableString TimelineUseSkinDescription => new TranslatableString(getKey(@"timeline_use_skin_description"),
            @"Brings the look and the feel closer to stable's.");

        /// <summary>
        /// "Hide the volume row"
        /// </summary>
        public static LocalisableString HideTimelineVolume => new TranslatableString(getKey(@"hide_timeline_volume"), @"Hide the volume row");

        /// <summary>
        /// "The objects take the space it gives up. The velocity row stays."
        /// </summary>
        public static LocalisableString HideTimelineVolumeDescription => new TranslatableString(getKey(@"hide_timeline_volume_description"),
            @"The objects take the space it gives up. The velocity row stays.");

        /// <summary>
        /// "Reverse handle size"
        /// </summary>
        public static LocalisableString TimelineDragAreaScale => new TranslatableString(getKey(@"timeline_drag_area_scale"), @"Reverse handle size");

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
        /// "Reverse drag sensitivity"
        /// </summary>
        public static LocalisableString TimelineRepeatDragThreshold => new TranslatableString(getKey(@"timeline_repeat_drag_threshold"), @"Reverse drag sensitivity");

        /// <summary>
        /// "How far a slider's end has to be dragged before another reverse is added."
        /// </summary>
        public static LocalisableString TimelineRepeatDragThresholdDescription => new TranslatableString(getKey(@"timeline_repeat_drag_threshold_description"),
            @"How far a slider's end has to be dragged before another reverse is added.");

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

        /// <summary>
        /// "Added "{0}" as a new difficulty."
        /// </summary>
        public static LocalisableString DroppedDifficultyAdded(string difficultyName) => new TranslatableString(getKey(@"dropped_difficulty_added"),
            @"Added ""{0}"" as a new difficulty.", difficultyName);

        /// <summary>
        /// "Replaced the difficulty "{0}"."
        /// </summary>
        public static LocalisableString DroppedDifficultyReplaced(string difficultyName) => new TranslatableString(getKey(@"dropped_difficulty_replaced"),
            @"Replaced the difficulty ""{0}"".", difficultyName);

        /// <summary>
        /// ""{0}" could not be read as a difficulty."
        /// </summary>
        public static LocalisableString DroppedDifficultyUnreadable(string fileName) => new TranslatableString(getKey(@"dropped_difficulty_unreadable"),
            @"""{0}"" could not be read as a difficulty.", fileName);

        /// <summary>
        /// "This set already has a difficulty named "{0}" in another mode, so the dropped file was left alone."
        /// </summary>
        public static LocalisableString DroppedDifficultyNameTaken(string difficultyName) => new TranslatableString(getKey(@"dropped_difficulty_name_taken"),
            @"This set already has a difficulty named ""{0}"" in another mode, so the dropped file was left alone.", difficultyName);

        /// <summary>
        /// "Click to write out the logs to send, with your Windows account name removed."
        /// </summary>
        public static LocalisableString ClickToExportLogs => new TranslatableString(getKey(@"click_to_export_logs"),
            @"Click to write out the logs to send, with your Windows account name removed.");

        /// <summary>
        /// "Open in Mapping Tools"
        /// </summary>
        public static LocalisableString OpenInMappingTools => new TranslatableString(getKey(@"open_in_mapping_tools"), @"Open in Mapping Tools");

        /// <summary>
        /// "Mapping Tools was not found. Set where it is installed in the Lazer-Kai options."
        /// </summary>
        public static LocalisableString MappingToolsNotFound => new TranslatableString(getKey(@"mapping_tools_not_found"),
            @"Mapping Tools was not found. Set where it is installed in the Lazer-Kai options.");

        /// <summary>
        /// "Mapping Tools is already open. It only reads which beatmap to work on as it starts, so close it and try again."
        /// </summary>
        public static LocalisableString MappingToolsAlreadyRunning => new TranslatableString(getKey(@"mapping_tools_already_running"),
            @"Mapping Tools is already open. It only reads which beatmap to work on as it starts, so close it and try again.");

        /// <summary>
        /// "The file set in the Lazer-Kai options is not Mapping Tools. Point it at Mapping Tools.exe."
        /// </summary>
        public static LocalisableString MappingToolsPathIsSomethingElse => new TranslatableString(getKey(@"mapping_tools_path_is_something_else"),
            @"The file set in the Lazer-Kai options is not Mapping Tools. Point it at Mapping Tools.exe.");

        /// <summary>
        /// "Mapping Tools could not be started."
        /// </summary>
        public static LocalisableString MappingToolsFailedToStart => new TranslatableString(getKey(@"mapping_tools_failed_to_start"), @"Mapping Tools could not be started.");

        /// <summary>
        /// "An object reaching outside the play area is outlined in red."
        /// </summary>
        public static LocalisableString DetectOffscreenDescription => new TranslatableString(getKey(@"detect_offscreen_description"), @"An object reaching outside the play area is outlined in red.");

        /// <summary>
        /// "How long the timeline takes to settle when the wheel or a key moves it."
        /// </summary>
        public static LocalisableString SeekAnimationDurationDescription => new TranslatableString(getKey(@"seek_animation_duration_description"), @"How long the timeline takes to settle when the wheel or a key moves it.");

        /// <summary>
        /// "Left off, the volume labels open and close as they please."
        /// </summary>
        public static LocalisableString AlwaysExpandTimelinePointLabelsDescription => new TranslatableString(getKey(@"always_expand_timeline_point_labels_description"), @"Left off, the volume labels open and close as they please.");

        /// <summary>
        /// "How much of a slider's end takes the drag. Drawing the timeline with the skin hands the whole end over instead, and this no longer applies."
        /// </summary>
        public static LocalisableString TimelineDragAreaScaleDescription => new TranslatableString(getKey(@"timeline_drag_area_scale_description"), @"How much of a slider's end takes the drag. Drawing the timeline with the skin hands the whole end over instead, and this no longer applies.");

        /// <summary>
        /// "Hides the five buttons a selection carries, flipping and the rest."
        /// </summary>
        public static LocalisableString HideSelectionButtonsDescription => new TranslatableString(getKey(@"hide_selection_buttons_description"), @"Hides the five buttons a selection carries, flipping and the rest.");

        /// <summary>
        /// "Stock lazer keeps objects selectable long after they are out of sight, so a drag takes in things nobody meant. Around 200ms is a starting point."
        /// </summary>
        public static LocalisableString SelectableAfterEndTimeDescription => new TranslatableString(getKey(@"selectable_after_end_time_description"), @"Stock lazer keeps objects selectable long after they are out of sight, so a drag takes in things nobody meant. Around 200ms is a starting point.");

        /// <summary>
        /// "The colour of the box drawn around a selection."
        /// </summary>
        public static LocalisableString SelectionColourDescription => new TranslatableString(getKey(@"selection_colour_description"), @"The colour of the box drawn around a selection.");

        /// <summary>
        /// "The thickness of the box drawn around a selection."
        /// </summary>
        public static LocalisableString SelectionThicknessDescription => new TranslatableString(getKey(@"selection_thickness_description"), @"The thickness of the box drawn around a selection.");

        /// <summary>
        /// "How solid the box drawn around a selection is."
        /// </summary>
        public static LocalisableString SelectionOpacityDescription => new TranslatableString(getKey(@"selection_opacity_description"), @"How solid the box drawn around a selection is.");

        /// <summary>
        /// "The colour of the outline drawn on a selected object itself."
        /// </summary>
        public static LocalisableString ObjectOutlineColourDescription => new TranslatableString(getKey(@"object_outline_colour_description"), @"The colour of the outline drawn on a selected object itself.");

        /// <summary>
        /// "The thickness of the outline drawn on a selected object itself."
        /// </summary>
        public static LocalisableString ObjectOutlineThicknessDescription => new TranslatableString(getKey(@"object_outline_thickness_description"), @"The thickness of the outline drawn on a selected object itself.");

        /// <summary>
        /// "How solid the outline drawn on a selected object itself is."
        /// </summary>
        public static LocalisableString ObjectOutlineOpacityDescription => new TranslatableString(getKey(@"object_outline_opacity_description"), @"How solid the outline drawn on a selected object itself is.");

        /// <summary>
        /// "The colour of the eight points a selection is resized by."
        /// </summary>
        public static LocalisableString SelectionHandleColourDescription => new TranslatableString(getKey(@"selection_handle_colour_description"), @"The colour of the eight points a selection is resized by.");

        /// <summary>
        /// "The size of the eight points a selection is resized by."
        /// </summary>
        public static LocalisableString SelectionHandleSizeDescription => new TranslatableString(getKey(@"selection_handle_size_description"), @"The size of the eight points a selection is resized by.");

        /// <summary>
        /// "The colour of the half circle on a slider's end, which is what shortens it."
        /// </summary>
        public static LocalisableString SliderEndDragMarkerColourDescription => new TranslatableString(getKey(@"slider_end_drag_marker_colour_description"), @"The colour of the half circle on a slider's end, which is what shortens it.");

        /// <summary>
        /// "The thickness of the half circle on a slider's end, which is what shortens it."
        /// </summary>
        public static LocalisableString SliderEndDragMarkerScaleDescription => new TranslatableString(getKey(@"slider_end_drag_marker_scale_description"), @"The thickness of the half circle on a slider's end, which is what shortens it.");

        /// <summary>
        /// "How solid the half circle on a slider's end is."
        /// </summary>
        public static LocalisableString SliderEndDragMarkerOpacityDescription => new TranslatableString(getKey(@"slider_end_drag_marker_opacity_description"), @"How solid the half circle on a slider's end is.");

        /// <summary>
        /// "After an object is placed the playhead moves on by the gap before it, so the wheel is not needed between placements."
        /// </summary>
        public static LocalisableString AdvanceByPreviousSpacingOnPlacementDescription => new TranslatableString(getKey(@"advance_by_previous_spacing_on_placement_description"), @"After an object is placed the playhead moves on by the gap before it, so the wheel is not needed between placements.");

        /// <summary>
        /// "Turn it off if placing a slider keeps drawing one by accident."
        /// </summary>
        public static LocalisableString FreehandSliderPlacementDescription => new TranslatableString(getKey(@"freehand_slider_placement_description"), @"Turn it off if placing a slider keeps drawing one by accident.");

        /// <summary>
        /// "Ctrl-clicking what stable calls a white anchor turns it red."
        /// </summary>
        public static LocalisableString CtrlClickCyclesCurveTypeDescription => new TranslatableString(getKey(@"ctrl_click_cycles_curve_type_description"), @"Ctrl-clicking what stable calls a white anchor turns it red.");

        /// <summary>
        /// "Right click deletes, and the wheel button opens the menu. Only while the editor is open."
        /// </summary>
        public static LocalisableString SwapRightAndMiddleMouseDescription => new TranslatableString(getKey(@"swap_right_and_middle_mouse_description"), @"Right click deletes, and the wheel button opens the menu. Only while the editor is open.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
