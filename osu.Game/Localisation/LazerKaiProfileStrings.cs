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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
