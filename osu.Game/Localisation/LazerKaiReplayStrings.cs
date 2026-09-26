// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiReplayStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiReplay";

        /// <summary>
        /// "Hide Hidden's effect"
        /// </summary>
        public static LocalisableString HideHiddenEffect => new TranslatableString(getKey(@"hide_hidden_effect"), @"Hide Hidden's effect");

        /// <summary>
        /// "Hide Flashlight's effect"
        /// </summary>
        public static LocalisableString HideFlashlightEffect => new TranslatableString(getKey(@"hide_flashlight_effect"), @"Hide Flashlight's effect");

        /// <summary>
        /// "Misses"
        /// </summary>
        public static LocalisableString Misses => new TranslatableString(getKey(@"misses"), @"Misses");

        /// <summary>
        /// "Judging the replay..."
        /// </summary>
        public static LocalisableString Judging => new TranslatableString(getKey(@"judging"), @"Judging the replay...");

        /// <summary>
        /// "No misses, slider breaks, 100s or 50s."
        /// </summary>
        public static LocalisableString NoMisses => new TranslatableString(getKey(@"no_misses"), @"No misses, slider breaks, 100s or 50s.");

        /// <summary>
        /// "Miss"
        /// </summary>
        public static LocalisableString Miss => new TranslatableString(getKey(@"miss"), @"Miss");

        /// <summary>
        /// "Slider break"
        /// </summary>
        public static LocalisableString SliderBreak => new TranslatableString(getKey(@"slider_break"), @"Slider break");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
