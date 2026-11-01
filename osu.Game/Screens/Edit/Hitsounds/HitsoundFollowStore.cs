// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.IO;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Which difficulty's hitsounds a difficulty always takes, kept beside it so that it goes on doing so the next time it
    /// is opened (2026-10-23).
    /// </summary>
    /// <remarks>
    /// A folder per difficulty, named by its ID (2026-11-01). It was named by the artist, title and difficulty name, so
    /// a copy of the set or another version of it took the same hitsounds, and renaming the difficulty lost them. What
    /// was kept that way is taken over by the first difficulty to look for it, and set aside. Nothing is written for a
    /// difficulty that has never had it turned on.
    /// </remarks>
    public class HitsoundFollowStore
    {
        public const string DIRECTORY = "hitsound-follow";

        private const string filename = "follow.json";

        private readonly Storage storage;

        public HitsoundFollowStore(Storage storage)
        {
            this.storage = storage;
        }

        public static string PathFor(BeatmapInfo info) => Path.Combine(DIRECTORY, info.ID.ToString("N"), filename);

        private static string pathByNamesFor(BeatmapInfo info) =>
            Path.Combine(DIRECTORY, $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]".GetValidFilename(), filename);

        /// <summary>
        /// The name of the difficulty this one always takes the hitsounds of, or null if it takes none.
        /// </summary>
        public string? Load(BeatmapInfo info)
        {
            string path = PathFor(info);

            if (!storage.Exists(path))
                return takeOverByNames(info);

            return read(path);
        }

        private string? takeOverByNames(BeatmapInfo info)
        {
            string old = pathByNamesFor(info);

            if (!storage.Exists(old))
                return null;

            string? source = read(old);

            Save(info, source);

            try
            {
                storage.Move(old, $"{old}.taken");
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not set aside which difficulty's hitsounds were taken");
            }

            return source;
        }

        private string? read(string path)
        {
            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<Kept>(reader.ReadToEnd())?.Source;
                }
            }
            catch (Exception e)
            {
                // not worth stopping the editor for; it is simply off.
                Logger.Error(e, "Could not read which difficulty's hitsounds are taken");
                return null;
            }
        }

        /// <summary>
        /// Keeps which difficulty this one always takes the hitsounds of, or null for none.
        /// </summary>
        public void Save(BeatmapInfo info, string? source)
        {
            string path = PathFor(info);

            try
            {
                if (source == null && !storage.Exists(path))
                    return;

                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(new Kept { Source = source }));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write which difficulty's hitsounds are taken");
            }
        }

        private class Kept
        {
            public string? Source { get; set; }
        }
    }
}
