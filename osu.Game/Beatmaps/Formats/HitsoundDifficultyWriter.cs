// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Audio;
using osu.Game.Beatmaps.Legacy;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Hitsounds;

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
        /// The difficulty in <paramref name="file"/> with <paramref name="circles"/> for its objects, and green lines
        /// only where the custom index or the volume changes: what a difficulty hitsounded in osu!mania is turned
        /// into (see <see cref="LaneSoundConversion"/>).
        /// </summary>
        /// <remarks>
        /// As Mapping Tools writes one. A circle says its sample sets itself and nothing else, so the lines have only
        /// the custom index and the volume to say, and hundreds of circles go by between two of them.
        /// </remarks>
        /// <param name="file">A difficulty as stable has it, for everything but its objects and green lines. It need not be
        /// an osu! one: what is written is, and one hitsounded in stable's osu!mania is written from itself, as Mapping
        /// Tools writes it.</param>
        /// <param name="name">What the new difficulty is called.</param>
        /// <param name="circles">The circles, in the order of their times.</param>
        /// <param name="volumes">The volume from each of these times on, in the order of their times.</param>
        public static string FromLanes(string file, string name, IReadOnlyList<LaneSoundConversion.Circle> circles, IReadOnlyList<(double Time, int Volume)> volumes)
        {
            var hitObjects = circles.Select(c => circle(wholeTime(c.Time), sound(c), $"{bank(c.NormalBank)}:{bank(c.AdditionBank)}:0:0:")).ToArray();

            return rewrite(file, name, hitObjects, timingLines(file, circles, volumes));
        }

        /// <summary>
        /// A difficulty of <paramref name="ruleset"/> with nothing in it, as stable has one: the song, the names and the
        /// red lines of <paramref name="like"/>, and none of its objects.
        /// </summary>
        /// <remarks>
        /// What <see cref="FromLanes"/> is given, where the difficulty hitsounded in is not kept as stable has it and
        /// has no file to write the new one from.
        /// </remarks>
        public static string EmptyLike(IBeatmap like, RulesetInfo ruleset)
        {
            var info = like.BeatmapInfo.Clone();

            info.Ruleset = ruleset;
            // what any osu! difficulty has until it is said otherwise: the lanes of the other are no size of circle.
            info.Difficulty = new BeatmapDifficulty(like.Difficulty) { CircleSize = 4 };

            var empty = new Beatmap
            {
                BeatmapInfo = info,
                AudioLeadIn = like.AudioLeadIn,
                Bookmarks = like.Bookmarks.ToArray(),
                TimelineZoom = like.TimelineZoom,
            };

            foreach (var timingPoint in like.ControlPointInfo.TimingPoints)
                empty.ControlPointInfo.Add(timingPoint.Time, timingPoint.DeepClone());

            var builder = new StringBuilder();

            using (var writer = new StringWriter(builder))
                new LegacyBeatmapEncoder(empty, null, null, LegacyBeatmapEncoder.STABLE_VERSION).Encode(writer);

            return builder.ToString();
        }

        /// <summary>
        /// The volume every red and green line of <paramref name="file"/> says, in the order of the file: what
        /// <see cref="FromLanes"/> is given to keep the volumes of a difficulty it writes again.
        /// </summary>
        public static (double Time, int Volume)[] VolumesOf(string file) =>
            file.Replace("\r\n", "\n").Split('\n')
                .SkipWhile(l => l.Trim() != "[TimingPoints]").Skip(1)
                .Select(l => l.Trim())
                .TakeWhile(l => !l.StartsWith('['))
                .Where(isObject)
                .Select(l => l.Split(','))
                .Where(f => f.Length > 5
                            && double.TryParse(f[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _)
                            && double.TryParse(f[5], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                .Select(f => (double.Parse(f[0], CultureInfo.InvariantCulture), (int)double.Parse(f[5], CultureInfo.InvariantCulture)))
                .ToArray();

        /// <summary>
        /// The sounds sliders make as they are followed under the red and green lines of <paramref name="file"/>, by the
        /// names stable looks them up under without their extensions: one for each sample set and custom index a line
        /// says together. A slider's body is of the sample set of its line unless it is given one of its own, and a
        /// line that says no custom index has the skin's sounds, which are not the beatmap's to put anything in place of.
        /// </summary>
        public static string[] SliderSlidesOf(string file)
        {
            string[] lines = file.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).ToArray();

            // what a line that says no sample set is of.
            string ofTheBeatmap = lines.FirstOrDefault(l => l.StartsWith("SampleSet", StringComparison.Ordinal) && l.Contains(':'))?.Split(':')[1].Trim().ToLowerInvariant() switch
            {
                HitSampleInfo.BANK_SOFT => HitSampleInfo.BANK_SOFT,
                HitSampleInfo.BANK_DRUM => HitSampleInfo.BANK_DRUM,
                _ => HitSampleInfo.BANK_NORMAL,
            };

            return lines.SkipWhile(l => l != "[TimingPoints]").Skip(1)
                        .TakeWhile(l => !l.StartsWith('['))
                        .Where(isObject)
                        .Select(l => l.Split(','))
                        .Where(f => f.Length > 4
                                    && int.TryParse(f[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out _)
                                    && int.TryParse(f[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) && index > 0)
                        .Select(f => (set: int.Parse(f[3], CultureInfo.InvariantCulture), index: int.Parse(f[4], CultureInfo.InvariantCulture)))
                        .Select(l => (bank: l.set switch
                        {
                            (int)LegacySampleBank.Normal => HitSampleInfo.BANK_NORMAL,
                            (int)LegacySampleBank.Soft => HitSampleInfo.BANK_SOFT,
                            (int)LegacySampleBank.Drum => HitSampleInfo.BANK_DRUM,
                            _ => ofTheBeatmap,
                        }, l.index))
                        .Distinct()
                        .OrderBy(l => l.bank, StringComparer.Ordinal).ThenBy(l => l.index)
                        .Select(l => $@"{l.bank}-sliderslide{(l.index == 1 ? string.Empty : l.index.ToString(CultureInfo.InvariantCulture))}")
                        .ToArray();
        }

        private static string sound(LaneSoundConversion.Circle circle) =>
            ((circle.Whistle ? (int)LegacyHitSoundType.Whistle : 0) | (circle.Finish ? (int)LegacyHitSoundType.Finish : 0) | (circle.Clap ? (int)LegacyHitSoundType.Clap : 0))
            .ToString(CultureInfo.InvariantCulture);

        private static int bank(string name) => name switch
        {
            HitSampleInfo.BANK_NORMAL => (int)LegacySampleBank.Normal,
            HitSampleInfo.BANK_DRUM => (int)LegacySampleBank.Drum,
            _ => (int)LegacySampleBank.Soft,
        };

        /// <summary>
        /// The red lines of <paramref name="file"/>, and a green line wherever the custom index of
        /// <paramref name="circles"/> or the volume of <paramref name="volumes"/> is other than the line before says.
        /// </summary>
        private static IReadOnlyList<string> timingLines(string file, IReadOnlyList<LaneSoundConversion.Circle> circles, IReadOnlyList<(double Time, int Volume)> volumes)
        {
            var redLines = file.Replace("\r\n", "\n").Split('\n')
                               .SkipWhile(l => l.Trim() != "[TimingPoints]").Skip(1)
                               .Select(l => l.Trim())
                               .TakeWhile(l => !l.StartsWith('['))
                               .Where(isObject)
                               .Select(l => l.Split(','))
                               // a line of a file too old to say which it is, is a red one if its beat length is not negative.
                               .Where(f => f.Length > 6 ? f[6].Trim() == "1" : f.Length > 1 && !f[1].TrimStart().StartsWith('-'))
                               .Select(f => (time: startOf(f), fields: f))
                               .OrderBy(r => r.time)
                               .ToList();

            // what is wanted from each time on: a custom index where the circles go to another, a volume where that
            // changes, either without the other.
            var wanted = new SortedDictionary<double, (int? index, int? volume)>();

            int? lastIndex = null;

            foreach (var c in circles)
            {
                if (c.CustomIndex == lastIndex)
                    continue;

                double time = Math.Floor(c.Time + 1e-6);

                wanted[time] = (c.CustomIndex, wanted.GetValueOrDefault(time).volume);
                lastIndex = c.CustomIndex;
            }

            foreach (var (volumeTime, volume) in volumes)
            {
                double time = Math.Floor(volumeTime + 1e-6);

                wanted[time] = (wanted.GetValueOrDefault(time).index, volume);
            }

            // stable goes by the first red line for anything before it, so what is wanted up to there is wanted from
            // that line: a green line before it would be a line with no beat to go by.
            if (redLines.Count > 0)
            {
                (int? index, int? volume) early = default;

                foreach (double time in wanted.Keys.Where(t => t <= redLines[0].time).ToArray())
                {
                    early = (wanted[time].index ?? early.index, wanted[time].volume ?? early.volume);
                    wanted.Remove(time);
                }

                if (early.index != null || early.volume != null)
                    wanted[redLines[0].time] = early;
            }

            var lines = new List<string>();

            string meter = "4";
            string sampleSet = "0";
            (int index, int volume) said = (0, 100);
            (int? index, int? volume) want = default;

            int red = 0;

            foreach (double time in redLines.Select(r => r.time).Concat(wanted.Keys).Distinct().OrderBy(t => t))
            {
                if (wanted.TryGetValue(time, out var change))
                    want = (change.index ?? want.index, change.volume ?? want.volume);

                for (; red < redLines.Count && redLines[red].time <= time; red++)
                {
                    string[] fields = (string[])redLines[red].fields.Clone();

                    meter = fields.ElementAtOrDefault(2)?.Trim() ?? meter;
                    sampleSet = fields.ElementAtOrDefault(3)?.Trim() ?? sampleSet;

                    // a red line says a custom index and a volume as a green one does, so it says what is wanted where
                    // it is, rather than what the file had it say and a green line on top of it to say otherwise.
                    said = (want.index ?? whole(fields.ElementAtOrDefault(4), 0), want.volume ?? whole(fields.ElementAtOrDefault(5), 100));

                    if (fields.Length > 5)
                    {
                        fields[4] = said.index.ToString(CultureInfo.InvariantCulture);
                        fields[5] = said.volume.ToString(CultureInfo.InvariantCulture);
                    }

                    lines.Add(string.Join(',', fields));
                }

                var now = (want.index ?? said.index, want.volume ?? said.volume);

                if (now == said)
                    continue;

                lines.Add(FormattableString.Invariant($"{time},-100,{meter},{sampleSet},{now.Item1},{now.Item2},0,0"));
                said = now;
            }

            return lines;

            static double startOf(string[] fields) =>
                double.TryParse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double time) ? time : 0;

            static int whole(string? field, int otherwise) =>
                double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? (int)value : otherwise;
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
        /// <param name="file">The file.</param>
        /// <param name="name">What the new difficulty is called.</param>
        /// <param name="hitObjects">The objects to put in place of the file's, if any are.</param>
        /// <param name="timingPoints">The red and green lines to have in place of the file's: all of them, whether or not
        /// the file has any. With these, <paramref name="hitObjects"/> are also put in whether or not the file has any.</param>
        private static string rewrite(string file, string name, IReadOnlyList<string>? hitObjects, IReadOnlyList<string>? timingPoints = null)
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

                    // put in at the head of their sections, which may have nothing in them to put these in place of.
                    if (timingPoints != null)
                    {
                        if (section == "[TimingPoints]")
                            written.AddRange(timingPoints);
                        else if (section == "[HitObjects]" && hitObjects != null)
                        {
                            written.AddRange(hitObjects);
                            objectsWritten = true;
                        }
                    }

                    continue;
                }

                switch (section)
                {
                    case "[TimingPoints]" when timingPoints != null && isObject(trimmed):
                        continue;

                    // an osu! difficulty, whatever the file is: one hitsounded in osu!mania is written from itself.
                    case "[General]" when timingPoints != null && trimmed.StartsWith("Mode:", StringComparison.Ordinal):
                        written.Add("Mode: 0");
                        continue;

                    // what any osu! difficulty has until it is said otherwise: the lanes of the other are no size of circle.
                    case "[Difficulty]" when timingPoints != null && trimmed.StartsWith("CircleSize:", StringComparison.Ordinal):
                        written.Add("CircleSize:4");
                        continue;

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
