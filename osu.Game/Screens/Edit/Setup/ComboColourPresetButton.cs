// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osuTK;

namespace osu.Game.Screens.Edit.Setup
{
    /// <summary>
    /// Sits beside the add button of the combo colours, and opens the presets they can be saved to and taken from.
    /// </summary>
    public partial class ComboColourPresetButton : RoundedButton, IHasPopover
    {
        private readonly BindableList<Colour4> colours;
        private readonly ComboColourPresetStore store;
        private readonly Action<IEnumerable<Colour4>> apply;

        /// <param name="colours">The colours on the beatmap now, which is what gets saved.</param>
        /// <param name="store">Where the presets are kept.</param>
        /// <param name="apply">Puts a preset's colours on the beatmap in place of every colour it has.</param>
        public ComboColourPresetButton(BindableList<Colour4> colours, ComboColourPresetStore store, Action<IEnumerable<Colour4>> apply)
        {
            this.colours = colours;
            this.store = store;
            this.apply = apply;

            // the same square as the add button it sits beside.
            Size = new Vector2(70);
            TooltipText = LazerKaiComboColourPresetStrings.Presets;
            Action = this.ShowPopover;

            Add(new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(20),
                Icon = FontAwesome.Solid.Swatchbook,
            });
        }

        public Popover GetPopover() => new ComboColourPresetPopover(colours, store, apply);
    }
}
