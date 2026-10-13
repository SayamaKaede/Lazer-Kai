// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Framework.Extensions;
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
        private readonly Func<string, byte[]?> read;

        /// <param name="beatmaps">What the difficulty and its files are written with.</param>
        /// <param name="working">The difficulty that is turned into one.</param>
        /// <param name="project">What is kept for it: here, what it was last turned into and whether its volumes go along.</param>
        /// <param name="keep">Keeps <paramref name="project"/>, once what it was turned into has been noted in it.</param>
        /// <param name="notifications">Where it is said what was done.</param>
        /// <param name="dialogOverlay">Where it is asked before a file of the mapper's own is written over.</param>
        /// <param name="read">
        /// What is in a sound, by what the lanes name it as, or null if it is not there: the lanes of a difficulty made
        /// to hitsound in are sounds of the library, and the notes of one hitsounded in stable are files of the beatmap
        /// set (<see cref="FromSet"/>).
        /// </param>
        public HitsoundDifficultyConverter(BeatmapManager beatmaps, WorkingBeatmap working, LaneSoundProject project, Action keep, INotificationOverlay? notifications,
                                           IDialogOverlay? dialogOverlay, Func<string, byte[]?> read)
        {
            this.beatmaps = beatmaps;
            this.working = working;
            this.project = project;
            this.keep = keep;
            this.notifications = notifications;
            this.dialogOverlay = dialogOverlay;
            this.read = read;
        }

        /// <summary>
        /// Reads the sounds of a difficulty hitsounded in stable, which are files of its beatmap set.
        /// </summary>
        public static Func<string, byte[]?> FromSet(WorkingBeatmap working) => name =>
        {
            if (working.BeatmapSetInfo.GetFile(name) is not RealmNamedFileUsage file)
                return null;

            using (var stream = working.GetStream(file.File.GetStoragePath()))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        };

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
            // each sound is read once, however many notes ask whether it is there.
            var sources = new Dictionary<string, byte[]?>();

            byte[]? bytesOf(string name)
            {
                if (!sources.TryGetValue(name, out byte[]? bytes))
                    sources[name] = bytes = read(name);

                return bytes;
            }

            var result = LaneSoundConversion.Convert(notes, sounds, fallback, f => bytesOf(f) != null);

            if (result.Circles.Count == 0)
            {
                notifications?.Post(new SimpleNotification { Text = LazerKaiHitsoundStrings.NothingToConvert });
                return;
            }

            var files = new List<SoundToWrite>();

            foreach (var sound in result.Files)
            {
                if (sound.Sources.Count == 1)
                {
                    byte[] source = bytesOf(sound.Sources[0])!;

                    using (var stream = new MemoryStream(source))
                        files.Add(new SoundToWrite(sound.Name + Path.GetExtension(sound.Sources[0]), stream.ComputeSHA2Hash(), () => new MemoryStream(source)));

                    continue;
                }

                // lanes that come to the one sound of a circle at the one time are heard together in one file.
                byte[]? mixed = SoundMixing.Mix(sound.Sources.Select(s => bytesOf(s)!).ToArray());

                if (mixed == null)
                {
                    notifications?.Post(new SimpleErrorNotification
                    {
                        Text = LazerKaiHitsoundStrings.CannotMixSounds(string.Join(@" + ", sound.Sources.Select(Path.GetFileName)))
                    });
                    return;
                }

                using (var stream = new MemoryStream(mixed))
                    files.Add(new SoundToWrite(sound.Name + @".wav", stream.ComputeSHA2Hash(), () => new MemoryStream(mixed)));
            }

            // a file of the mapper's own that one of these is to be found in place of.
            var inTheWay = files.SelectMany(f => set.Files.Where(other => isTheSameSound(other.Filename, f.Name)
                                                                          && !(other.Filename == f.Name && other.File.Hash == f.Hash)
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

        /// <summary>
        /// A sound file the circles are to find.
        /// </summary>
        /// <param name="Name">Its name in the beatmap set.</param>
        /// <param name="Hash">The hash of what is in it, as the files of a beatmap set are told apart.</param>
        /// <param name="Open">What is in it.</param>
        private record SoundToWrite(string Name, string Hash, Func<Stream> Open);

        private void write(LaneSoundConversion.Result result, IReadOnlyList<SoundToWrite> files, Func<string> file, IReadOnlyList<(double Time, int Volume)> volumes)
        {
            var written = new Dictionary<string, string>();

            foreach (var (name, hash, open) in files)
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

                if (there?.File.Hash != hash)
                {
                    using (var stream = open())
                        beatmaps.AddFile(set, stream, name);

                    writtenHere = true;
                }

                if (writtenHere)
                    written[name] = hash;
            }

            var before = Converted;

            string difficultyName = before?.DifficultyName ?? NamingUtils.GetNextBestName(set.Beatmaps.Select(b => b.DifficultyName), CONVERTED_NAME);

            string text = HitsoundDifficultyWriter.FromLanes(file(), difficultyName, result.Circles, project.ConvertVolumes ? volumes : volumesOf(before));

            if (project.MuteSliderBodies)
            {
                string silent;

                using (var stream = new MemoryStream(silent_sound))
                    silent = stream.ComputeSHA2Hash();

                foreach (string slide in HitsoundDifficultyWriter.SliderSlidesOf(text))
                {
                    string name = slide + @".wav";

                    // one the mapper put there, of whatever kind, is the sound sliders are to make.
                    if (set.Files.Any(f => isTheSameSound(f.Filename, name) && !wasWrittenHere(f)))
                        continue;

                    if (set.GetFile(name)?.File.Hash != silent)
                    {
                        using (var stream = new MemoryStream(silent_sound))
                            beatmaps.AddFile(set, stream, name);
                    }

                    written[name] = silent;
                }
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

            byte[] bytes = Encoding.UTF8.GetBytes(text);

            var made = before != null
                ? beatmaps.ReplaceDifficultyFile(before, bytes)
                : beatmaps.AddDifficultyFromFile(set, working, bytes);

            project.Converted = new ConvertedDifficulty { Difficulty = made.BeatmapInfo.ID, Files = written };
            keep();

            notifications?.Post(new SimpleNotification
            {
                Text = LazerKaiHitsoundStrings.ConvertedToHitsoundDifficulty(difficultyName, result.Circles.Count, files.Count)
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

        /// <summary>
        /// A sound file with no sound in it: a wave file that says how its samples would be and has none. What mappers
        /// put in a beatmap for a sound they do not want heard.
        /// </summary>
        private static readonly byte[] silent_sound = silentSound();

        private static byte[] silentSound()
        {
            const int rate = 44100;
            const short channels = 2;
            const short bytes_per_sample = 2;

            using (var file = new MemoryStream())
            using (var writer = new BinaryWriter(file, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes(@"RIFF"));
                writer.Write(36);
                writer.Write(Encoding.ASCII.GetBytes(@"WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write(channels);
                writer.Write(rate);
                writer.Write(rate * channels * bytes_per_sample);
                writer.Write((short)(channels * bytes_per_sample));
                writer.Write((short)(bytes_per_sample * 8));
                writer.Write(Encoding.ASCII.GetBytes(@"data"));
                writer.Write(0);

                writer.Flush();
                return file.ToArray();
            }
        }

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
