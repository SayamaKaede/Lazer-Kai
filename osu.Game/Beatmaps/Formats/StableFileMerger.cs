// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using DiffPlex;
using osu.Game.Audio;
using osu.Game.Database;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Beatmaps.Formats
{
    /// <summary>
    /// Writes the edits made to a beatmap into the stable file it was read from, leaving every line that was not edited
    /// as it was.
    /// </summary>
    /// <remarks>
    /// What was edited is found by comparing two encodings by this game's own encoder, of the beatmap as it was opened
    /// and as it is now, section by section and line by line. An object whose line is unchanged keeps its line from the
    /// file; one that changed, or is new, is written the way an export for stable writes it: whole numbers, one curve
    /// type, and its sample banks on the object itself.
    ///
    /// Volume and custom sample index are left to the green lines, the way stable's own editor keeps them. The file's
    /// green lines are all kept, so wherever an object's sounds no longer agree with them — it was moved, or its sounds
    /// were edited — a green line is put in at the object and another straight after it puts back what was there. The
    /// same goes for a slider whose velocity was changed.
    ///
    /// Whether the sounds agree is not worked out here. The file being written is read back by this game's own decoder,
    /// and every object's sounds and velocity compared with the beatmap being saved; lines are put in until they agree.
    /// Anything that cannot be made to agree, or any edit this does not know how to write, gives up, and the beatmap is
    /// saved in this game's own format instead.
    /// </remarks>
    public class StableFileMerger
    {
        /// <summary>
        /// How many rounds of reading back and putting in green lines before giving up. Each round fixes everything it
        /// finds, so a second is only needed where a fix lands inside something another fix put in.
        /// </summary>
        private const int max_rounds = 5;

        private readonly byte[] file;
        private readonly string baseline;
        private readonly string current;
        private readonly IBeatmap beatmap;

        private readonly string newLine;

        public StableFileMerger(byte[] file, string baseline, string current, IBeatmap beatmap)
        {
            this.file = file;
            this.baseline = baseline;
            this.current = current;
            this.beatmap = beatmap;

            newLine = latin1(file).Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
        }

        /// <summary>
        /// Works out the file to save.
        /// </summary>
        /// <returns>The file, or null if the edits cannot all be written in stable's format.</returns>
        /// <summary>
        /// Why <see cref="Merge"/> gave up, when it did: for the log, so that a save that went to this game's format
        /// can be traced back to what caused it.
        /// </summary>
        public string? Failure { get; private set; }

        private byte[]? fail(string reason)
        {
            Failure = reason;
            return null;
        }

        public byte[]? Merge()
        {
            // the object model and the sounds are only checked the way an osu! beatmap reads them.
            if (beatmap.BeatmapInfo.Ruleset.OnlineID != 0)
                return fail(@"edits are only written into osu! beatmaps");

            var sections = SourceFile.Read(file);

            var before = canonicalSections(baseline);
            var after = canonicalSections(current);

            foreach (string name in before.Keys.Union(after.Keys))
            {
                if (name == @"TimingPoints" || name == @"HitObjects")
                    continue;

                var b = before.GetValueOrDefault(name) ?? new List<string>();
                var a = after.GetValueOrDefault(name) ?? new List<string>();

                if (b.SequenceEqual(a))
                    continue;

                bool merged;

                switch (name)
                {
                    case @"General":
                    case @"Editor":
                        merged = mergeKeyValues(sections, name, b, a, @": ");
                        break;

                    case @"Metadata":
                    case @"Difficulty":
                        merged = mergeKeyValues(sections, name, b, a, @":");
                        break;

                    case @"Colours":
                        merged = mergeColours(sections, b, a);
                        break;

                    case @"Events":
                        merged = mergeEvents(sections, b, a);
                        break;

                    default:
                        merged = false;
                        break;
                }

                if (!merged)
                    return fail($@"[{name}] changed in a way that cannot be written: {describeDifference(b, a)}");
            }

            if (!mergeHitObjects(sections, before.GetValueOrDefault(@"HitObjects"), after.GetValueOrDefault(@"HitObjects")))
                return fail(Failure ?? @"the objects could not be lined up with the file's");

            var timing = sections.Find(@"TimingPoints");

            if (timing == null)
                return fail(@"the file has no timing points");

            var lines = TimingLine.ReadAll(timing);

            for (int round = 0; round < max_rounds; round++)
            {
                timing.SetData(lines.Select(l => l.Raw));

                byte[] candidate = sections.Write();
                var moments = readMoments(candidate);

                if (moments == null)
                    return fail(Failure ?? @"the written file did not read back as the beatmap");

                if (moments.All(m => m.Agrees))
                    return candidate;

                // velocities first: a slider's repeats and tail move with its velocity, so their sounds are only looked
                // at once every velocity is right. Looked at before, they are at the times the old velocity put them,
                // and lines placed for those times stay behind once the objects have moved away.
                bool velocitiesOnly = moments.Any(m => !m.VelocityAgrees);

                if (!putRight(lines, moments, velocitiesOnly))
                    return fail(Failure ?? @"a green line could not be placed");
            }

            var left = readMoments(sections.Write())?.FirstOrDefault(m => !m.Agrees);
            string around = left == null ? string.Empty : string.Join(@" / ", lines.Where(l => l.Time >= left.At - 1000 && l.Time <= left.LookupTime).Select(l => l.Raw));
            string momentsAround = left == null
                ? string.Empty
                : string.Join(@" / ", readMoments(sections.Write())!.Where(m => m.At >= left.At - 600 && m.At <= left.At)
                                                                  .Select(m => FormattableString.Invariant($@"{m.At:0.#} wants {m.WantVolume} has {m.HaveVolume}")));

            return fail($@"the sounds did not settle after {max_rounds} rounds (first left: {left}; lines before it: {around}; moments before it: {momentsAround})");
        }

        private static string describeDifference(List<string> before, List<string> after)
        {
            string gone = string.Join(@" / ", before.Except(after).Take(3));
            string added = string.Join(@" / ", after.Except(before).Take(3));
            return $@"was ""{gone}"", now ""{added}""";
        }

        #region Sections other than the timing points and objects

        private bool mergeKeyValues(SourceFile sections, string name, List<string> before, List<string> after, string separator)
        {
            var b = keyValues(before);
            var a = keyValues(after);

            var section = sections.Find(name);

            if (section == null)
                return false;

            foreach (string key in b.Keys.Union(a.Keys))
            {
                b.TryGetValue(key, out string? oldValue);

                if (!a.TryGetValue(key, out string? value))
                    return false;

                if (oldValue == value)
                    continue;

                if (key == @"GridSize")
                    value = stableGridSize(value);

                section.SetValue(key, value, separator, newLine);
            }

            return true;
        }

        private bool mergeColours(SourceFile sections, List<string> before, List<string> after)
        {
            var b = keyValues(before);
            var a = keyValues(after);

            var section = sections.Find(@"Colours") ?? sections.Insert(@"Colours", before: @"HitObjects", newLine);

            if (section == null)
                return false;

            var beforeCombos = b.Where(p => isCombo(p.Key)).ToList();
            var afterCombos = a.Where(p => isCombo(p.Key)).ToList();

            if (!beforeCombos.SequenceEqual(afterCombos))
                section.ReplaceKeys(isCombo, afterCombos.Select(p => $@"{p.Key} : {stableColour(p.Value)}"), newLine);

            foreach (string key in b.Keys.Union(a.Keys).Where(k => !isCombo(k)))
            {
                b.TryGetValue(key, out string? oldValue);

                if (!a.TryGetValue(key, out string? value))
                    return false;

                if (oldValue != value)
                    section.SetValue(key, stableColour(value), @" : ", newLine);
            }

            return true;

            static bool isCombo(string key) => key.StartsWith(@"Combo", StringComparison.Ordinal);
        }

        private bool mergeEvents(SourceFile sections, List<string> before, List<string> after)
        {
            // anything but the breaks is the background, a video or a storyboard, none of which the editor changes
            // in a way this knows how to write.
            if (!before.Where(l => !isBreak(l)).SequenceEqual(after.Where(l => !isBreak(l))))
                return false;

            var section = sections.Find(@"Events");

            if (section == null)
                return false;

            var breaks = new List<string>();

            foreach (string line in after.Where(isBreak))
            {
                string[] fields = line.Split(',');

                if (fields.Length < 3
                    || !double.TryParse(fields[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double start)
                    || !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double end))
                    return false;

                breaks.Add(FormattableString.Invariant($"2,{Math.Floor(start)},{Math.Floor(end)}"));
            }

            section.ReplaceBreaks(breaks, isBreak, newLine);
            return true;

            static bool isBreak(string line) => line.StartsWith(@"2,", StringComparison.Ordinal) || line.StartsWith(@"Break,", StringComparison.Ordinal);
        }

        private static string stableGridSize(string value)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int size))
                return value;

            // the sizes stable has, as the export picks them.
            if (size >= 24)
                return @"32";
            if (size >= 12)
                return @"16";
            if (size >= 6)
                return @"8";

            return @"4";
        }

        /// <summary>
        /// A colour as stable writes it: three components, the alpha this game adds left off.
        /// </summary>
        private static string stableColour(string value)
        {
            string[] parts = value.Split(',').Select(p => p.Trim()).ToArray();
            return string.Join(',', parts.Take(3));
        }

        private static Dictionary<string, string> keyValues(List<string> lines)
        {
            var values = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                int colon = line.IndexOf(':');

                if (colon < 0)
                    continue;

                values[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }

            return values;
        }

        #endregion

        #region Objects

        private bool failed(string reason)
        {
            Failure = reason;
            return false;
        }

        private static string describe(HitSampleInfo sample) => $@"{sample.Name}/{sample.Bank}";

        private bool mergeHitObjects(SourceFile sections, List<string>? before, List<string>? after)
        {
            var section = sections.Find(@"HitObjects");

            if (section == null || before == null || after == null)
                return failed(@"there are no objects to line up");

            var original = section.DataLines().ToList();

            // an object's line in the file stands for the one at the same place in the encoding of the beatmap as it
            // was opened. If they do not line up one for one, nothing can be said about which is which.
            if (original.Count != before.Count)
                return failed($@"the file has {original.Count} objects where the beatmap as opened had {before.Count}");

            var converted = convertedForStable();

            if (converted == null || converted.Count != after.Count)
                return failed(@"the objects could not be converted for stable");

            var diff = Differ.Instance.CreateLineDiffs(string.Join('\n', before), string.Join('\n', after), false, false);
            var merged = new List<string>();

            int fromBefore = 0;

            foreach (var block in diff.DiffBlocks)
            {
                while (fromBefore < block.DeleteStartA)
                    merged.Add(original[fromBefore++]);

                fromBefore += block.DeleteCountA;

                for (int i = 0; i < block.InsertCountB; i++)
                    merged.Add(fromUtf8(converted[block.InsertStartB + i]));
            }

            while (fromBefore < original.Count)
                merged.Add(original[fromBefore++]);

            if (merged.Count != after.Count)
                return failed($@"{merged.Count} objects came out of lining up {after.Count}");

            section.SetData(merged);
            return true;
        }

        /// <summary>
        /// The beatmap's objects written the way an export for stable writes them, in the order this game encodes them.
        /// </summary>
        private List<string>? convertedForStable()
        {
            try
            {
                using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(current)));

                // read as this game's own format, so that coordinates are not cut down on the way in: the conversion
                // rounds them itself.
                var decoded = new LegacyBeatmapDecoder(LegacyBeatmapEncoder.FIRST_LAZER_VERSION).Decode(reader);
                var playable = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(beatmap.BeatmapInfo.Ruleset);

                LegacyBeatmapExporter.ConvertForStable(playable);

                var builder = new StringBuilder();

                using (var writer = new StringWriter(builder))
                    new LegacyBeatmapEncoder(playable, null, null, LegacyBeatmapEncoder.STABLE_VERSION).Encode(writer);

                return canonicalSections(builder.ToString()).GetValueOrDefault(@"HitObjects");
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region Sounds and velocity

        /// <summary>
        /// One moment of one object: what the beatmap wants there, and what the file being written reads back as.
        /// </summary>
        /// <param name="At">The moment itself: a head, a repeat, a tail, or the end of an object without repeats.</param>
        /// <param name="LookupTime">When the decoder looks up the green line for its sounds, a few milliseconds after.</param>
        /// <param name="WantVolume">The volume the beatmap gives it.</param>
        /// <param name="WantIndex">The custom sample index the beatmap gives it.</param>
        /// <param name="HaveVolume">The volume the file reads back as.</param>
        /// <param name="HaveIndex">The custom sample index the file reads back as.</param>
        /// <param name="WantVelocity">For a slider's head, the velocity the beatmap gives it.</param>
        /// <param name="HaveVelocity">For a slider's head, the velocity the file reads back as.</param>
        private record Moment(double At, double LookupTime, int WantVolume, int WantIndex, int HaveVolume, int HaveIndex, double? WantVelocity, double? HaveVelocity)
        {
            public bool Agrees => WantVolume == HaveVolume && WantIndex == HaveIndex && VelocityAgrees;

            public bool VelocityAgrees => WantVelocity == null || HaveVelocity == null || Math.Abs(WantVelocity.Value - HaveVelocity.Value) <= 0.0005;
        }

        /// <summary>
        /// Reads a candidate file back and lines every moment of every object up against the beatmap being saved.
        /// </summary>
        /// <returns>Every moment, in the order the decoder looks them up, or null if something differs that no green line can put right.</returns>
        private List<Moment>? readMoments(byte[] candidate)
        {
            Beatmap written;

            try
            {
                using var reader = new LineBufferedReader(new MemoryStream(candidate));
                written = new LegacyBeatmapDecoder().Decode(reader);
            }
            catch (Exception e)
            {
                Failure = $@"the written file could not be read back: {e.Message}";
                return null;
            }

            var wanted = beatmap.HitObjects;
            var got = written.HitObjects;

            if (wanted.Count != got.Count)
            {
                Failure = $@"the written file reads back with {got.Count} objects, not {wanted.Count}";
                return null;
            }

            var moments = new List<Moment>();

            for (int i = 0; i < wanted.Count; i++)
            {
                var want = wanted[i];
                var have = got[i];

                // the file holds whole milliseconds; an object that moved further than that was not written where it is.
                if (Math.Abs(Math.Floor(want.StartTime) - have.StartTime) > 1)
                {
                    Failure = FormattableString.Invariant($@"the object at {want.StartTime} reads back at {have.StartTime}");
                    return null;
                }

                double start = have.StartTime;
                double end = have.GetEndTime();

                if (want is IHasRepeats wantRepeats && have is IHasRepeats haveRepeats)
                {
                    // the decoder takes a slider's own sounds from just after the head, and its velocity from the head.
                    double? wantVelocity = (want as IHasSliderVelocity)?.SliderVelocityMultiplier;
                    double? haveVelocity = (have as IHasSliderVelocity)?.SliderVelocityMultiplier;

                    // the velocity is a moment of its own, read at the head itself rather than after it.
                    if (wantVelocity != null && haveVelocity != null)
                        moments.Add(new Moment(start, start, -1, -1, -1, -1, wantVelocity, haveVelocity));

                    if (!add(moments, want.Samples, have.Samples, start, start + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY + 1, null, null))
                        return null;

                    if (wantRepeats.NodeSamples.Count != haveRepeats.NodeSamples.Count)
                    {
                        Failure = FormattableString.Invariant($@"the slider at {want.StartTime} reads back with {haveRepeats.NodeSamples.Count} nodes, not {wantRepeats.NodeSamples.Count}");
                        return null;
                    }

                    for (int node = 0; node < wantRepeats.NodeSamples.Count; node++)
                    {
                        double nodeTime = start + node * haveRepeats.Duration / haveRepeats.SpanCount();

                        if (!add(moments, wantRepeats.NodeSamples[node], haveRepeats.NodeSamples[node], nodeTime, nodeTime + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY, null, null))
                            return null;
                    }
                }
                else if (!add(moments, want.Samples, have.Samples, end, end + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY, null, null))
                    return null;
            }

            return moments.OrderBy(m => m.LookupTime).ToList();
        }

        /// <summary>
        /// Lines up the sounds of one moment of one object.
        /// </summary>
        /// <returns>Whether they are ones a green line can put right, if they differ.</returns>
        private bool add(List<Moment> moments, IList<HitSampleInfo> want, IList<HitSampleInfo> have, double at, double lookupTime, double? wantVelocity, double? haveVelocity)
        {
            // the banks and which sounds play are written on the object, so they have to agree already.
            foreach (var sample in want)
            {
                var match = have.FirstOrDefault(h => h.Name == sample.Name);

                if (match == null || match.Bank != sample.Bank || want.Count != have.Count)
                {
                    Failure = FormattableString.Invariant(
                        $@"at {at} the beatmap has [{string.Join(@", ", want.Select(describe))}] and the file reads back [{string.Join(@", ", have.Select(describe))}]");
                    return false;
                }
            }

            var wantFirst = want.FirstOrDefault();
            var haveFirst = have.FirstOrDefault();

            if (wantFirst == null || haveFirst == null)
                return true;

            moments.Add(new Moment(at, lookupTime, wantFirst.Volume, customIndex(wantFirst), haveFirst.Volume, customIndex(haveFirst), wantVelocity, haveVelocity));
            return true;
        }

        /// <summary>
        /// The custom sample index stable writes for a sample: the number it carries, or else 1 for the beatmap's own
        /// samples and 0 for the skin's.
        /// </summary>
        private static int customIndex(HitSampleInfo sample)
        {
            if (sample.Suffix != null && int.TryParse(sample.Suffix, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
                return index;

            return sample.UseBeatmapSamples ? 1 : 0;
        }

        /// <summary>
        /// Goes through every moment in time order and puts a green line at each one the lines before it no longer say
        /// right, the way a mapper would place one: at the object.
        /// </summary>
        /// <remarks>
        /// There is no line putting things back straight after an object. The next moment that wants what was there
        /// before gets a line of its own when its turn comes, at its own object, and one that wants the same as the
        /// line before it needs none at all. A line already standing at the time is changed rather than joined by a
        /// second one.
        ///
        /// Which moments follow the lines is judged against the lines the file was read back with: a volume or index
        /// that differs from its line there is one the object carries itself, and no line changes it.
        /// </remarks>
        /// <returns>Whether every difference could be given a line.</returns>
        private bool putRight(List<TimingLine> lines, List<Moment> moments, bool velocitiesOnly)
        {
            var readBackWith = lines.ToList();

            // a line may not go in at or before a moment already passed: that moment would read it too.
            double earliest = double.MinValue;

            foreach (var moment in moments)
            {
                double notBefore = earliest;

                // only a moment that reads sounds is one a later line could disturb.
                if (moment.WantVolume >= 0)
                    earliest = Math.Floor(moment.LookupTime) + 1;

                var then = TimingLine.ActiveAt(readBackWith, moment.LookupTime);
                var now = TimingLine.ActiveAt(lines, moment.LookupTime);

                if (then == null || now == null)
                {
                    Failure = FormattableString.Invariant($@"there is no line before {moment.At}");
                    return false;
                }

                bool volumeFollows = moment.HaveVolume == then.Volume;
                bool indexFollows = moment.HaveIndex == then.CustomIndex;

                int volume = volumeFollows ? now.Volume : moment.HaveVolume;
                int index = indexFollows ? now.CustomIndex : moment.HaveIndex;

                // a velocity's own moment says nothing about sounds.
                bool soundMoment = moment.WantVolume >= 0;

                int? wantVolume = soundMoment && volume != moment.WantVolume && !velocitiesOnly ? moment.WantVolume : null;
                int? wantIndex = soundMoment && index != moment.WantIndex && !velocitiesOnly ? moment.WantIndex : null;
                double? wantVelocity = null;

                if (moment.WantVelocity is double velocity)
                {
                    // a slider's velocity is taken from the line at its head itself.
                    var velocityLine = TimingLine.ActiveAt(lines, moment.At);

                    if (velocityLine == null)
                        return false;

                    if (Math.Abs(velocityLine.SliderVelocity - velocity) > 0.0005)
                        wantVelocity = velocity;
                }

                if (wantVolume == null && wantIndex == null && wantVelocity == null)
                    continue;

                if ((wantVolume != null && !volumeFollows) || (wantIndex != null && !indexFollows))
                {
                    Failure = FormattableString.Invariant($@"the object at {moment.At} carries its own volume or index, which a line cannot change");
                    return false;
                }

                // at the object, unless something just before is looked up at or after that; then straight after it.
                // A slider's head is read 5ms after it and the slider's own sounds 6ms after, so a head that differs
                // from the rest of its slider takes a second line a few milliseconds in, as it does in stable.
                // a line changing only the velocity carries the sounds on as they were, so the moments before it
                // read the same from it and it can stand at the head regardless.
                double time = wantVolume != null || wantIndex != null ? Math.Max(Math.Floor(moment.At), notBefore) : Math.Floor(moment.At);

                if (time > moment.LookupTime)
                {
                    Failure = FormattableString.Invariant($@"the moment at {moment.At} is read too soon after the one before it for a line to fit between");
                    return false;
                }

                if (!TimingLine.Set(lines, time, wantVelocity, wantVolume, wantIndex))
                {
                    Failure = FormattableString.Invariant($@"there is no line before {moment.At} to carry on from");
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region Reading and writing lines

        private static Dictionary<string, List<string>> canonicalSections(string text)
        {
            var sections = new Dictionary<string, List<string>>();
            List<string>? currentSection = null;

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.Trim();

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    currentSection = new List<string>();
                    sections[line[1..^1]] = currentSection;
                    continue;
                }

                if (currentSection == null || line.Length == 0 || line.StartsWith(@"//", StringComparison.Ordinal))
                    continue;

                currentSection.Add(line);
            }

            return sections;
        }

        /// <summary>
        /// Text as it is written into the file, byte for byte: the file is held as Latin-1 so that whatever it contains
        /// comes back out unchanged, and anything new is put into it as the UTF-8 it will be written as.
        /// </summary>
        private static string fromUtf8(string text) => latin1(Encoding.UTF8.GetBytes(text));

        private static string latin1(byte[] bytes) => Encoding.Latin1.GetString(bytes);

        /// <summary>
        /// The file, split into its sections with every line kept as it was, line endings and all.
        /// </summary>
        private class SourceFile
        {
            private readonly List<Section> sections = new List<Section>();

            public static SourceFile Read(byte[] bytes)
            {
                var result = new SourceFile();
                var section = new Section(null);
                result.sections.Add(section);

                string text = latin1(bytes);
                int start = 0;

                while (start < text.Length)
                {
                    int end = text.IndexOf('\n', start);
                    end = end < 0 ? text.Length : end + 1;

                    string line = text[start..end];
                    string content = line.TrimEnd('\r', '\n').Trim();

                    if (content.StartsWith('[') && content.EndsWith(']'))
                    {
                        section = new Section(content[1..^1]);
                        result.sections.Add(section);
                    }

                    section.Lines.Add(line);
                    start = end;
                }

                return result;
            }

            public Section? Find(string name) => sections.FirstOrDefault(s => s.Name == name);

            public Section? Insert(string name, string before, string newLine)
            {
                int index = sections.FindIndex(s => s.Name == before);

                if (index < 0)
                    return null;

                var section = new Section(name);
                section.Lines.Add($@"[{name}]{newLine}");
                section.Lines.Add(newLine);
                sections.Insert(index, section);
                return section;
            }

            public byte[] Write() => Encoding.Latin1.GetBytes(string.Concat(sections.SelectMany(s => s.Lines)));
        }

        private class Section
        {
            public readonly string? Name;

            /// <summary>
            /// Every line, the heading first, each with its own line ending.
            /// </summary>
            public readonly List<string> Lines = new List<string>();

            public Section(string? name)
            {
                Name = name;
            }

            private static string content(string line) => line.TrimEnd('\r', '\n').Trim();

            private static bool isData(string line)
            {
                string c = content(line);
                return c.Length > 0 && !c.StartsWith(@"//", StringComparison.Ordinal) && !(c.StartsWith('[') && c.EndsWith(']'));
            }

            private string lineEnding => Lines.Count > 0 && Lines[0].EndsWith("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";

            /// <summary>
            /// The section's data lines, without their line endings.
            /// </summary>
            public IEnumerable<string> DataLines() => Lines.Where(isData).Select(l => l.TrimEnd('\r', '\n'));

            /// <summary>
            /// Replaces the data lines, keeping the heading and whatever follows the last of them.
            /// </summary>
            public void SetData(IEnumerable<string> data)
            {
                int lastData = Lines.FindLastIndex(isData);
                var tail = lastData < 0 ? Lines.Skip(1).ToList() : Lines.Skip(lastData + 1).ToList();

                string heading = Lines[0];
                string ending = lineEnding;

                Lines.Clear();
                Lines.Add(heading);
                Lines.AddRange(data.Select(d => d + ending));
                Lines.AddRange(tail);
            }

            /// <summary>
            /// Sets one key's value, in the style the line already has, or adds the line after the last data line.
            /// </summary>
            public void SetValue(string key, string value, string separator, string newLine)
            {
                string encoded = fromUtf8(value);
                int index = Lines.FindIndex(l => keyOf(l) == key);

                if (index >= 0)
                {
                    string line = Lines[index];
                    string ending = line[line.TrimEnd('\r', '\n').Length..];
                    string body = line.TrimEnd('\r', '\n');
                    int colon = body.IndexOf(':');
                    int valueStart = colon + 1;

                    while (valueStart < body.Length && body[valueStart] == ' ')
                        valueStart++;

                    Lines[index] = body[..valueStart] + encoded + ending;
                    return;
                }

                int insertAt = Lines.FindLastIndex(isData) + 1;

                if (insertAt <= 0)
                    insertAt = 1;

                Lines.Insert(insertAt, key + separator + encoded + newLine);
            }

            /// <summary>
            /// Replaces every line whose key matches with the given lines, where the first of them stood.
            /// </summary>
            public void ReplaceKeys(Func<string, bool> matches, IEnumerable<string> replacement, string newLine)
            {
                int first = Lines.FindIndex(l => keyOf(l) is string k && matches(k));

                if (first < 0)
                    first = Math.Max(1, Lines.FindLastIndex(isData) + 1);

                Lines.RemoveAll(l => keyOf(l) is string k && matches(k));
                first = Math.Min(first, Lines.Count);

                Lines.InsertRange(first, replacement.Select(r => fromUtf8(r) + newLine));
            }

            public void ReplaceBreaks(List<string> breaks, Func<string, bool> isBreak, string newLine)
            {
                int first = Lines.FindIndex(l => isBreak(content(l)));

                if (first < 0)
                {
                    int comment = Lines.FindIndex(l => content(l).Replace(" ", string.Empty, StringComparison.Ordinal) == @"//BreakPeriods");
                    first = comment >= 0 ? comment + 1 : Math.Max(1, Lines.FindLastIndex(isData) + 1);
                }

                Lines.RemoveAll(l => isBreak(content(l)));
                first = Math.Min(first, Lines.Count);

                Lines.InsertRange(first, breaks.Select(b => b + newLine));
            }

            private static string? keyOf(string line)
            {
                string c = content(line);
                int colon = c.IndexOf(':');

                if (colon <= 0 || !isData(line))
                    return null;

                return c[..colon].Trim();
            }
        }

        /// <summary>
        /// One line of the timing points, kept as written if it came from the file.
        /// </summary>
        private class TimingLine
        {
            public readonly double Time;
            public readonly string Raw;

            private readonly string[] fields;

            private TimingLine(string raw)
            {
                Raw = raw;
                fields = raw.Split(',');
                Time = double.Parse(fields[0], NumberStyles.Float, CultureInfo.InvariantCulture);
            }

            private bool uninherited => fields.Length <= 6 ? !fields[1].StartsWith('-') : fields[6].Trim() == @"1";

            public int Volume => int.TryParse(field(5, @"100"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int volume) ? volume : 100;

            public int CustomIndex => int.TryParse(field(4, @"0"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) ? index : 0;

            /// <summary>
            /// The velocity a slider starting under this line gets, clamped the way the decoder clamps it.
            /// </summary>
            public double SliderVelocity
            {
                get
                {
                    if (uninherited || !double.TryParse(field(1, @"-100"), NumberStyles.Float, CultureInfo.InvariantCulture, out double beatLength) || beatLength >= 0)
                        return 1;

                    return Math.Clamp(100 / -beatLength, 0.1, 10);
                }
            }

            private string field(int index, string fallback) => fields.Length > index ? fields[index].Trim() : fallback;

            /// <summary>
            /// This line with the given things changed and everything else as written.
            /// </summary>
            private TimingLine with(double? sliderVelocity, int? volume, int? customIndex)
            {
                string[] changed = fields.ToArray();

                if (sliderVelocity != null)
                    changed[1] = (-100 / sliderVelocity.Value).ToString(@"R", CultureInfo.InvariantCulture);
                if (customIndex != null && changed.Length > 4)
                    changed[4] = customIndex.Value.ToString(CultureInfo.InvariantCulture);
                if (volume != null && changed.Length > 5)
                    changed[5] = volume.Value.ToString(CultureInfo.InvariantCulture);

                return new TimingLine(string.Join(',', changed));
            }

            /// <summary>
            /// Makes the lines say the given things from a time on: by changing the line already standing there, or
            /// else by putting one in that carries on from the line before.
            /// </summary>
            /// <remarks>
            /// A red line cannot carry a velocity, so where one stands and a velocity is wanted, a green line goes in
            /// beside it — the way stable itself pairs them.
            /// </remarks>
            /// <returns>Whether there was a line before the time to carry on from.</returns>
            public static bool Set(List<TimingLine> lines, double time, double? sliderVelocity, int? volume, int? customIndex)
            {
                int standing = lines.FindLastIndex(l => l.Time == time);

                if (standing >= 0 && lines[standing].fields.Length > 5 && (!lines[standing].uninherited || sliderVelocity == null))
                {
                    lines[standing] = lines[standing].with(sliderVelocity, volume, customIndex);
                    return true;
                }

                var before = ActiveAt(lines, time);

                if (before == null)
                    return false;

                InsertAfterAll(lines, before.Inherited(time, sliderVelocity, volume, customIndex));
                return true;
            }

            public static List<TimingLine> ReadAll(Section section) => section.DataLines().Select(l => new TimingLine(l)).ToList();

            /// <summary>
            /// The line in effect at a time: the last one at or before it.
            /// </summary>
            public static TimingLine? ActiveAt(List<TimingLine> lines, double time) => lines.LastOrDefault(l => l.Time <= time);

            /// <summary>
            /// A green line at the given time carrying on from this one, with the given things changed.
            /// </summary>
            public TimingLine Inherited(double time, double? sliderVelocity, int? volume, int? customIndex)
            {
                string beatLength = uninherited ? @"-100" : field(1, @"-100");

                if (sliderVelocity != null)
                    beatLength = (-100 / sliderVelocity.Value).ToString(@"R", CultureInfo.InvariantCulture);

                string meter = field(2, @"4");
                string sampleSet = field(3, @"0");
                string index = customIndex?.ToString(CultureInfo.InvariantCulture) ?? field(4, @"0");
                string volumeText = volume?.ToString(CultureInfo.InvariantCulture) ?? field(5, @"100");
                string effects = field(7, @"0");

                return new TimingLine(string.Join(',', time.ToString(CultureInfo.InvariantCulture), beatLength, meter, sampleSet, index, volumeText, @"0", effects));
            }

            /// <summary>
            /// Puts a line after every line at or before its time, so that it is the one in effect there.
            /// </summary>
            private static void InsertAfterAll(List<TimingLine> lines, TimingLine line)
            {
                int index = lines.FindIndex(l => l.Time > line.Time);
                lines.Insert(index < 0 ? lines.Count : index, line);
            }
        }

        #endregion
    }
}
