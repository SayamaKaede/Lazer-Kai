// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Headings for the settings section gathering everything this fork adds.
    /// </summary>
    /// <remarks>
    /// The settings themselves reuse <see cref="EditorStrings"/>, so a control carries the same wording wherever it
    /// is shown — the editor menu and this section are two ways to the same setting, not two settings.
    /// </remarks>
    public static class LazerKaiSettingsStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiSettings";

        /// <summary>
        /// "Lazer-Kai"
        /// </summary>
        public static LocalisableString SectionHeader => new TranslatableString(getKey(@"section_header"), @"Lazer-Kai");

        /// <summary>
        /// "Manual and changelog"
        /// </summary>
        public static LocalisableString ManualHeader => new TranslatableString(getKey(@"manual_header"), @"Manual and changelog");

        /// <summary>
        /// "Open the manual"
        /// </summary>
        public static LocalisableString OpenManual => new TranslatableString(getKey(@"open_manual"), @"Open the manual");

        /// <summary>
        /// "Opens the page that ships with this build, in whatever reads a web page."
        /// </summary>
        public static LocalisableString OpenManualDescription => new TranslatableString(getKey(@"open_manual_description"),
            @"Opens the page that ships with this build, in whatever reads a web page.");

        /// <summary>
        /// "Open "Getting started""
        /// </summary>
        public static LocalisableString OpenGuide => new TranslatableString(getKey(@"open_guide"), @"Open ""Getting started""");

        /// <summary>
        /// "The main features and frequently asked questions, in whatever reads a web page."
        /// </summary>
        public static LocalisableString OpenGuideDescription => new TranslatableString(getKey(@"open_guide_description"),
            @"The main features and frequently asked questions, in whatever reads a web page.");

        /// <summary>
        /// "Show the changelog"
        /// </summary>
        public static LocalisableString ShowChangelog => new TranslatableString(getKey(@"show_changelog"), @"Show the changelog");

        /// <summary>
        /// "changelog"
        /// </summary>
        public static LocalisableString ChangelogTitle => new TranslatableString(getKey(@"changelog_title"), @"changelog");

        /// <summary>
        /// "Beatmap"
        /// </summary>
        public static LocalisableString BeatmapSectionHeader => new TranslatableString(getKey(@"beatmap_section_header"), @"Beatmap");

        /// <summary>
        /// "Mods"
        /// </summary>
        public static LocalisableString TestPlaySectionHeader => new TranslatableString(getKey(@"test_play_section_header"), @"Mods");

        /// <summary>
        /// "Other"
        /// </summary>
        public static LocalisableString OtherSectionHeader => new TranslatableString(getKey(@"other_section_header"), @"Other");

        /// <summary>
        /// "Editor display"
        /// </summary>
        public static LocalisableString DisplayHeader => new TranslatableString(getKey(@"display_header"), @"Editor display");

        /// <summary>
        /// "Bottom timeline"
        /// </summary>
        public static LocalisableString DifficultyGraphHeader => new TranslatableString(getKey(@"difficulty_graph_header"), @"Bottom timeline");

        /// <summary>
        /// "Ghost difficulty"
        /// </summary>
        public static LocalisableString GhostDifficultyHeader => new TranslatableString(getKey(@"ghost_difficulty_header"), @"Ghost difficulty");

        /// <summary>
        /// "Hitsounds"
        /// </summary>
        public static LocalisableString HitsoundHeader => new TranslatableString(getKey(@"hitsound_header"), @"Hitsounds");

        /// <summary>
        /// "Other customisation"
        /// </summary>
        public static LocalisableString OtherCustomisationHeader => new TranslatableString(getKey(@"other_customisation_header"), @"Other customisation");

        /// <summary>
        /// "Toolboxes"
        /// </summary>
        public static LocalisableString ToolboxHeader => new TranslatableString(getKey(@"toolbox_header"), @"Toolboxes");

        /// <summary>
        /// "Toolbox buttons"
        /// </summary>
        public static LocalisableString ButtonShapeHeader => new TranslatableString(getKey(@"button_shape_header"), @"Toolbox buttons");

        /// <summary>
        /// "Timeline"
        /// </summary>
        public static LocalisableString TimelineHeader => new TranslatableString(getKey(@"timeline_header"), @"Timeline");

        /// <summary>
        /// "Selection and drag box"
        /// </summary>
        public static LocalisableString SelectionHeader => new TranslatableString(getKey(@"selection_header"), @"Selection and drag box");

        /// <summary>
        /// "STATUS"
        /// </summary>
        public static LocalisableString StatusHeader => new TranslatableString(getKey(@"status_header"), @"STATUS");

        /// <summary>
        /// "Input"
        /// </summary>
        public static LocalisableString InputHeader => new TranslatableString(getKey(@"input_header"), @"Input");

        /// <summary>
        /// "Mods"
        /// </summary>
        public static LocalisableString TestPlayHeader => new TranslatableString(getKey(@"test_play_header"), @"Mods");

        /// <summary>
        /// "Backups"
        /// </summary>
        public static LocalisableString BackupHeader => new TranslatableString(getKey(@"backup_header"), @"Backups");

        /// <summary>
        /// "Takes effect the next time the editor is opened"
        /// </summary>
        public static LocalisableString TakesEffectOnReopen => new TranslatableString(getKey(@"takes_effect_on_reopen"), @"Takes effect the next time the editor is opened");

        /// <summary>
        /// "Instant"
        /// </summary>
        public static LocalisableString NoDelay => new TranslatableString(getKey(@"no_delay"), @"Instant");

        /// <summary>
        /// "{0} minutes"
        /// </summary>
        public static LocalisableString Minutes(string count) => new TranslatableString(getKey(@"minutes"), @"{0} minutes", count);

        /// <summary>
        /// "Beatmap skin"
        /// </summary>
        public static LocalisableString BeatmapSkinHeader => new TranslatableString(getKey(@"beatmap_skin_header"), @"Beatmap skin");

        /// <summary>
        /// "Beatmap colours"
        /// </summary>
        public static LocalisableString EditorBeatmapColours => new TranslatableString(getKey(@"editor_beatmap_colours"), @"Beatmap colours");

        /// <summary>
        /// "Forces the beatmap's own combo colours."
        /// </summary>
        public static LocalisableString EditorBeatmapColoursDescription => new TranslatableString(getKey(@"editor_beatmap_colours_description"),
            @"Forces the beatmap's own combo colours.");

        /// <summary>
        /// "Beatmap hitsounds"
        /// </summary>
        public static LocalisableString EditorBeatmapHitsounds => new TranslatableString(getKey(@"editor_beatmap_hitsounds"), @"Beatmap hitsounds");

        /// <summary>
        /// "Forces the beatmap's own hitsounds."
        /// </summary>
        public static LocalisableString EditorBeatmapHitsoundsDescription => new TranslatableString(getKey(@"editor_beatmap_hitsounds_description"),
            @"Forces the beatmap's own hitsounds.");

        /// <summary>
        /// "External tools"
        /// </summary>
        public static LocalisableString ExternalToolHeader => new TranslatableString(getKey(@"external_tool_header"), @"External tools");

        /// <summary>
        /// "Mapping Tools"
        /// </summary>
        public static LocalisableString MappingToolsPath => new TranslatableString(getKey(@"mapping_tools_path"), @"Mapping Tools");

        /// <summary>
        /// "Export logs for sending"
        /// </summary>
        public static LocalisableString ExportLogs => new TranslatableString(getKey(@"export_logs"), @"Export logs for sending");

        /// <summary>
        /// "Copies the recent logs with your Windows account name replaced, and opens the folder they went to."
        /// </summary>
        public static LocalisableString ExportLogsDescription => new TranslatableString(getKey(@"export_logs_description"),
            @"Copies the recent logs with your Windows account name replaced, and opens the folder they went to.");

        /// <summary>
        /// "{0} log files were written. Click to open the folder."
        /// </summary>
        public static LocalisableString ExportedLogs(string count) => new TranslatableString(getKey(@"exported_logs"),
            @"{0} log files were written. Click to open the folder.", count);

        /// <summary>
        /// "The logs could not be exported."
        /// </summary>
        public static LocalisableString ExportLogsFailed => new TranslatableString(getKey(@"export_logs_failed"), @"The logs could not be exported.");

        /// <summary>
        /// "Send error reports automatically"
        /// </summary>
        public static LocalisableString ErrorReporting => new TranslatableString(getKey(@"error_reporting"), @"Send error reports automatically");

        /// <summary>
        /// "Sends errors to Lazer-Kai's developer. Your osu! account, your Windows account name and your computer's name are left out."
        /// </summary>
        public static LocalisableString ErrorReportingDescription => new TranslatableString(getKey(@"error_reporting_description"),
            @"Sends errors to Lazer-Kai's developer. Your osu! account, your Windows account name and your computer's name are left out.");

        /// <summary>
        /// "Settings presets"
        /// </summary>
        public static LocalisableString SettingsPresetHeader => new TranslatableString(getKey(@"settings_preset_header"), @"Settings presets");

        /// <summary>
        /// "Preset"
        /// </summary>
        public static LocalisableString SettingsPreset => new TranslatableString(getKey(@"settings_preset"), @"Preset");

        /// <summary>
        /// "Only Lazer-Kai's settings. Stable compatibility, sending error reports and the Mapping Tools path are left as they are, and the recommended settings leave the colours and the skin as well."
        /// </summary>
        public static LocalisableString SettingsPresetDescription => new TranslatableString(getKey(@"settings_preset_description"),
            @"Only Lazer-Kai's settings. Stable compatibility, sending error reports and the Mapping Tools path are left as they are, and the recommended settings leave the colours and the skin as well.");

        /// <summary>
        /// "Recommended settings"
        /// </summary>
        public static LocalisableString RecommendedSettings => new TranslatableString(getKey(@"recommended_settings"), @"Recommended settings");

        /// <summary>
        /// "Settings before applying a preset"
        /// </summary>
        public static LocalisableString SettingsBeforeApplying => new TranslatableString(getKey(@"settings_before_applying"), @"Settings before applying a preset");

        /// <summary>
        /// "Apply this preset"
        /// </summary>
        public static LocalisableString ApplySettingsPreset => new TranslatableString(getKey(@"apply_settings_preset"), @"Apply this preset");

        /// <summary>
        /// "Apply {0}?"
        /// </summary>
        public static LocalisableString ApplySettingsPresetHeader(LocalisableString name) => new TranslatableString(getKey(@"apply_settings_preset_header"), @"Apply {0}?", name);

        /// <summary>
        /// "Lazer-Kai's settings are overwritten. The colours, the skin, stable compatibility, sending error reports and the Mapping Tools path are left as they are. The settings as they are now are kept as the settings before applying a preset."
        /// </summary>
        public static LocalisableString ApplySettingsPresetBody => new TranslatableString(getKey(@"apply_settings_preset_body"),
            @"Lazer-Kai's settings are overwritten. The colours, the skin, stable compatibility, sending error reports and the Mapping Tools path are left as they are. The settings as they are now are kept as the settings before applying a preset.");

        /// <summary>
        /// "Lazer-Kai's settings are overwritten, the colours and the skin included. Stable compatibility, sending error reports and the Mapping Tools path are left as they are. The settings as they are now are kept as the settings before applying a preset."
        /// </summary>
        public static LocalisableString ApplySavedSettingsPresetBody => new TranslatableString(getKey(@"apply_saved_settings_preset_body"),
            @"Lazer-Kai's settings are overwritten, the colours and the skin included. Stable compatibility, sending error reports and the Mapping Tools path are left as they are. The settings as they are now are kept as the settings before applying a preset.");

        /// <summary>
        /// "Apply"
        /// </summary>
        public static LocalisableString ApplySettingsPresetConfirm => new TranslatableString(getKey(@"apply_settings_preset_confirm"), @"Apply");

        /// <summary>
        /// "Applied {0}."
        /// </summary>
        public static LocalisableString AppliedSettingsPreset(LocalisableString name) => new TranslatableString(getKey(@"applied_settings_preset"), @"Applied {0}.", name);

        /// <summary>
        /// "Name"
        /// </summary>
        public static LocalisableString SettingsPresetName => new TranslatableString(getKey(@"settings_preset_name"), @"Name");

        /// <summary>
        /// "Save the current settings under this name"
        /// </summary>
        public static LocalisableString SaveSettingsPreset => new TranslatableString(getKey(@"save_settings_preset"), @"Save the current settings under this name");

        /// <summary>
        /// "Saved as {0}."
        /// </summary>
        public static LocalisableString SavedSettingsPreset(string name) => new TranslatableString(getKey(@"saved_settings_preset"), @"Saved as {0}.", name);

        /// <summary>
        /// "Delete this preset"
        /// </summary>
        public static LocalisableString DeleteSettingsPreset => new TranslatableString(getKey(@"delete_settings_preset"), @"Delete this preset");

        /// <summary>
        /// "Delete {0}?"
        /// </summary>
        public static LocalisableString DeleteSettingsPresetHeader(string name) => new TranslatableString(getKey(@"delete_settings_preset_header"), @"Delete {0}?", name);

        /// <summary>
        /// "Open the presets folder"
        /// </summary>
        public static LocalisableString OpenSettingsPresetFolder => new TranslatableString(getKey(@"open_settings_preset_folder"), @"Open the presets folder");

        /// <summary>
        /// "Show the Mapping Tools path"
        /// </summary>
        public static LocalisableString ShowMappingToolsPath => new TranslatableString(getKey(@"show_mapping_tools_path"), @"Show the Mapping Tools path");

        /// <summary>
        /// "Hide the Mapping Tools path"
        /// </summary>
        public static LocalisableString HideMappingToolsPath => new TranslatableString(getKey(@"hide_mapping_tools_path"), @"Hide the Mapping Tools path");

        /// <summary>
        /// "Mapping Tools was not found"
        /// </summary>
        public static LocalisableString MappingToolsNotDetected => new TranslatableString(getKey(@"mapping_tools_not_detected"), @"Mapping Tools was not found");

        /// <summary>
        /// "Where Mapping Tools.exe is. Leave empty to use the usual install."
        /// </summary>
        public static LocalisableString MappingToolsPathDescription => new TranslatableString(getKey(@"mapping_tools_path_description"),
            @"Where Mapping Tools.exe is. Leave empty to use the usual install.");

        /// <summary>
        /// "Export"
        /// </summary>
        public static LocalisableString ExportHeader => new TranslatableString(getKey(@"export_header"), @"Export");

        /// <summary>
        /// "Appearance"
        /// </summary>
        public static LocalisableString AppearanceHeader => new TranslatableString(getKey(@"appearance_header"), @"Appearance");

        /// <summary>
        /// "Theme"
        /// </summary>
        public static LocalisableString UiTheme => new TranslatableString(getKey(@"ui_theme"), @"Theme");

        /// <summary>
        /// "Takes effect the next time the game is started."
        /// </summary>
        public static LocalisableString UiThemeDescription => new TranslatableString(getKey(@"ui_theme_description"),
            @"Takes effect the next time the game is started.");

        /// <summary>
        /// "osu!lazer"
        /// </summary>
        public static LocalisableString UiThemeLazer => new TranslatableString(getKey(@"ui_theme_lazer"), @"osu!lazer");

        /// <summary>
        /// "Lazer-Kai"
        /// </summary>
        public static LocalisableString UiThemeLazerKai => new TranslatableString(getKey(@"ui_theme_lazer_kai"), @"Lazer-Kai");

        /// <summary>
        /// "Skin for each mode"
        /// </summary>
        public static LocalisableString RulesetSkinHeader => new TranslatableString(getKey(@"ruleset_skin_header"), @"Skin for each mode");

        /// <summary>
        /// "Not set"
        /// </summary>
        public static LocalisableString RulesetSkinNotSet => new TranslatableString(getKey(@"ruleset_skin_not_set"), @"Not set");

        /// <summary>
        /// "Used while this mode is selected. When not set, the skin chosen in the skin settings is used."
        /// </summary>
        public static LocalisableString RulesetSkinDescription => new TranslatableString(getKey(@"ruleset_skin_description"),
            @"Used while this mode is selected. When not set, the skin chosen in the skin settings is used.");

        /// <summary>
        /// "osu!mania"
        /// </summary>
        public static LocalisableString ManiaHeader => new TranslatableString(getKey(@"mania_header"), @"osu!mania");

        /// <summary>
        /// "Hide the keys at the bottom in the editor"
        /// </summary>
        public static LocalisableString ManiaHideKeys => new TranslatableString(getKey(@"mania_hide_keys"), @"Hide the keys at the bottom in the editor");

        /// <summary>
        /// "The lanes go on under the judgement line, where notes that have gone by are shown and notes can be placed"
        /// </summary>
        public static LocalisableString ManiaHideKeysDescription => new TranslatableString(getKey(@"mania_hide_keys_description"),
            @"The lanes go on under the judgement line, where notes that have gone by are shown and notes can be placed");

        /// <summary>
        /// "Gap between stages in the editor"
        /// </summary>
        public static LocalisableString ManiaStageSpacing => new TranslatableString(getKey(@"mania_stage_spacing"), @"Gap between stages in the editor");

        /// <summary>
        /// "For a beatmap with dual stages. The stages are placed side by side in the middle, this far apart"
        /// </summary>
        public static LocalisableString ManiaStageSpacingDescription => new TranslatableString(getKey(@"mania_stage_spacing_description"),
            @"For a beatmap with dual stages. The stages are placed side by side in the middle, this far apart");

        /// <summary>
        /// "Thickness of the beat snap grid lines in the editor"
        /// </summary>
        public static LocalisableString ManiaTickThickness => new TranslatableString(getKey(@"mania_tick_thickness"), @"Thickness of the beat snap grid lines in the editor");

        /// <summary>
        /// "Opacity of the beat snap grid lines in the editor"
        /// </summary>
        public static LocalisableString ManiaTickOpacity => new TranslatableString(getKey(@"mania_tick_opacity"), @"Opacity of the beat snap grid lines in the editor");

        /// <summary>
        /// "The lines drawn across the lanes, not those of the timeline"
        /// </summary>
        public static LocalisableString ManiaTickDescription => new TranslatableString(getKey(@"mania_tick_description"), @"The lines drawn across the lanes, not those of the timeline");

        /// <summary>
        /// "Playback"
        /// </summary>
        public static LocalisableString KeyBindingPlaybackHeader => new TranslatableString(getKey(@"key_binding_playback_header"), @"Playback");

        /// <summary>
        /// "Placement and editing"
        /// </summary>
        public static LocalisableString KeyBindingEditingHeader => new TranslatableString(getKey(@"key_binding_editing_header"), @"Placement and editing");

        /// <summary>
        /// "Hitsound mode"
        /// </summary>
        public static LocalisableString KeyBindingHitsoundModeHeader => new TranslatableString(getKey(@"key_binding_hitsound_mode_header"), @"Hitsound mode");

        /// <summary>
        /// "Song select"
        /// </summary>
        public static LocalisableString SongSelectHeader => new TranslatableString(getKey(@"song_select_header"), @"Song select");

        /// <summary>
        /// "Open the editor from song select"
        /// </summary>
        public static LocalisableString SongSelectOpensEditor => new TranslatableString(getKey(@"song_select_opens_editor"), @"Open the editor from song select");

        /// <summary>
        /// "Clicking the selected difficulty, Enter and the logo open it in the editor. Alt-click to play it."
        /// </summary>
        public static LocalisableString SongSelectOpensEditorDescription => new TranslatableString(getKey(@"song_select_opens_editor_description"),
            @"Clicking the selected difficulty, Enter and the logo open it in the editor. Alt-click to play it.");

        /// <summary>
        /// "A panel for each difficulty when sorted by Date Updated"
        /// </summary>
        public static LocalisableString DateUpdatedSplitsDifficulties => new TranslatableString(getKey(@"date_updated_splits_difficulties"),
            @"A panel for each difficulty when sorted by Date Updated");

        /// <summary>
        /// "Like stable, each difficulty is listed where its own save puts it. One set's in a row are stacked, and the selected set's are brought together."
        /// </summary>
        public static LocalisableString DateUpdatedSplitsDifficultiesDescription => new TranslatableString(getKey(@"date_updated_splits_difficulties_description"),
            @"Like stable, each difficulty is listed where its own save puts it. One set's in a row are stacked, and the selected set's are brought together.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
