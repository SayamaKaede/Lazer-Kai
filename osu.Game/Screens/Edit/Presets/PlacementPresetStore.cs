// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// Keeps the placement presets on disk, beside the game's other working files.
    /// </summary>
    /// <remarks>
    /// One file for the presets shared by every beatmap, and a folder per set for the ones belonging to a single
    /// song. A set rather than a difficulty: a shape is drawn once and used across the whole spread, unlike the
    /// combo colours and the collab assignments next door, which are a difficulty's own.
    /// </remarks>
    public class PlacementPresetStore
    {
        public const string DIRECTORY = "placement-presets";

        private const string global_filename = "global.json";
        private const string filename = "presets.json";

        private readonly Storage storage;

        public PlacementPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// The folder a set's presets are kept in, relative to <see cref="DIRECTORY"/>.
        /// </summary>
        public static string FolderNameFor(EditorBeatmap beatmap)
        {
            var metadata = beatmap.BeatmapInfo.Metadata;

            return $"{metadata.Artist} - {metadata.Title}".GetValidFilename();
        }

        private static string? beatmapSetIdFor(EditorBeatmap beatmap) => beatmap.BeatmapInfo.BeatmapSet?.ID.ToString();

        public PlacementPresetList LoadGlobal() => read(Path.Combine(DIRECTORY, global_filename)) ?? new PlacementPresetList();

        public void SaveGlobal(PlacementPresetList list) => write(Path.Combine(DIRECTORY, global_filename), list);

        /// <summary>
        /// Reads the presets belonging to the given beatmap's set.
        /// </summary>
        /// <remarks>
        /// Found by folder name first, and by the set's id second: editing the artist or title changes what the
        /// folder would be called, and the presets are still the same set's.
        /// </remarks>
        public PlacementPresetList LoadForBeatmap(EditorBeatmap beatmap)
        {
            string folder = FolderNameFor(beatmap);

            var byName = read(Path.Combine(DIRECTORY, folder, filename));

            if (byName != null)
            {
                byName.Folder = folder;
                return byName;
            }

            string? id = beatmapSetIdFor(beatmap);

            if (id != null)
            {
                var found = findById(id);

                if (found != null)
                    return found;
            }

            return new PlacementPresetList { BeatmapSetId = id, Folder = folder };
        }

        public void SaveForBeatmap(EditorBeatmap beatmap, PlacementPresetList list)
        {
            list.BeatmapSetId ??= beatmapSetIdFor(beatmap);
            list.Folder ??= FolderNameFor(beatmap);

            write(Path.Combine(DIRECTORY, list.Folder, filename), list);
        }

        private PlacementPresetList? findById(string id)
        {
            if (!storage.ExistsDirectory(DIRECTORY))
                return null;

            foreach (string directory in storage.GetDirectories(DIRECTORY))
            {
                string? folder = Path.GetFileName(directory);

                if (string.IsNullOrEmpty(folder))
                    continue;

                var list = read(Path.Combine(DIRECTORY, folder, filename));

                if (list?.BeatmapSetId != id)
                    continue;

                list.Folder = folder;
                return list;
            }

            return null;
        }

        private PlacementPresetList? read(string path)
        {
            if (!storage.Exists(path))
                return null;

            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<PlacementPresetList>(reader.ReadToEnd());
                }
            }
            catch (Exception e)
            {
                // a list that cannot be read is not worth stopping the editor for; carry on without it.
                Logger.Error(e, "Could not read the placement presets");
                return null;
            }
        }

        private void write(string path, PlacementPresetList list)
        {
            try
            {
                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the placement presets");
            }
        }

        /// <summary>
        /// The sets which have presets saved, for reading the store by hand.
        /// </summary>
        public string[] SavedSets()
        {
            if (!storage.ExistsDirectory(DIRECTORY))
                return Array.Empty<string>();

            return storage.GetDirectories(DIRECTORY)
                          .Select(Path.GetFileName)
                          .Where(name => !string.IsNullOrEmpty(name))
                          .OrderBy(name => name)
                          .ToArray()!;
        }
    }
}
