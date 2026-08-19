// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit.ComboColours
{
    /// <summary>
    /// Keeps each difficulty's colour points on disk, beside the game's other working files.
    /// </summary>
    /// <remarks>
    /// A folder per difficulty, named the way the backups are, so the two sit side by side and a layout can be found by
    /// hand. Nothing here reaches the beatmap itself — the `.osu` has nowhere to put it.
    /// </remarks>
    public class ComboColourProjectStore
    {
        public const string DIRECTORY = "combo-colours";

        private const string filename = "project.json";

        private readonly Storage storage;

        public ComboColourProjectStore(Storage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// The folder a difficulty's layout is kept in, relative to <see cref="DIRECTORY"/>.
        /// </summary>
        public static string FolderNameFor(EditorBeatmap beatmap)
        {
            var info = beatmap.BeatmapInfo;

            return $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]".GetValidFilename();
        }

        public ComboColourProject Load(EditorBeatmap beatmap) => Load(FolderNameFor(beatmap));

        public ComboColourProject Load(string folderName)
        {
            string path = Path.Combine(DIRECTORY, folderName, filename);

            if (!storage.Exists(path))
                return new ComboColourProject();

            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return new ComboColourProject();

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<ComboColourProject>(reader.ReadToEnd()) ?? new ComboColourProject();
                }
            }
            catch (Exception e)
            {
                // a layout that cannot be read is not worth stopping the editor for; start from an empty one.
                Logger.Error(e, "Could not read the combo colour layout");
                return new ComboColourProject();
            }
        }

        public void Save(EditorBeatmap beatmap, ComboColourProject project)
        {
            string path = Path.Combine(DIRECTORY, FolderNameFor(beatmap), filename);

            try
            {
                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(project, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the combo colour layout");
            }
        }

        /// <summary>
        /// The difficulties which have a layout saved, for picking one to copy from.
        /// </summary>
        public string[] SavedLayouts()
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
