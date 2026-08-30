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
        /// "Beatmap"
        /// </summary>
        public static LocalisableString BeatmapSectionHeader => new TranslatableString(getKey(@"beatmap_section_header"), @"Beatmap");

        /// <summary>
        /// "Test play"
        /// </summary>
        public static LocalisableString TestPlaySectionHeader => new TranslatableString(getKey(@"test_play_section_header"), @"Test play");

        /// <summary>
        /// "Other"
        /// </summary>
        public static LocalisableString OtherSectionHeader => new TranslatableString(getKey(@"other_section_header"), @"Other");

        /// <summary>
        /// "Editor display"
        /// </summary>
        public static LocalisableString DisplayHeader => new TranslatableString(getKey(@"display_header"), @"Editor display");

        /// <summary>
        /// "Selection"
        /// </summary>
        public static LocalisableString SelectionHeader => new TranslatableString(getKey(@"selection_header"), @"Selection");

        /// <summary>
        /// "Status panel"
        /// </summary>
        public static LocalisableString StatusHeader => new TranslatableString(getKey(@"status_header"), @"Status panel");

        /// <summary>
        /// "Composing"
        /// </summary>
        public static LocalisableString ComposingHeader => new TranslatableString(getKey(@"composing_header"), @"Composing");

        /// <summary>
        /// "Input"
        /// </summary>
        public static LocalisableString InputHeader => new TranslatableString(getKey(@"input_header"), @"Input");

        /// <summary>
        /// "Test play"
        /// </summary>
        public static LocalisableString TestPlayHeader => new TranslatableString(getKey(@"test_play_header"), @"Test play");

        /// <summary>
        /// "Backups"
        /// </summary>
        public static LocalisableString BackupHeader => new TranslatableString(getKey(@"backup_header"), @"Backups");

        /// <summary>
        /// "Applies outside the editor as well"
        /// </summary>
        public static LocalisableString AppliesGameWide => new TranslatableString(getKey(@"applies_game_wide"), @"Applies outside the editor as well");

        /// <summary>
        /// "Takes effect the next time the editor is opened"
        /// </summary>
        public static LocalisableString TakesEffectOnReopen => new TranslatableString(getKey(@"takes_effect_on_reopen"), @"Takes effect the next time the editor is opened");

        /// <summary>
        /// "Only while the editor is open"
        /// </summary>
        public static LocalisableString EditorOnly => new TranslatableString(getKey(@"editor_only"), @"Only while the editor is open");

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
        /// "Off uses your skin's combo colours instead. Does not change how the beatmap plays."
        /// </summary>
        public static LocalisableString EditorBeatmapColoursDescription => new TranslatableString(getKey(@"editor_beatmap_colours_description"),
            @"Off uses your skin's combo colours instead. Does not change how the beatmap plays.");

        /// <summary>
        /// "Beatmap hitsounds"
        /// </summary>
        public static LocalisableString EditorBeatmapHitsounds => new TranslatableString(getKey(@"editor_beatmap_hitsounds"), @"Beatmap hitsounds");

        /// <summary>
        /// "Off uses your skin's hitsounds instead. Does not change how the beatmap plays."
        /// </summary>
        public static LocalisableString EditorBeatmapHitsoundsDescription => new TranslatableString(getKey(@"editor_beatmap_hitsounds_description"),
            @"Off uses your skin's hitsounds instead. Does not change how the beatmap plays.");

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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
