// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using osu.Framework.Bindables;
using osu.Framework.Graphics;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The red and green lines as they stand in the beatmap's saved file, which is not what the editor holds.
    /// </summary>
    /// <remarks>
    /// The editor moves every green line's sounds onto the objects and makes up lines of its own on the way out, so
    /// what a stable mapper would see in the file cannot be read back from the editor's own state. This holds the
    /// file's lines instead, read when the beatmap is opened and again on every save.
    /// </remarks>
    public partial class SavedTimingLines : Component
    {
        public readonly BindableList<SavedTimingLine> Lines = new BindableList<SavedTimingLine>();

        /// <summary>
        /// Takes the lines from a saved file.
        /// </summary>
        public void Update(byte[] file)
        {
            Lines.Clear();
            Lines.AddRange(Read(file));
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

                    if (tryParse(line, out var parsed))
                        lines.Add(parsed);
                }
            }

            return lines;
        }

        /// <summary>
        /// Reads one line the way stable does, including files old enough to leave the later fields out.
        /// </summary>
        private static bool tryParse(string line, out SavedTimingLine parsed)
        {
            parsed = default;

            string[] fields = line.Split(',');

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

            parsed = new SavedTimingLine(time, beatLength, meter, sampleSet, sampleIndex, volume, uninherited, (effects & 1) != 0);
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
        public double BeatsPerMinute => 60000 / BeatLength;

        public double SliderVelocity => BeatLength < 0 ? 100 / -BeatLength : 1;
    }
}
