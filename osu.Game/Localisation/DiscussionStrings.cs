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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
