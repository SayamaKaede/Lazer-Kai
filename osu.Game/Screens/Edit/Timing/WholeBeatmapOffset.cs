// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.GreenLines;

namespace osu.Game.Screens.Edit.Timing
{
    /// <summary>
    /// Moves a whole beatmap in time: its red lines, its green lines, its objects, and everything else it has at a time.
    /// </summary>
    /// <remarks>
    /// For a song whose offset turns out to be some milliseconds off once the beatmap is made. A red line is otherwise
    /// moved one at a time: from the timing screen it takes the objects of its own stretch with it, from the green line
    /// panel nothing, and from neither the green lines.
    ///
    /// Everything goes by the same amount, so what was on a tick is on that tick afterwards. What was already a
    /// millisecond off one is then put on it, which is the mistake a change of offset is most often made to mend. What
    /// is further off than that was put there, and stays.
    /// </remarks>
    public static class WholeBeatmapOffset
    {
        /// <summary>
        /// How far from a tick something stands and is still put on it, in milliseconds. From there on it is unsnapped
        /// to the game's own checks as well, and taken to be meant.
        /// </summary>
        public const double SNAP_LENIENCY = 2;

        /// <summary>
        /// The divisors stable's editor offers.
        /// </summary>
        private static readonly int[] divisors = { 1, 2, 3, 4, 6, 8, 12, 16 };

        /// <summary>
        /// How many objects and green lines were put on a tick.
        /// </summary>
        public readonly record struct Result(int Objects, int Lines);

        /// <summary>
        /// Moves everything by a number of milliseconds and then puts what is just off a tick on it, as one step to undo.
        /// </summary>
        /// <param name="beatmap">The beatmap.</param>
        /// <param name="lines">The lines of its file.</param>
        /// <param name="milliseconds">How far, later for a positive number. Nothing is moved for 0, and what is just off a
        /// tick is still put on it.</param>
        /// <param name="linesAreKept">Whether the lines are the file's to keep, as they are for a beatmap kept in stable's
        /// format. Lines that are made up again on every save are moved, so that they are shown where they will be, and
        /// not put on ticks: they say nothing the objects do not.</param>
        public static Result Move(EditorBeatmap beatmap, SavedTimingLines? lines, int milliseconds, bool linesAreKept)
        {
            beatmap.BeginChange();

            if (milliseconds != 0)
            {
                moveTiming(beatmap.ControlPointInfo, milliseconds);

                if (lines != null)
                    moveLines(lines, milliseconds);

                moveObjects(beatmap, milliseconds);
                moveTheRest(beatmap, milliseconds);

                // a slider's length in time is its beat length's, which is looked up where the slider now is.
                beatmap.UpdateAllHitObjects();
            }

            // the objects first: what a line is moved over is given what the line says, where it then is.
            int objects = snapObjects(beatmap);
            int snappedLines = lines != null && linesAreKept ? snapLines(beatmap, lines) : 0;

            beatmap.EndChange();

            return new Result(objects, snappedLines);
        }

        /// <summary>
        /// A time moved by whole milliseconds, with what it has after the point left as it is written: 1234.56 and 3 are
        /// 1237.56, which adding them as they are held does not always give.
        /// </summary>
        private static double moved(double time, int milliseconds) => (double)((decimal)time + milliseconds);

        private static void moveTiming(ControlPointInfo info, int milliseconds)
        {
            var points = info.Groups.SelectMany(g => g.ControlPoints.Select(p => (time: g.Time, point: p))).ToList();

            // all out and then all in: one at a time, a point would land on the group of one not yet moved.
            info.Clear();

            foreach (var (time, point) in points)
                info.Add(moved(time, milliseconds), point);
        }

        private static void moveLines(SavedTimingLines lines, int milliseconds)
        {
            if (lines.Lines.Count == 0)
                return;

            int selected = lines.Selected.Value is SavedTimingLine line ? lines.Lines.IndexOf(line) : -1;
            var movedLines = lines.Lines.Select(l => l.WithTime(moved(l.Time, milliseconds))).ToList();

            lines.Lines.Clear();
            lines.Lines.AddRange(movedLines);

            // the one the green line panel is on is still that one.
            if (selected >= 0)
                lines.Selected.Value = movedLines[selected];
        }

        private static void moveObjects(EditorBeatmap beatmap, int milliseconds)
        {
            // from the end it is moved towards, so that none goes past one not yet moved: the beatmap puts an object back
            // in its place among the others whenever its time changes.
            var objects = milliseconds > 0 ? beatmap.HitObjects.Reverse().ToArray() : beatmap.HitObjects.ToArray();

            foreach (var hitObject in objects)
                hitObject.StartTime += milliseconds;
        }

