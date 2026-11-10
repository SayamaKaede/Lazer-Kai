// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace osu.Game.Screens.Edit.Snapshots
{
    /// <summary>
    /// What differs between two versions of a difficulty's file, or of a set's files (2026-11-10 request).
    /// </summary>
    /// <remarks>
    /// Read from the text of the files rather than from decoded beatmaps: what is listed is then what the file holds,
    /// whichever ruleset it is for and whichever format it was saved in, and nothing of the game is needed to work it
    /// out. An object moved in time is listed as one removed and one added, since it is the time that says which
    /// object of one version is which of the other.
    /// </remarks>
    public static class BeatmapSnapshotDiff
    {
        /// <summary>
        /// The sections whose lines are a name and a value, and are listed by name.
        /// </summary>
        private static readonly string[] setting_sections = { @"General", @"Metadata", @"Difficulty", @"Colours" };

        /// <summary>
        /// What differs between two versions of a difficulty's file, in the order of the times they are at, with what
        /// has no time first.
        /// </summary>
        public static IReadOnlyList<SnapshotChange> Compare(string before, string after)
        {
            var old = sections(before);
            var now = sections(after);

            var changes = new List<SnapshotChange>();

            foreach (string section in setting_sections)
                compareSettings(linesOf(old, section), linesOf(now, section), changes);

            compareBookmarks(linesOf(old, @"Editor"), linesOf(now, @"Editor"), changes);
            compareEvents(linesOf(old, @"Events"), linesOf(now, @"Events"), changes);
            compareLines(linesOf(old, @"TimingPoints"), linesOf(now, @"TimingPoints"), changes);
            compareObjects(linesOf(old, @"HitObjects"), linesOf(now, @"HitObjects"), changes);

            // stable, so that what is at one time stays in the order it was found in.
            return changes.OrderBy(c => c.Time ?? double.NegativeInfinity).ToList();
        }

        /// <summary>
        /// What differs between the files of a set at two times, each given as the hash of every file by its name.
        /// </summary>
        public static IReadOnlyList<SnapshotChange> CompareFiles(IReadOnlyDictionary<string, string> before, IReadOnlyDictionary<string, string> after)
        {
            var changes = new List<SnapshotChange>();

            foreach (string name in before.Keys.Union(after.Keys).OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
            {
                bool was = before.TryGetValue(name, out string? oldHash);
                bool isNow = after.TryGetValue(name, out string? newHash);

                if (was && isNow && oldHash == newHash)
                    continue;

                changes.Add(new SnapshotChange(!was ? SnapshotChangeKind.Added : !isNow ? SnapshotChangeKind.Removed : SnapshotChangeKind.Changed, SnapshotSubject.File)
                {
                    Name = name,
                });
            }

            return changes;
        }

        private static Dictionary<string, List<string>> sections(string text)
        {
            var found = new Dictionary<string, List<string>>();
            List<string>? current = null;

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith(@"//", StringComparison.Ordinal))
                    continue;

                if (line.StartsWith('[') && line.EndsWith(']'))
                {
                    string name = line[1..^1];

                    if (!found.TryGetValue(name, out current))
                        found[name] = current = new List<string>();

                    continue;
                }

                current?.Add(line);
            }

            return found;
        }

        private static List<string> linesOf(Dictionary<string, List<string>> sections, string name) =>
            sections.TryGetValue(name, out var lines) ? lines : new List<string>();

        private static Dictionary<string, string> settings(List<string> lines)
        {
            var found = new Dictionary<string, string>();

            foreach (string line in lines)
            {
                int colon = line.IndexOf(':');

                if (colon < 0)
                    continue;

                found[line[..colon].Trim()] = line[(colon + 1)..].Trim();
            }

            return found;
        }

        private static void compareSettings(List<string> before, List<string> after, List<SnapshotChange> changes)
        {
            var old = settings(before);
            var now = settings(after);

            foreach (string name in old.Keys.Union(now.Keys))
            {
                bool was = old.TryGetValue(name, out string? oldValue);
                bool isNow = now.TryGetValue(name, out string? newValue);

                if (was && isNow && oldValue == newValue)
                    continue;

                changes.Add(new SnapshotChange(!was ? SnapshotChangeKind.Added : !isNow ? SnapshotChangeKind.Removed : SnapshotChangeKind.Changed, SnapshotSubject.Setting)
                {
                    Name = name,
                    Before = oldValue,
                    After = newValue,
                });
            }
        }

        /// <summary>
        /// Of the editor's own section only the bookmarks are compared: the rest is where the editor was left, such as
        /// the zoom of the timeline, and differs after every sitting.
        /// </summary>
        private static void compareBookmarks(List<string> before, List<string> after, List<SnapshotChange> changes)
        {
            var old = bookmarks(before);
            var now = bookmarks(after);

            foreach (double time in old.Except(now))
                changes.Add(new SnapshotChange(SnapshotChangeKind.Removed, SnapshotSubject.Bookmark) { Time = time });

            foreach (double time in now.Except(old))
                changes.Add(new SnapshotChange(SnapshotChangeKind.Added, SnapshotSubject.Bookmark) { Time = time });
        }

        private static HashSet<double> bookmarks(List<string> lines)
        {
            var found = new HashSet<double>();

            if (!settings(lines).TryGetValue(@"Bookmarks", out string? value))
                return found;

            foreach (string part in value.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (number(part) is double time)
                    found.Add(time);
            }

            return found;
        }

        private static void compareEvents(List<string> before, List<string> after, List<SnapshotChange> changes)
        {
            var oldBreaks = new Dictionary<double, double>();
            var newBreaks = new Dictionary<double, double>();

            var old = withoutBreaks(before, oldBreaks);
            var now = withoutBreaks(after, newBreaks);

            foreach (double start in oldBreaks.Keys.Union(newBreaks.Keys))
            {
                bool was = oldBreaks.TryGetValue(start, out double oldEnd);
                bool isNow = newBreaks.TryGetValue(start, out double newEnd);

                if (was && isNow && oldEnd == newEnd)
                    continue;

                changes.Add(new SnapshotChange(!was ? SnapshotChangeKind.Added : !isNow ? SnapshotChangeKind.Removed : SnapshotChangeKind.Changed, SnapshotSubject.Break)
                {
                    Time = start,
                    Aspects = was && isNow ? SnapshotAspects.Length : SnapshotAspects.None,
                });
            }

            // whatever else is there is the background, the video and the storyboard, which have no one time to go to.
            var left = new List<string>(now);

            foreach (string line in old)
            {
                if (!left.Remove(line))
                    changes.Add(new SnapshotChange(SnapshotChangeKind.Removed, SnapshotSubject.Event) { Name = line.Trim() });
            }

            foreach (string line in left)
                changes.Add(new SnapshotChange(SnapshotChangeKind.Added, SnapshotSubject.Event) { Name = line.Trim() });
        }

        private static List<string> withoutBreaks(List<string> lines, Dictionary<double, double> breaks)
        {
            var rest = new List<string>();

            foreach (string line in lines)
            {
                string[] fields = line.Split(',');

                if (fields.Length >= 3 && (fields[0] == @"2" || fields[0] == @"Break") && number(fields[1]) is double start && number(fields[2]) is double end)
                    breaks[start] = end;
                else
                    rest.Add(line);
            }

            return rest;
        }

        private static void compareLines(List<string> before, List<string> after, List<SnapshotChange> changes)
        {
            var old = before.Select(TimingLine.Parse).OfType<TimingLine>().ToList();
            var now = after.Select(TimingLine.Parse).OfType<TimingLine>().ToList();

            foreach (var key in old.Select(l => (l.Time, l.Red)).Union(now.Select(l => (l.Time, l.Red))))
            {
                var were = old.Where(l => l.Time == key.Time && l.Red == key.Red).ToList();
                var are = now.Where(l => l.Time == key.Time && l.Red == key.Red).ToList();
                var subject = key.Red ? SnapshotSubject.RedLine : SnapshotSubject.GreenLine;

                for (int i = 0; i < Math.Max(were.Count, are.Count); i++)
                {
                    if (i >= were.Count)
                        changes.Add(new SnapshotChange(SnapshotChangeKind.Added, subject) { Time = key.Time, Details = details(TimingLine.SHOWN, null, are[i].ValueOf) });
                    else if (i >= are.Count)
                        changes.Add(new SnapshotChange(SnapshotChangeKind.Removed, subject) { Time = key.Time, Details = details(TimingLine.SHOWN, were[i].ValueOf, null) });
                    else if (were[i].DifferenceFrom(are[i]) is var aspects && aspects != SnapshotAspects.None)
                    {
                        changes.Add(new SnapshotChange(SnapshotChangeKind.Changed, subject)
                        {
                            Time = key.Time,
                            Aspects = aspects,
                            Details = details(aspects, were[i].ValueOf, are[i].ValueOf),
                        });
                    }
                }
            }
        }

        private static void compareObjects(List<string> before, List<string> after, List<SnapshotChange> changes)
        {
            var old = before.Select(ObjectLine.Parse).OfType<ObjectLine>().ToLookup(o => o.Time);
            var now = after.Select(ObjectLine.Parse).OfType<ObjectLine>().ToLookup(o => o.Time);

            foreach (double time in old.Select(g => g.Key).Union(now.Select(g => g.Key)))
            {
                var were = old[time].ToList();
                var are = now[time].ToList();

                // what is written the same in both is the same object, wherever among the others at its time it is.
                for (int i = were.Count - 1; i >= 0; i--)
                {
                    int same = are.FindIndex(o => o.Raw == were[i].Raw);

                    if (same < 0)
                        continue;

                    are.RemoveAt(same);
                    were.RemoveAt(i);
                }

                for (int i = 0; i < Math.Max(were.Count, are.Count); i++)
                {
                    if (i < were.Count && i < are.Count && were[i].Subject == are[i].Subject)
                    {
                        var aspects = were[i].DifferenceFrom(are[i]);

                        if (aspects != SnapshotAspects.None)
                        {
                            changes.Add(new SnapshotChange(SnapshotChangeKind.Changed, are[i].Subject)
                            {
                                Time = time,
                                Aspects = aspects,
                                Details = details(aspects, were[i].ValueOf, are[i].ValueOf),
                            });
                        }

                        continue;
                    }

                    if (i < were.Count)
                        changes.Add(new SnapshotChange(SnapshotChangeKind.Removed, were[i].Subject) { Time = time, Details = details(ObjectLine.SHOWN, were[i].ValueOf, null) });

                    if (i < are.Count)
                        changes.Add(new SnapshotChange(SnapshotChangeKind.Added, are[i].Subject) { Time = time, Details = details(ObjectLine.SHOWN, null, are[i].ValueOf) });
                }
            }
        }

        /// <summary>
        /// What each of some aspects was and is, for those that have something to show on either side.
        /// </summary>
        private static IReadOnlyList<SnapshotDetail> details(SnapshotAspects aspects, Func<SnapshotAspects, string?>? before, Func<SnapshotAspects, string?>? after)
        {
            var found = new List<SnapshotDetail>();

            foreach (var aspect in Enum.GetValues<SnapshotAspects>())
            {
                if (aspect == SnapshotAspects.None || !aspects.HasFlag(aspect))
                    continue;

                string? was = before?.Invoke(aspect);
                string? isNow = after?.Invoke(aspect);

                if (was != null || isNow != null)
                    found.Add(new SnapshotDetail(aspect, was, isNow));
            }

            return found;
        }

        private static string shown(double value) => value.ToString(@"0.##", CultureInfo.InvariantCulture);

        private static string? point(string? x, string? y) =>
            x != null && y != null && number(x) is double first && number(y) is double second ? $@"({shown(first)}, {shown(second)})" : null;

        private static double? number(string text) =>
            double.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : null;

        /// <summary>
        /// Whether two fields say different things: as numbers where both are, so that 100 and 100.0 are the same.
        /// </summary>
        private static bool differ(string? a, string? b)
        {
            if (a == b)
                return false;

            if (a != null && b != null && number(a) is double first && number(b) is double second)
                return first != second;

            return true;
        }

        private static string? at(string[] fields, int index) => index < fields.Length ? fields[index] : null;

        private class TimingLine
        {
            public double Time;
            public bool Red;

            private string[] fields = Array.Empty<string>();

            public static TimingLine? Parse(string line)
            {
                string[] fields = line.Split(',');

                if (fields.Length < 2 || number(fields[0]) is not double time || number(fields[1]) is not double beatLength)
                    return null;

                return new TimingLine
                {
                    Time = time,
                    // the oldest files have no field saying which it is, and there the sign of the length says it.
                    Red = fields.Length > 6 ? fields[6].Trim() == @"1" : beatLength > 0,
                    fields = fields,
                };
            }

            public SnapshotAspects DifferenceFrom(TimingLine other)
            {
                var aspects = SnapshotAspects.None;

                if (differ(at(fields, 1), at(other.fields, 1)))
                    aspects |= Red ? SnapshotAspects.Bpm : SnapshotAspects.SliderVelocity;

                if (Red && differ(at(fields, 2), at(other.fields, 2)))
                    aspects |= SnapshotAspects.Meter;

                if (differ(at(fields, 3), at(other.fields, 3)) || differ(at(fields, 4), at(other.fields, 4)))
                    aspects |= SnapshotAspects.Hitsound;

                if (differ(at(fields, 5), at(other.fields, 5)))
                    aspects |= SnapshotAspects.Volume;

                if (kiai != other.kiai)
                    aspects |= SnapshotAspects.Kiai;

                return aspects;
            }

            /// <summary>
            /// What of a line is said when it is added or removed whole.
            /// </summary>
            public const SnapshotAspects SHOWN = SnapshotAspects.Bpm | SnapshotAspects.SliderVelocity;

            /// <summary>
            /// One aspect of the line as it is listed, or null for one that has nothing to list.
            /// </summary>
            public string? ValueOf(SnapshotAspects aspect)
            {
                switch (aspect)
                {
                    case SnapshotAspects.Bpm when Red && number(fields[1]) is double beatLength && beatLength > 0:
                        return (60000 / beatLength).ToString(@"0.###", CultureInfo.InvariantCulture);

                    case SnapshotAspects.SliderVelocity when !Red && number(fields[1]) is double length && length < 0:
                        return $@"x{(-100 / length).ToString(@"0.00", CultureInfo.InvariantCulture)}";

                    case SnapshotAspects.Meter when number(at(fields, 2) ?? string.Empty) is double meter:
                        return $@"{shown(meter)}/4";

                    case SnapshotAspects.Volume when number(at(fields, 5) ?? string.Empty) is double volume:
                        return $@"{shown(volume)}%";

                    default:
                        return null;
                }
            }

            private bool kiai =>number(at(fields, 7) ?? @"0") is double effects && ((int)effects & 1) != 0;
        }

        private class ObjectLine
        {
            public string Raw = string.Empty;
            public double Time;
            public SnapshotSubject Subject;

            private int type;
            private string[] fields = Array.Empty<string>();

            public static ObjectLine? Parse(string line)
            {
                string[] fields = line.Split(',');

                if (fields.Length < 5 || number(fields[2]) is not double time || !int.TryParse(fields[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type))
                    return null;

                return new ObjectLine
                {
                    Raw = line,
                    Time = time,
                    Subject = (type & 2) != 0 ? SnapshotSubject.Slider
                        : (type & 8) != 0 ? SnapshotSubject.Spinner
                        : (type & 128) != 0 ? SnapshotSubject.HoldNote
                        : SnapshotSubject.Circle,
                    type = type,
                    fields = fields,
                };
            }

            /// <summary>
            /// What of this differs from another of the same kind at the same time.
            /// </summary>
            public SnapshotAspects DifferenceFrom(ObjectLine other)
            {
                var aspects = SnapshotAspects.None;

                if (differ(fields[0], other.fields[0]) || differ(fields[1], other.fields[1]))
                    aspects |= SnapshotAspects.Position;

                // the new combo itself, and how many colours it skips.
                if ((type & 0b1110100) != (other.type & 0b1110100))
                    aspects |= SnapshotAspects.NewCombo;

                if (differ(fields[4], other.fields[4]))
                    aspects |= SnapshotAspects.Hitsound;

                switch (Subject)
                {
                    case SnapshotSubject.Slider:
                        if (differ(at(fields, 5), at(other.fields, 5)))
                            aspects |= SnapshotAspects.Shape;

                        if (differ(at(fields, 6), at(other.fields, 6)))
                            aspects |= SnapshotAspects.Repeats;

                        if (differ(at(fields, 7), at(other.fields, 7)))
                            aspects |= SnapshotAspects.Length;

                        if (restDiffers(other, 8))
                            aspects |= SnapshotAspects.Hitsound;

                        break;

                    case SnapshotSubject.Spinner:
                        if (differ(at(fields, 5), at(other.fields, 5)))
                            aspects |= SnapshotAspects.Length;

                        if (restDiffers(other, 6))
                            aspects |= SnapshotAspects.Hitsound;

                        break;

                    case SnapshotSubject.HoldNote:
                        // where it ends and how it sounds are in one field, the end first.
                        string[] mine = (at(fields, 5) ?? string.Empty).Split(':', 2);
                        string[] theirs = (at(other.fields, 5) ?? string.Empty).Split(':', 2);

                        if (differ(mine[0], theirs[0]))
                            aspects |= SnapshotAspects.Length;

                        if (differ(at(mine, 1), at(theirs, 1)))
                            aspects |= SnapshotAspects.Hitsound;

                        break;

                    default:
                        if (restDiffers(other, 5))
                            aspects |= SnapshotAspects.Hitsound;

                        break;
                }

                return aspects;
            }

            /// <summary>
            /// What of an object is said when it is added or removed whole: where it is, and for a slider its anchors.
            /// </summary>
            public const SnapshotAspects SHOWN = SnapshotAspects.Position | SnapshotAspects.Shape;

            /// <summary>
            /// One aspect of the object as it is listed, or null for one that has nothing to list.
            /// </summary>
            public string? ValueOf(SnapshotAspects aspect)
            {
                switch (aspect)
                {
                    // a spinner is in the middle whatever its file says.
                    case SnapshotAspects.Position when Subject != SnapshotSubject.Spinner:
                        return point(fields[0], fields[1]);

                    case SnapshotAspects.Shape when Subject == SnapshotSubject.Slider:
                        return anchors(at(fields, 5));

                    case SnapshotAspects.Repeats when Subject == SnapshotSubject.Slider && number(at(fields, 6) ?? string.Empty) is double slides:
                        return shown(slides - 1);

                    case SnapshotAspects.Length when Subject == SnapshotSubject.Slider && number(at(fields, 7) ?? string.Empty) is double length:
                        return shown(length);

                    default:
                        return null;
                }
            }

            /// <summary>
            /// The anchors of a slider after its head, each kind of curve named before the anchors drawn as it.
            /// </summary>
            private static string? anchors(string? curve)
            {
                if (string.IsNullOrEmpty(curve))
                    return null;

                var parts = new List<string>();

                foreach (string token in curve.Split('|'))
                {
                    string[] coordinates = token.Split(':');

                    if (coordinates.Length == 2 && point(coordinates[0], coordinates[1]) is string anchor)
                    {
                        parts.Add(anchor);
                        continue;
                    }

                    switch (token.Length > 0 ? token[0] : ' ')
                    {
                        case 'B':
                            parts.Add(@"Bezier");
                            break;

                        case 'P':
                            parts.Add(@"Perfect");
                            break;

                        case 'L':
                            parts.Add(@"Linear");
                            break;

                        case 'C':
                            parts.Add(@"Catmull");
                            break;
                    }
                }

                return string.Join(' ', parts);
            }

            private bool restDiffers(ObjectLine other, int from) =>
                string.Join(',', fields.Skip(from)) != string.Join(',', other.fields.Skip(from));
        }
    }

    public enum SnapshotChangeKind
    {
        Added,
        Removed,
        Changed,
    }

    public enum SnapshotSubject
    {
        Circle,
        Slider,
        Spinner,
        HoldNote,
        RedLine,
        GreenLine,
        Bookmark,
        Break,

        /// <summary>
        /// A line of a section that is names and values, such as the title or the approach rate.
        /// </summary>
        Setting,

        /// <summary>
        /// A line of the events that is not a break: the background, the video, the storyboard.
        /// </summary>
        Event,

        /// <summary>
        /// A file of the set.
        /// </summary>
        File,
    }

    /// <summary>
    /// What of an object or a line was changed.
    /// </summary>
    [Flags]
    public enum SnapshotAspects
    {
        None = 0,
        Position = 1,
        NewCombo = 1 << 1,
        Hitsound = 1 << 2,
        Shape = 1 << 3,
        Repeats = 1 << 4,
        Length = 1 << 5,
        Bpm = 1 << 6,
        Meter = 1 << 7,
        SliderVelocity = 1 << 8,
        Volume = 1 << 9,
        Kiai = 1 << 10,
    }

    /// <summary>
    /// What one aspect was and is. One side is null for what was added or removed, or had nothing to show there.
    /// </summary>
    public record SnapshotDetail(SnapshotAspects Aspect, string? Before, string? After);

    /// <summary>
    /// One thing that differs between two versions.
    /// </summary>
    public class SnapshotChange
    {
        public readonly SnapshotChangeKind Kind;

        public readonly SnapshotSubject Subject;

        /// <summary>
        /// Where in the song it is, for what is at a time.
        /// </summary>
        public double? Time { get; init; }

        /// <summary>
        /// What of it was changed, for an object or a line that was.
        /// </summary>
        public SnapshotAspects Aspects { get; init; }

        /// <summary>
        /// What it is called in the file, for a setting; the line itself, for an event; the file's name, for a file.
        /// </summary>
        public string? Name { get; init; }

        /// <summary>
        /// What a setting was, where it was there before.
        /// </summary>
        public string? Before { get; init; }

        /// <summary>
        /// What a setting is, where it is still there.
        /// </summary>
        public string? After { get; init; }

        /// <summary>
        /// The values there are to show: of what was changed, before and after; of an object or a line added or
        /// removed, where it is and what it holds.
        /// </summary>
        public IReadOnlyList<SnapshotDetail> Details { get; init; } = Array.Empty<SnapshotDetail>();

        public SnapshotChange(SnapshotChangeKind kind, SnapshotSubject subject)
        {
            Kind = kind;
            Subject = subject;
        }
    }
}
