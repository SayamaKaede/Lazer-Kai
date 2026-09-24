// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiStableCompatibilityStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiStableCompatibility";

        /// <summary>
        /// "Improve compatibility with stable (experimental)"
        /// </summary>
        public static LocalisableString ImproveCompatibility => new TranslatableString(getKey(@"improve_compatibility"), @"Improve compatibility with stable (experimental)");

        /// <summary>
        /// "Beatmaps in stable's format (v14 and earlier) are saved with every line that was not edited kept as it was, and exported for stable as they are."
        /// </summary>
        public static LocalisableString ImproveCompatibilityDescription => new TranslatableString(getKey(@"improve_compatibility_description"),
            @"Beatmaps in stable's format (v14 and earlier) are saved with every line that was not edited kept as it was, and exported for stable as they are.");

        /// <summary>
        /// "This edit cannot be kept in stable's format yet, so the beatmap was saved in lazer's."
        /// </summary>
        public static LocalisableString SavedInLazerFormat => new TranslatableString(getKey(@"saved_in_lazer_format"),
            @"This edit cannot be kept in stable's format yet, so the beatmap was saved in lazer's.");

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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
