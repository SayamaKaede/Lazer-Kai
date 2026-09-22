// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;

namespace osu.Game.Screens.Edit.Setup
{
    /// <summary>
    /// Every combo colour preset, shared by all beatmaps.
    /// </summary>
    public class ComboColourPresetList
    {
        public List<ComboColourPreset> Presets { get; set; } = new List<ComboColourPreset>();

        /// <summary>
        /// The preset already kept under the given name, if there is one.
        /// </summary>
        public ComboColourPreset? Find(string name)
        {
            name = name.Trim();

            return Presets.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// Keeps the colours under the given name, replacing whatever was kept under it before.
        /// </summary>
        /// <remarks>
        /// A replaced preset keeps its place in the list, so that saving over one does not move it to the end.
        /// </remarks>
        public void Save(string name, IEnumerable<Colour4> colours)
        {
            var preset = ComboColourPreset.FromColours(name.Trim(), colours);
            int existing = Presets.FindIndex(p => p.Name == preset.Name);

            if (existing >= 0)
                Presets[existing] = preset;
            else
                Presets.Add(preset);
        }
    }
}
