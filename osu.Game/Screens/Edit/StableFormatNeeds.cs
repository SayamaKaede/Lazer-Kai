// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// What a difficulty is missing before it is kept in stable's format, which the hitsound screen's green lines, copying
    /// from another difficulty and making a hitsound difficulty all need.
    /// </summary>
    [Flags]
    public enum StableFormatNeeds
    {
        None = 0,

        /// <summary>
        /// The option is off, so even a file in stable's format is saved as lazer's.
        /// </summary>
        Compatibility = 1,

        /// <summary>
        /// The file is lazer's, or there is none yet.
        /// </summary>
        Conversion = 2,
    }
}
