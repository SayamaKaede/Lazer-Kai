// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Edit.Snapshots
{
    /// <summary>
    /// Keeps a difficulty's file and the list of its set's files as they were at the end of each day they changed on,
    /// so that what changed on a day can be listed (2026-11-10 request).
    /// </summary>
    /// <remarks>
    /// Apart from the backups, which are there to put back what was lost and are deleted from the oldest once there
    /// are more than were asked for: these are one a day, compressed, and kept for as long as the set says.
    ///
    /// A day's file is how things were the last time that day they were looked at: at a save, or as the editor opened
    /// the difficulty, which is where an update from the website or a change made outside the game is noticed. What it
    /// is compared with is the file before it: the day before's, or, on the first day, the file as it first was.
    ///
    /// Only for the sets it has been turned on for (2026-11-11): every set of a collection would be a lot kept for
    /// little. The mapper's own are turned on as they are opened, and others from the panel.
    ///
    /// A set and its difficulties are filed under IDs of this store's own, found by either of the game's two: the one
    /// the game gives, which an update from the website replaces, and the website's, which a set has only once it is
    /// uploaded. Filed under either alone, the history would stop at an update or at the first upload.
    /// </remarks>
    public class BeatmapSnapshotStore
    {
        /// <summary>
        /// Directory snapshots are written under, relative to the game's storage.
        /// </summary>
        public const string DIRECTORY = "snapshots";

        /// <summary>
        /// The most days a set can be told to keep, short of all of them.
        /// </summary>
        public const int MAX_KEEP_DAYS = 365;

        private const string index_file = @"index.json";
        private const string settings_file = @"settings.json";

        private const string day_format = @"yyyyMMdd";

        /// <summary>
        /// What a file is called after its day when it is how things were before anything was recorded.
        /// </summary>
        private const string first_suffix = @"-0";

        /// <summary>
        /// What a file is called after its day when it is how things were at the end of that day.
        /// </summary>
        private const string day_suffix = @"-1";

        private const string difficulty_extension = @".osu.gz";
        private const string set_extension = @".json.gz";

        /// <summary>
        /// The day it is. A test's to change.
        /// </summary>
        public Func<DateTime> Today = () => DateTime.Now.Date;

        private readonly Storage storage;

        /// <summary>
        /// The store's own ID for each ID of the game's and of the website's it has seen.
        /// </summary>
        private Dictionary<string, string>? index;

        public BeatmapSnapshotStore(Storage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// Whether snapshots are kept for a difficulty's set.
        /// </summary>
        public bool IsEnabled(BeatmapInfo beatmap) => SettingsOf(beatmap).Enabled == true;

        /// <summary>
        /// What was chosen for a difficulty's set, or what is so until something is.
        /// </summary>
        public SnapshotSettings SettingsOf(BeatmapInfo beatmap)
        {
            try
            {
                if (directoryOf(beatmap.BeatmapSet, false) is string directory && storage.Exists(Path.Combine(directory, settings_file)))
                {
                    using (var stream = storage.GetStream(Path.Combine(directory, settings_file)))
                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<SnapshotSettings>(reader.ReadToEnd()) ?? new SnapshotSettings();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read what was chosen for a beatmap's snapshots");
            }

            return new SnapshotSettings();
        }

        public void SetEnabled(BeatmapInfo beatmap, bool enabled) => change(beatmap, s => s.Enabled = enabled);

        /// <param name="beatmap">A difficulty of the set.</param>
        /// <param name="days">How many days back snapshots are kept; zero for all of them.</param>
        public void SetKeepDays(BeatmapInfo beatmap, int days) => change(beatmap, s => s.KeepDays = Math.Clamp(days, 0, MAX_KEEP_DAYS));

        /// <summary>
        /// Turns snapshots on for a set of the mapper's own, unless they have been turned on or off for it already.
        /// </summary>
        /// <remarks>
        /// Written down rather than worked out each time, so that it stays on while logged out, when whose it is cannot
        /// be told.
        /// </remarks>
        public void EnableAsOwn(BeatmapInfo beatmap)
        {
            if (SettingsOf(beatmap).Enabled == null)
                SetEnabled(beatmap, true);
        }

        private void change(BeatmapInfo beatmap, Action<SnapshotSettings> change)
        {
            try
            {
                var settings = SettingsOf(beatmap);
                change(settings);

                using (var stream = storage.CreateFileSafely(Path.Combine(directoryOf(beatmap.BeatmapSet, true)!, settings_file)))
                    stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(settings, Formatting.Indented)));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write what was chosen for a beatmap's snapshots");
            }
        }

        /// <summary>
        /// Keeps how things are as the editor opens a difficulty: as the day's where it is not what was kept last, which
        /// is an update from the website or a change made outside the game, and as what the first day is compared with
        /// where nothing has been kept yet.
        /// </summary>
        /// <param name="beatmap">The difficulty.</param>
        /// <param name="file">Its file as it is stored, or null where it has none yet.</param>
        /// <param name="setFiles">The hash of every file of its set by its name.</param>
        public void RecordOpened(BeatmapInfo beatmap, byte[]? file, IReadOnlyDictionary<string, string> setFiles) => record(beatmap, file, setFiles, true);

        /// <summary>
        /// Keeps how things are after a save as the day's, in place of what was kept of the day before it.
        /// </summary>
        public void RecordSaved(BeatmapInfo beatmap, byte[]? file, IReadOnlyDictionary<string, string> setFiles) => record(beatmap, file, setFiles, false);

        private void record(BeatmapInfo beatmap, byte[]? file, IReadOnlyDictionary<string, string> setFiles, bool opened)
        {
            try
            {
                var settings = SettingsOf(beatmap);

                if (settings.Enabled != true)
                    return;

                if (file != null)
                    record(directoryOf(beatmap, true)!, difficulty_extension, file, opened, settings.KeepDays);

                if (setFiles.Count > 0)
                    record(directoryOf(beatmap.BeatmapSet, true)!, set_extension, encode(setFiles), opened, settings.KeepDays);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not keep a snapshot of the beatmap");
            }
        }

        private void record(string directory, string extension, byte[] content, bool opened, int keepDays)
        {
            string today = Today().ToString(day_format, CultureInfo.InvariantCulture);
            string? latest = namesIn(directory, extension).LastOrDefault();

            if (latest == null && opened)
            {
                write(directory, today + first_suffix + extension, content);
                return;
            }

            string todays = today + day_suffix + extension;

            // nothing changed since the last day something was kept: the day is left out rather than listed empty. A
            // save puts what it wrote in place of the day's own all the same, which may be back to how the day began.
            if (latest != null && (opened || latest != todays) && readFile(directory, latest).AsSpan().SequenceEqual(content))
                return;

            write(directory, todays, content);
            prune(directory, extension, keepDays);
        }

        /// <summary>
        /// Deletes what was kept longer ago than the set keeps, but for the latest of it, which the earliest day still
        /// kept is compared with.
        /// </summary>
        private void prune(string directory, string extension, int keepDays)
        {
            if (keepDays <= 0)
                return;

            string cutoff = Today().AddDays(-keepDays).ToString(day_format, CultureInfo.InvariantCulture);
            var old = namesIn(directory, extension).Where(n => string.CompareOrdinal(n[..day_format.Length], cutoff) < 0).ToList();

            foreach (string name in old.SkipLast(1))
                storage.Delete(Path.Combine(directory, name));
        }

        /// <summary>
        /// The days something was kept for a difficulty or its set, the latest first.
        /// </summary>
        public IReadOnlyList<DateTime> DaysOf(BeatmapInfo beatmap) =>
            daysIn(directoryOf(beatmap, false), difficulty_extension)
                .Union(daysIn(directoryOf(beatmap.BeatmapSet, false), set_extension))
                .OrderByDescending(d => d)
                .ToList();

        /// <summary>
        /// A difficulty's file at the end of a day, and the one before it that it is compared with.
        /// </summary>
        /// <returns>Null where nothing of the difficulty changed that day; a null <c>before</c> where nothing is earlier.</returns>
        public (string? before, string after)? DifficultyOn(BeatmapInfo beatmap, DateTime day)
        {
            if (read(directoryOf(beatmap, false), difficulty_extension, day) is not (var before, var after))
                return null;

            return (before == null ? null : Encoding.UTF8.GetString(before), Encoding.UTF8.GetString(after));
        }

        /// <summary>
        /// The files of a difficulty's set at the end of a day, and those before that they are compared with.
        /// </summary>
        /// <returns>Null where nothing of the set changed that day; a null <c>before</c> where nothing is earlier.</returns>
        public (IReadOnlyDictionary<string, string>? before, IReadOnlyDictionary<string, string> after)? SetOn(BeatmapInfo beatmap, DateTime day)
        {
            if (read(directoryOf(beatmap.BeatmapSet, false), set_extension, day) is not (var before, var after))
                return null;

            return (before == null ? null : decode(before), decode(after));
        }

        private (byte[]? before, byte[] after)? read(string? directory, string extension, DateTime day)
        {
            if (directory == null)
                return null;

            var names = namesIn(directory, extension);
            int index = names.IndexOf(day.ToString(day_format, CultureInfo.InvariantCulture) + day_suffix + extension);

            if (index < 0)
                return null;

            return (index == 0 ? null : readFile(directory, names[index - 1]), readFile(directory, names[index]));
        }

        private IEnumerable<DateTime> daysIn(string? directory, string extension)
        {
            if (directory == null)
                yield break;

            foreach (string name in namesIn(directory, extension))
            {
                string stem = name[..^extension.Length];

                if (stem.EndsWith(day_suffix, StringComparison.Ordinal)
                    && DateTime.TryParseExact(stem[..^day_suffix.Length], day_format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var day))
                    yield return day;
            }
        }

        /// <summary>
        /// The files kept in a directory, the earliest first: their names are their days.
        /// </summary>
        private List<string> namesIn(string directory, string extension)
        {
            if (!storage.ExistsDirectory(directory))
                return new List<string>();

            return storage.GetFiles(directory, $@"*{extension}").Select(Path.GetFileName).OfType<string>().OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        private byte[] readFile(string directory, string name)
        {
            using (var stream = storage.GetStream(Path.Combine(directory, name)))
            using (var unzipped = new GZipStream(stream, CompressionMode.Decompress))
            using (var content = new MemoryStream())
            {
                unzipped.CopyTo(content);
                return content.ToArray();
            }
        }

        private void write(string directory, string name, byte[] content)
        {
            using (var stream = storage.CreateFileSafely(Path.Combine(directory, name)))
            using (var zipped = new GZipStream(stream, CompressionLevel.Optimal))
                zipped.Write(content);
        }

        private static byte[] encode(IReadOnlyDictionary<string, string> setFiles) =>
            Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new SortedDictionary<string, string>(setFiles.ToDictionary(f => f.Key, f => f.Value), StringComparer.Ordinal), Formatting.Indented));

        private static IReadOnlyDictionary<string, string> decode(byte[] content) =>
            JsonConvert.DeserializeObject<Dictionary<string, string>>(Encoding.UTF8.GetString(content)) ?? new Dictionary<string, string>();

        private string? directoryOf(BeatmapInfo beatmap, bool create)
        {
            string? set = directoryOf(beatmap.BeatmapSet, create);
            string? own = idOf(@"map", beatmap.ID, beatmap.OnlineID, create);

            return set == null || own == null ? null : Path.Combine(set, own);
        }

        private string? directoryOf(BeatmapSetInfo? set, bool create) =>
            idOf(@"set", set?.ID ?? Guid.Empty, set?.OnlineID ?? -1, create) is string own ? Path.Combine(DIRECTORY, own) : null;

        /// <summary>
        /// The store's own ID for a set or a difficulty, found by the website's ID before the game's: after an update
        /// from the website the game's is a new one, and the website's is the one that has not changed.
        /// </summary>
        /// <param name="kind">Which of the two it is, which have IDs of their own on the website.</param>
        /// <param name="id">The game's ID.</param>
        /// <param name="onlineId">The website's ID, or not above zero where it has none.</param>
        /// <param name="create">Whether one is made where there is none; otherwise null is returned.</param>
        private string? idOf(string kind, Guid id, int onlineId, bool create)
        {
            index ??= readIndex();

            var keys = new List<string>();

            if (onlineId > 0)
                keys.Add($@"{kind}:online:{onlineId.ToString(CultureInfo.InvariantCulture)}");

            keys.Add($@"{kind}:game:{id:N}");

            string? own = keys.Select(k => index.GetValueOrDefault(k)).FirstOrDefault(found => found != null);

            if (own == null && !create)
                return null;

            own ??= Guid.NewGuid().ToString(@"N");

            // every ID it goes by is written down: the one it is found by next may be the other.
            if (keys.Any(k => index.GetValueOrDefault(k) != own))
            {
                foreach (string key in keys)
                    index[key] = own;

                using (var stream = storage.CreateFileSafely(Path.Combine(DIRECTORY, index_file)))
                    stream.Write(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(index, Formatting.Indented)));
            }

            return own;
        }

        private Dictionary<string, string> readIndex()
        {
            try
            {
                string path = Path.Combine(DIRECTORY, index_file);

                if (storage.Exists(path))
                {
                    using (var stream = storage.GetStream(path))
                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd()) ?? new Dictionary<string, string>();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read which snapshots are which beatmap's");
            }

            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// What was chosen for the snapshots of one set.
    /// </summary>
    public class SnapshotSettings
    {
        /// <summary>
        /// Whether snapshots are kept; null until it is chosen, or the set is found to be the mapper's own.
        /// </summary>
        public bool? Enabled { get; set; }

        /// <summary>
        /// How many days back snapshots are kept; zero for all of them.
        /// </summary>
        public int KeepDays { get; set; }
    }
}
