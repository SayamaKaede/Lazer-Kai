// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.GreenLines
{
    /// <summary>
    /// Finds and takes out the green lines that change nothing stable plays or shows.
    /// </summary>
    /// <remarks>
    /// A green line changes nothing when it says what the line before it does, or what the line after it at the same
    /// time does. Two lines at the same time that say different things both stay. Red lines are never taken out: they
    /// are the timing.
    ///
    /// A line nothing reads yet stays, as it does in stable's own clean-up: the next object put under it reads it, and
    /// mappers leave such lines in on purpose to set the index back between notes.
    ///
    /// Taking one out can leave the next saying what the one before it now does, so it goes round until none is left.
    /// </remarks>
    public static class GreenLineCleanup
    {
        /// <summary>
        /// Takes out every green line that changes nothing, as one step to undo.
        /// </summary>
        /// <returns>How many were taken out.</returns>
        public static int Remove(EditorBeatmap beatmap, SavedTimingLines lines)
        {
            var unneeded = Unneeded(lines.Lines.ToList());

            if (unneeded.Count == 0)
                return 0;

            beatmap.BeginChange();

            foreach (var line in unneeded)
                lines.Lines.Remove(line);

            beatmap.EndChange();
            return unneeded.Count;
        }

        /// <summary>
        /// Every green line that changes nothing, in the order they are found.
        /// </summary>
        public static List<SavedTimingLine> Unneeded(IReadOnlyList<SavedTimingLine> lines)
        {
            var kept = lines.ToList();
            var removed = new List<SavedTimingLine>();

            int i = 0;

            while (i < kept.Count)
            {
                if (!kept[i].Uninherited && isUnneeded(kept, i))
                {
                    removed.Add(kept[i]);
                    kept.RemoveAt(i);

                    // the line after it now comes straight after the one before, and may say what it does.
                    i = Math.Max(0, i - 1);
                    continue;
                }

                i++;
            }

            return removed;
        }

        /// <summary>
        /// Takes out every green line that changes nothing this difficulty plays, and gives each line left whose velocity
        /// no slider starts under the velocity of the line before it, as one step to undo (2026-10-27).
        /// </summary>
        /// <remarks>
        /// Unlike <see cref="Remove"/>, a line nothing here reads goes too: kept in a hitsound difficulty for its volumes, it
        /// changes nothing in one with its own notes. The velocities are set back so that lines copied from a hitsound
        /// difficulty do not show its velocity among this one's.
        /// </remarks>
        /// <returns>How many were taken out, and how many had their velocity set back.</returns>
        public static (int removed, int setBack) RemoveUnusedHere(EditorBeatmap beatmap, SavedTimingLines lines)
        {
            var (kept, removed, setBack) = UnusedHere(lines.Lines.ToList(), beatmap.HitObjects);

            if (removed == 0 && setBack == 0)
                return (0, 0);

            beatmap.BeginChange();

            lines.Lines.Clear();
            lines.Lines.AddRange(kept);

            beatmap.EndChange();
            return (removed, setBack);
        }

        /// <summary>
        /// The lines left once every green line that changes nothing <paramref name="hitObjects"/> play is taken out, with
        /// the velocities no slider reads set back to the line before.
        /// </summary>
        /// <remarks>
        /// What reads a line is looked up where the editor looks it up when lines change: a sound where its object's
        /// samples are read, a slider's velocity where it starts. Under a slider or spinner the volume is heard throughout,
        /// as it is in stable, but not the sampleset or the index. Kiai is always seen.
        /// </remarks>
        public static (List<SavedTimingLine> kept, int removed, int setBack) UnusedHere(IReadOnlyList<SavedTimingLine> lines, IEnumerable<HitObject> hitObjects)
        {
            var sounds = new List<double>();
            var bodies = new List<(double start, double end)>();
            var sliderStarts = new List<double>();

            foreach (var hitObject in hitObjects)
            {
                if (hitObject is IHasRepeats repeats)
                {
                    sounds.Add(hitObject.StartTime + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1);

                    for (int node = 0; node < repeats.NodeSamples.Count; node++)
                        sounds.Add(hitObject.StartTime + node * repeats.Duration / repeats.SpanCount() + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY);

                    if (hitObject is IHasSliderVelocity)
                        sliderStarts.Add(hitObject.StartTime);
                }
                else
                    sounds.Add(hitObject.GetEndTime() + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY);

                if (hitObject.GetEndTime() > hitObject.StartTime)
                    bodies.Add((hitObject.StartTime, hitObject.GetEndTime()));
            }

            var kept = new List<SavedTimingLine>();
            int removed = 0;
            int setBack = 0;

            for (int i = 0; i < lines.Count; i++)
            {
                var line = lines[i];

                if (line.Uninherited || kept.Count == 0)
                {
                    kept.Add(line);
                    continue;
                }

                // where it is the line read: up to the next line, which takes over even at the same time.
                double from = line.Time;
                double to = i + 1 < lines.Count ? lines[i + 1].Time : double.PositiveInfinity;

                bool soundHeard = sounds.Any(t => t >= from && t < to);
                bool volumeHeard = soundHeard || bodies.Any(b => b.start < to && b.end > from);
                bool velocityRead = sliderStarts.Any(t => t >= from && t < to);

                var before = kept[^1];
                double velocityBefore = before.Uninherited ? 1 : before.ClampedSliderVelocity;

                bool changes = line.Kiai != before.Kiai
                               || (soundHeard && (line.SampleSet != before.SampleSet || line.SampleIndex != before.SampleIndex))
                               || (volumeHeard && line.Volume != before.Volume)
                               || (velocityRead && line.ClampedSliderVelocity != velocityBefore);

                if (!changes)
                {
                    removed++;
                    continue;
                }

                if (!velocityRead && line.ClampedSliderVelocity != velocityBefore)
                {
                    line = line.WithSliderVelocity(velocityBefore);
                    setBack++;
                }

                kept.Add(line);
            }

            return (kept, removed, setBack);
        }

        private static bool isUnneeded(List<SavedTimingLine> lines, int index)
        {
            var line = lines[index];

            // another line at the same time is read in its place. when it says something else, which of the two the
            // mapper meant is theirs to sort out, so the line stays even if it says what the line before it does.
            if (index + 1 < lines.Count && lines[index + 1].Time == line.Time)
                return saysWhat(lines[index + 1], line);

            if (index == 0)
                return false;

            return saysWhat(lines[index - 1], line);
        }

        /// <summary>
        /// Whether a green line says what another line does.
        /// </summary>
        private static bool saysWhat(SavedTimingLine other, SavedTimingLine line)
        {
            // what a slider under the other line gets: always 1 under a red line.
            double velocity = other.Uninherited ? 1 : other.ClampedSliderVelocity;

            return line.SampleSet == other.SampleSet && line.SampleIndex == other.SampleIndex && line.Volume == other.Volume
                   && line.Kiai == other.Kiai && line.ClampedSliderVelocity == velocity;
        }
    }
}
