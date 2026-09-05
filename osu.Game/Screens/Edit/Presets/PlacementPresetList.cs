// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// One list of presets: either the one shared by every beatmap, or the one belonging to a single set.
    /// </summary>
    public class PlacementPresetList
    {
        /// <summary>
        /// The set these belong to, for a list that belongs to one.
        /// </summary>
        /// <remarks>
        /// The folder is named after the artist and title so that it can be found by hand, which stops being true
        /// the moment the metadata is edited. This is what the list is actually found by; the name is a label.
        /// </remarks>
        public string? BeatmapSetId { get; set; }

        public List<PlacementPreset> Presets { get; set; } = new List<PlacementPreset>();

        /// <summary>
        /// The folder this was read from, so that it can be written back to the same place.
        /// </summary>
        /// <remarks>
        /// Deliberately not moved to match the current metadata. Renaming would mean deleting the folder that was
        /// just read, and a stale label costs nothing next to that.
        /// </remarks>
        [JsonIgnore]
        public string? Folder { get; set; }

        /// <summary>
        /// The preset the given digit selects, if any holds it.
        /// </summary>
        public PlacementPreset? ForDigit(int digit) => Presets.FirstOrDefault(p => p.Digit == digit);

        /// <summary>
        /// Gives a preset a digit, taking it off whichever preset held it before.
        /// </summary>
        /// <remarks>
        /// Last one wins rather than refusing: the digit was pressed while looking at the preset it was meant for,
        /// and a refusal would leave the reason off-screen.
        /// </remarks>
        public void Assign(PlacementPreset preset, int? digit)
        {
            if (digit != null)
            {
                foreach (var other in Presets.Where(p => p.Digit == digit && p != preset))
                    other.Digit = null;
            }

            preset.Digit = digit;
        }
    }
}
