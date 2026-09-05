// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class BeatmapExportStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.BeatmapExport";

        /// <summary>
        /// "Export format"
        /// </summary>
        public static LocalisableString Format => new TranslatableString(getKey(@"format"), @"Export format");

        /// <summary>
        /// "For stable (v14 / .osz)"
        /// </summary>
        public static LocalisableString FormatStable => new TranslatableString(getKey(@"format_stable"), @"For stable (v14 / .osz)");

        /// <summary>
        /// "For lazer (as stored / .olz)"
        /// </summary>
        public static LocalisableString FormatLazer => new TranslatableString(getKey(@"format_lazer"), @"For lazer (as stored / .olz)");

        /// <summary>
        /// "Export this difficulty"
        /// </summary>
        public static LocalisableString ExportDifficulty => new TranslatableString(getKey(@"export_difficulty"), @"Export this difficulty");

        /// <summary>
        /// "Export this set"
        /// </summary>
        public static LocalisableString ExportSet => new TranslatableString(getKey(@"export_set"), @"Export this set");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
