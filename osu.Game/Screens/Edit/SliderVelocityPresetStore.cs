// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Where a difficulty's slider velocity presets are kept beside its file.
    /// </summary>
    /// <remarks>
    /// Lazer writes them into the `.osu`, as a line stable has never had, so a difficulty kept in stable's format has
    /// them here instead and its file stays as stable wrote it. Every other difficulty has them in both places, and
    /// whichever was written last is the one used. A folder per difficulty, named the way the backups and the
    /// collaboration splits are. Nothing is written for a difficulty whose presets have never left the ones every
    /// beatmap starts with.
    /// </remarks>
    public class SliderVelocityPresetStore
    {
        public const string DIRECTORY = "sv-presets";

        private const string filename = "presets.json";

        /// <summary>
        /// The presets every beatmap starts with.
        /// </summary>
        public static double[] Default => new Beatmap().SliderVelocityPresets;

        private readonly Storage storage;

        public SliderVelocityPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        public static string PathFor(BeatmapInfo info) =>
            Path.Combine(DIRECTORY, $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]".GetValidFilename(), filename);

        /// <summary>
        /// The presets kept for a difficulty, or null if none are.
        /// </summary>
        public double[]? Load(BeatmapInfo info)
        {
            string path = PathFor(info);

            if (!storage.Exists(path))
                return null;

            try
            {
                using (var stream = storage.GetStream(path))
                {
                    if (stream == null)
                        return null;

                    using (var reader = new StreamReader(stream))
                    {
                        double[]? presets = JsonConvert.DeserializeObject<double[]>(reader.ReadToEnd());
                        return presets?.Length > 0 ? presets : null;
                    }
                }
            }
            catch (Exception e)
            {
                // presets that cannot be read are not worth stopping the editor for; the beatmap's own are used.
                Logger.Error(e, "Could not read the slider velocity presets");
                return null;
            }
        }

        /// <summary>
        /// When a difficulty's presets were last kept, or null if they never were.
        /// </summary>
        public DateTimeOffset? LastWritten(BeatmapInfo info)
        {
            string path = PathFor(info);

            if (!storage.Exists(path))
                return null;

            return new DateTimeOffset(File.GetLastWriteTimeUtc(storage.GetFullPath(path)), TimeSpan.Zero);
        }

        /// <summary>
        /// Keeps a difficulty's presets.
        /// </summary>
        /// <remarks>
        /// Presets back at the ones every beatmap starts with are still written over what was kept, rather than it being
        /// removed: the `.osu` may say otherwise, and would be taken for the newer.
        /// </remarks>
        public void Save(BeatmapInfo info, double[] presets)
        {
            string path = PathFor(info);
            double[] ordered = presets.Distinct().OrderBy(p => p).ToArray();

            try
            {
                if (ordered.SequenceEqual(Default) && !storage.Exists(path))
                    return;

                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(ordered));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the slider velocity presets");
            }
        }
    }
}
