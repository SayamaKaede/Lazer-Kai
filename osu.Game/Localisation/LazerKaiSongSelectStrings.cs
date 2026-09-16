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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
