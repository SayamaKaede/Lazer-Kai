// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class PlacementPresetStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.PlacementPreset";

        /// <summary>
        /// "Placement presets"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Placement presets");

        /// <summary>
        /// "Placement presets..."
        /// </summary>
        public static LocalisableString MenuItem => new TranslatableString(getKey(@"menu_item"), @"Placement presets...");

        /// <summary>
        /// "Every beatmap"
        /// </summary>
        public static LocalisableString EveryBeatmap => new TranslatableString(getKey(@"every_beatmap"), @"Every beatmap");

        /// <summary>
        /// "This beatmap"
        /// </summary>
        public static LocalisableString ThisBeatmap => new TranslatableString(getKey(@"this_beatmap"), @"This beatmap");

        /// <summary>
        /// "Keep the selection"
        /// </summary>
        public static LocalisableString KeepTheSelection => new TranslatableString(getKey(@"keep_the_selection"), @"Keep the selection");

        /// <summary>
        /// "Select something to keep it here."
        /// </summary>
        public static LocalisableString NothingKeptYet => new TranslatableString(getKey(@"nothing_kept_yet"), @"Select something to keep it here.");

        /// <summary>
        /// "Press a digit to give this one a key, or escape to take away the one it has."
        /// </summary>
        public static LocalisableString PressADigit => new TranslatableString(getKey(@"press_a_digit"),
            @"Press a digit to give this one a key, or escape to take away the one it has.");

        /// <summary>
        /// "Assign a key"
        /// </summary>
        public static LocalisableString AssignAKey => new TranslatableString(getKey(@"assign_a_key"), @"Assign a key");

        /// <summary>
        /// "Remove"
        /// </summary>
        public static LocalisableString Remove => new TranslatableString(getKey(@"remove"), @"Remove");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