        /// <summary>
        /// The breaks, the bookmarks and the preview point.
        /// </summary>
        private static void moveTheRest(EditorBeatmap beatmap, int milliseconds)
        {
            if (beatmap.Breaks.Count > 0)
            {
                var breaks = beatmap.Breaks.Select(b => b is ManualBreakPeriod
                    ? new ManualBreakPeriod(b.StartTime + milliseconds, b.EndTime + milliseconds)
                    : new BreakPeriod(b.StartTime + milliseconds, b.EndTime + milliseconds)).ToList();

                beatmap.Breaks.Clear();
                beatmap.Breaks.AddRange(breaks);
            }

            if (beatmap.Bookmarks.Count > 0)
            {
                // as one change, which says of each bookmark where it went. A collaboration is divided at the bookmarks,
                // and with them all taken out and put in again it would be one nobody had agreed to anything in.
                beatmap.Bookmarks.ReplaceRange(0, beatmap.Bookmarks.Count, beatmap.Bookmarks.Select(b => b + milliseconds).ToList());
            }

            // -1 is a beatmap with no preview point set.
            if (beatmap.PreviewTime.Value >= 0)
                beatmap.PreviewTime.Value = Math.Max(0, beatmap.PreviewTime.Value + milliseconds);
        }

        /// <summary>
        /// Puts every object that is just off a tick on it: where it starts, and where it ends for one that is as long as
        /// it is set to be. A slider ends where its length and velocity put it, and is moved whole.
        /// </summary>
        /// <returns>How many were.</returns>
        private static int snapObjects(EditorBeatmap beatmap)
        {
            var info = beatmap.ControlPointInfo;
            int snapped = 0;

            foreach (var hitObject in beatmap.HitObjects.ToArray())
            {
                double start = tickJustMissed(info, hitObject.StartTime) ?? hitObject.StartTime;

                if (hitObject is IHasDuration withDuration and not IHasPath)
                {
                    double end = tickJustMissed(info, withDuration.EndTime) ?? withDuration.EndTime;

                    if (start == hitObject.StartTime && end == withDuration.EndTime)
                        continue;

                    hitObject.StartTime = start;
                    withDuration.Duration = Math.Max(0, end - start);
                }
                else
                {
                    if (start == hitObject.StartTime)
                        continue;

                    hitObject.StartTime = start;
                }

                beatmap.Update(hitObject);
                snapped++;
            }

            return snapped;
        }

        /// <summary>
        /// Puts every green line that is just off a tick on it, and gives the objects it is moved over what it says.
        /// Red lines stay: they are what the ticks are counted from.
        /// </summary>
        /// <returns>How many were.</returns>
        private static int snapLines(EditorBeatmap beatmap, SavedTimingLines lines)
        {
            var info = beatmap.ControlPointInfo;
            var edited = lines.Lines.ToList();
            int snapped = 0;

            int i = 0;

            while (i < edited.Count)
            {
                var line = edited[i];

                if (line.Uninherited || placeOnTick(info, line.Time) is not double place)
                {
                    i++;
                    continue;
                }

                edited.RemoveAt(i);

                // among the others as it was: still after the ones before it that are now at its time, and before the
                // ones after it that are. Which of two lines at a time is read is which comes later.
                int at = i;

                while (at > 0 && edited[at - 1].Time > place)
                    at--;

                while (at < edited.Count && edited[at].Time < place)
                    at++;

                edited.Insert(at, line.WithTime(place));
                snapped++;

                // one that went past the next has left it here, to be looked at in its turn.
                if (at <= i)
                    i++;
            }

            if (snapped == 0)
                return 0;

            int selected = lines.Selected.Value is SavedTimingLine selectedLine ? lines.Lines.IndexOf(selectedLine) : -1;

            // through what the green line panel changes them with, so that the objects under a line that was moved are
            // given what it says, as they would be read from the file.
            var editing = new GreenLineEditing(beatmap, lines);

            editing.SetAll(edited);
            editing.Confirm();

            // moved by under two milliseconds, it is where it was among the others in all but name.
            if (selected >= 0 && selected < lines.Lines.Count)
                lines.Selected.Value = lines.Lines[selected];

            return snapped;
        }

        /// <summary>
        /// The tick a time is just off: a millisecond or more from it, and less than <see cref="SNAP_LENIENCY"/>.
        /// </summary>
        /// <returns>The tick, or null for a time that is on one or too far from any.</returns>
        private static double? tickJustMissed(ControlPointInfo info, double time)
        {
            double tick = closestTick(info, time);
            double off = Math.Abs(time - tick);

            // times kept in whole milliseconds stand up to one from the tick they are on.
            return off >= 1 && off < SNAP_LENIENCY ? tick : null;
        }

        /// <summary>
        /// Where a green line just off a tick goes: the whole millisecond at or before the tick, which is where one put
        /// in on the tick is written, and from where it is read by an object on the tick.
        /// </summary>
        /// <returns>The time, or null for a line that is there already or too far from any tick.</returns>
        private static double? placeOnTick(ControlPointInfo info, double time)
        {
            double tick = closestTick(info, time);

            if (Math.Abs(time - tick) >= SNAP_LENIENCY)
                return null;

            double place = Math.Floor(tick);

            return time >= place && time <= tick ? null : place;
        }

        private static double closestTick(ControlPointInfo info, double time)
        {
            double closest = double.NaN;

            foreach (int divisor in divisors)
            {
                double tick = info.GetClosestSnappedTime(time, divisor);

                if (double.IsNaN(closest) || Math.Abs(time - tick) < Math.Abs(time - closest))
                    closest = tick;
            }

            return closest;
        }
    }
}
