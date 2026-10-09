// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using osu.Framework.Logging;
using osu.Game.Updater;

namespace osu.Game.IO
{
    /// <summary>
    /// The changelog of every distributed build, shipped beside the game.
    /// </summary>
    /// <remarks>
    /// One text file a release, written to be pasted into Discord and edited by hand before the build is made.
    /// publish.ps1 copies them into <see cref="DIRECTORY"/> beside the executable, as it does the manual, and refuses
    /// a distributed build whose own is not there. That folder is replaced with the rest of the build on every update,
    /// so it holds the changelogs as they were when the running build was made.
    /// </remarks>
    public static class LazerKaiChangelog
    {
        /// <summary>The folder beside the executable the changelogs are shipped in.</summary>
        public const string DIRECTORY = @"changelog";

        // `20261008-0.txt` is 20261008.0's. Anything else in the folder is not a release's.
        private static readonly Regex file_name = new Regex(@"^(\d{8})-(\d+)\.txt$");

        private static readonly Regex bold_line = new Regex(@"^\*\*(.+)\*\*$");

        /// <summary>
        /// Every release with a changelog in the folder under <paramref name="directory"/>, newest first.
        /// </summary>
        /// <remarks>
        /// Empty for a build that was never published: nothing copies the changelogs beside a binary built straight
        /// out of the repository.
        /// </remarks>
        public static IReadOnlyList<Release> Read(string? directory)
        {
            if (string.IsNullOrEmpty(directory))
                return Array.Empty<Release>();

            string folder = Path.Combine(directory, DIRECTORY);

            if (!Directory.Exists(folder))
                return Array.Empty<Release>();

            var releases = new List<Release>();

            foreach (string path in Directory.GetFiles(folder))
            {
                var match = file_name.Match(Path.GetFileName(path));

                if (!match.Success)
                    continue;

                string version = $@"{match.Groups[1].Value}.{match.Groups[2].Value}";

                try
                {
                    releases.Add(Parse(version, File.ReadAllText(path)));
                }
                catch (Exception e) when (e is IOException or UnauthorizedAccessException)
                {
                    // read as the game starts: one file that cannot be read is not a reason to stop there.
                    Logger.Error(e, $@"Could not read the changelog of {version}");
                }
            }

            return newestFirst(releases);
        }

        /// <summary>
        /// The releases not seen yet by someone now running <paramref name="running"/>, who last ran <paramref name="last"/>.
        /// </summary>
        /// <remarks>
        /// Every release after the last one run, up to the one running, so that a release skipped over is read too.
        /// Coming from a build that was not distributed (a portable one) there is nothing to count from, so it is only
        /// the running one's. Nothing for a first run, as osu! has it: there was no version before to have updated from.
        /// Nothing while running a build that was not distributed, which has no changelog of its own.
        /// </remarks>
        public static IReadOnlyList<Release> Unseen(IEnumerable<Release> releases, string? last, string running)
        {
            if (string.IsNullOrEmpty(last) || last == running || !LazerKaiRelease.TryReadVersion(running, out var now))
                return Array.Empty<Release>();

            bool fromRelease = LazerKaiRelease.TryReadVersion(last, out var before);

            return newestFirst(releases.Where(r =>
            {
                if (!LazerKaiRelease.TryReadVersion(r.Version, out var version))
                    return false;

                return fromRelease
                    ? version.CompareTo(before) > 0 && version.CompareTo(now) <= 0
                    : version == now;
            }));
        }

        /// <summary>
        /// Reads one changelog as it is written: a bold title, bold headings, a line per change starting with "- ", and
        /// indented lines saying more about the change above them.
        /// </summary>
        /// <remarks>
        /// The title is left out, since it only names the release, and <paramref name="version"/> does that. A line of
        /// any other shape is kept as a line of text: the earlier changelogs have paragraphs and numbered steps.
        /// </remarks>
        public static Release Parse(string version, string text)
        {
            var lines = new List<Line>();
            List<string>? details = null;
            bool first = true;

            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd();

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var bold = bold_line.Match(line);

                if (first)
                {
                    first = false;

                    if (bold.Success)
                        continue;
                }

                if (char.IsWhiteSpace(line[0]) && details != null)
                {
                    details.Add(line.Trim());
                    continue;
                }

                details = null;

                if (bold.Success)
                    lines.Add(new Line(LineKind.Heading, bold.Groups[1].Value.Trim(), Array.Empty<string>()));
                else if (line.StartsWith(@"- ", StringComparison.Ordinal))
                    lines.Add(new Line(LineKind.Change, line[2..].Trim(), details = new List<string>()));
                else
                    lines.Add(new Line(LineKind.Text, line.Trim(), Array.Empty<string>()));
            }

            return new Release(version, lines);
        }

        private static IReadOnlyList<Release> newestFirst(IEnumerable<Release> releases) =>
            releases.OrderByDescending(r => LazerKaiRelease.TryReadVersion(r.Version, out var order) ? order : default).ToArray();

        public record Release(string Version, IReadOnlyList<Line> Lines);

        /// <param name="Kind">What the line is.</param>
        /// <param name="Text">The line, without the marks that say what it is.</param>
        /// <param name="Details">For a change, the indented lines under it.</param>
        public record Line(LineKind Kind, string Text, IReadOnlyList<string> Details);

        public enum LineKind
        {
            /// <summary>A bold line, naming the kind of change below it.</summary>
            Heading,

            /// <summary>A line starting with "- ".</summary>
            Change,

            /// <summary>Any other line.</summary>
            Text,
        }
    }
}
