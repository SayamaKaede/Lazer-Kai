// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.GreenLines;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Copies another difficulty's hitsounds onto the one being edited, the way stable hears them (2026-10-08).
    /// </summary>
    /// <remarks>
    /// The green lines come first, since in stable the sampleset, custom index and volume are theirs: every line here is
    /// made to say what the other's line in effect at its time says, and a line is put in wherever the other's change
    /// and these do not, keeping the velocity and kiai of the line before it.
    ///
    /// Then every point a sound is played at is given the sounds the other plays there. Where it plays none, the point
    /// is given no additions and follows its line, rather than keeping whatever it had: copied from a difficulty made
    /// for its hitsounds, that difficulty is what is heard. A slider's body is given the other's slider body at the same
    /// time where there is one, and otherwise keeps its additions, which a difficulty of circles has nothing to say about.
    /// </remarks>
    public static class HitsoundCopying
    {
        /// <summary>
        /// What is copied from a difficulty: its objects and its lines.
        /// </summary>
        public readonly record struct Source(IBeatmap Beatmap, IReadOnlyList<SavedTimingLine> Lines);

        /// <summary>
        /// Reads a difficulty's file for its hitsounds, or gives null if it has no lines to read them from.
        /// </summary>
        public static Source? Read(byte[] file)
        {
            var lines = SavedTimingLines.Read(file);

            if (lines.Count == 0)
                return null;

            using var reader = new LineBufferedReader(new MemoryStream(file));

            var beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return new Source(beatmap, lines);
        }

        /// <summary>
        /// Copies <paramref name="source"/>'s hitsounds onto <paramref name="target"/>, as one change to undo.
        /// </summary>
        /// <param name="target">The beatmap being edited.</param>
        /// <param name="lines">Its lines.</param>
        /// <param name="lineEditing">What changes them, carrying the change over to the objects.</param>
        /// <param name="source">What is copied.</param>
        public static void CopyOnto(EditorBeatmap target, SavedTimingLines lines, GreenLineEditing lineEditing, Source source)
        {
            target.BeginChange();

            copyLines(lines, lineEditing, source.Lines);
            CopyPointsOnto(target, lineEditing, new SourcePoints(source.Beatmap), target.HitObjects);

            lineEditing.Confirm();
            target.EndChange();
        }

        /// <summary>
        /// The points a difficulty plays sounds at, in time order, ready to be looked up again and again.
        /// </summary>
        public class SourcePoints
        {
            public readonly IBeatmap Beatmap;

            internal readonly HitPoint[] Points;

            internal readonly double[] Times;

            public SourcePoints(IBeatmap beatmap)
            {
                Beatmap = beatmap;
                Points = beatmap.HitObjects.SelectMany(h => HitPoints.Of(h, true)).OrderBy(p => p.Time).ToArray();
                Times = Points.Select(p => p.Time).ToArray();
            }
        }

        /// <summary>
        /// Gives <paramref name="hitObjects"/> the sounds <paramref name="source"/> plays at their times, leaving the lines
        /// as they are: for objects placed or moved once the lines were copied (2026-10-23).
        /// </summary>
        public static void CopyPointsOnto(EditorBeatmap target, GreenLineEditing lineEditing, SourcePoints source, IEnumerable<HitObject> hitObjects)
        {
            var sourcePoints = source.Points;
            double[] sourceTimes = source.Times;

            foreach (var hitObject in hitObjects)
            {
                bool changed = false;

                foreach (var point in HitPoints.Of(hitObject, true))
                {
                    var line = lineEditing.InEffectAt(point.Time + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY);

                    if (line == null)
                        continue;

                    var heard = heardAt(sourcePoints, sourceTimes, point.Time);

                    // where the other plays nothing, the sampleset follows the line, as it does in stable with 0 for it.
                    var samples = heard.Count > 0
                        ? HitsoundEditing.WithHitsound(point.Samples, HitsoundEditing.HitsoundOf(heard), bankOf(line.Value))
                        : HitsoundEditing.WithHitsound(point.Samples, new PointHitsound(HitObjectComposer.HIT_BANK_AUTO, null, Array.Empty<string>()), bankOf(line.Value));

                    changed |= set(point, followingLine(samples, line.Value));
                }

                if (hitObject is IHasRepeats && hitObject.Samples.Count > 0)
                {
                    var body = new HitPoint(hitObject, null, hitObject.StartTime);
                    var line = lineEditing.InEffectAt(hitObject.StartTime + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1);

                    if (line != null)
                    {
                        var sourceBody = source.Beatmap.HitObjects.FirstOrDefault(h => h is IHasRepeats && Math.Abs(h.StartTime - hitObject.StartTime) <= HitPoints.LENIENCY);

                        var hitsound = sourceBody != null
                            ? HitsoundEditing.HitsoundOf(new[] { new HitPoint(sourceBody, null, sourceBody.StartTime) })
                            : HitsoundEditing.HitsoundOf(new[] { body }) with { Bank = HitObjectComposer.HIT_BANK_AUTO };

                        changed |= set(body, followingLine(HitsoundEditing.WithHitsound(body.Samples, hitsound, bankOf(line.Value)), line.Value));
                    }
                }

                if (changed)
                    target.Update(hitObject);
            }
        }

        /// <remarks>
        /// All at once: each line put in on its own would go over every object again.
        /// </remarks>
        private static void copyLines(SavedTimingLines lines, GreenLineEditing lineEditing, IReadOnlyList<SavedTimingLine> from)
        {
            var edited = lines.Lines.Select(l => withSoundsOf(l, soundsAt(from, l.Time))).ToList();

            foreach (var line in from)
            {
                // the last of the other's lines at the time, as a red and a green line can stand together.
                var wanted = soundsAt(from, line.Time);

                // before the first line here, which was given what the other says there, there is nothing to put one in from.
                if (edited.Where(l => l.Time <= line.Time).Cast<SavedTimingLine?>().LastOrDefault() is not SavedTimingLine inEffect
                    || sameSounds(inEffect, wanted))
                    continue;

                // after any line at the time, so that it is the one read there.
                var added = withSoundsOf(inEffect.GreenLineAt(line.Time), wanted);
                edited.Insert(edited.Count(l => l.Time <= line.Time), added);
            }

            lineEditing.SetAll(edited);
        }

        /// <summary>
        /// The line of <paramref name="lines"/> in effect at a time, or the first of them before any is.
        /// </summary>
        private static SavedTimingLine soundsAt(IReadOnlyList<SavedTimingLine> lines, double time) => lines.LastOrDefault(l => l.Time <= time, lines[0]);

        private static bool sameSounds(SavedTimingLine a, SavedTimingLine b) => a.SampleSet == b.SampleSet && a.SampleIndex == b.SampleIndex && a.Volume == b.Volume;

        private static SavedTimingLine withSoundsOf(SavedTimingLine line, SavedTimingLine sounds) =>
            sameSounds(line, sounds) ? line : line.WithSampleSet(sounds.SampleSet).WithSampleIndex(sounds.SampleIndex).WithVolume(sounds.Volume);

        /// <summary>
        /// The points of <paramref name="sorted"/> heard with one at <paramref name="time"/>: those at the time nearest to
        /// it, as <see cref="HitPoints.At"/> finds them.
        /// </summary>
        private static IReadOnlyList<HitPoint> heardAt(HitPoint[] sorted, double[] times, double time)
        {
            int index = Array.BinarySearch(times, time - HitPoints.LENIENCY);

            if (index < 0)
                index = ~index;

            var near = new List<HitPoint>();

            for (; index < sorted.Length && sorted[index].Time <= time + HitPoints.LENIENCY; index++)
                near.Add(sorted[index]);

            if (near.Count == 0)
                return near;

            double nearest = near.MinBy(p => Math.Abs(p.Time - time)).Time;
            return near.Where(p => Math.Abs(p.Time - nearest) < 1).ToArray();
        }

        /// <summary>
        /// Every sound as loud as the line, and from its custom index, as stable plays it.
        /// </summary>
        private static IList<HitSampleInfo> followingLine(IList<HitSampleInfo> samples, SavedTimingLine line) =>
            samples.Select(s => s.With(newVolume: line.Volume,
                       newSuffix: line.SampleIndex >= 2 ? line.SampleIndex.ToString(CultureInfo.InvariantCulture) : null,
                       newUseBeatmapSamples: line.SampleIndex >= 1))
                   .ToList();

        private static bool set(HitPoint point, IList<HitSampleInfo> samples)
        {
            var current = point.Samples;

            // a sample's own equality leaves its volume out, which is much of what is copied.
            if (current.Count == samples.Count && current.Zip(samples).All(p => p.First.Equals(p.Second) && p.First.Volume == p.Second.Volume && p.First.EditorAutoBank == p.Second.EditorAutoBank))
                return false;

            point.SetSamples(samples);
            return true;
        }

        private static string? bankOf(SavedTimingLine line)
        {
            switch (line.SampleSet)
            {
                case 1:
                    return HitSampleInfo.BANK_NORMAL;

                case 2:
                    return HitSampleInfo.BANK_SOFT;

                case 3:
                    return HitSampleInfo.BANK_DRUM;

                default:
                    return null;
            }
        }
    }
}
