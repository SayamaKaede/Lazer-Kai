// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// What this fork adds to song select.
    /// </summary>
    public static class LazerKaiSongSelectStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiSongSelect";

        /// <summary>
        /// "Check for an update"
        /// </summary>
        public static LocalisableString CheckForUpdate => new TranslatableString(getKey(@"check_for_update"), @"Check for an update");

        /// <summary>
        /// "Checking "{0}" for an update..."
        /// </summary>
        public static LocalisableString Checking(string beatmapSet) => new TranslatableString(getKey(@"checking"), @"Checking ""{0}"" for an update...", beatmapSet);

        /// <summary>
        /// ""{0}" is up to date."
        /// </summary>
        public static LocalisableString UpToDate(string beatmapSet) => new TranslatableString(getKey(@"up_to_date"), @"""{0}"" is up to date.", beatmapSet);

        /// <summary>
        /// "An update is available for "{0}"."
        /// </summary>
        public static LocalisableString UpdateAvailable(string beatmapSet) => new TranslatableString(getKey(@"update_available"), @"An update is available for ""{0}"".", beatmapSet);

        /// <summary>
        /// "Could not check for an update. Are you online?"
        /// </summary>
        public static LocalisableString CheckFailed => new TranslatableString(getKey(@"check_failed"), @"Could not check for an update. Are you online?");

        /// <summary>
        /// "Delete this difficulty..."
        /// </summary>
        public static LocalisableString DeleteDifficulty => new TranslatableString(getKey(@"delete_difficulty"), @"Delete this difficulty...");

        /// <summary>
        /// "Search presets"
        /// </summary>
        public static LocalisableString SearchPresets => new TranslatableString(getKey(@"search_presets"), @"Search presets");

        /// <summary>
        /// "No presets saved yet."
        /// </summary>
        public static LocalisableString NoSearchPresets => new TranslatableString(getKey(@"no_search_presets"), @"No presets saved yet.");

        /// <summary>
        /// "Name (the search itself if left empty)"
        /// </summary>
        public static LocalisableString SearchPresetName => new TranslatableString(getKey(@"search_preset_name"), @"Name (the search itself if left empty)");

        /// <summary>
        /// "Save the current search"
        /// </summary>
        public static LocalisableString SaveSearchPreset => new TranslatableString(getKey(@"save_search_preset"), @"Save the current search");

        /// <summary>
        /// "User tags"
        /// </summary>
        public static LocalisableString UserTags => new TranslatableString(getKey(@"user_tags"), @"User tags");

        /// <summary>
        /// "Each number is how many of the difficulties shown now have that tag."
        /// </summary>
        public static LocalisableString UserTagCounts => new TranslatableString(getKey(@"user_tag_counts"), @"Each number is how many of the difficulties shown now have that tag.");

        /// <summary>
        /// "None of the difficulties shown now have user tags."
        /// </summary>
        public static LocalisableString NoUserTags => new TranslatableString(getKey(@"no_user_tags"), @"None of the difficulties shown now have user tags.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
