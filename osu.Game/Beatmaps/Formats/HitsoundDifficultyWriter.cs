// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps.Legacy;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Beatmaps.Formats
{
    /// <summary>
    /// Writes the new difficulties a beatmap is hitsounded with, from a difficulty's file as stable has it.
    /// </summary>
    /// <remarks>
    /// Hitsounds are made once and copied to every difficulty, and what makes that possible is that the sample sets,
    /// volumes and custom indices live on green lines rather than on each object. Written from the file as text, the
    /// green lines come along exactly as they are — encoding the objects again would put them where the objects are
    /// instead.
    /// </remarks>
    public static class HitsoundDifficultyWriter
    {
        /// <summary>
        /// Where every circle of a hitsound difficulty goes: the middle, as in the user's own (2026-10-08).
        /// </summary>
        private const string position = "256,192";

        /// <summary>
        /// A circle starting a new combo.
        /// </summary>
        private const int circle_type = 5;

        /// <summary>
        /// A copy of the difficulty in <paramref name="file"/>, as one of its own named <paramref name="name"/>.
        /// </summary>
        public static string CopyAs(string file, string name) => rewrite(file, name, null);

        /// <summary>
        /// The difficulty in <paramref name="file"/> with every point a sound is played made a circle of its own, which
        /// sounds as that point did: each object's head, each end of a slider, and the end of a spinner.
        /// </summary>
        /// <returns>The new file, or null if the objects in <paramref name="file"/> could not all be read.</returns>
        public static string? AsHitCircles(string file, string name)
        {
            var circles = hitCirclesOf(file);
            return circles == null ? null : rewrite(file, name, circles);
        }

        /// <summary>
        /// The stack leniency of a hitsound difficulty: none, so that its circles, all in one place, are not drawn as a
        /// stack walking off up and to the left (2026-10-08).
        /// </summary>
        private const string stack_leniency = "StackLeniency: 0";

        /// <summary>
        /// <paramref name="file"/> named <paramref name="name"/>, without its online id, and with its objects replaced by
        /// <paramref name="hitObjects"/> if there are any to put in their place.
        /// </summary>
        private static string rewrite(string file, string name, IReadOnlyList<string>? hitObjects)
        {
            string newLine = file.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = file.Replace("\r\n", "\n").Split('\n');

            var written = new List<string>(lines.Length);
            string? section = null;
            bool objectsWritten = false;
            bool stackLeniencyWritten = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    // the end of [General] without a stack leniency of its own: one is put in at the end of it.
                    if (section == "[General]" && hitObjects != null && !stackLeniencyWritten)
                    {
                        int end = written.Count;

                        while (end > 0 && written[end - 1].Trim().Length == 0)
                            end--;

                        written.Insert(end, stack_leniency);
                        stackLeniencyWritten = true;
                    }

                    section = trimmed;
                    written.Add(line);
                    continue;
                }

                switch (section)
                {
                    case "[General]" when hitObjects != null && trimmed.StartsWith("StackLeniency:", StringComparison.Ordinal):
                        written.Add(stack_leniency);
                        stackLeniencyWritten = true;
                        continue;

                    case "[Metadata]" when trimmed.StartsWith("Version:", StringComparison.Ordinal):
                        written.Add($"Version:{name}");
                        continue;

                    // not yet submitted, as stable writes a difficulty it has just made.
                    case "[Metadata]" when trimmed.StartsWith("BeatmapID:", StringComparison.Ordinal):
                        written.Add("BeatmapID:0");
                        continue;

                    case "[HitObjects]" when hitObjects != null && isObject(trimmed):
                        if (!objectsWritten)
                            written.AddRange(hitObjects);

                        objectsWritten = true;
                        continue;
                }

                written.Add(line);
            }

            return string.Join(newLine, written);
        }

        private static bool isObject(string line) => line.Length > 0 && !line.StartsWith("//", StringComparison.Ordinal);

        /// <summary>
        /// A circle line for every point a sound is played in <paramref name="file"/>, in the order they are played.
        /// </summary>
        private static IReadOnlyList<string>? hitCirclesOf(string file)
        {
            string[] objectLines = file.Replace("\r\n", "\n").Split('\n')
                                       .SkipWhile(l => l.Trim() != "[HitObjects]").Skip(1)
                                       .Select(l => l.Trim()).Where(isObject)
                                       .ToArray();

            // how long a slider's span is depends on the red and green lines and the difficulty, which reading the file
            // works out the same way it does for the game. Read objects come back in the order of their start times,
            // with objects starting together left in the order of the file, so the lines are put in the same order.
            var sliders = new Queue<IHasRepeats>(read(file).HitObjects.OfType<IHasRepeats>().Where(h => h is IHasPath));

            var circles = new List<(double time, string line)>();

            foreach (string[] fields in objectLines.Select(l => l.Split(',')).OrderBy(f => startTime(f)))
            {
                if (fields.Length < 5 || !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type))
                    return null;

                double time = startTime(fields);
                string sound = fields[4];

                if ((type & (int)LegacyHitObjectType.Circle) > 0)
                    circles.Add((time, circle(fields[2], sound, fields.ElementAtOrDefault(5) ?? "0:0:0:0:")));
                else if ((type & (int)LegacyHitObjectType.Slider) > 0)
                {
                    if (fields.Length < 8 || !sliders.TryDequeue(out var slider))
                        return null;

                    int spans = slider.SpanCount();
                    double spanDuration = slider.Duration / spans;

                    string[]? edgeSounds = fields.ElementAtOrDefault(8)?.Split('|');
                    string[]? edgeSets = fields.ElementAtOrDefault(9)?.Split('|');

                    // the custom index, volume and file name are said once for the whole slider.
                    string extras = string.Join(':', (fields.ElementAtOrDefault(10) ?? "0:0:0:0:").Split(':').Skip(2));

                    for (int i = 0; i <= spans; i++)
                    {
                        double nodeTime = time + i * spanDuration;
                        string set = edgeSets?.ElementAtOrDefault(i) ?? "0:0";

                        circles.Add((nodeTime, circle(wholeTime(nodeTime), edgeSounds?.ElementAtOrDefault(i) ?? sound, $"{set}:{extras}")));
                    }
                }
                else if ((type & (int)LegacyHitObjectType.Spinner) > 0)
                {
                    if (fields.Length < 6)
                        return null;

                    circles.Add((double.Parse(fields[5], CultureInfo.InvariantCulture), circle(fields[5], sound, fields.ElementAtOrDefault(6) ?? "0:0:0:0:")));
                }
            }

            return sliders.Count > 0 ? null : circles.OrderBy(c => c.time).Select(c => c.line).ToArray();
        }

        private static string circle(string time, string sound, string sample) => $"{position},{time},{circle_type},{sound},{sample}";

        private static double startTime(string[] fields) =>
            double.TryParse(fields.ElementAtOrDefault(2), NumberStyles.Float, CultureInfo.InvariantCulture, out double time) ? time : double.MaxValue;

        /// <summary>
        /// A time as stable writes one: cut to the millisecond, with a value a hair under a whole number counting as that
        /// number, since the ends of a slider are worked out by adding up fractions.
        /// </summary>
        private static string wholeTime(double time) => Math.Floor(time + 1e-6).ToString(CultureInfo.InvariantCulture);

        private static IBeatmap read(string file)
        {
            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(file)));

            var beatmap = Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }
    }
}
