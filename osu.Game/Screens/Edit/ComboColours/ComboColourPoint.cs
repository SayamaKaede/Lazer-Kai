// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// From a point in the song onwards, which of the beatmap's combo colours the combos take, and in what order.
    /// </summary>
    public class ComboColourPoint
    {
        /// <summary>
        /// When this point starts applying.
        /// </summary>
        public double Time { get; set; }

        public ComboColourPointMode Mode { get; set; }

        /// <summary>
        /// Indices into the beatmap's combo colours, walked in order, one per combo.
        /// </summary>
        /// <remarks>
        /// Held as indices rather than colours so that changing a colour in the beatmap's palette carries through to
        /// every point using it, and so that two identical colours in the palette stay distinguishable.
        /// </remarks>
        [JsonProperty]
        public List<int> Colours { get; private set; } = new List<int>();
    }

    public enum ComboColourPointMode
    {
        /// <summary>
        /// Applies from its time until the next point takes over.
        /// </summary>
        Normal,

        /// <summary>
        /// Applies to the single combo starting at its time, and only when that combo is short
        /// (see <see cref="ComboColourProject.MaxBurstLength"/>). Used once and then spent.
        /// </summary>
        Burst,
    }
}
