// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Configuration
{
    /// <summary>
    /// Which format song select writes a beatmap out in.
    /// </summary>
    /// <remarks>
    /// One setting rather than one per file kind: a difficulty and the set it belongs to are handed to the same
    /// program, so the pair always moves together.
    /// </remarks>
    public enum BeatmapExportFormat
    {
        /// <summary>
        /// A difficulty as <c>.osu</c> converted back to v14, a set as <c>.osz</c>. What stable and the editors
        /// built around it read.
        /// </summary>
        [LocalisableDescription(typeof(BeatmapExportStrings), nameof(BeatmapExportStrings.FormatStable))]
        Stable,

        /// <summary>
        /// A difficulty as the <c>.osu</c> on disk, and a set as <c>.olz</c>. Nothing is converted, so nothing is
        /// lost on the way out -- and nothing is gained either: a beatmap that has never been saved in lazer comes
        /// out as whatever version it was imported as, which may be older than v14.
        /// </summary>
        [LocalisableDescription(typeof(BeatmapExportStrings), nameof(BeatmapExportStrings.FormatLazer))]
        Lazer,
    }
}
