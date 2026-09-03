// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// The share format: who is taking which stretch, as text that can be pasted into a chat.
    /// </summary>
    /// <remarks>
    /// Readable on its own, so the person receiving it can check the split without opening anything. The timestamps
    /// are written the way the editor writes them everywhere else, which is also the way they are quoted in a modding
    /// post, so they can be pasted straight back into a jump box.
    ///
    /// A line is one bookmark and the name of whoever takes the beatmap from there. The stretch before the first
    /// bookmark has no bookmark of its own, so it is written against the beatmap's own start.
    ///
    /// The timestamp is written the way a beatmap discussion writes one -- minutes, seconds and milliseconds all
    /// separated by colons -- so that a line pasted into a discussion becomes a link to the moment it names.
    /// </remarks>
    public static class CollabClipboard
    {
        /// <summary>The line ending the share format uses, written as a code so no editor can convert it.</summary>
        private const char NEWLINE = (char)10;

        public static string Write(string beatmapName, IEnumerable<CollabBlock> blocks, IReadOnlyDictionary<int, string> assignment)
        {
            var text = new StringBuilder();

            // a plain newline rather than the platform's: this text is pasted between people, and a split written on
            // one machine has to read the same on another.
            text.Append(beatmapName).Append(NEWLINE);

            foreach (var block in blocks)
            {
                if (!assignment.TryGetValue(block.Key, out string? name) || string.IsNullOrWhiteSpace(name))
                    continue;

                text.Append($"{Timestamp(block.Start)} - {name}").Append(NEWLINE);
            }

            return text.ToString().TrimEnd();
        }

        /// <summary>
        /// Reads back what <see cref="Write"/> produced, keyed the way an assignment is.
        /// </summary>
        /// <remarks>
        /// Lines that are not a timestamp and a name are skipped rather than refused: the first line names the
        /// beatmap, and text pasted out of a chat tends to arrive with something else stuck to it.
        /// </remarks>
        public static Dictionary<int, string> Read(string text, IEnumerable<CollabBlock> blocks)
        {
            var starts = blocks.ToDictionary(b => (int)Math.Round(b.Start), b => b.Key);
            var assignment = new Dictionary<int, string>();

            foreach (string line in text.Split('\n'))
            {
                string trimmed = line.Trim();
                int space = trimmed.IndexOf(' ');

                if (space <= 0)
                    continue;

                if (!TryReadTimestamp(trimmed[..space], out int time))
                    continue;

                string name = trimmed[(space + 1)..].Trim();

                // the dash between the moment and the name is how a discussion separates a timestamp from what is
                // being said about it. A name of its own may well contain one, so only the leading one comes off.
                if (name.StartsWith("- ", StringComparison.Ordinal))
                    name = name[2..].Trim();

                if (name.Length == 0)
                    continue;

                // a timestamp naming no block is a split of a beatmap whose bookmarks have moved since, or of another
                // beatmap altogether. Nothing sensible can be done with it.
                if (starts.TryGetValue(time, out int key))
                    assignment[key] = name;
            }

            return assignment;
        }

        /// <summary>
        /// A moment as a beatmap discussion writes it: <c>mm:ss:fff</c>, minutes padded so the timestamps in a
        /// pasted split line up under each other.
        /// </summary>
        public static string Timestamp(double milliseconds)
        {
            int total = (int)Math.Round(Math.Max(milliseconds, 0), MidpointRounding.AwayFromZero);

            return $"{total / 60_000:00}:{total / 1_000 % 60:00}:{total % 1_000:000}";
        }

        /// <summary>
        /// Reads <c>m:ss:fff</c>, with the minutes counting past sixty the way the editor writes them.
        /// </summary>
        public static bool TryReadTimestamp(string text, out int milliseconds)
        {
            milliseconds = 0;

            string[] parts = text.Split(':');

            if (parts.Length != 3)
                return false;

            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int m)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int s)
                || !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out int ms))
            {
                return false;
            }

            milliseconds = (m * 60 + s) * 1000 + ms;
            return true;
        }
    }
}
