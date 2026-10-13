// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace osu.Game.Beatmaps.Formats
{
    /// <summary>
    /// Writes a difficulty of another beatmap set as a difficulty of the one being edited, from its file as it is kept.
    /// </summary>
    /// <remarks>
    /// The file of a difficulty says again what is the whole set's: the song, the background, and the names of the song
    /// and of the mapper. Brought into another set, a difficulty says these as that set does, or it would be played to
    /// a song that is not there. All the rest is left as it is to the byte: a file stable wrote is not what lazer would
    /// write of it, which puts the green lines where the objects are (see <see cref="HitsoundDifficultyWriter"/>).
    /// </remarks>
    public static class ImportedDifficultyWriter
    {
        private const string general = @"[General]";
        private const string metadata = @"[Metadata]";
        private const string events = @"[Events]";
        private const string timing_points = @"[TimingPoints]";
        private const string hit_objects = @"[HitObjects]";

        /// <summary>
        /// What a difficulty says that is the whole set's, by the section it is said in.
        /// </summary>
        private static readonly Dictionary<string, string[]> of_the_set = new Dictionary<string, string[]>
        {
            [general] = new[] { @"AudioFilename", @"PreviewTime" },
            [metadata] = new[] { @"Title", @"TitleUnicode", @"Artist", @"ArtistUnicode", @"Creator", @"Source", @"Tags", @"BeatmapSetID" },
        };

        /// <summary>
        /// The comment stable puts over the background and the video.
        /// </summary>
        private const string background_heading = @"//Background and Video events";

        /// <summary>
        /// <paramref name="file"/> as a difficulty of the set <paramref name="like"/> is one of.
        /// </summary>
        /// <param name="file">The file of the difficulty brought in.</param>
        /// <param name="like">The file of a difficulty of the set it is brought into.</param>
        /// <param name="name">What it is called there.</param>
        /// <param name="shift">How many custom sets on from their own its sounds are brought into the set as, where they
        /// are: every line and every object of a custom set is then of the one that many on, to sound as it did.</param>
        public static string AsOneOf(string file, string like, string name, int shift = 0)
        {
            var (said, backgrounds) = read(like);

            string newLine = file.Contains("\r\n") ? "\r\n" : "\n";
            string[] lines = file.Replace("\r\n", "\n").Split('\n');

            var written = new List<string>(lines.Length);
            var saidHere = new HashSet<string>();

            string? section = null;
            int eventsStart = 0;
            bool backgroundsWritten = false;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    endSection();

                    section = trimmed;
                    written.Add(line);

                    if (section == events)
                        eventsStart = written.Count;

                    continue;
                }

                string? key = keyOf(trimmed);

                if (key != null && section != null && of_the_set.TryGetValue(section, out string[]? keys) && keys.Contains(key))
                {
                    // as the set says it, and not at all where the set does not say it.
                    if (said.TryGetValue(key, out string? theirs) && saidHere.Add(key))
                        written.Add(theirs);

                    continue;
                }

                switch (section)
                {
                    case metadata when key == @"Version":
                        written.Add($@"Version:{name}");
                        continue;

                    // not yet submitted, as stable writes a difficulty it has just made.
                    case metadata when key == @"BeatmapID":
                        written.Add(@"BeatmapID:0");
                        continue;

                    case events when isBackground(line):
                        if (!backgroundsWritten)
                            written.AddRange(backgrounds);

                        backgroundsWritten = true;
                        continue;

                    case timing_points when shift != 0 && isData(trimmed):
                        written.Add(lineOfCustomSet(line, shift));
                        continue;

                    case hit_objects when shift != 0 && isData(trimmed):
                        written.Add(objectOfCustomSet(line, shift));
                        continue;
                }

                written.Add(line);
            }

            endSection();

            return string.Join(newLine, written);

            // puts in what the set says in this section and the file did not say at all, as an old file does not say
            // the names in the script they are written in.
            void endSection()
            {
                if (section == events && !backgroundsWritten)
                {
                    int at = eventsStart;

                    if (at < written.Count && written[at].Trim() == background_heading)
                        at++;

                    written.InsertRange(at, backgrounds);
                    backgroundsWritten = true;
                    return;
                }

                if (section == null || !of_the_set.TryGetValue(section, out string[]? keys))
                    return;

                string[] missing = keys.Where(k => said.ContainsKey(k) && saidHere.Add(k)).Select(k => said[k]).ToArray();

                // over the empty lines the section ends with.
                int end = written.Count;

                while (end > 0 && written[end - 1].Trim().Length == 0)
                    end--;

                written.InsertRange(end, missing);
            }
        }

        /// <summary>
        /// What <paramref name="like"/> says that is the whole set's: its lines by what they say, and the lines of its
        /// background and video.
        /// </summary>
        private static (Dictionary<string, string> said, List<string> backgrounds) read(string like)
        {
            var said = new Dictionary<string, string>();
            var backgrounds = new List<string>();

            string? section = null;

            foreach (string line in like.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    section = trimmed;
                    continue;
                }

                if (section == events && isBackground(line))
                    backgrounds.Add(line);

                if (section != null && of_the_set.TryGetValue(section, out string[]? keys) && keyOf(trimmed) is string key && keys.Contains(key))
                    said.TryAdd(key, line);
            }

            return (said, backgrounds);
        }

        private static bool isData(string line) => line.Length > 0 && !line.StartsWith(@"//", StringComparison.Ordinal);

        /// <summary>
        /// A red or green line as one of the custom set <paramref name="shift"/> on from its own. A line of none
        /// follows the skin, and stays as it is.
        /// </summary>
        private static string lineOfCustomSet(string line, int shift)
        {
            string[] fields = line.Split(',');

            if (fields.Length <= 4 || !shifted(ref fields[4], shift))
                return line;

            return string.Join(',', fields);
        }

        /// <summary>
        /// An object as one of the custom set <paramref name="shift"/> on from its own, where it says one of its own
        /// rather than following its line.
        /// </summary>
        private static string objectOfCustomSet(string line, int shift)
        {
            string[] fields = line.Split(',');

            if (fields.Length < 5 || !int.TryParse(fields[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int type))
                return line;

            // where each kind of object says its own sounds. A hold note of osu!mania says them after where it ends,
            // in the same field.
            bool hold = (type & 128) != 0;
            int field = hold ? 5 : (type & 2) != 0 ? 10 : (type & 8) != 0 ? 6 : 5;

            if (fields.Length <= field)
                return line;

            // the sample sets of the hit and of the additions, and then the custom set.
            string[] sounds = fields[field].Split(':');
            int index = hold ? 3 : 2;

            if (sounds.Length <= index || !shifted(ref sounds[index], shift))
                return line;

            fields[field] = string.Join(':', sounds);
            return string.Join(',', fields);
        }

        /// <summary>
        /// Makes the custom set written in <paramref name="field"/> the one <paramref name="shift"/> on.
        /// </summary>
        /// <returns>Whether there was one written there.</returns>
        private static bool shifted(ref string field, int shift)
        {
            if (!int.TryParse(field.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int index) || index < 1)
                return false;

            field = (index + shift).ToString(CultureInfo.InvariantCulture);
            return true;
        }

        /// <summary>
        /// What a line of a section of names and values is the value of.
        /// </summary>
        private static string? keyOf(string line)
        {
            int colon = line.IndexOf(':');

            return colon <= 0 ? null : line[..colon].Trim();
        }

        /// <summary>
        /// Whether a line of the events is the background or the video.
        /// </summary>
        private static bool isBackground(string line)
        {
            // what a sprite of the storyboard does is set in under it.
            if (line.Length == 0 || line[0] == ' ' || line[0] == '_')
                return false;

            string kind = line.Split(',')[0].Trim();

            return kind == @"0" || kind == @"1"
                                || kind.Equals(@"Background", StringComparison.OrdinalIgnoreCase)
                                || kind.Equals(@"Video", StringComparison.OrdinalIgnoreCase);
        }
    }
}
