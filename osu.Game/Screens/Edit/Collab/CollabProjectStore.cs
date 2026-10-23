// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Extensions;
using osu.Game.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.IO;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// Where a collaboration's split is kept.
    /// </summary>
    /// <remarks>
    /// A folder per difficulty, named the way the backups and the combo colour layouts are, so the three sit side by
    /// side. Nothing here reaches the beatmap itself — the `.osu` has nowhere to put it, which is also why the split
    /// is shared as text rather than travelling with the file.
    /// </remarks>
    public class CollabProjectStore
    {
        public const string DIRECTORY = "collab";

        private const string filename = "project.json";

        private readonly Storage storage;

        /// <summary>
        /// Files that could not be read and are still where they were, which a save would write over.
        /// </summary>
        private readonly HashSet<string> stuck = new HashSet<string>();

        public CollabProjectStore(Storage storage)
        {
            this.storage = storage;
        }

        public static string FolderNameFor(EditorBeatmap beatmap)
        {
            var info = beatmap.BeatmapInfo;

            return $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]".GetValidFilename();
        }

        /// <summary>
        /// How the beatmap names itself at the top of a shared split.
        /// </summary>
        public static string BeatmapNameFor(EditorBeatmap beatmap)
        {
            var info = beatmap.BeatmapInfo;

            return $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]";
        }

        public CollabProject Load(EditorBeatmap beatmap)
        {
            string path = Path.Combine(DIRECTORY, FolderNameFor(beatmap), filename);

            if (!storage.Exists(path))
                return new CollabProject();

            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return new CollabProject();

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<CollabProject>(reader.ReadToEnd()) ?? new CollabProject();
                }
            }
            catch (Exception e)
            {
                // a split that cannot be read is not worth stopping the editor for; start from an empty one.
                Logger.Error(e, "Could not read the collaboration split");

                if (!UnreadableFile.SetAside(storage, path))
                    stuck.Add(path);

                return new CollabProject();
            }
        }

        public void Save(EditorBeatmap beatmap, CollabProject project)
        {
            string path = Path.Combine(DIRECTORY, FolderNameFor(beatmap), filename);

            try
            {
                if (stuck.Contains(path))
                    return;

                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(project, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the collaboration split");
            }
        }
    }
}
