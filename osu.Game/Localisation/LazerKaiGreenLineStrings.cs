// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiGreenLineStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiGreenLine";

        /// <summary>
        /// "Green lines"
        /// </summary>
        public static LocalisableString Title => new TranslatableString(getKey(@"title"), @"Green lines");

        /// <summary>
        /// "Green line"
        /// </summary>
        public static LocalisableString GreenLine => new TranslatableString(getKey(@"green_line"), @"Green line");

        /// <summary>
        /// "Red line"
        /// </summary>
        public static LocalisableString RedLine => new TranslatableString(getKey(@"red_line"), @"Red line");

        /// <summary>
        /// "Add"
        /// </summary>
        public static LocalisableString Add => new TranslatableString(getKey(@"add"), @"Add");

        /// <summary>
        /// "Remove"
        /// </summary>
        public static LocalisableString Remove => new TranslatableString(getKey(@"remove"), @"Remove");

        /// <summary>
        /// "Cancel"
        /// </summary>
        public static LocalisableString Cancel => new TranslatableString(getKey(@"cancel"), @"Cancel");

        /// <summary>
        /// "Confirm"
        /// </summary>
        public static LocalisableString Confirm => new TranslatableString(getKey(@"confirm"), @"Confirm");

        /// <summary>
        /// "Use current time"
        /// </summary>
        public static LocalisableString UseCurrentTime => new TranslatableString(getKey(@"use_current_time"), @"Use current time");

        /// <summary>
        /// "Time"
        /// </summary>
        public static LocalisableString Time => new TranslatableString(getKey(@"time"), @"Time");

        /// <summary>
        /// "Slider velocity"
        /// </summary>
        public static LocalisableString SliderVelocity => new TranslatableString(getKey(@"slider_velocity"), @"Slider velocity");

        /// <summary>
        /// "Sample set"
        /// </summary>
        public static LocalisableString SampleSet => new TranslatableString(getKey(@"sample_set"), @"Sample set");

        /// <summary>
        /// "Custom index"
        /// </summary>
        public static LocalisableString CustomIndex => new TranslatableString(getKey(@"custom_index"), @"Custom index");

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString Volume => new TranslatableString(getKey(@"volume"), @"Volume");

        /// <summary>
        /// "Green lines can be edited on osu! beatmaps in stable's format (v14 and earlier), with "Improve compatibility with stable" on."
        /// </summary>
        public static LocalisableString NotEditable => new TranslatableString(getKey(@"not_editable"),
            @"Green lines can be edited on osu! beatmaps in stable's format (v14 and earlier), with ""Improve compatibility with stable"" on.");

        /// <summary>
        /// "Sound the green lines inside sliders and spinners in the editor"
        /// </summary>
        public static LocalisableString SoundLinesInsideObjects => new TranslatableString(getKey(@"sound_lines_inside_objects"),
            @"Sound the green lines inside sliders and spinners in the editor");

        /// <summary>
        /// "Slider ticks and slides, and spinner spins, play at the volume of the green line in effect as they sound, as in stable."
        /// </summary>
        public static LocalisableString SoundLinesInsideObjectsDescription => new TranslatableString(getKey(@"sound_lines_inside_objects_description"),
            @"Slider ticks and slides, and spinner spins, play at the volume of the green line in effect as they sound, as in stable.");

        /// <summary>
        /// "Also in play and test play"
        /// </summary>
        public static LocalisableString SoundLinesInsideObjectsInGameplay => new TranslatableString(getKey(@"sound_lines_inside_objects_in_gameplay"), @"Also in play and test play");

        /// <summary>
        /// "Set apart from the editor's. Gameplay then sounds different from how lazer plays it."
        /// </summary>
        public static LocalisableString SoundLinesInsideObjectsInGameplayDescription => new TranslatableString(getKey(@"sound_lines_inside_objects_in_gameplay_description"),
            @"Set apart from the editor's. Gameplay then sounds different from how lazer plays it.");

        /// <summary>
        /// "Remove unneeded green lines"
        /// </summary>
        public static LocalisableString RemoveUnneeded => new TranslatableString(getKey(@"remove_unneeded"), @"Remove unneeded green lines");

        /// <summary>
        /// "Remove the unneeded green lines?"
        /// </summary>
        public static LocalisableString RemoveUnneededHeader => new TranslatableString(getKey(@"remove_unneeded_header"), @"Remove the unneeded green lines?");

        /// <summary>
        /// "Green lines that change nothing are removed: ones saying what the line before them does, and ones another line at the same time replaces. It can be undone."
        /// </summary>
        public static LocalisableString RemoveUnneededBody => new TranslatableString(getKey(@"remove_unneeded_body"),
            @"Green lines that change nothing are removed: ones saying what the line before them does, and ones another line at the same time replaces. It can be undone.");

        /// <summary>
        /// "Removed {0} unneeded green lines."
        /// </summary>
        public static LocalisableString Removed(int count) => new TranslatableString(getKey(@"removed"), @"Removed {0} unneeded green lines.", count);

        /// <summary>
        /// "There were no unneeded green lines."
        /// </summary>
        public static LocalisableString NoneRemoved => new TranslatableString(getKey(@"none_removed"), @"There were no unneeded green lines.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
