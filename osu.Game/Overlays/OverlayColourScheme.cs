// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Overlays
{
    public enum OverlayColourScheme
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeRed))]
        Red,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeOrange))]
        Orange,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeLime))]
        Lime,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeGreen))]
        Green,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeAquamarine))]
        Aquamarine,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemeBlue))]
        Blue,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemePurple))]
        Purple,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemePlum))]
        Plum,
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourSchemePink))]
        Pink,
    }

    public static class OverlayColourSchemeExtensions
    {
        public static int GetHue(this OverlayColourScheme colourScheme)
        {
            // See https://github.com/ppy/osu-web/blob/5a536d217a21582aad999db50a981003d3ad5659/app/helpers.php#L1620-L1628
            switch (colourScheme)
            {
                default:
                    throw new ArgumentOutOfRangeException(nameof(colourScheme));

                case OverlayColourScheme.Red:
                    return 0;

                case OverlayColourScheme.Orange:
                    return 45;

                case OverlayColourScheme.Lime:
                    return 90;

                case OverlayColourScheme.Green:
                    return 125;

                case OverlayColourScheme.Aquamarine:
                    return 160;

                case OverlayColourScheme.Blue:
                    return 200;

                case OverlayColourScheme.Purple:
                    return 255;

                case OverlayColourScheme.Plum:
                    return 320;

                case OverlayColourScheme.Pink:
                    return 333;
            }
        }
    }
}
