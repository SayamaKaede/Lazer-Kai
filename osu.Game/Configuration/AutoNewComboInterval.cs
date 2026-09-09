// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Configuration
{
    /// <summary>
    /// How far apart the combos are wanted when they are being started automatically.
    /// </summary>
    /// <remarks>
    /// Stored by name, so these can be reordered or added to without disturbing what anyone has chosen.
    /// </remarks>
    public enum AutoNewComboInterval
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AutoNewComboHalfBar))]
        HalfBar,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AutoNewComboOneBar))]
        OneBar,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AutoNewComboTwoBars))]
        TwoBars,
    }

    public static class AutoNewComboIntervalExtensions
    {
        /// <summary>
        /// How many bars the interval is, for the arithmetic.
        /// </summary>
        public static double Bars(this AutoNewComboInterval interval)
        {
            switch (interval)
            {
                case AutoNewComboInterval.HalfBar:
                    return 0.5;

                case AutoNewComboInterval.TwoBars:
                    return 2;

                default:
                    return 1;
            }
        }
    }
}
