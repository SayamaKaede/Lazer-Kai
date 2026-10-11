// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Where the wheel and the arrow keys take the editor while it is playing: from white tick to white tick, as
    /// stable's editor does.
    /// </summary>
    /// <remarks>
    /// Stopped, a notch of the wheel is a tick. Playing, lazer made it half a second whatever the tempo, to wherever
    /// that comes to: a beat and a half at 180 BPM, and nowhere a tick is, so that going back to hear a bar again
    /// meant guessing how many notches it was (2026-10-11).
    ///
    /// A white tick is the start of a beat: every beat length from where a timing point starts.
    /// </remarks>
    public static class WhiteTickSeeking
    {
        /// <summary>
        /// A tick this close ahead is as good as reached, and one this close behind as good as not left. Times here are
        /// sums of beat lengths that are seldom whole, so a tick never quite equals itself worked out another way.
        /// </summary>
        private const double leniency = 1;

        /// <summary>
        /// The white tick to go back to from <paramref name="time"/>.
        /// </summary>
        /// <remarks>
        /// The last one, unless the first tick after it has not gone by yet, when it is the one before: the playhead
        /// has only just left it, and going back to it would be going nowhere. Which tick that is depends on what the
        /// ticks are divided into: the blue one a quarter of a beat on with quarters, the next white tick with whole beats.
        /// </remarks>
        /// <param name="timing">The beatmap's timing.</param>
        /// <param name="time">Where the editor is.</param>
        /// <param name="divisor">What a beat is divided into by the ticks.</param>
        /// <param name="count">How many white ticks to go back.</param>
        public static double Before(ControlPointInfo timing, double time, int divisor, int count = 1)
        {
            double tick = timing.TimingPointAt(time).BeatLength / divisor;
            double target = atOrBefore(timing, time - tick - leniency);

            for (int i = 1; i < count; i++)
                target = atOrBefore(timing, target - leniency);

            return target;
        }

        /// <summary>
        /// The white tick to go forward to from <paramref name="time"/>: the next one, which at a change of timing is
        /// where the new timing starts.
        /// </summary>
        /// <param name="timing">The beatmap's timing.</param>
        /// <param name="time">Where the editor is.</param>
        /// <param name="count">How many white ticks to go forward.</param>
        public static double After(ControlPointInfo timing, double time, int count = 1)
        {
            double target = time;

            for (int i = 0; i < count; i++)
                target = after(timing, target + leniency);

            return target;
        }

        private static double atOrBefore(ControlPointInfo timing, double time)
        {
            var point = timing.TimingPointAt(time);

            return point.Time + Math.Floor((time - point.Time) / point.BeatLength) * point.BeatLength;
        }

        private static double after(ControlPointInfo timing, double time)
        {
            var point = timing.TimingPointAt(time);
            double next = point.Time + (Math.Floor((time - point.Time) / point.BeatLength) + 1) * point.BeatLength;

            // a timing point starts a beat of its own, wherever the one before would have had its next.
            var following = timing.TimingPointAfter(time);

            return following != null && following.Time < next ? following.Time : next;
        }
    }
}
