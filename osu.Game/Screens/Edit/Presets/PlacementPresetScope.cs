// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// Which list of presets is being looked at.
    /// </summary>
    public enum PlacementPresetScope
    {
        [LocalisableDescription(typeof(PlacementPresetStrings), nameof(PlacementPresetStrings.EveryBeatmap))]
        Global,

        [LocalisableDescription(typeof(PlacementPresetStrings), nameof(PlacementPresetStrings.ThisBeatmap))]
        Beatmap,
    }
}
