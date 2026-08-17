// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Colours offered for the outlines and handles drawn around a selection.
    /// </summary>
    /// <remarks>
    /// Named rather than stored as hex, so the choice reads the same in a menu, in the settings, and in the config
    /// file, and so a malformed value is not something that has to be handled at all.
    /// </remarks>
    public enum EditorSelectionColour
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourDefault))]
        Default,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourRed))]
        Red,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourOrange))]
        Orange,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourGreen))]
        Green,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourCyan))]
        Cyan,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourBlue))]
        Blue,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourMagenta))]
        Magenta,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.ColourWhite))]
        White,
    }

    public static class EditorSelectionColourExtensions
    {
        /// <summary>
        /// The colour to draw with, or null to leave the piece at whatever it uses by default.
        /// </summary>
        public static Color4? ToColour(this EditorSelectionColour colour)
        {
            switch (colour)
            {
                case EditorSelectionColour.Red:
                    return Color4Extensions.FromHex(@"#FF5555");

                case EditorSelectionColour.Orange:
                    return Color4Extensions.FromHex(@"#FF9C22");

                case EditorSelectionColour.Green:
                    return Color4Extensions.FromHex(@"#88FF55");

                case EditorSelectionColour.Cyan:
                    return Color4Extensions.FromHex(@"#55DDFF");

                case EditorSelectionColour.Blue:
                    return Color4Extensions.FromHex(@"#5599FF");

                case EditorSelectionColour.Magenta:
                    return Color4Extensions.FromHex(@"#FF55CC");

                case EditorSelectionColour.White:
                    return Color4.White;

                default:
                    return null;
            }
        }
    }
}
