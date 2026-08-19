// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class ComboColourStudioStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.ComboColourStudio";

        /// <summary>
        /// "Combo colour studio"
        /// </summary>
        public static LocalisableString ComboColourStudio => new TranslatableString(getKey(@"combo_colour_studio"), @"Combo colour studio");

        /// <summary>
        /// "No colour points yet"
        /// </summary>
        public static LocalisableString NoPointsYet => new TranslatableString(getKey(@"no_points_yet"), @"No colour points yet");

        /// <summary>
        /// "Add a point at the current time"
        /// </summary>
        public static LocalisableString AddPointAtCurrentTime => new TranslatableString(getKey(@"add_point_at_current_time"), @"Add a point at the current time");

        /// <summary>
        /// "Use the current time"
        /// </summary>
        public static LocalisableString UseCurrentTime => new TranslatableString(getKey(@"use_current_time"), @"Use the current time");

        /// <summary>
        /// "Add a colour"
        /// </summary>
        public static LocalisableString AddColour => new TranslatableString(getKey(@"add_colour"), @"Add a colour");

        /// <summary>
        /// "Remove the last colour"
        /// </summary>
        public static LocalisableString RemoveColour => new TranslatableString(getKey(@"remove_colour"), @"Remove the last colour");

        /// <summary>
        /// "Remove this point"
        /// </summary>
        public static LocalisableString RemovePoint => new TranslatableString(getKey(@"remove_point"), @"Remove this point");

        /// <summary>
        /// "Normal"
        /// </summary>
        public static LocalisableString ModeNormal => new TranslatableString(getKey(@"mode_normal"), @"Normal");

        /// <summary>
        /// "Burst"
        /// </summary>
        public static LocalisableString ModeBurst => new TranslatableString(getKey(@"mode_burst"), @"Burst");

        /// <summary>
        /// "Longest combo a burst takes"
        /// </summary>
        public static LocalisableString MaxBurstLength => new TranslatableString(getKey(@"max_burst_length"), @"Longest combo a burst takes");

        /// <summary>
        /// "A burst point passes over combos longer than this."
        /// </summary>
        public static LocalisableString MaxBurstLengthDescription => new TranslatableString(getKey(@"max_burst_length_description"), @"A burst point passes over combos longer than this.");

        /// <summary>
        /// "Copy from another difficulty"
        /// </summary>
        public static LocalisableString CopyFromAnotherDifficulty => new TranslatableString(getKey(@"copy_from_another_difficulty"), @"Copy from another difficulty");

        /// <summary>
        /// "Apply to the beatmap"
        /// </summary>
        public static LocalisableString Apply => new TranslatableString(getKey(@"apply"), @"Apply to the beatmap");

        /// <summary>
        /// "Apply the colour layout?"
        /// </summary>
        public static LocalisableString ApplyConfirmationHeader => new TranslatableString(getKey(@"apply_confirmation_header"), @"Apply the colour layout?");

        /// <summary>
        /// "Every combo colour skip in this difficulty will be replaced, including any set by hand."
        /// </summary>
        public static LocalisableString ApplyConfirmationBody => new TranslatableString(getKey(@"apply_confirmation_body"),
            @"Every combo colour skip in this difficulty will be replaced, including any set by hand.");

        /// <summary>
        /// "Apply"
        /// </summary>
        public static LocalisableString ApplyConfirmationAccept => new TranslatableString(getKey(@"apply_confirmation_accept"), @"Apply");

        /// <summary>
        /// "Cannot apply"
        /// </summary>
        public static LocalisableString CannotApply => new TranslatableString(getKey(@"cannot_apply"), @"Cannot apply");

        /// <summary>
        /// "Based on Combo Colour Studio from Mapping Tools by OliBomby"
        /// </summary>
        public static LocalisableString BasedOnMappingTools => new TranslatableString(getKey(@"based_on_mapping_tools"),
            @"Based on Combo Colour Studio from Mapping Tools by OliBomby");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
