// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Beatmaps.Formats;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The red and green lines as a stable file has them: the saved file's, and any edits to them since.
    /// </summary>
    /// <remarks>
    /// The editor moves every green line's sounds onto the objects and makes up lines of its own on the way out, so
    /// what a stable mapper would see in the file cannot be read back from the editor's own state. This holds the
    /// file's lines instead, read when the beatmap is opened and again on every save, so whatever a save puts in is
    /// here afterwards.
    ///
    /// Edits to them are part of the editor's undo history, and a beatmap kept in stable's format is saved with them.
    /// </remarks>
    public partial class SavedTimingLines : Component, ILineVolumes
    {
        /// <remarks>
        /// Always, in the editor: it was a setting of its own until 2026-09-25, and there was no reason to hear the
        /// objects any other way while placing them.
        /// </remarks>
        public bool Enabled => Lines.Count > 0;

        public int VolumeAt(double time)
        {
            int volume = 100;

            foreach (var line in Lines)
            {
                if (line.Time > time)
                    break;

                volume = line.Volume;
            }

            return volume;
        }

        public readonly BindableList<SavedTimingLine> Lines = new BindableList<SavedTimingLine>();

        /// <summary>
        /// The lines as the timeline shows them: <see cref="Lines"/>, and while a beatmap kept in stable's format is
        /// edited, the lines a save will put in for the sliders' velocities as well, from the moment a slider needs one
        /// rather than from the save (2026-09-25).
        /// </summary>
        public readonly BindableList<SavedTimingLine> Shown = new BindableList<SavedTimingLine>();

        private EditorBeatmap? velocitiesOf;

        /// <summary>
        /// Whether <see cref="Shown"/> has the lines a save will put in for the velocities.
        /// </summary>
        public bool ShowsVelocityLines => velocitiesOf != null;

        /// <summary>
        /// Shows the lines a save will put in for the velocities of a beatmap's sliders.
        /// </summary>
        public void ShowVelocityLinesOf(EditorBeatmap beatmap)
        {
            if (velocitiesOf != null)
                return;

            velocitiesOf = beatmap;
            velocitiesOf.HitObjectAdded += refreshShownLater;
            velocitiesOf.HitObjectUpdated += refreshShownLater;
            velocitiesOf.HitObjectRemoved += refreshShownLater;

            refreshShown();
        }

        /// <summary>
        /// Stops showing them, once the beatmap is no longer saved in stable's format.
        /// </summary>
        public void StopShowingVelocityLines()
        {
            if (velocitiesOf == null)
                return;

            velocitiesOf.HitObjectAdded -= refreshShownLater;
            velocitiesOf.HitObjectUpdated -= refreshShownLater;
            velocitiesOf.HitObjectRemoved -= refreshShownLater;
            velocitiesOf = null;

            refreshShown();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            Lines.BindCollectionChanged((_, _) => refreshShownLater(null), true);
        }

        // once a frame at most: a drag updates its objects every frame.
        private void refreshShownLater(HitObject? _) => Scheduler.AddOnce(refreshShown);

        /// <summary>
        /// Brings <see cref="Shown"/> up to date now, rather than on the next frame, for something about to act on it.
        /// </summary>
        public void RefreshShown() => refreshShown();

        private void refreshShown()
        {
            IEnumerable<SavedTimingLine> shown = Lines;

            if (velocitiesOf != null)
            {
                shown = StableFileMerger.WithVelocityLines(Lines.Select(l => l.Raw), velocitiesOf)
                                        .Select(Parse)
                                        .Where(l => l != null)
                                        .Select(l => l!.Value);
            }

            var list = shown.ToList();

            if (list.SequenceEqual(Shown))
                return;

            Shown.Clear();
            Shown.AddRange(list);
        }

        protected override void Dispose(bool isDisposing)
        {
            StopShowingVelocityLines();
            base.Dispose(isDisposing);
        }

        /// <summary>
        /// The line the green line panel is editing, which the timeline marks so that it can be seen which one it is.
        /// </summary>
        public readonly Bindable<SavedTimingLine?> Selected = new Bindable<SavedTimingLine?>();

        /// <summary>
        /// The lines as the file was last saved or opened, before any edit to them.
        /// </summary>
        private IReadOnlyList<SavedTimingLine> asSaved = Array.Empty<SavedTimingLine>();

        /// <summary>
        /// Whether the lines were edited since the file was last saved or opened, and have to be written into it.
        /// </summary>
        public bool Edited => !Lines.SequenceEqual(asSaved);

        /// <summary>
        /// Takes the lines from a saved file.
        /// </summary>
        public void Update(byte[] file)
        {
            asSaved = Read(file);

            Lines.Clear();
            Lines.AddRange(asSaved);
        }

        /// <summary>
        /// The red and green lines in a beatmap file, in the order they are written.
        /// </summary>
        public static IReadOnlyList<SavedTimingLine> Read(byte[] file)
        {
            var lines = new List<SavedTimingLine>();
            bool inTimingPoints = false;

            using (var reader = new StreamReader(new MemoryStream(file), Encoding.UTF8, true))
            {
                string? raw;

                while ((raw = reader.ReadLine()) != null)
                {
                    string line = raw.Trim();

                    if (line.StartsWith('[') && line.EndsWith(']'))
                    {
                        inTimingPoints = line == @"[TimingPoints]";
                        continue;
                    }

                    if (!inTimingPoints || line.Length == 0 || line.StartsWith(@"//", StringComparison.Ordinal))
                        continue;

                    if (tryParse(raw, out var parsed))
                        lines.Add(parsed);
                }
            }

            return lines;
        }

        /// <summary>
        /// Reads one line of a beatmap file's timing points.
        /// </summary>
        /// <returns>The line, or null if it is not one.</returns>
        public static SavedTimingLine? Parse(string line) => tryParse(line, out var parsed) ? parsed : null;

        /// <summary>
        /// Reads one line the way stable does, including files old enough to leave the later fields out.
        /// </summary>
        private static bool tryParse(string line, out SavedTimingLine parsed)
        {
            parsed = default;

            string[] fields = line.Trim().Split(',');

            if (fields.Length < 2
                || !double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double time)
                || !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double beatLength))
                return false;

            int meter = field(2, 4);
            int sampleSet = field(3, 0);
            int sampleIndex = field(4, 0);
            int volume = field(5, 100);
            // before the field existed, a negative beat length was what made a line inherited.
            bool uninherited = fields.Length > 6 ? field(6, 1) == 1 : beatLength >= 0;
            int effects = field(7, 0);

            parsed = new SavedTimingLine(time, beatLength, meter, sampleSet, sampleIndex, volume, uninherited, (effects & 1) != 0) { Raw = line };
            return true;

            int field(int index, int fallback) =>
                fields.Length > index && int.TryParse(fields[index], NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : fallback;
        }
    }

    /// <summary>
    /// One line from a beatmap file's timing points, as stable reads it.
    /// </summary>
    public readonly record struct SavedTimingLine(double Time, double BeatLength, int Meter, int SampleSet, int SampleIndex, int Volume, bool Uninherited, bool Kiai)
    {
        /// <summary>
        /// The line as it is written in the file, spacing and all, so that a line nobody edited is written back as it was.
        /// </summary>
        public string Raw { get; init; } = string.Empty;

        public double BeatsPerMinute => 60000 / BeatLength;

        public double SliderVelocity => BeatLength < 0 ? 100 / -BeatLength : 1;

        /// <summary>
        /// The velocity a slider under this line gets, held to the range the decoder holds it to.
        /// </summary>
        public double ClampedSliderVelocity => Math.Clamp(SliderVelocity, 0.1, 10);

        /// <summary>
        /// A green line at the given time, saying what this one says: the line to start from when one is put in.
        /// </summary>
        /// <remarks>
        /// From a red line it takes the sounds and kiai but not the beat length, which on a green line is the velocity.
        /// </remarks>
        public SavedTimingLine GreenLineAt(double time)
        {
            string beatLength = Uninherited ? @"-100" : field(1, @"-100");
            string text = string.Join(',', format(time), beatLength, Meter.ToString(CultureInfo.InvariantCulture), SampleSet.ToString(CultureInfo.InvariantCulture),
                SampleIndex.ToString(CultureInfo.InvariantCulture), Volume.ToString(CultureInfo.InvariantCulture), @"0", Kiai ? @"1" : @"0");

            return SavedTimingLines.Parse(text)!.Value;
        }

        /// <summary>
        /// A red line at the given time and of the given beat, saying of the sounds and kiai what this one says: the
        /// line a red line copied from another difficulty is put in as, where this is what was followed there.
        /// </summary>
        /// <param name="time">Where it is.</param>
        /// <param name="beatLength">How long its beat is, in milliseconds.</param>
        /// <param name="meter">How many beats its bar has.</param>
        /// <param name="omitFirstBarLine">Whether the bar line it starts with is left out.</param>
        public SavedTimingLine RedLineAt(double time, double beatLength, int meter, bool omitFirstBarLine)
        {
            // the effects stable keeps in the one number: kiai, and the first bar line left out.
            int effects = (Kiai ? 1 : 0) | (omitFirstBarLine ? 8 : 0);

            string text = string.Join(',', format(time), beatLength.ToString(@"R", CultureInfo.InvariantCulture), meter.ToString(CultureInfo.InvariantCulture),
                SampleSet.ToString(CultureInfo.InvariantCulture), SampleIndex.ToString(CultureInfo.InvariantCulture), Volume.ToString(CultureInfo.InvariantCulture), @"1",
                effects.ToString(CultureInfo.InvariantCulture));

            return SavedTimingLines.Parse(text)!.Value;
        }

        public SavedTimingLine WithTime(double time) => withField(0, format(time));

        public SavedTimingLine WithSliderVelocity(double velocity) => withField(1, (-100 / velocity).ToString(@"R", CultureInfo.InvariantCulture));

        public SavedTimingLine WithSampleSet(int sampleSet) => withField(3, sampleSet.ToString(CultureInfo.InvariantCulture));

        public SavedTimingLine WithSampleIndex(int sampleIndex) => withField(4, sampleIndex.ToString(CultureInfo.InvariantCulture));

        public SavedTimingLine WithVolume(int volume) => withField(5, volume.ToString(CultureInfo.InvariantCulture));

        public SavedTimingLine WithKiai(bool kiai)
        {
            int effects = int.TryParse(field(7, @"0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
            return withField(7, (kiai ? effects | 1 : effects & ~1).ToString(CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// This line with one field written anew and every other as it was, filling in any the file left out.
        /// </summary>
        private SavedTimingLine withField(int index, string value)
        {
            var fields = Raw.Trim().Split(',').ToList();

            string[] missing = { string.Empty, @"-100", @"4", @"0", @"0", @"100", Uninherited ? @"1" : @"0", @"0" };

            while (fields.Count < missing.Length)
                fields.Add(missing[fields.Count]);

            fields[index] = value;
            return SavedTimingLines.Parse(string.Join(',', fields))!.Value;
        }

        private string field(int index, string fallback)
        {
            string[] fields = Raw.Trim().Split(',');
            return fields.Length > index ? fields[index].Trim() : fallback;
        }

        /// <summary>
        /// A time the way stable writes one: a whole millisecond where it is one.
        /// </summary>
        private static string format(double time) => time.ToString(CultureInfo.InvariantCulture);
    }
}
