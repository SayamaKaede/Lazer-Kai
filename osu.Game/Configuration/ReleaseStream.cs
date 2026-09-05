// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.ComponentModel;

namespace osu.Game.Configuration
{
    public enum ReleaseStream
    {
        Lazer,

        [Description("Tachyon (Unstable)")]
        Tachyon,

        /// <summary>
        /// The only one this fork has. Its releases come from one place, so offering a choice would be naming
        /// streams that do not exist.
        /// </summary>
        [Description("Lazer-Kai")]
        LazerKai
    }
}
