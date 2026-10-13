// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.GreenLines;

namespace osu.Game.Screens.Edit.Timing
{
    /// <summary>
    /// Puts the red lines of one difficulty in the place of another's, and what was on a tick back on one (2026-10-13).
    /// </summary>
    /// <remarks>
    /// The timing of a song is found once and is every difficulty's. Mended in one of them, it is copied to the rest,
    /// which in stable is done with Mapping Tools' Timing Copier. A red line moved by a millisecond or two leaves every
    /// object after it that far off its tick, so what is no further than is told from a tick of the new lines is put on
    /// it: the objects, the green lines and the bookmarks. What is further off was not on a tick before, or belongs to
    /// a part of the song the two difficulties time differently, and stays.
    ///
    /// Only what a red line says of the timing is copied: where it is, its beat, its bar, and whether its first bar
    /// line is left out. A red line of stable's also says a sample set, a volume and kiai, as a green line does, and
    /// those are the difficulty's own: each red line put in says what was followed where it is put.
    /// </remarks>
    public static class RedLineCopying
    {
        /// <summary>
        /// What a red line says of the timing.
        /// </summary>
        /// <param name="Time">Where it is.</param>
        /// <param name="BeatLength">How long its beat is, in milliseconds.</param>
        /// <param name="Meter">How many beats its bar has.</param>
        /// <param name="OmitFirstBarLine">Whether the bar line it starts with is left out.</param>
        public readonly record struct RedLine(double Time, double BeatLength, int Meter, bool OmitFirstBarLine);

        /// <summary>
        /// What copying did.
        /// </summary>
        /// <param name="RedLines">How many red lines the difficulty now has.</param>
        /// <param name="Objects">How many objects were put on a tick.</param>
        /// <param name="Lines">How many green lines were, or changes of kiai where the difficulty has no lines to keep.</param>
        /// <param name="OffTicks">How many objects start off every tick afterwards, as some may have before.</param>
        public readonly record struct Result(int RedLines, int Objects, int Lines, int OffTicks);

        /// <summary>
        /// The red lines of a difficulty.
        /// </summary>
        public static IReadOnlyList<RedLine> Of(ControlPointInfo info) =>
            info.TimingPoints.Select(t => new RedLine(t.Time, t.BeatLength, t.TimeSignature.Numerator, t.OmitFirstBarLine)).ToArray();

