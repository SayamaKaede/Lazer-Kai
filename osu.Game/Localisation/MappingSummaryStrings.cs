// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class MappingSummaryStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.MappingSummary";

        /// <summary>
        /// "Density"
        /// </summary>
        public static LocalisableString Density => new TranslatableString(getKey(@"density"), @"Density");

        /// <summary>
        /// "{0}/s"
        /// </summary>
        public static LocalisableString DensityValue(double perSecond) => new TranslatableString(getKey(@"density_value"), @"{0}/s", perSecond.ToLocalisableString(@"0.00"));

        /// <summary>
        /// "Snap"
        /// </summary>
        public static LocalisableString Snap => new TranslatableString(getKey(@"snap"), @"Snap");

        /// <summary>
        /// "Unsnapped"
        /// </summary>
        public static LocalisableString Unsnapped => new TranslatableString(getKey(@"unsnapped"), @"Unsnapped");

        /// <summary>
        /// "None"
        /// </summary>
        public static LocalisableString NoneOfThem => new TranslatableString(getKey(@"none_of_them"), @"None");

        /// <summary>
        /// "BPM changes"
        /// </summary>
        public static LocalisableString BpmChanges => new TranslatableString(getKey(@"bpm_changes"), @"BPM changes");

        /// <summary>
        /// "{0} times"
        /// </summary>
        public static LocalisableString TimesOver(int count) => new TranslatableString(getKey(@"times_over"), @"{0} times", count.ToLocalisableString(@"N0"));

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString Volume => new TranslatableString(getKey(@"volume"), @"Volume");

        /// <summary>
        /// "SV"
        /// </summary>
        public static LocalisableString SliderVelocity => new TranslatableString(getKey(@"slider_velocity"), @"SV");

        /// <summary>
        /// "Kiai"
        /// </summary>
        public static LocalisableString Kiai => new TranslatableString(getKey(@"kiai"), @"Kiai");

        /// <summary>
        /// "Breaks"
        /// </summary>
        public static LocalisableString Breaks => new TranslatableString(getKey(@"breaks"), @"Breaks");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
