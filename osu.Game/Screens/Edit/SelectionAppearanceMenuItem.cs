// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Menu controlling how selection outlines look in the editor.
    /// </summary>
    internal class SelectionAppearanceMenuItem : MenuItem
    {
        public SelectionAppearanceMenuItem(Bindable<string> colour, Bindable<float> thickness, Bindable<float> opacity)
            : base(EditorStrings.SelectionAppearance)
        {
            Items = new MenuItem[]
            {
                new ChoiceMenuItem<string>(EditorStrings.SelectionColour, colour, new[]
                {
                    (CommonStrings.Default, EditorSelectionAppearance.DEFAULT_COLOUR),
                    ((LocalisableString)@"Red", @"#FF5555"),
                    ((LocalisableString)@"Orange", @"#FF9C22"),
                    ((LocalisableString)@"Green", @"#88FF55"),
                    ((LocalisableString)@"Cyan", @"#55DDFF"),
                    ((LocalisableString)@"Blue", @"#5599FF"),
                    ((LocalisableString)@"Magenta", @"#FF55CC"),
                    ((LocalisableString)@"White", @"#FFFFFF"),
                }),
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
            };
        }

        /// <summary>
        /// A submenu presenting a fixed set of values for a setting, with the active one ticked.
        /// </summary>
        private class ChoiceMenuItem<T> : MenuItem
            where T : notnull
        {
            private readonly Bindable<T> current;
            private readonly Dictionary<T, TernaryStateRadioMenuItem> menuItemLookup = new Dictionary<T, TernaryStateRadioMenuItem>();

            public ChoiceMenuItem(LocalisableString text, Bindable<T> current, IEnumerable<(LocalisableString label, T value)> choices)
                : base(text)
            {
                this.current = current;

                Items = choices.Select(createMenuItem).ToArray();

                current.BindValueChanged(_ =>
                {
                    foreach (var (value, item) in menuItemLookup)
                        item.State.Value = EqualityComparer<T>.Default.Equals(value, current.Value) ? TernaryState.True : TernaryState.False;
                }, true);
            }

            private TernaryStateRadioMenuItem createMenuItem((LocalisableString label, T value) choice)
            {
                var item = new TernaryStateRadioMenuItem(choice.label, MenuItemType.Standard, _ => current.Value = choice.value);
                menuItemLookup[choice.value] = item;
                return item;
            }
        }
    }
}
