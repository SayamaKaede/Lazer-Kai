// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// Headings for the profile listings this fork adds.
    /// </summary>
    /// <remarks>
    /// Worded to sit beside the game's own beatmap listings on a profile rather than to stand out from them, so
    /// they read as more of the same list — which is what they are.
    /// </remarks>
    public static class LazerKaiProfileStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiProfile";

        /// <summary>
        /// "Pending Guest Participation Beatmaps"
        /// </summary>
        public static LocalisableString GuestPendingTitle => new TranslatableString(getKey(@"guest_pending_title"), @"Pending Guest Participation Beatmaps");

        /// <summary>
        /// "Graveyarded Guest Participation Beatmaps"
        /// </summary>
        public static LocalisableString GuestGraveyardTitle => new TranslatableString(getKey(@"guest_graveyard_title"), @"Graveyarded Guest Participation Beatmaps");

        /// <summary>
        /// "Download all"
        /// </summary>
        public static LocalisableString DownloadAll => new TranslatableString(getKey(@"download_all"), @"Download all");

        /// <summary>
        /// "Hold to download every beatmap in this listing that you do not have yet."
        /// </summary>
        public static LocalisableString DownloadAllTooltip => new TranslatableString(getKey(@"download_all_tooltip"),
            @"Hold to download every beatmap in this listing that you do not have yet.");

        /// <summary>
        /// "Log in to download."
        /// </summary>
        public static LocalisableString DownloadAllNeedsLogin => new TranslatableString(getKey(@"download_all_needs_login"), @"Log in to download.");

        /// <summary>
        /// "Reading the listing..."
        /// </summary>
        public static LocalisableString DownloadAllReading => new TranslatableString(getKey(@"download_all_reading"), @"Reading the listing...");

        /// <summary>
        /// "The listing could not be read."
        /// </summary>
        public static LocalisableString DownloadAllCouldNotRead => new TranslatableString(getKey(@"download_all_could_not_read"), @"The listing could not be read.");

        /// <summary>
        /// "Every beatmap in this listing is downloaded already."
        /// </summary>
        public static LocalisableString DownloadAllNothingLeft => new TranslatableString(getKey(@"download_all_nothing_left"),
            @"Every beatmap in this listing is downloaded already.");

        /// <summary>
        /// "Downloading {0}/{1}"
        /// </summary>
        public static LocalisableString DownloadAllProgress(int done, int total) => new TranslatableString(getKey(@"download_all_progress"), @"Downloading {0}/{1}", done, total);

        /// <summary>
        /// "{0} downloaded already"
        /// </summary>
        public static LocalisableString DownloadAllAlreadyHave(int count) => new TranslatableString(getKey(@"download_all_already_have"), @"{0} downloaded already", count);

        /// <summary>
        /// "{0} taken down"
        /// </summary>
        public static LocalisableString DownloadAllUnavailable(int count) => new TranslatableString(getKey(@"download_all_unavailable"), @"{0} taken down", count);

        /// <summary>
        /// "Downloaded {0} beatmaps."
        /// </summary>
        public static LocalisableString DownloadAllDone(int count) => new TranslatableString(getKey(@"download_all_done"), @"Downloaded {0} beatmaps.", count);

        /// <summary>
        /// "{0} failed"
        /// </summary>
        public static LocalisableString DownloadAllFailed(int count) => new TranslatableString(getKey(@"download_all_failed"), @"{0} failed", count);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
