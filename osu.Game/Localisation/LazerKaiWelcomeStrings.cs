// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    /// <summary>
    /// What is shown once the first-run setup is out of the way, pointing somebody new at where to start.
    /// </summary>
    public static class LazerKaiWelcomeStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiWelcome";

        /// <summary>
        /// "Welcome to Lazer-Kai"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Welcome to Lazer-Kai");

        /// <summary>
        /// "Lazer-Kai is osu!lazer made for mapping. "Getting started" lists its main features, and the manual goes through each of them in full. Both can be opened at any time from Lazer-Kai in the settings."
        /// </summary>
        public static LocalisableString Body => new TranslatableString(getKey(@"body"),
            @"Lazer-Kai is osu!lazer made for mapping. ""Getting started"" lists its main features, and the manual goes through each of them in full. Both can be opened at any time from Lazer-Kai in the settings.");

        /// <summary>
        /// "Apply the recommended settings"
        /// </summary>
        public static LocalisableString ApplyRecommendedSettings => new TranslatableString(getKey(@"apply_recommended_settings"), @"Apply the recommended settings");

        /// <summary>
        /// "Close"
        /// </summary>
        public static LocalisableString Close => new TranslatableString(getKey(@"close"), @"Close");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
