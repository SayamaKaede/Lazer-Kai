// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Hands the beatmap being edited externally to Mapping Tools, opened rather than waiting to be dragged in.
    /// </summary>
    /// <remarks>
    /// Mapping Tools takes no file on its command line — its `App.xaml.cs` reads no arguments at all, and the
    /// only way in through the window is a drag. What it does do is read the top of its own recent-maps list at
    /// startup and treat that as the map it is working on (`SettingsManager.GetLatestCurrentMaps`, which takes
    /// the first entry and splits it on a pipe). So the beatmap is put there, and then it is started.
    ///
    /// The catch is the same fact from the other side: the list is read **only** at startup and written back on
    /// exit. Writing it while Mapping Tools is open does nothing and is then overwritten, so an already-running
    /// copy has to be refused rather than quietly handed a map it will never show.
    /// </remarks>
    public static class MappingToolsHandoff
    {
        public const string EXECUTABLE_NAME = @"Mapping Tools.exe";

        private const string process_name = @"Mapping Tools";

        /// <summary>
        /// What Mapping Tools calls itself in its file details, used to tell it apart from anything else a path
        /// could be pointed at.
        /// </summary>
        private const string product_name = @"Mapping Tools";

        /// <summary>
        /// How many entries Mapping Tools keeps, matched so that it does not have to trim a list it was handed.
        /// </summary>
        private const int recent_map_limit = 20;

        /// <summary>
        /// The shape Mapping Tools writes its own dates in. Only shown in its dropdown — the path beside it is
        /// what is acted on — but a date in another shape would read as the wrong day.
        /// </summary>
        private const string date_format = @"yyyy/MM/dd H:mm:ss";

        public static string ConfigPath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), process_name, @"config.json");

        /// <summary>
        /// Where the installer puts it, used when nothing has been set in the options.
        /// </summary>
        public static string DefaultExecutablePath => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), process_name, EXECUTABLE_NAME);

        /// <summary>
        /// Whether a file is Mapping Tools, rather than merely being where one was said to be.
        /// </summary>
        /// <remarks>
        /// Worth checking because of what is done just before starting it: another program's settings file is
        /// written to. Pointing this at something that is not Mapping Tools would edit Mapping Tools' settings
        /// and then run the something else, which is a strange enough pair of things to do that it should not be
        /// possible by typing a path wrong.
        ///
        /// Read out of the file rather than off its name, so that a copy sitting anywhere — the installer's
        /// folder, a portable build unzipped somewhere — answers the same. Both report a product name of
        /// "Mapping Tools"; their versions do not agree, so the version is no use for this.
        /// </remarks>
        public static bool IsMappingTools(string path)
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return false;

            try
            {
                return string.Equals(FileVersionInfo.GetVersionInfo(path).ProductName?.Trim(), product_name, StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // anything unreadable is answered for the same way as anything that is not it.
                return false;
            }
        }

        /// <summary>
        /// The executable to start: what was set in the options, or the usual install if nothing was.
        /// </summary>
        /// <remarks>
        /// A path that was set and is wrong answers with nothing rather than falling back. Falling back would
        /// start a different copy of Mapping Tools than the one that was asked for, and say nothing about the
        /// path being wrong — the one case where somebody has told us exactly what they wanted is the last case
        /// to go quietly looking elsewhere.
        /// </remarks>
        public static string? FindExecutable(string configured)
        {
            if (!string.IsNullOrWhiteSpace(configured))
                return IsMappingTools(configured) ? configured : null;

            return IsMappingTools(DefaultExecutablePath) ? DefaultExecutablePath : null;
        }

        public static bool IsRunning() => Process.GetProcessesByName(process_name).Length > 0;

        /// <summary>
        /// Opens Mapping Tools on the given beatmap.
        /// </summary>
        public static void Open(string executablePath, string beatmapPath)
        {
            string configPath = ConfigPath;

            // no config yet means Mapping Tools has never been run, and it writes its own on first start. It is
            // still worth starting: the map has to be dragged in that once, which is where everybody began.
            if (File.Exists(configPath))
                File.WriteAllText(configPath, PutAtTopOfRecentMaps(File.ReadAllText(configPath), beatmapPath, DateTime.Now));

            Process.Start(new ProcessStartInfo(executablePath)
            {
                UseShellExecute = true,
                WorkingDirectory = Path.GetDirectoryName(executablePath)!,
            });
        }

        /// <summary>
        /// Puts a beatmap at the top of Mapping Tools' recent list, which is where it takes its current map from.
        /// </summary>
        /// <remarks>
        /// Everything else in the file is carried across untouched. It is another program's settings — window
        /// size, the osu! install it was pointed at, which tools were starred — and the one thing wanted here is
        /// no reason to rewrite any of it.
        /// </remarks>
        public static string PutAtTopOfRecentMaps(string configJson, string beatmapPath, DateTime now)
        {
            var config = JObject.Parse(configJson);

            var recent = config[@"RecentMaps"] as JArray ?? new JArray();

            // the same entry is moved rather than repeated, which is what Mapping Tools does to its own list.
            // Compared whole: several maps at once are held as one entry with the paths joined by a pipe.
            for (int i = recent.Count - 1; i >= 0; i--)
            {
                if (recent[i] is JArray entry && entry.Count > 0
                                              && string.Equals((string?)entry[0], beatmapPath, StringComparison.OrdinalIgnoreCase))
                {
                    recent.RemoveAt(i);
                }
            }

            recent.Insert(0, new JArray(beatmapPath, now.ToString(date_format, CultureInfo.InvariantCulture)));

            while (recent.Count > recent_map_limit)
                recent.RemoveAt(recent.Count - 1);

            config[@"RecentMaps"] = recent;

            return config.ToString(Formatting.Indented);
        }
    }
}