        /// <summary>
        /// Puts red lines in the place of a beatmap's own and what is just off a tick of them on it, as one step to undo.
        /// </summary>
        /// <param name="beatmap">The beatmap.</param>
        /// <param name="lines">The lines of its file, if it has one.</param>
        /// <param name="linesAreKept">Whether the lines are the file's to keep, as they are for a beatmap kept in stable's
        /// format: see <see cref="WholeBeatmapOffset.Move"/>.</param>
        /// <param name="redLines">The red lines to have.</param>
        /// <param name="within">How many milliseconds from a tick something stands and is still put on it. Nothing is
        /// for 0.</param>
        /// <param name="keepBeats">Whether everything is first moved to where its beat is under the red lines to have:
        /// see <see cref="KeepingBeats"/>.</param>
        public static Result CopyOnto(EditorBeatmap beatmap, SavedTimingLines? lines, bool linesAreKept, IReadOnlyList<RedLine> redLines, int within, bool keepBeats)
        {
            // an object is on a whole millisecond and a tick is not: one put on a tick stands up to a millisecond from
            // it, and with the red line gone some milliseconds further, up to one more than those.
            double leniency = Math.Max(0, within) + 1;

            var info = beatmap.ControlPointInfo;

            // counted by the red lines it has, before they go.
            var move = keepBeats ? KeepingBeats(Of(info), redLines) : null;

            beatmap.BeginChange();

            replaceTiming(info, redLines);

            if (move != null)
            {
                moveObjects(beatmap, move);
                moveTheRest(beatmap, move);

                if (!linesAreKept)
                    moveKiai(info, move);
            }

            // a slider's length in time is its beat length's, which is another now.
            beatmap.UpdateAllHitObjects();

            // the objects before the lines: an object is given what a line says where the line then is over it.
            int objects = WholeBeatmapOffset.SnapObjects(beatmap, leniency);
            int snappedLines = 0;

            if (lines != null && lines.Lines.Count > 0)
            {
                var standing = lines.Lines.ToList();

                // each on the whole millisecond at or before where its beat is, which is where a line is written and
                // from where the object on the beat reads it. The red lines among them come to where the ones to have
                // are, and are taken for the same lines.
                if (move != null)
                {
                    standing = standing.Select(l =>
                    {
                        double to = move(l.Time);

                        // one that stays is left as it is written.
                        return Math.Abs(to - l.Time) < 1e-6 ? l : l.WithTime(Math.Floor(to + 1e-6));
                    }).ToList();
                }

                var edited = LinesWith(standing, redLines, leniency);

                if (linesAreKept)
                {
                    // through what the green line panel changes them with, which makes the kiai of the timing screen
                    // what the lines say and looks every object's line up again.
                    var editing = new GreenLineEditing(beatmap, lines);

                    editing.SetAll(edited);
                    editing.Confirm();

                    snappedLines = WholeBeatmapOffset.SnapLines(beatmap, lines, leniency);
                }
                else
                {
                    // made up again on every save: changed for them to be shown where they will be.
                    lines.Lines.Clear();
                    lines.Lines.AddRange(edited);
                }

                // the line the green line panel was on may be one that is gone.
                if (lines.Selected.Value is SavedTimingLine selected && !lines.Lines.Contains(selected))
                    lines.Selected.Value = null;
            }

            if (!linesAreKept)
                snappedLines = snapKiai(info, leniency);

            snapBookmarks(beatmap, leniency);

            beatmap.EndChange();

            int offTicks = beatmap.HitObjects.Count(h => Math.Abs(h.StartTime - WholeBeatmapOffset.ClosestTick(info, h.StartTime)) >= 1);

            return new Result(redLines.Count, objects, snappedLines, offTicks);
        }

        /// <summary>
        /// Does the same to a difficulty that is not open, and saves it as the editor would: in stable's format still,
        /// where it was and stable compatibility is on.
        /// </summary>
        /// <param name="beatmaps">Where the difficulty is saved.</param>
        /// <param name="working">The difficulty.</param>
        /// <param name="keepStableFiles">Whether stable compatibility is on.</param>
        /// <param name="redLines">The red lines to have.</param>
        /// <param name="within">How many milliseconds from a tick something stands and is still put on it.</param>
        /// <param name="keepBeats">Whether everything is first moved to where its beat is under the red lines to have.</param>
        /// <param name="load">Loads the beatmap the difficulty is edited as, for it to be changed as the one in the editor is.</param>
        /// <returns>What was done, or null if the difficulty could not be kept in stable's format, when nothing is saved:
        /// a difficulty nobody has open is not turned into another kind of file without being asked.</returns>
        public static Result? CopyTo(BeatmapManager beatmaps, WorkingBeatmap working, bool keepStableFiles, IReadOnlyList<RedLine> redLines, int within, bool keepBeats,
                                     Action<EditorBeatmap> load)
        {
            var info = working.BeatmapInfo;
            var playable = working.GetPlayableBeatmap(info.Ruleset);
            var skin = working.GetSkin();
            var storyboard = working.Storyboard;

            byte[]? file = keepStableFiles ? readFile(working) : null;

            if (file != null && !StableFileKeeper.IsStableFile(file))
                file = null;

            var keeper = file == null ? null : new StableFileKeeper(file, playable, skin, storyboard);

            using (var beatmap = new EditorBeatmap(playable, skin, storyboard, info))
            using (var lines = new SavedTimingLines())
            {
                load(beatmap);

                if (file != null)
                    lines.Update(file);

                var result = CopyOnto(beatmap, file == null ? null : lines, file != null && info.Ruleset.OnlineID == 0, redLines, within, keepBeats);

                if (keeper == null)
                {
                    beatmaps.Save(info, beatmap.PlayableBeatmap, skin, storyboard);
                    return result;
                }

                byte[]? written = keeper.Write(beatmap.PlayableBeatmap, skin, storyboard, lines.Edited ? lines.Lines.Select(l => l.Raw).ToArray() : null, out _);

                if (written == null)
                {
                    Logger.Log($@"{info.GetDisplayTitle()} could not be kept in stable's format with the red lines copied to it: {keeper.LastFailure ?? @"no reason given"}");
                    return null;
                }

                beatmaps.SaveFile(info, beatmap.PlayableBeatmap, written);
                return result;
            }
        }

