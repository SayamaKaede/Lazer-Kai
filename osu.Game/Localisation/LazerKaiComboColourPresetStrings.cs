// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiComboColourPresetStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiComboColourPreset";

        /// <summary>
        /// "Presets"
        /// </summary>
        public static LocalisableString Presets => new TranslatableString(getKey(@"presets"), @"Presets");

        /// <summary>
        /// "Choose a preset"
        /// </summary>
        public static LocalisableString ChooseAPreset => new TranslatableString(getKey(@"choose_a_preset"), @"Choose a preset");

        /// <summary>
        /// "Save as a preset"
        /// </summary>
        public static LocalisableString SaveAsAPreset => new TranslatableString(getKey(@"save_as_a_preset"), @"Save as a preset");

        /// <summary>
        /// "No presets saved yet."
        /// </summary>
        public static LocalisableString NoPresetsYet => new TranslatableString(getKey(@"no_presets_yet"), @"No presets saved yet.");

        /// <summary>
        /// "Preset name"
        /// </summary>
        public static LocalisableString PresetName => new TranslatableString(getKey(@"preset_name"), @"Preset name");

        /// <summary>
        /// "Save"
        /// </summary>
        public static LocalisableString Save => new TranslatableString(getKey(@"save"), @"Save");

        /// <summary>
        /// "Overwrite"
        /// </summary>
        public static LocalisableString Overwrite => new TranslatableString(getKey(@"overwrite"), @"Overwrite");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
