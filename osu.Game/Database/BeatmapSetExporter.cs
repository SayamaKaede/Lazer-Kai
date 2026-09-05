// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays.Notifications;
using osu.Game.Utils;

namespace osu.Game.Database
{
    /// <summary>
    /// What the two beatmap archive exporters have in common: writing one difficulty out on its own.
    /// </summary>
    /// <remarks>
    /// The archives carry a whole set. This is for handing one difficulty to a program that reads <c>.osu</c> files
    /// directly. Which <c>.osu</c> comes out is decided by the exporter it is asked of: the legacy one converts back
    /// to v14 on the way, the other writes what lazer stored, which is v128.
    /// </remarks>
    public abstract class BeatmapSetExporter : LegacyArchiveExporter<BeatmapSetInfo>
    {
        protected BeatmapSetExporter(Storage storage)
            : base(storage)
        {
        }

        /// <summary>
        /// Opens the directory exports are written to.
        /// </summary>
        public void PresentExportedFiles() => ExportStorage.PresentExternally();

        /// <summary>
        /// Writes a single difficulty out as a bare <c>.osu</c> file into the export directory.
        /// </summary>
        /// <returns>The name of the file written.</returns>
        public string ExportBeatmapFile(BeatmapInfo beatmapInfo)
        {
            var set = beatmapInfo.BeatmapSet;

            if (set == null)
                throw new InvalidOperationException("The beatmap is not part of a set, so its file cannot be located.");

            var file = set.GetFile(beatmapInfo.Path ?? string.Empty);

            if (file == null)
                throw new InvalidOperationException("The beatmap has no stored file to write out. Save it first.");

            using var contents = GetFileContents(set, file)
                                 ?? throw new InvalidOperationException("The beatmap could not be read.");

            string baseName = $"{beatmapInfo.Metadata.Artist} - {beatmapInfo.Metadata.Title} ({beatmapInfo.Metadata.Author.Username}) [{beatmapInfo.DifficultyName}]".GetValidFilename();

            if (baseName.Length > MAX_FILENAME_LENGTH - 4)
                baseName = baseName.Remove(MAX_FILENAME_LENGTH - 4);

            string filename = NamingUtils.GetNextBestFilename(ExportStorage.GetFiles(string.Empty, $"{baseName}*.osu"), $"{baseName}.osu");

            using (var output = ExportStorage.CreateFileSafely(filename))
                contents.CopyTo(output);

            return filename;
        }

        /// <summary>
        /// Writes a single difficulty out and says so, the way the archive exports do.
        /// </summary>
        /// <remarks>
        /// One file is written in one go, so there is no progress to report -- but something has to say where it
        /// went, and clicking the notification is how every other export here reveals its file.
        /// </remarks>
        public void ExportBeatmapFileWithNotification(BeatmapInfo beatmapInfo)
        {
            string filename;

            try
            {
                filename = ExportBeatmapFile(beatmapInfo);
            }
            catch (Exception e)
            {
                PostNotification?.Invoke(new SimpleErrorNotification { Text = e.Message });
                return;
            }

            PostNotification?.Invoke(new SimpleNotification
            {
                Text = NotificationsStrings.FileExportFinished(filename),
                Activated = () =>
                {
                    ExportStorage.PresentFileExternally(filename);
                    return true;
                },
            });
        }
    }
}