        /// <summary>
        /// Where a time goes for it to be on the same beat under other red lines.
        /// </summary>
        /// <remarks>
        /// With the red lines some way further on, as those of a difficulty timed to another cut of the song are, or
        /// with another beat length, nothing of the beatmap is near its tick any more, and putting what is near one on
        /// it mends nothing. The beats are counted from the first red line through every one after it, and a time goes
        /// to where the same count comes to under the other red lines: what Mapping Tools' Timing Copier does by
        /// keeping the number of beats between objects.
        ///
        /// A red line starts the count of its stretch anew, and is put in where the song is, not where the beat length
        /// before it comes to, so the stretch before it is counted as the ticks nearest its length: a red line put in
        /// where the song has drifted ten milliseconds is where that beat is, and what was on the beat goes onto it.
        /// </remarks>
        /// <param name="from">The red lines the times are under.</param>
        /// <param name="to">The red lines they are to be under.</param>
        public static Func<double, double> KeepingBeats(IReadOnlyList<RedLine> from, IReadOnlyList<RedLine> to)
        {
            // with none to count by on either side, and with the same on both, everything is where it belongs.
            if (from.Count == 0 || to.Count == 0 || from.SequenceEqual(to))
                return time => time;

            double[] fromBeats = beatsAtEach(from);
            double[] toBeats = beatsAtEach(to);

            return time =>
            {
                // before the first red line the beats are counted back from it.
                int stretch = Math.Max(0, lastIndex(from, l => l.Time <= time));

                double beats = fromBeats[stretch] + (time - from[stretch].Time) / from[stretch].BeatLength;

                // the stretch may be some milliseconds longer than its beats: what is in those is no later than the
                // beat the next red line is on, or it would go past what is on that.
                if (stretch + 1 < from.Count)
                    beats = Math.Min(beats, fromBeats[stretch + 1]);

                int under = Math.Max(0, Array.FindLastIndex(toBeats, b => b <= beats));

                return to[under].Time + (beats - toBeats[under]) * to[under].BeatLength;
            };
        }

        /// <summary>
        /// How many beats into the song each red line is, counting each stretch as the ticks nearest its length.
        /// </summary>
        private static double[] beatsAtEach(IReadOnlyList<RedLine> redLines)
        {
            double[] beats = new double[redLines.Count];

            for (int i = 1; i < redLines.Count; i++)
            {
                double stretch = (redLines[i].Time - redLines[i - 1].Time) / redLines[i - 1].BeatLength;

                // of the sixteenths and the twelfths, which between them are every tick stable's editor offers.
                double sixteenths = Math.Round(stretch * 16) / 16;
                double twelfths = Math.Round(stretch * 12) / 12;

                beats[i] = beats[i - 1] + Math.Max(0, Math.Abs(sixteenths - stretch) <= Math.Abs(twelfths - stretch) ? sixteenths : twelfths);
            }

            return beats;
        }

        private static int lastIndex(IReadOnlyList<RedLine> redLines, Func<RedLine, bool> match)
        {
            for (int i = redLines.Count - 1; i >= 0; i--)
            {
                if (match(redLines[i]))
                    return i;
            }

            return -1;
        }

        private static void moveObjects(EditorBeatmap beatmap, Func<double, double> move)
        {
            var moves = beatmap.HitObjects.Select(h => (hitObject: h, start: move(h.StartTime), end: h is IHasDuration lasting and not IHasPath ? move(lasting.EndTime) : (double?)null))
                               .ToArray();

            // those going later from the last of them and those going earlier from the first, so that none goes past
            // one not yet moved: the beatmap puts an object back in its place among the others whenever its time changes.
            foreach (var (hitObject, start, end) in moves.Where(m => m.start > m.hitObject.StartTime).Reverse().Concat(moves.Where(m => m.start <= m.hitObject.StartTime)))
            {
                hitObject.StartTime = start;

                // one that is as long as it is set to be ends on its own beat. A slider ends where its length and
                // velocity put it.
                if (end is double endTime)
                    ((IHasDuration)hitObject).Duration = Math.Max(0, endTime - start);
            }
        }

