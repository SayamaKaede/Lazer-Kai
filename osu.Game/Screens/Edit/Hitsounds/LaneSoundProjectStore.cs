// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Keeps the sounds of each difficulty's lanes on disk, beside the game's other working files.
    /// </summary>
    /// <remarks>
    /// A folder per difficulty, named the way the backups and the combo colour layouts are. Having one is what makes a
    /// difficulty one made to hitsound in: any other osu!mania difficulty is left as it is.
    /// </remarks>
    public class LaneSoundProjectStore
    {
        public const string DIRECTORY = "hitsound-lanes";

        private const string filename = "lanes.json";

        private readonly Storage storage;

        public LaneSoundProjectStore(Storage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// The folder a difficulty's lanes are kept in, relative to <see cref="DIRECTORY"/>.
        /// </summary>
        public static string FolderNameFor(BeatmapInfo info) =>
            $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]".GetValidFilename();

        /// <summary>
        /// The lanes kept for <paramref name="info"/>, or null if none are: it was not made to hitsound in.
        /// </summary>
        public LaneSoundProject? Load(BeatmapInfo info)
        {
            string path = Path.Combine(DIRECTORY, FolderNameFor(info), filename);

            if (!storage.Exists(path))
                return null;

            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<LaneSoundProject>(reader.ReadToEnd()) ?? new LaneSoundProject();
                }
            }
            catch (Exception e)
            {
                // lanes that cannot be read are not worth stopping the editor for; the difficulty is still one made to
                // hitsound in, and starts with none.
                Logger.Error(e, "Could not read the sounds of the lanes");
                return new LaneSoundProject();
            }
        }

        public void Save(BeatmapInfo info, LaneSoundProject project)
        {
            string path = Path.Combine(DIRECTORY, FolderNameFor(info), filename);

            try
            {
                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(project, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the sounds of the lanes");
            }
        }
    }
}
