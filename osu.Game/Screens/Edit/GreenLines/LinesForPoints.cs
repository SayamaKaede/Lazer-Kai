// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Screens.Edit.GreenLines
{
    /// <summary>
    /// Works out the lines that give some of a beatmap's sounds something else to follow, and leave every other sound
    /// following what it did.
    /// </summary>
    /// <remarks>
    /// A line says what everything after it follows, up to the next line, so one changed for a few objects is heard on
    /// all that come after them as well. Here a line is put in where a run of the objects to change begins, and another
    /// at the first sound after the run, saying what that one already followed (2026-10-14).
    /// </remarks>
    public static class LinesForPoints
    {
        /// <summary>
        /// The lines with the sample set, custom index and volume of the points to change made what
        /// <paramref name="change"/> makes of them.
        /// </summary>
        /// <param name="lines">The lines as they are, in the order they stand.</param>
        /// <param name="points">When each sound of the beatmap is played, and whether it is one to change.</param>
        /// <param name="change">What a line a point to change follows is to say instead.</param>
        public static IReadOnlyList<SavedTimingLine> Change(IReadOnlyList<SavedTimingLine> lines, IEnumerable<(double time, bool toChange)> points,
                                                            Func<SavedTimingLine, SavedTimingLine> change)
        {
            var result = lines.ToList();

            int was = -1;
            int now = -1;

            // up to where the sounds gone over so far look their lines up: a line at or before it is one of theirs.
            double readUpTo = double.NegativeInfinity;

            // lines are on whole milliseconds, and the sounds of one are heard together.
            foreach (var point in points.GroupBy(p => Math.Floor(p.time + 1e-6)).OrderBy(g => g.Key))
            {
                double lookup = point.Max(p => p.time) + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY;

                while (was + 1 < lines.Count && lines[was + 1].Time <= lookup)
                    was++;

                while (now + 1 < result.Count && result[now + 1].Time <= lookup)
                    now++;

                if (was < 0 || now < 0)
                    continue;

                var wanted = point.Any(p => p.toChange) ? change(lines[was]) : lines[was];
                var followed = result[now];

                if (followed.SampleSet != wanted.SampleSet || followed.SampleIndex != wanted.SampleIndex || followed.Volume != wanted.Volume)
                {
                    if (!followed.Uninherited && followed.Time > readUpTo)
                        result[now] = withSoundsOf(followed, wanted);
                    else
                    {
                        // after a red line that stands a little after the point, where one before it would not be read.
                        result.Insert(now + 1, withSoundsOf(followed.GreenLineAt(Math.Max(point.Key, followed.Time)), wanted));
                        now++;
                    }
                }

                readUpTo = lookup;
            }

            return result;
        }

        private static SavedTimingLine withSoundsOf(SavedTimingLine line, SavedTimingLine sounds)
        {
            if (line.SampleSet != sounds.SampleSet)
                line = line.WithSampleSet(sounds.SampleSet);

            if (line.SampleIndex != sounds.SampleIndex)
                line = line.WithSampleIndex(sounds.SampleIndex);

            if (line.Volume != sounds.Volume)
                line = line.WithVolume(sounds.Volume);

            return line;
        }
    }
}
