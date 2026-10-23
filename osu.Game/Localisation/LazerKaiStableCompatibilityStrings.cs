// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiStableCompatibilityStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiStableCompatibility";

        /// <summary>
        /// "Improve compatibility with stable"
        /// </summary>
        public static LocalisableString ImproveCompatibility => new TranslatableString(getKey(@"improve_compatibility"), @"Improve compatibility with stable");

        /// <summary>
        /// "Beatmaps in stable's format (v14 and earlier) are saved with every line that was not edited kept as it was, and exported for stable as they are."
        /// </summary>
        public static LocalisableString ImproveCompatibilityDescription => new TranslatableString(getKey(@"improve_compatibility_description"),
            @"Beatmaps in stable's format (v14 and earlier) are saved with every line that was not edited kept as it was, and exported for stable as they are.");

        /// <summary>
        /// "About improving compatibility with stable"
        /// </summary>
        public static LocalisableString NoticeHeader => new TranslatableString(getKey(@"notice_header"), @"About improving compatibility with stable");

        /// <summary>
        /// "- Only osu! (standard) beatmaps are supported for now."
        /// </summary>
        public static LocalisableString NoticeSupportedModes => new TranslatableString(getKey(@"notice_supported_modes"), @"- Only osu! (standard) beatmaps are supported for now.");

        /// <summary>
        /// "- While this is on, a backup is taken whenever a beatmap in stable's format is opened. Keeping more backups is recommended, in the Backups options under Other (for example, every 15 minutes, 100 kept)."
        /// </summary>
        public static LocalisableString NoticeBackups => new TranslatableString(getKey(@"notice_backups"),
            @"- While this is on, a backup is taken whenever a beatmap in stable's format is opened. Keeping more backups is recommended, in the Backups options under Other (for example, every 15 minutes, 100 kept).");

        /// <summary>
        /// "- If a save cannot stay in stable's format, a warning is shown. The error is reported automatically; please wait for a fix."
        /// </summary>
        public static LocalisableString NoticeWarning => new TranslatableString(getKey(@"notice_warning"),
            @"- If a save cannot stay in stable's format, a warning is shown. The error is reported automatically; please wait for a fix.");

        /// <summary>
        /// "OK"
        /// </summary>
        public static LocalisableString NoticeOk => new TranslatableString(getKey(@"notice_ok"), @"OK");

        /// <summary>
        /// "This edit cannot be kept in stable's format yet, so the beatmap was saved in lazer's."
        /// </summary>
        public static LocalisableString SavedInLazerFormat => new TranslatableString(getKey(@"saved_in_lazer_format"),
            @"This edit cannot be kept in stable's format yet, so the beatmap was saved in lazer's.");

        /// <summary>
        /// "The dropped file "{0}" could not be kept in stable's format, so it was saved in lazer's."
        /// </summary>
        public static LocalisableString DroppedFileSavedInLazerFormat(string name) => new TranslatableString(getKey(@"dropped_file_saved_in_lazer_format"),
            @"The dropped file ""{0}"" could not be kept in stable's format, so it was saved in lazer's.", name);

        /// <summary>
        /// "{0} could not be kept in stable's format, so it was left as it was."
        /// </summary>
        public static LocalisableString OtherDifficultiesLeftAsTheyWere(string names) => new TranslatableString(getKey(@"other_difficulties_left_as_they_were"),
            @"{0} could not be kept in stable's format, so it was left as it was.", names);

        /// <summary>
        /// "Convert"
        /// </summary>
        public static LocalisableString Convert => new TranslatableString(getKey(@"convert"), @"Convert");

        /// <summary>
        /// "Convert to v14 format"
        /// </summary>
        public static LocalisableString ConvertToStableFormat => new TranslatableString(getKey(@"convert_to_stable_format"), @"Convert to v14 format");

        /// <summary>
        /// "Convert this difficulty to v14 format?"
        /// </summary>
        public static LocalisableString ConvertToStableFormatHeader => new TranslatableString(getKey(@"convert_to_stable_format_header"), @"Convert this difficulty to v14 format?");

        /// <summary>
        /// "It is saved, unsaved changes included, the way exporting for stable writes it, and kept in stable's format from then on: times and positions become whole numbers, and sliders of several segments become Bézier curves. It cannot be undone."
        /// </summary>
        public static LocalisableString ConvertToStableFormatBody => new TranslatableString(getKey(@"convert_to_stable_format_body"),
            @"It is saved, unsaved changes included, the way exporting for stable writes it, and kept in stable's format from then on: times and positions become whole numbers, and sliders of several segments become Bézier curves. It cannot be undone.");

        /// <summary>
        /// "This edit cannot be saved in stable's format yet"
        /// </summary>
        public static LocalisableString CannotStayStableHeader => new TranslatableString(getKey(@"cannot_stay_stable_header"), @"This edit cannot be saved in stable's format yet");

        /// <summary>
        /// "Saved, the difficulty goes to lazer's format and stays there. The file as it is now is kept in the backups' folder first. What stopped it is written in the logs."
        /// </summary>
        public static LocalisableString CannotStayStableBody => new TranslatableString(getKey(@"cannot_stay_stable_body"),
            @"Saved, the difficulty goes to lazer's format and stays there. The file as it is now is kept in the backups' folder first. What stopped it is written in the logs.");

        /// <summary>
        /// "Save in lazer's format"
        /// </summary>
        public static LocalisableString SaveInLazerFormat => new TranslatableString(getKey(@"save_in_lazer_format"), @"Save in lazer's format");

        /// <summary>
        /// "Don't save"
        /// </summary>
        public static LocalisableString DoNotSave => new TranslatableString(getKey(@"do_not_save"), @"Don't save");

        /// <summary>
        /// "Turn on "Improve compatibility with stable"?"
        /// </summary>
        public static LocalisableString TurnOnCompatibilityHeader => new TranslatableString(getKey(@"turn_on_compatibility_header"),
            @"Turn on ""Improve compatibility with stable""?");

        /// <summary>
        /// "It applies to every beatmap. This difficulty is in stable's format already, and is opened again to be kept in it."
        /// </summary>
        public static LocalisableString TurnOnCompatibilityBody => new TranslatableString(getKey(@"turn_on_compatibility_body"),
            @"It applies to every beatmap. This difficulty is in stable's format already, and is opened again to be kept in it.");

        /// <summary>
        /// "Turn on "Improve compatibility with stable" and convert this difficulty to v14 format?"
        /// </summary>
        public static LocalisableString TurnOnAndConvertHeader => new TranslatableString(getKey(@"turn_on_and_convert_header"),
            @"Turn on ""Improve compatibility with stable"" and convert this difficulty to v14 format?");

        /// <summary>
        /// "The option applies to every beatmap. {0}"
        /// </summary>
        public static LocalisableString TurnOnAndConvertBody(LocalisableString conversion) => new TranslatableString(getKey(@"turn_on_and_convert_body"),
            @"The option applies to every beatmap. {0}", conversion);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
