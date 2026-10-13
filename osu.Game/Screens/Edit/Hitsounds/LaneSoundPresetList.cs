// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Every preset of lane sounds, shared by all beatmaps.
    /// </summary>
    public class LaneSoundPresetList
    {
        [JsonProperty("presets")]
        public List<LaneSoundPreset> Presets { get; set; } = new List<LaneSoundPreset>();

        /// <summary>
        /// The preset already kept under the given name, if there is one.
        /// </summary>
        public LaneSoundPreset? Find(string name)
        {
            name = name.Trim();

            return Presets.FirstOrDefault(p => p.Name == name);
        }

        /// <summary>
        /// Keeps the preset, replacing whatever was kept under its name before.
        /// </summary>
        /// <remarks>
        /// A replaced preset keeps its place in the list, so that saving over one does not move it to the end.
        /// </remarks>
        public void Save(LaneSoundPreset preset)
        {
            preset.Name = preset.Name.Trim();

            int existing = Presets.FindIndex(p => p.Name == preset.Name);

            if (existing >= 0)
                Presets[existing] = preset;
            else
                Presets.Add(preset);
        }
    }
}
