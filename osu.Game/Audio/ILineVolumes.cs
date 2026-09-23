// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Audio
{
    /// <summary>
    /// The volume stable's green lines give at any time, for the sounds that follow them as they play: a slider's ticks
    /// and slide, and a spinner's spin. The objects themselves carry a volume only for where they are hit.
    /// </summary>
    public interface ILineVolumes
    {
        /// <summary>
        /// Whether the sounds are to follow the lines at all, which is a setting.
        /// </summary>
        bool Enabled { get; }

        /// <summary>
        /// The volume, from 0 to 100, of the line in effect at a time.
        /// </summary>
        int VolumeAt(double time);
    }
}
