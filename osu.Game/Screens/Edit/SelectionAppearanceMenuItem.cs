// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Menu controlling how selection outlines look in the editor.
    /// </summary>
    internal class SelectionAppearanceMenuItem : MenuItem
    {
        /// <summary>
        /// Offered for both the selection handles and slider anchors, which scale their stock sizes by these factors.
        /// </summary>
        private static readonly (LocalisableString, float)[] scale_choices =
        {
            ((LocalisableString)@"50%", 0.5f),
            ((LocalisableString)@"100%", 1f),
            ((LocalisableString)@"150%", 1.5f),
            ((LocalisableString)@"200%", 2f),
            ((LocalisableString)@"300%", 3f),
        };

        private static readonly (LocalisableString, string)[] colour_choices =
        {
            (CommonStrings.Default, EditorSelectionAppearance.DEFAULT_COLOUR),
            ((LocalisableString)@"Red", @"#FF5555"),
            ((LocalisableString)@"Orange", @"#FF9C22"),
            ((LocalisableString)@"Green", @"#88FF55"),
            ((LocalisableString)@"Cyan", @"#55DDFF"),
            ((LocalisableString)@"Blue", @"#5599FF"),
            ((LocalisableString)@"Magenta", @"#FF55CC"),
            ((LocalisableString)@"White", @"#FFFFFF"),
        };

        public SelectionAppearanceMenuItem(Bindable<string> colour, Bindable<float> thickness, Bindable<float> opacity,
                                           Bindable<float> handleScale, Bindable<string> handleColour, Bindable<float> anchorScale)
            : base(EditorStrings.SelectionAppearance)
        {
            Items = new MenuItem[]
            {
                new ChoiceMenuItem<string>(EditorStrings.SelectionColour, colour, colour_choices),
                new ChoiceMenuItem<float>(EditorStrings.SelectionThickness, thickness, new[]
                {
                    ((LocalisableString)@"50%", 0.5f),
                    ((LocalisableString)@"75%", 0.75f),
                    ((LocalisableString)@"100%", 1f),
                    ((LocalisableString)@"150%", 1.5f),
                    ((LocalisableString)@"200%", 2f),
                }),
                new ChoiceMenuItem<float>(EditorStrings.SelectionOpacity, opacity, new[]
                {
                    ((LocalisableString)@"25%", 0.25f),
                    ((LocalisableString)@"50%", 0.5f),
                    ((LocalisableString)@"75%", 0.75f),
                    ((LocalisableString)@"100%", 1f),
                }),
                new ChoiceMenuItem<float>(EditorStrings.SelectionHandleSize, handleScale, scale_choices),
                new ChoiceMenuItem<string>(EditorStrings.SelectionHandleColour, handleColour, colour_choices),
                new ChoiceMenuItem<float>(EditorStrings.SliderAnchorSize, anchorScale, scale_choices),
            };
        }

    }
}
