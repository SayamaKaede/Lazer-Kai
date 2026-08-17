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
        /// "Editor display"
        /// </summary>
        public static LocalisableString DisplayHeader => new TranslatableString(getKey(@"display_header"), @"Editor display");

        /// <summary>
        /// "Selection"
        /// </summary>
        public static LocalisableString SelectionHeader => new TranslatableString(getKey(@"selection_header"), @"Selection");

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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
