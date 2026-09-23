// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Audio
{
    /// <summary>
    /// The volumes of a fixed set of lines, for gameplay, where the lines do not change while it goes on.
    /// </summary>
    public class LineVolumes : ILineVolumes
    {
        private readonly Func<bool> enabled;
        private readonly double[] times;
        private readonly int[] volumes;

        /// <param name="enabled">Whether the sounds are to follow the lines, which is a setting that can change.</param>
        /// <param name="lines">The time and volume of every line, in time order.</param>
        public LineVolumes(Func<bool> enabled, IEnumerable<(double time, int volume)> lines)
        {
            this.enabled = enabled;

            var all = lines.ToArray();
            times = all.Select(l => l.time).ToArray();
            volumes = all.Select(l => l.volume).ToArray();
        }

        public bool Enabled => times.Length > 0 && enabled();

        public int VolumeAt(double time)
        {
            int index = Array.BinarySearch(times, time);

            // the last line at or before the time, which a search that misses says by where it would go.
            if (index < 0)
                index = ~index - 1;
            else
            {
                while (index + 1 < times.Length && times[index + 1] == time)
                    index++;
            }

            return index < 0 ? 100 : volumes[index];
        }
    }
}