        /// <summary>
        /// The bookmarks and the breaks. The preview point is a place in the song, not in the beatmap, and stays.
        /// </summary>
        private static void moveTheRest(EditorBeatmap beatmap, Func<double, double> move)
        {
            if (beatmap.Breaks.Count > 0)
            {
                var breaks = beatmap.Breaks.Select(b => b is ManualBreakPeriod
                    ? new ManualBreakPeriod(move(b.StartTime), move(b.EndTime))
                    : new BreakPeriod(move(b.StartTime), move(b.EndTime))).ToList();

                beatmap.Breaks.Clear();
                beatmap.Breaks.AddRange(breaks);
            }

            if (beatmap.Bookmarks.Count > 0)
            {
                var moved = beatmap.Bookmarks.Select(b => (int)Math.Round(move(b))).ToList();

                // as one change, which says of each bookmark where it went: see WholeBeatmapOffset.
                if (!moved.SequenceEqual(beatmap.Bookmarks))
                    beatmap.Bookmarks.ReplaceRange(0, beatmap.Bookmarks.Count, moved);
            }
        }

        /// <summary>
        /// The changes of kiai, for a beatmap whose kiai is not said by lines kept.
        /// </summary>
        private static void moveKiai(ControlPointInfo info, Func<double, double> move)
        {
            var points = info.EffectPoints.Select(p => (point: p, time: move(p.Time))).ToArray();

            // all out and then all in: one at a time, a point would land on the group of one not yet moved.
            foreach (var (point, _) in points)
            {
                var group = info.GroupAt(point.Time);

                group.Remove(point);

                if (group.ControlPoints.Count == 0)
                    info.RemoveGroup(group);
            }

            foreach (var (point, time) in points)
                info.Add(time, point);
        }

        /// <summary>
        /// The lines of a file with red lines in the place of its own.
        /// </summary>
        /// <remarks>
        /// A red line of the file that is no further than <paramref name="within"/> from one put in is that line, moved:
        /// the one put in says of the sounds what it said. Any other put in says what was followed where it is put. A
        /// red line of the file with no line put in near it becomes a green line, for what it said of the sounds to be
        /// followed still, unless the line before it says the same.
        /// </remarks>
        /// <param name="lines">The lines of the file, in the order they stand.</param>
        /// <param name="redLines">The red lines to have, in the order of their times.</param>
        /// <param name="within">How far apart, in milliseconds, a red line of the file and one put in are the same line.</param>
        public static IReadOnlyList<SavedTimingLine> LinesWith(IReadOnlyList<SavedTimingLine> lines, IReadOnlyList<RedLine> redLines, double within)
        {
            // which line of the file each red line put in is, moved.
            var movedFrom = new int?[redLines.Count];
            var taken = new HashSet<int>();

            for (int i = 0; i < redLines.Count; i++)
            {
                double closest = within;

                for (int j = 0; j < lines.Count; j++)
                {
                    double apart = Math.Abs(lines[j].Time - redLines[i].Time);

                    if (!lines[j].Uninherited || taken.Contains(j) || apart >= closest)
                        continue;

                    closest = apart;
                    movedFrom[i] = j;
                }

                if (movedFrom[i] is int from)
                    taken.Add(from);
            }

            var result = new List<SavedTimingLine>(lines.Count + redLines.Count);

            // which of them stands in for a red line that is no more.
            var standsIn = new List<bool>(lines.Count + redLines.Count);

            for (int j = 0; j < lines.Count; j++)
            {
                if (taken.Contains(j))
                    continue;

                result.Add(lines[j].Uninherited ? lines[j].GreenLineAt(lines[j].Time) : lines[j]);
                standsIn.Add(lines[j].Uninherited);
            }

            for (int i = 0; i < redLines.Count; i++)
            {
                var redLine = redLines[i];

                // before any line of the file, what the first of them says is what is followed.
                var followed = movedFrom[i] is int from ? lines[from] : lines.LastOrDefault(l => l.Time <= redLine.Time, lines.Count > 0 ? lines[0] : nothing_said);

                // at its own time, before the green lines: where stable has it, so that the green lines are still read.
                int place = result.Count(l => l.Time < redLine.Time || (l.Time == redLine.Time && l.Uninherited));

                result.Insert(place, followed.RedLineAt(redLine.Time, redLine.BeatLength, redLine.Meter, redLine.OmitFirstBarLine));
                standsIn.Insert(place, false);
            }

            for (int k = result.Count - 1; k > 0; k--)
            {
                if (standsIn[k] && saysTheSame(result[k - 1], result[k]))
                    result.RemoveAt(k);
            }

            return result;
        }

