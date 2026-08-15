// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Game.Graphics.UserInterface;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// A submenu presenting a fixed set of values for a setting, with the active one ticked.
    /// </summary>
    internal class ChoiceMenuItem<T> : MenuItem
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
