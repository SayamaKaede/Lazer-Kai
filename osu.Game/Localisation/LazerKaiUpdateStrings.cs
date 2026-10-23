// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiUpdateStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiUpdate";

        /// <summary>
        /// "Lazer-Kai {0} is out. Click to open the download page."
        /// </summary>
        public static LocalisableString ReleaseAvailable(string version) => new TranslatableString(getKey(@"release_available"),
            @"Lazer-Kai {0} is out. Click to open the download page.", version);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
