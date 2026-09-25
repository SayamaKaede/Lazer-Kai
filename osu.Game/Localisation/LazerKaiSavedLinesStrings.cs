// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiSavedLinesStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiSavedLines";

        /// <summary>
        /// "Show stable's green lines"
        /// </summary>
        public static LocalisableString ShowStableGreenLines => new TranslatableString(getKey(@"show_stable_green_lines"), @"Show stable's green lines");

        /// <summary>
        /// "Only applies while the top timeline is in the Stable or Lazer-Kai style."
        /// </summary>
        public static LocalisableString ShowStableGreenLinesDescription => new TranslatableString(getKey(@"show_stable_green_lines_description"),
            @"Only applies while the top timeline is in the Stable or Lazer-Kai style.");

        /// <summary>
        /// "Red line {0} · {1} BPM · {2}/4 · {3} #{4} · {5}%"
        /// </summary>
        public static LocalisableString RedLine(string time, string bpm, int meter, string sampleSet, int index, int volume) =>
            new TranslatableString(getKey(@"red_line"), @"Red line {0} · {1} BPM · {2}/4 · {3} #{4} · {5}%", time, bpm, meter, sampleSet, index, volume);

        /// <summary>
        /// "Green line {0} · SV x{1} · {2} #{3} · {4}%"
        /// </summary>
        public static LocalisableString GreenLine(string time, string sliderVelocity, string sampleSet, int index, int volume) =>
            new TranslatableString(getKey(@"green_line"), @"Green line {0} · SV x{1} · {2} #{3} · {4}%", time, sliderVelocity, sampleSet, index, volume);

        /// <summary>
        /// " · Kiai"
        /// </summary>
        public static LocalisableString Kiai => new TranslatableString(getKey(@"kiai"), @" · Kiai");

        /// <summary>
        /// " (put in on saving)"
        /// </summary>
        public static LocalisableString PutInOnSave => new TranslatableString(getKey(@"put_in_on_save"), @" (put in on saving)");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
