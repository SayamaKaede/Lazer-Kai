// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;

namespace osu.Game.Screens.Edit.Setup
{
    /// <summary>
    /// A set of combo colours kept under a name, to be put on another beatmap as a whole.
    /// </summary>
    public class ComboColourPreset
    {
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The colours in the order the setup screen shows them, as hex so that the file can be read by hand.
        /// </summary>
        public List<string> Colours { get; set; } = new List<string>();

        public IEnumerable<Colour4> ToColours() => Colours.Select(c => Colour4.FromHex(c));

        public static ComboColourPreset FromColours(string name, IEnumerable<Colour4> colours) => new ComboColourPreset
        {
            Name = name,
            Colours = colours.Select(c => c.ToHex()).ToList(),
        };
    }
}
