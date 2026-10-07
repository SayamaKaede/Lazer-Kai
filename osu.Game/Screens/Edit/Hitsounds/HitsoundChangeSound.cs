// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// What the hitsound screen plays as a change is made with its keys.
    /// </summary>
    public enum HitsoundChangeSound
    {
        /// <summary>
        /// The menu's opening sound for a sound put on, and its closing one for a sound taken off.
        /// </summary>
        [LocalisableDescription(typeof(LazerKaiHitsoundStrings), nameof(LazerKaiHitsoundStrings.ChangeSoundMenu))]
        Menu,

        /// <summary>
        /// What was changed, as it sounds now.
        /// </summary>
        [LocalisableDescription(typeof(LazerKaiHitsoundStrings), nameof(LazerKaiHitsoundStrings.ChangeSoundHitsound))]
        Hitsound,
    }
}