        /// <summary>
        /// What a line of a file with none in it would be said to follow: no sample set of its own, at full volume.
        /// </summary>
        private static readonly SavedTimingLine nothing_said = SavedTimingLines.Parse(@"0,-100,4,0,0,100,0,0")!.Value;

        /// <summary>
        /// Whether an object would be the same under either line: its sounds, its kiai and its velocity.
        /// </summary>
        private static bool saysTheSame(SavedTimingLine one, SavedTimingLine other) =>
            one.SampleSet == other.SampleSet && one.SampleIndex == other.SampleIndex && one.Volume == other.Volume && one.Kiai == other.Kiai
            && one.ClampedSliderVelocity == other.ClampedSliderVelocity;

        private static void replaceTiming(ControlPointInfo info, IReadOnlyList<RedLine> redLines)
        {
            foreach (var point in info.TimingPoints.ToArray())
            {
                var group = info.GroupAt(point.Time);

                group.Remove(point);

                if (group.ControlPoints.Count == 0)
                    info.RemoveGroup(group);
            }

            foreach (var redLine in redLines)
            {
                info.Add(redLine.Time, new TimingControlPoint
                {
                    BeatLength = redLine.BeatLength,
                    TimeSignature = new TimeSignature(redLine.Meter),
                    OmitFirstBarLine = redLine.OmitFirstBarLine,
                });
            }
        }

        /// <summary>
        /// Puts every change of kiai that is just off a tick on it, for a beatmap whose kiai is not said by lines kept.
        /// </summary>
        private static int snapKiai(ControlPointInfo info, double leniency)
        {
            int snapped = 0;

            foreach (var point in info.EffectPoints.ToArray())
            {
                if (WholeBeatmapOffset.TickJustMissed(info, point.Time, leniency) is not double tick)
                    continue;

                var group = info.GroupAt(point.Time);

                group.Remove(point);

                if (group.ControlPoints.Count == 0)
                    info.RemoveGroup(group);

                info.Add(tick, point);
                snapped++;
            }

            return snapped;
        }

        private static void snapBookmarks(EditorBeatmap beatmap, double leniency)
        {
            if (beatmap.Bookmarks.Count == 0)
                return;

            var info = beatmap.ControlPointInfo;

            // on the whole millisecond a tick is written as.
            var snapped = beatmap.Bookmarks.Select(b => WholeBeatmapOffset.TickJustMissed(info, b, leniency) is double tick ? (int)Math.Floor(tick) : b).ToList();

            // as one change, which says of each bookmark where it went: see WholeBeatmapOffset.
            if (!snapped.SequenceEqual(beatmap.Bookmarks))
                beatmap.Bookmarks.ReplaceRange(0, beatmap.Bookmarks.Count, snapped);
        }

        private static byte[]? readFile(WorkingBeatmap working)
        {
            string? path = working.BeatmapInfo.Path == null ? null : working.BeatmapSetInfo?.GetPathForFile(working.BeatmapInfo.Path);

            if (path == null)
                return null;

            using (var stream = working.GetStream(path))
                return stream?.ReadAllRemainingBytesToArray();
        }
    }
}
