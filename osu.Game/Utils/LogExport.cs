// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Platform;

namespace osu.Game.Utils
{
    /// <summary>
    /// Copies the logs somewhere they can be handed over, with the account name taken out of them.
    /// </summary>
    /// <remarks>
    /// This fork is given out to people who cannot send a crash report automatically, so when something breaks
    /// the only way to find out what happened is for them to send their logs. Their logs name them: the folder
    /// the game runs from is written out once per launch, and it very often sits under their account.
    ///
    /// Masking as they are copied rather than as they are written: what a log will say cannot all be known in
    /// advance — an exception message quotes whatever path it failed on — so the last thing before the file
    /// leaves the machine is the only place the whole of it can be seen at once.
    ///
    /// The originals are not touched. Somebody diagnosing their own problem needs the real paths.
    /// </remarks>
    public static class LogExport
    {
        /// <summary>
        /// Where the copies go, beside the game's own data so that it is findable again without being asked for.
        /// </summary>
        public const string DESTINATION_DIRECTORY = @"logs-to-send";

        /// <summary>
        /// The log files that are worth sending, in the order they are most likely to be read.
        /// </summary>
        /// <remarks>
        /// The network log is left out. It is by far the largest, it is a record of what somebody was browsing,
        /// and it has never been what a crash was hiding in.
        /// </remarks>
        private static readonly string[] wanted = { @"runtime", @"database", @"performance", @"updater" };

        public static IEnumerable<string> Sanitise(IEnumerable<string> lines) => lines.Select(PathDisplay.MaskIn);

        /// <summary>
        /// Writes the copies into the game's own folder, and answers with where they went and how many there are.
        /// </summary>
        /// <remarks>
        /// Reached from two places — the button in the options, and clicking an error notification — and where
        /// the copies land has to be the same both times, or the answer to "where is that folder" depends on
        /// which way somebody got there.
        /// </remarks>
        public static (string Destination, int Written) WriteForSending(Storage dataStorage, Storage logStorage, int sessions = 3)
        {
            string destination = dataStorage.GetFullPath(DESTINATION_DIRECTORY, true);

            return (destination, Write(logStorage.GetFullPath(string.Empty), destination, sessions));
        }

        /// <summary>
        /// Whether a log file is one of the kinds that gets copied.
        /// </summary>
        public static bool ShouldExport(string fileName)
        {
            // named "<session>.<kind>.log" by the framework.
            string[] parts = Path.GetFileName(fileName).Split('.');

            return parts.Length >= 3
                   && parts[^1].Equals(@"log", StringComparison.OrdinalIgnoreCase)
                   && wanted.Contains(parts[^2], StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Copies the most recent logs into <paramref name="destination"/> with names taken out, and answers
        /// with how many files were written.
        /// </summary>
        /// <param name="source">The folder the game writes its logs to.</param>
        /// <param name="destination">Where the copies go. Created if it is not there, emptied if it is.</param>
        /// <param name="sessions">How many launches back to include.</param>
        public static int Write(string source, string destination, int sessions = 3)
        {
            if (Directory.Exists(destination))
            {
                foreach (string stale in Directory.GetFiles(destination))
                    File.Delete(stale);
            }

            Directory.CreateDirectory(destination);

            var files = Directory.GetFiles(source, @"*.log").Where(ShouldExport).ToList();

            // the framework starts a new set of files each launch and names them after the session, so the most
            // recent launches are the newest few sessions rather than the newest few files.
            var recent = files.Select(f => Path.GetFileName(f).Split('.')[0])
                              .Distinct()
                              .OrderByDescending(s => s, StringComparer.Ordinal)
                              .Take(sessions)
                              .ToHashSet(StringComparer.Ordinal);

            int written = 0;

            foreach (string file in files)
            {
                if (!recent.Contains(Path.GetFileName(file).Split('.')[0]))
                    continue;

                File.WriteAllLines(Path.Combine(destination, Path.GetFileName(file)), Sanitise(File.ReadLines(file)));
                written++;
            }

            return written;
        }
    }
}
