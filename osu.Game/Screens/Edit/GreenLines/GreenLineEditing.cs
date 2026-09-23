// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.GreenLines
{
    /// <summary>
    /// Puts in, changes and takes out green lines, and carries what they say over to the objects under them.
    /// </summary>
    /// <remarks>
    /// The editor keeps sounds and velocity on the objects, and stable keeps them on the lines. Changing a line changes
    /// the objects the way stable would hear them: every object that reads its sounds or velocity from a line, and
    /// whose line says something different afterwards, is given what the line says now. Only what changed is carried
    /// over, so an object's own bank, or a line put in that says what the one before it did, touches nothing.
    ///
    /// Where an object reads its line is where the decoder looks it up: a slider's body just after its head, its head,
    /// repeats and tail just after each, its velocity at the head itself, and anything else just after it ends.
    ///
    /// Changes are pending until confirmed: they show everywhere as they are made, are one step to undo once
    /// confirmed, and are put back as they were if cancelled instead.
    /// </remarks>
    public class GreenLineEditing
    {
        private readonly EditorBeatmap beatmap;
        private readonly SavedTimingLines lines;

        public GreenLineEditing(EditorBeatmap beatmap, SavedTimingLines lines)
        {
            this.beatmap = beatmap;
            this.lines = lines;
        }

        /// <summary>
        /// The line in effect at a time: the last one at or before it.
        /// </summary>
        public SavedTimingLine? InEffectAt(double time) => inEffectAt(lines.Lines, time);

        /// <summary>
        /// Whether there are changes that have been neither confirmed nor cancelled.
        /// </summary>
        public bool Pending { get; private set; }

        /// <summary>
        /// The lines as they were when the pending changes began, to tell the unconfirmed ones apart and to go back to.
        /// </summary>
        public IReadOnlyList<SavedTimingLine> Confirmed => Pending ? confirmedLines : lines.Lines.ToList();

        private List<SavedTimingLine> confirmedLines = new List<SavedTimingLine>();

        /// <summary>
        /// The timing screen's kiai as it was when the pending changes began.
        /// </summary>
        private List<(double time, bool kiai)> confirmedKiai = new List<(double, bool)>();

        /// <summary>
        /// Every object's sounds and velocity as they were when the pending changes began.
        /// </summary>
        private readonly Dictionary<HitObject, (HitSampleInfo[] samples, HitSampleInfo[][] nodes, double? velocity)> confirmedObjects =
            new Dictionary<HitObject, (HitSampleInfo[], HitSampleInfo[][], double?)>();

        /// <summary>
        /// Keeps whatever is changed from here on as one change, until it is confirmed or cancelled. Everything shows as
        /// changed in the meantime.
        /// </summary>
        private void begin()
        {
            if (Pending)
                return;

            confirmedLines = lines.Lines.ToList();
            confirmedKiai = beatmap.ControlPointInfo.EffectPoints.Select(e => (e.Time, e.KiaiMode)).ToList();
            confirmedObjects.Clear();

            foreach (var hitObject in beatmap.HitObjects)
            {
                confirmedObjects[hitObject] = (hitObject.Samples.ToArray(),
                    (hitObject as IHasRepeats)?.NodeSamples.Select(n => n.ToArray()).ToArray() ?? Array.Empty<HitSampleInfo[]>(),
                    (hitObject as IHasSliderVelocity)?.SliderVelocityMultiplier);
            }

            // held open until confirmed or cancelled, so that the whole of it is one step to undo.
            beatmap.BeginChange();
            Pending = true;
        }

        /// <summary>
        /// Takes the pending changes, as one step to undo.
        /// </summary>
        public void Confirm()
        {
            if (!Pending)
                return;

            Pending = false;
            beatmap.EndChange();
        }

        /// <summary>
        /// Puts the lines, and the objects' sounds and velocity, back to how they were before the pending changes.
        /// </summary>
        /// <remarks>
        /// Only what this changes is put back: anything else edited while the panel was open stays.
        /// </remarks>
        public void Cancel()
        {
            if (!Pending)
                return;

            lines.Lines.Clear();
            lines.Lines.AddRange(confirmedLines);

            if (!beatmap.ControlPointInfo.EffectPoints.Select(e => (e.Time, e.KiaiMode)).SequenceEqual(confirmedKiai))
                setKiai(confirmedKiai);

            foreach (var hitObject in beatmap.HitObjects)
            {
                if (!confirmedObjects.TryGetValue(hitObject, out var was))
                    continue;

                bool changed = putBack(hitObject.Samples, was.samples);

                if (hitObject is IHasRepeats repeats)
                {
                    for (int node = 0; node < Math.Min(repeats.NodeSamples.Count, was.nodes.Length); node++)
                        changed |= putBack(repeats.NodeSamples[node], was.nodes[node]);
                }

                if (hitObject is IHasSliderVelocity velocity && was.velocity != null && velocity.SliderVelocityMultiplier != was.velocity)
                {
                    velocity.SliderVelocityMultiplier = was.velocity.Value;
                    changed = true;
                }

                if (changed)
                    beatmap.Update(hitObject);
            }

            confirmedObjects.Clear();
            Pending = false;
            beatmap.EndChange();

            static bool putBack(IList<HitSampleInfo> samples, HitSampleInfo[] was)
            {
                // a sample's own equality leaves its volume out, which is most of what is being put back.
                if (samples.Count == was.Length && samples.Zip(was).All(p => p.First.Equals(p.Second) && p.First.Volume == p.Second.Volume && p.First.EditorAutoBank == p.Second.EditorAutoBank))
                    return false;

                samples.Clear();
                foreach (var sample in was)
                    samples.Add(sample);

                return true;
            }
        }

        /// <summary>
        /// Puts in a green line at a time, saying what the line before it does, and gives it back. Where a green line
        /// already stands at the time, that one is given back instead.
        /// </summary>
        public SavedTimingLine? Add(double time)
        {
            var standing = lines.Lines.Where(l => l.Time == time && !l.Uninherited).Cast<SavedTimingLine?>().LastOrDefault();

            if (standing != null)
                return standing;

            var before = InEffectAt(time);

            if (before == null)
                return null;

            var line = before.Value.GreenLineAt(time);

            change(() => lines.Lines.Insert(lines.Lines.Count(l => l.Time <= time), line));
            return line;
        }

        /// <summary>
        /// The line stable's timing panel picks when opened at a time: one standing there, else a green line within a
        /// few milliseconds either side, else the last one before it.
        /// </summary>
        /// <remarks>
        /// The playhead is rarely on a line's exact millisecond even when it looks to be, since lines are whole
        /// milliseconds and the beat divisor is not, so "standing there" allows for less than a millisecond.
        /// </remarks>
        public SavedTimingLine? PickAt(double time)
        {
            var standing = lines.Lines.Where(l => Math.Abs(l.Time - time) < 1)
                                .OrderBy(l => l.Uninherited)
                                .Cast<SavedTimingLine?>()
                                .FirstOrDefault();

            if (standing != null)
                return standing;

            var near = lines.Lines.Where(l => !l.Uninherited && Math.Abs(l.Time - time) <= pick_leniency)
                            .OrderBy(l => Math.Abs(l.Time - time))
                            .Cast<SavedTimingLine?>()
                            .FirstOrDefault();

            return near ?? InEffectAt(time);
        }

        /// <summary>
        /// How far from where the panel is opened a green line still counts as being there.
        /// </summary>
        private const double pick_leniency = 5;

        /// <summary>
        /// Puts a changed line in the place of the one it was made from. A change of time moves it among the others.
        /// </summary>
        public void Replace(SavedTimingLine line, SavedTimingLine changed)
        {
            int index = lines.Lines.IndexOf(line);

            if (index < 0 || line == changed)
                return;

            change(() =>
            {
                if (changed.Time == line.Time)
                {
                    lines.Lines[index] = changed;
                    return;
                }

                lines.Lines.RemoveAt(index);
                lines.Lines.Insert(lines.Lines.Count(l => l.Time <= changed.Time), changed);
            });
        }

        /// <summary>
        /// Takes out a green line. Red lines are the timing screen's to take out.
        /// </summary>
        public void Remove(SavedTimingLine line)
        {
            if (line.Uninherited || !lines.Lines.Contains(line))
                return;

            change(() => lines.Lines.Remove(line));
        }

        private void change(Action changeLines)
        {
            begin();

            var before = lines.Lines.ToList();

            beatmap.BeginChange();
            changeLines();

            var after = lines.Lines.ToList();

            carryOver(before, after);

            if (!kiaiOf(before).SequenceEqual(kiaiOf(after)))
                setKiai(after.Select(l => (l.Time, l.Kiai)));

            beatmap.EndChange();

            static IEnumerable<(double, bool)> kiaiOf(IEnumerable<SavedTimingLine> lines) => lines.Select(l => (l.Time, l.Kiai));
        }

        /// <summary>
        /// Makes the timing screen's kiai what the lines say, which is where the editor itself reads it from: each line
        /// that turns it on or off becomes a point that does, and the ones there were go.
        /// </summary>
        private void setKiai(IEnumerable<(double time, bool kiai)> changes)
        {
            var info = beatmap.ControlPointInfo;

            foreach (var point in info.EffectPoints.ToArray())
            {
                var group = info.GroupAt(point.Time);
                group.Remove(point);

                if (group.ControlPoints.Count == 0)
                    info.RemoveGroup(group);
            }

            bool kiai = false;

            foreach (var (time, lineKiai) in changes)
            {
                if (lineKiai == kiai)
                    continue;

                kiai = lineKiai;
                info.Add(time, new EffectControlPoint { KiaiMode = kiai });
            }
        }

        /// <summary>
        /// Gives every object what its line says now, wherever that is different from what its line said before.
        /// </summary>
        private void carryOver(IReadOnlyList<SavedTimingLine> before, IReadOnlyList<SavedTimingLine> after)
        {
            foreach (var hitObject in beatmap.HitObjects)
            {
                bool changed = false;

                if (hitObject is IHasRepeats repeats)
                {
                    double start = hitObject.StartTime;

                    if (hitObject is IHasSliderVelocity velocity)
                    {
                        var was = inEffectAt(before, start);
                        var now = inEffectAt(after, start);

                        if (was != null && now != null && was.Value.ClampedSliderVelocity != now.Value.ClampedSliderVelocity)
                        {
                            velocity.SliderVelocityMultiplier = now.Value.ClampedSliderVelocity;

                            // the repeats and the tail move with the velocity, so they are looked up where they are now.
                            hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
                            changed = true;
                        }
                    }

                    changed |= carryOver(hitObject.Samples, start + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1, before, after);

                    for (int node = 0; node < repeats.NodeSamples.Count; node++)
                    {
                        double nodeTime = start + node * repeats.Duration / repeats.SpanCount();
                        changed |= carryOver(repeats.NodeSamples[node], nodeTime + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY, before, after);
                    }
                }
                else
                    changed |= carryOver(hitObject.Samples, hitObject.GetEndTime() + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY, before, after);

                if (changed)
                    beatmap.Update(hitObject);
            }
        }

        private static bool carryOver(IList<HitSampleInfo> samples, double lookupTime, IReadOnlyList<SavedTimingLine> before, IReadOnlyList<SavedTimingLine> after)
        {
            var was = inEffectAt(before, lookupTime);
            var now = inEffectAt(after, lookupTime);

            if (was == null || now == null || samples.Count == 0)
                return false;

            var (w, n) = (was.Value, now.Value);

            if (w.Volume == n.Volume && w.SampleIndex == n.SampleIndex && w.SampleSet == n.SampleSet)
                return false;

            for (int i = 0; i < samples.Count; i++)
            {
                var sample = samples[i];

                if (w.Volume != n.Volume)
                    sample = sample.With(newVolume: n.Volume);

                if (w.SampleIndex != n.SampleIndex)
                    sample = sample.With(newSuffix: n.SampleIndex >= 2 ? n.SampleIndex.ToString(CultureInfo.InvariantCulture) : null, newUseBeatmapSamples: n.SampleIndex >= 1);

                // an addition with a bank of its own keeps it, the way it keeps it in stable.
                if (w.SampleSet != n.SampleSet && bankOf(n.SampleSet) is string bank && (sample.Name == HitSampleInfo.HIT_NORMAL || sample.EditorAutoBank))
                    sample = sample.With(newBank: bank);

                samples[i] = sample;
            }

            return true;
        }

        private static string? bankOf(int sampleSet)
        {
            switch (sampleSet)
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

        private static SavedTimingLine? inEffectAt(IReadOnlyList<SavedTimingLine> lines, double time)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Time <= time)
                    return lines[i];
            }

            return null;
        }
    }
}
