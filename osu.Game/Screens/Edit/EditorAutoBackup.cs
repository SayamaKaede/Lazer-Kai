// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Configuration;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Writes periodic copies of the beatmap being edited to disk, so work is recoverable after a crash or a
    /// regretted change.
    /// </summary>
    /// <remarks>
    /// Backups are written in the same format the editor saves in, not the stable-compatible one. Converting to
    /// stable's format rounds coordinates and truncates times, which is fine for handing a beatmap to another program
    /// but would make a backup restore to something subtly different from what was lost.
    /// </remarks>
    public partial class EditorAutoBackup : Component
    {
        /// <summary>
        /// Directory backups are written under, relative to the game's storage.
        /// </summary>
        public const string BACKUP_DIRECTORY = "backups";

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private INotificationOverlay? notifications { get; set; }

        private readonly Bindable<bool> enabled = new Bindable<bool>();
        private readonly Bindable<bool> notifyOnBackup = new Bindable<bool>();
        private readonly Bindable<double> intervalMinutes = new Bindable<double>();
        private readonly Bindable<int> keepCount = new Bindable<int>();

        /// <summary>
        /// Hash of the last content written, so an idle editor does not fill the directory with identical copies.
        /// </summary>
        private string? lastWrittenHash;

        private ScheduledDelegate? scheduledBackup;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorAutoBackup, enabled);
            config.BindWith(OsuSetting.EditorAutoBackupInterval, intervalMinutes);
            config.BindWith(OsuSetting.EditorAutoBackupKeepCount, keepCount);
            config.BindWith(OsuSetting.EditorNotifyOnBackup, notifyOnBackup);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.BindValueChanged(_ => reschedule());
            intervalMinutes.BindValueChanged(_ => reschedule(), true);

            if (OpenedFile != null)
                keep(OpenedFile, @"opened");
        }

        /// <summary>
        /// Keeps the difficulty's file as it is just before a save in lazer's format replaces it.
        /// </summary>
        public void KeepBeforeLazerSave(byte[] file) => keep(file, @"before-lazer-save");

        /// <summary>
        /// Keeps a file byte for byte among the backups, unless the newest backup is the same.
        /// </summary>
        /// <remarks>
        /// Unlike the backups written while editing, this is the file itself rather than an encoding of the beatmap: it
        /// is kept so that whatever a save then does to the file, what was there can be put back exactly. It is named
        /// and counted as any other backup.
        /// </remarks>
        /// <param name="file">The file.</param>
        /// <param name="when">What it is kept at, for the log.</param>
        private void keep(byte[] file, string when)
        {
            string directory = DirectoryFor(editorBeatmap.BeatmapInfo);
            string filename = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.osu";

            Task.Run(() =>
            {
                try
                {
                    string? newest = storage.ExistsDirectory(directory)
                        ? storage.GetFiles(directory, "*.osu").OrderByDescending(f => f).FirstOrDefault()
                        : null;

                    if (newest != null)
                    {
                        using (var stream = storage.GetStream(newest))
                        {
                            if (stream != null && stream.ReadAllRemainingBytesToArray().AsSpan().SequenceEqual(file))
                                return;
                        }
                    }

                    using (var stream = storage.CreateFileSafely(Path.Combine(directory, filename)))
                        stream.Write(file);

                    prune(directory);
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Could not keep the beatmap's file ({when})");
                }
            });
        }

        private void reschedule()
        {
            scheduledBackup?.Cancel();

            if (!enabled.Value)
                return;

            double interval = intervalMinutes.Value * 60000;

            scheduledBackup = Scheduler.AddDelayed(() => Backup(), interval, true);
        }

        /// <summary>
        /// Writes a backup now, unless the beatmap is unchanged since the last one.
        /// </summary>
        /// <param name="asked">
        /// Whether somebody asked for this one rather than the clock coming round. A backup that was asked for is
        /// always reported, including when there was nothing to write: the alternative is a keypress that does
        /// nothing visible and no way to tell it apart from one that did not arrive.
        /// </param>
        public void Backup(bool asked = false)
        {
            string content;

            try
            {
                content = encode();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not encode the beatmap for backup");
                return;
            }

            string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

            if (hash == lastWrittenHash)
            {
                if (asked)
                    announce(EditorStrings.BackupUpToDate);

                return;
            }

            lastWrittenHash = hash;

            string directory = DirectoryFor(editorBeatmap.BeatmapInfo);
            string filename = $"{DateTimeOffset.Now:yyyy-MM-dd_HH-mm-ss}.osu";

            // written away from the update thread: this is a whole beatmap being serialised and flushed to disk, and
            // nothing on screen is waiting for the result.
            Task.Run(() =>
            {
                try
                {
                    using (var stream = storage.CreateFileSafely(Path.Combine(directory, filename)))
                    using (var writer = new StreamWriter(stream, Encoding.UTF8))
                    {
                        writer.NewLine = "\r\n";
                        writer.Write(content);
                    }

                    prune(directory);

                    if (asked || notifyOnBackup.Value)
                        announce(EditorStrings.BackupWritten);
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not write a beatmap backup");
                }
            });
        }

        private void announce(LocalisableString what) => notifications?.Post(new SimpleNotification
        {
            Text = what,
            Icon = FontAwesome.Solid.Save,
            // a backup is routine. It should get out of the way after a couple of seconds rather than sitting there
            // for twelve and flashing the taskbar as an important notification would.
            IsImportant = false,
        });

        private string encode()
        {
            var writer = new StringWriter { NewLine = "\r\n" };

            new LegacyBeatmapEncoder(editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin, editorBeatmap.Storyboard).Encode(writer);

            return writer.ToString();
        }

        /// <summary>
        /// Deletes the oldest backups of this difficulty beyond the number the user asked to keep.
        /// </summary>
        private void prune(string directory)
        {
            if (keepCount.Value <= 0)
                return;

            var existing = storage.GetFiles(directory, "*.osu")
                                  // the names are timestamps in a sortable layout, so ordering by name orders by age.
                                  .OrderByDescending(f => f)
                                  .Skip(keepCount.Value)
                                  .ToArray();

            foreach (string file in existing)
                storage.Delete(file);
        }

        /// <summary>
        /// Where a difficulty's backups are kept, relative to the game's storage.
        /// </summary>
        /// <remarks>
        /// Both the writing and the menu item offering to open the folder go through here, so what the editor shows
        /// is always where the files actually went.
        /// </remarks>
        public static string DirectoryFor(BeatmapInfo info) => Path.Combine(BACKUP_DIRECTORY, folderNameFor(info));

        /// <summary>
        /// The difficulty's file as the editor opened it, to be kept among the backups as it is; null to keep nothing.
        /// Set before this is loaded.
        /// </summary>
        public byte[]? OpenedFile { get; init; }

        /// <summary>
        /// A folder per difficulty, so restoring means picking the newest file in an obvious place.
        /// </summary>
        private static string folderNameFor(BeatmapInfo info)
        {
            string name = $"{info.Metadata.Artist} - {info.Metadata.Title} [{info.DifficultyName}]";

            return name.GetValidFilename();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            scheduledBackup?.Cancel();
        }
    }
}
