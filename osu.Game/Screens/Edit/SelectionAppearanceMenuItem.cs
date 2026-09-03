// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Localisation;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit
{
    internal class SelectionAppearanceMenuItem : MenuItem
    {
        /// <summary>
        /// Offered for both the selection handles and slider anchors, which scale their stock sizes by these factors.
        /// </summary>
        private static readonly (LocalisableString, float)[] scale_choices =
        {
            ((LocalisableString)@"50%", 0.5f),
            ((LocalisableString)@"60%", 0.6f),
            ((LocalisableString)@"70%", 0.7f),
            ((LocalisableString)@"80%", 0.8f),
            ((LocalisableString)@"90%", 0.9f),
            ((LocalisableString)@"100%", 1f),
        };

        private static (LocalisableString, EditorSelectionColour)[] colourChoices =>
            Enum.GetValues<EditorSelectionColour>().Select(c => (c.GetLocalisableDescription(), c)).ToArray();

        /// <summary>
        /// Offered for both the box and the outline, which are set apart from one another but adjusted the same way.
        /// </summary>
        private static readonly (LocalisableString, float)[] thickness_choices =
        {
            ((LocalisableString)@"50%", 0.5f),
            ((LocalisableString)@"75%", 0.75f),
            ((LocalisableString)@"100%", 1f),
            ((LocalisableString)@"150%", 1.5f),
            ((LocalisableString)@"200%", 2f),
        };

        private static readonly (LocalisableString, float)[] opacity_choices =
        {
            ((LocalisableString)@"25%", 0.25f),
            ((LocalisableString)@"50%", 0.5f),
            ((LocalisableString)@"75%", 0.75f),
            ((LocalisableString)@"100%", 1f),
        };

        public SelectionAppearanceMenuItem(Bindable<EditorSelectionColour> colour, Bindable<float> thickness, Bindable<float> opacity,
                                           Bindable<EditorSelectionColour> outlineColour, Bindable<float> outlineThickness, Bindable<float> outlineOpacity,
                                           Bindable<float> handleScale, Bindable<EditorSelectionColour> handleColour, Bindable<float> anchorScale)
            : base(EditorStrings.SelectionAppearance)
        {
            Items = new MenuItem[]
            {
                new ChoiceMenuItem<EditorSelectionColour>(EditorStrings.ObjectOutlineColour, outlineColour, colourChoices),
                new ChoiceMenuItem<float>(EditorStrings.ObjectOutlineThickness, outlineThickness, thickness_choices),
                new ChoiceMenuItem<float>(EditorStrings.ObjectOutlineOpacity, outlineOpacity, opacity_choices),
                new ChoiceMenuItem<EditorSelectionColour>(EditorStrings.SelectionColour, colour, colourChoices),
                new ChoiceMenuItem<float>(EditorStrings.SelectionThickness, thickness, thickness_choices),
                new ChoiceMenuItem<float>(EditorStrings.SelectionOpacity, opacity, opacity_choices),
                new ChoiceMenuItem<float>(EditorStrings.SelectionHandleSize, handleScale, scale_choices),
                new ChoiceMenuItem<EditorSelectionColour>(EditorStrings.SelectionHandleColour, handleColour, colourChoices),
                new ChoiceMenuItem<float>(EditorStrings.SliderAnchorSize, anchorScale, scale_choices),
            };
        }
    }
}
