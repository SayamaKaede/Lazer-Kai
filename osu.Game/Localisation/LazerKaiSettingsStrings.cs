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
        /// "Manual"
        /// </summary>
        public static LocalisableString ManualHeader => new TranslatableString(getKey(@"manual_header"), @"Manual");

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
        /// "Sends errors, and saves that could not stay in stable's format, to Lazer-Kai's developer. Your osu! account, your Windows account name and your computer's name are left out."
        /// </summary>
        public static LocalisableString ErrorReportingDescription => new TranslatableString(getKey(@"error_reporting_description"),
            @"Sends errors, and saves that could not stay in stable's format, to Lazer-Kai's developer. Your osu! account, your Windows account name and your computer's name are left out.");

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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
