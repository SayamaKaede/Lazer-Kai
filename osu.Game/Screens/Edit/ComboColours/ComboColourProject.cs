// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// The colour points laid out for one difficulty.
    /// </summary>
    /// <remarks>
    /// The `.osu` format has nowhere to keep these: all that reaches the file is the combo skip each object ended up
    /// with. They are therefore stored beside the beatmap, so a layout can be adjusted later instead of rebuilt.
    /// </remarks>
    public class ComboColourProject
    {
        [JsonProperty]
        public List<ComboColourPoint> Points { get; private set; } = new List<ComboColourPoint>();

        /// <summary>
        /// The longest combo a <see cref="ComboColourPointMode.Burst"/> point will take over.
        /// </summary>
        public int MaxBurstLength { get; set; } = 1;
    }
}
