// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;
using osu.Game.Localisation;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Turns what sounds in an osu!mania difficulty into an osu! hitsound difficulty of the same beatmap set: a circle
    /// wherever something sounds, and the sound files those circles are to find. Done again, it is put in place of the
    /// one made before.
    /// </summary>
    /// <remarks>
    /// As Mapping Tools' Hitsound Studio does it (see <see cref="LaneSoundConversion"/>), but for where it is written:
    /// straight into the beatmap set, where Mapping Tools writes to a folder of its own for the mapper to move from. So
    /// nothing of the mapper's own is written over without asking: only the difficulty and the files this made the last
    /// time are (2026-10-13).
    /// </remarks>
    public class HitsoundDifficultyConverter
    {
        /// <summary>
        /// What the hitsound difficulty is called where there is none yet.
        /// </summary>
        public const string CONVERTED_NAME = @"Hitsounds";

        private readonly BeatmapManager beatmaps;
        private readonly WorkingBeatmap working;
        private readonly LaneSoundProject project;
        private readonly Action keep;
        private readonly INotificationOverlay? notifications;
        private readonly IDialogOverlay? dialogOverlay;

        /// <param name="beatmaps">What the difficulty and its files are written with.</param>
        /// <param name="working">The difficulty that is turned into one.</param>
        /// <param name="project">What is kept for it: here, what it was last turned into and whether its volumes go along.</param>
        /// <param name="keep">Keeps <paramref name="project"/>, once what it was turned into has been noted in it.</param>
        /// <param name="notifications">Where it is said what was done.</param>
        /// <param name="dialogOverlay">Where it is asked before a file of the mapper's own is written over.</param>
        public HitsoundDifficultyConverter(BeatmapManager beatmaps, WorkingBeatmap working, LaneSoundProject project, Action keep, INotificationOverlay? notifications,
                                           IDialogOverlay? dialogOverlay)
        {
            this.beatmaps = beatmaps;
            this.working = working;
            this.project = project;
            this.keep = keep;
            this.notifications = notifications;
            this.dialogOverlay = dialogOverlay;
        }

        private BeatmapSetInfo set => working.BeatmapSetInfo;

        /// <summary>
        /// The hitsound difficulty it was last turned into, if it is still there.
        /// </summary>
        public BeatmapInfo? Converted => project.Converted == null ? null : set.Beatmaps.FirstOrDefault(b => b.ID == project.Converted.Difficulty);

        /// <summary>
        /// Turns it into a hitsound difficulty.
        /// </summary>
        /// <param name="notes">What sounds when, the lane of each being its place in <paramref name="sounds"/>.</param>
        /// <param name="sounds">The sounds there are.</param>
        /// <param name="fallback">The hit sound of a circle none of whose sounds is one.</param>
        /// <param name="file">A difficulty as stable has it, which the new one is written from: only asked for if it is.</param>
        /// <param name="volumes">The volumes of the difficulty itself, which go into the new one if they are to.</param>
        public void Convert(IEnumerable<LaneSoundConversion.Note> notes, IReadOnlyList<LaneSound> sounds, LaneSound fallback, Func<string> file,
                            IReadOnlyList<(double Time, int Volume)> volumes)
        {
            var result = LaneSoundConversion.Convert(notes, sounds, fallback, f => set.GetFile(f) != null);

            if (result.Circles.Count == 0)
            {
                notifications?.Post(new SimpleNotification { Text = LazerKaiHitsoundStrings.NothingToConvert });
                return;
            }

            if (result.Files.FirstOrDefault(f => f.Sources.Count > 1) is LaneSoundConversion.SoundFile mixed)
            {
                notifications?.Post(new SimpleErrorNotification
                {
                    Text = LazerKaiHitsoundStrings.CannotMixSounds(string.Join(@" + ", mixed.Sources.Select(Path.GetFileName)))
                });
                return;
            }

            var files = result.Files.Select(f => (name: f.Name + Path.GetExtension(f.Sources[0]), source: set.GetFile(f.Sources[0])!)).ToArray();

            // a file of the mapper's own that one of these is to be found in place of.
            var inTheWay = files.SelectMany(f => set.Files.Where(other => isTheSameSound(other.Filename, f.name)
                                                                          && !(other.Filename == f.name && other.File.Hash == f.source.File.Hash)
                                                                          && !wasWrittenHere(other)))
                                .Distinct()
                                .ToArray();

            if (inTheWay.Length > 0 && dialogOverlay != null)
            {
                dialogOverlay.Push(new ConfirmDialog(LazerKaiHitsoundStrings.ReplaceSoundFiles(string.Join(@", ", inTheWay.Select(f => f.Filename))),
                    () => write(result, files, file, volumes)));
                return;
            }

            write(result, files, file, volumes);
        }

        private void write(LaneSoundConversion.Result result, (string name, RealmNamedFileUsage source)[] files, Func<string> file, IReadOnlyList<(double Time, int Volume)> volumes)
        {
            var written = new Dictionary<string, string>();

            foreach (var (name, source) in files)
            {
                // stable finds a sound by its name whatever kind of file it is, so another kind of the same name would
                // be found as well as this one, or before it.
                foreach (var other in set.Files.Where(f => f.Filename != name && isTheSameSound(f.Filename, name)).ToArray())
                    beatmaps.DeleteFile(set, other);

                var there = set.GetFile(name);

                // one that is there already and is the very sound is left as it is, and is no more this one's to take
                // away again than it was: a difficulty hitsounded in stable sounds as the beatmap's own files, under
                // their own names.
                bool writtenHere = there != null && wasWrittenHere(there);

                if (there?.File.Hash != source.File.Hash)
                {
                    using (var stream = working.GetStream(source.File.GetStoragePath()))
                        beatmaps.AddFile(set, stream, name);

                    writtenHere = true;
                }

                if (writtenHere)
                    written[name] = source.File.Hash;
            }

            // what was written the last time and is not wanted now, where it is still what was written.
            if (project.Converted != null)
            {
                foreach (var (name, hash) in project.Converted.Files)
                {
                    if (!written.ContainsKey(name) && set.GetFile(name) is RealmNamedFileUsage stale && stale.File.Hash == hash)
                        beatmaps.DeleteFile(set, stale);
                }
            }

            var before = Converted;

            string difficultyName = before?.DifficultyName ?? NamingUtils.GetNextBestName(set.Beatmaps.Select(b => b.DifficultyName), CONVERTED_NAME);

            byte[] bytes = Encoding.UTF8.GetBytes(HitsoundDifficultyWriter.FromLanes(file(), difficultyName, result.Circles,
                project.ConvertVolumes ? volumes : volumesOf(before)));

            var made = before != null
                ? beatmaps.ReplaceDifficultyFile(before, bytes)
                : beatmaps.AddDifficultyFromFile(set, working, bytes);

            project.Converted = new ConvertedDifficulty { Difficulty = made.BeatmapInfo.ID, Files = written };
            keep();

            notifications?.Post(new SimpleNotification
            {
                Text = LazerKaiHitsoundStrings.ConvertedToHitsoundDifficulty(difficultyName, result.Circles.Count, files.Length)
            });
        }

        /// <summary>
        /// The volumes of the hitsound difficulty made before, as its lines say them, which are kept where the ones of
        /// the difficulty itself are not to go into it.
        /// </summary>
        private (double, int)[] volumesOf(BeatmapInfo? before)
        {
            if (before?.Path == null || set.GetFile(before.Path) is not RealmNamedFileUsage file)
                return Array.Empty<(double, int)>();

            using (var stream = working.GetStream(file.File.GetStoragePath()))
            using (var reader = new StreamReader(stream, Encoding.UTF8))
                return HitsoundDifficultyWriter.VolumesOf(reader.ReadToEnd());
        }

        private bool wasWrittenHere(RealmNamedFileUsage file) =>
            project.Converted?.Files.TryGetValue(file.Filename, out string? hash) == true && hash == file.File.Hash;

        private static readonly string[] sound_extensions = { @".wav", @".ogg", @".mp3" };

        /// <summary>
        /// Whether stable would take <paramref name="filename"/> for the sound <paramref name="name"/> is: a sound file
        /// of that name in the same folder, of whatever kind.
        /// </summary>
        private static bool isTheSameSound(string filename, string name) =>
            sound_extensions.Contains(Path.GetExtension(filename).ToLowerInvariant())
            && string.Equals(Path.ChangeExtension(filename, null), Path.ChangeExtension(name, null), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The sound file of the beatmap set that stable plays for a name without its extension, by its name in the set.
        /// </summary>
        public static string? FileOf(BeatmapSetInfo set, string name) =>
            set.Files.FirstOrDefault(f => isTheSameSound(f.Filename, name))?.Filename;
    }
}
