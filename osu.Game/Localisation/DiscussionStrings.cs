// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class DiscussionStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiDiscussion";

        /// <summary>
        /// "Discussion"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Discussion");

        /// <summary>
        /// "Discussion..."
        /// </summary>
        public static LocalisableString MenuItem => new TranslatableString(getKey(@"menu_item"), @"Discussion...");

        /// <summary>
        /// "Reload"
        /// </summary>
        public static LocalisableString Reload => new TranslatableString(getKey(@"reload"), @"Reload");

        /// <summary>
        /// "Timeline"
        /// </summary>
        public static LocalisableString Timeline => new TranslatableString(getKey(@"timeline"), @"Timeline");

        /// <summary>
        /// "General (this difficulty)"
        /// </summary>
        public static LocalisableString ThisDifficulty => new TranslatableString(getKey(@"this_difficulty"), @"General (this difficulty)");

        /// <summary>
        /// "General (all difficulties)"
        /// </summary>
        public static LocalisableString WholeSet => new TranslatableString(getKey(@"whole_set"), @"General (all difficulties)");

        /// <summary>
        /// "This beatmap has not been uploaded."
        /// </summary>
        public static LocalisableString NotUploaded => new TranslatableString(getKey(@"not_uploaded"), @"This beatmap has not been uploaded.");

        /// <summary>
        /// "Log in to read the discussion."
        /// </summary>
        public static LocalisableString NotLoggedIn => new TranslatableString(getKey(@"not_logged_in"), @"Log in to read the discussion.");

        /// <summary>
        /// "Nothing has been posted yet."
        /// </summary>
        public static LocalisableString NothingPosted => new TranslatableString(getKey(@"nothing_posted"), @"Nothing has been posted yet.");

        /// <summary>
        /// "The discussion could not be loaded."
        /// </summary>
        public static LocalisableString LoadFailed => new TranslatableString(getKey(@"load_failed"), @"The discussion could not be loaded.");

        /// <summary>
        /// "Loading..."
        /// </summary>
        public static LocalisableString Loading => new TranslatableString(getKey(@"loading"), @"Loading...");

        /// <summary>
        /// "Show replies"
        /// </summary>
        public static LocalisableString ShowReplies => new TranslatableString(getKey(@"show_replies"), @"Show replies");

        /// <summary>
        /// "Hide replies"
        /// </summary>
        public static LocalisableString HideReplies => new TranslatableString(getKey(@"hide_replies"), @"Hide replies");

        /// <summary>
        /// "No replies"
        /// </summary>
        public static LocalisableString NoReplies => new TranslatableString(getKey(@"no_replies"), @"No replies");

        /// <summary>
        /// "Mine"
        /// </summary>
        public static LocalisableString FilterMine => new TranslatableString(getKey(@"filter_mine"), @"Mine");

        /// <summary>
        /// "Notes"
        /// </summary>
        public static LocalisableString FilterNotes => new TranslatableString(getKey(@"filter_notes"), @"Notes");

        /// <summary>
        /// "Resolved"
        /// </summary>
        public static LocalisableString FilterResolved => new TranslatableString(getKey(@"filter_resolved"), @"Resolved");

        /// <summary>
        /// "Pending"
        /// </summary>
        public static LocalisableString FilterPending => new TranslatableString(getKey(@"filter_pending"), @"Pending");

        /// <summary>
        /// "Praises"
        /// </summary>
        public static LocalisableString FilterPraises => new TranslatableString(getKey(@"filter_praises"), @"Praises");

        /// <summary>
        /// "All"
        /// </summary>
        public static LocalisableString FilterAll => new TranslatableString(getKey(@"filter_all"), @"All");

        /// <summary>
        /// "Everyone"
        /// </summary>
        public static LocalisableString EveryUser => new TranslatableString(getKey(@"every_user"), @"Everyone");

        /// <summary>
        /// "By time"
        /// </summary>
        public static LocalisableString SortDefault => new TranslatableString(getKey(@"sort_default"), @"By time");

        /// <summary>
        /// "Last updated"
        /// </summary>
        public static LocalisableString SortUpdated => new TranslatableString(getKey(@"sort_updated"), @"Last updated");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString Opacity => new TranslatableString(getKey(@"opacity"), @"Opacity");

        /// <summary>
        /// "Copy text"
        /// </summary>
        public static LocalisableString Copy => new TranslatableString(getKey(@"copy"), @"Copy text");

        /// <summary>
        /// "Copied"
        /// </summary>
        public static LocalisableString Copied => new TranslatableString(getKey(@"copied"), @"Copied");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
