// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Models;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The sound files of a beatmap set: its hitsounds, and whatever else it plays that is not its song.
    /// </summary>
    public static class BeatmapSetSounds
    {
        /// <summary>
        /// The sounds of <paramref name="set"/>, in the order of their names.
        /// </summary>
        public static IEnumerable<RealmNamedFileUsage> Of(BeatmapSetInfo set)
        {
            string[] songs = songsOf(set);

            return set.Files.Where(f => IsSound(f.Filename) && !songs.Contains(f.Filename, StringComparer.OrdinalIgnoreCase))
                      .OrderBy(f => f.Filename, StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// The sounds of <paramref name="source"/>, and what bringing each into <paramref name="target"/> would do there.
        /// </summary>
        public static IReadOnlyList<ImportableSound> ToBringInto(BeatmapSetInfo target, BeatmapSetInfo source) =>
            Of(source).Select(f => new ImportableSound(f.Filename, standing(f, target))).ToArray();

        /// <summary>
        /// Whether <paramref name="filename"/> is the sound stable plays for <paramref name="name"/>: it finds a sound by
        /// its name whatever kind of file it is, so another kind of the same name is found as well, or before.
        /// </summary>
        public static bool IsTheSameSound(string filename, string name) =>
            IsSound(filename) && string.Equals(Path.ChangeExtension(filename, null), Path.ChangeExtension(name, null), StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// The file of <paramref name="set"/> that is the sound <paramref name="name"/> would be, if another than
        /// <paramref name="except"/> is: a sound is not given a name another one is played by.
        /// </summary>
        public static RealmNamedFileUsage? TakenBy(BeatmapSetInfo set, string name, string except) =>
            set.Files.FirstOrDefault(f => !string.Equals(f.Filename, except, StringComparison.OrdinalIgnoreCase) && IsTheSameSound(f.Filename, name));

        /// <summary>
        /// Whether <paramref name="filename"/> is a file of sound, by its kind.
        /// </summary>
        public static bool IsSound(string filename) => SupportedExtensions.AUDIO_EXTENSIONS.Contains(Path.GetExtension(filename).ToLowerInvariant());

        private static ImportStanding standing(RealmNamedFileUsage file, BeatmapSetInfo target)
        {
            var there = target.Files.Where(f => IsTheSameSound(f.Filename, file.Filename)).ToArray();

            if (there.Length == 0)
                return ImportStanding.New;

            string[] songs = songsOf(target);

            if (there.Any(f => songs.Contains(f.Filename, StringComparer.OrdinalIgnoreCase)))
                return ImportStanding.NameOfTheSong;

            if (there.Length == 1 && string.Equals(there[0].Filename, file.Filename, StringComparison.OrdinalIgnoreCase) && there[0].File.Hash == file.File.Hash)
                return ImportStanding.ThereAlready;

            return ImportStanding.InPlaceOfAnother;
        }

        private static string[] songsOf(BeatmapSetInfo set) =>
            set.Beatmaps.Select(b => b.Metadata.AudioFile).Where(f => !string.IsNullOrEmpty(f)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// A sound of another beatmap set, by its name there.
    /// </summary>
    /// <param name="Name">Its name in the set it is a file of, which is its name where it is brought in.</param>
    /// <param name="Standing">What bringing it in would do.</param>
    public record ImportableSound(string Name, ImportStanding Standing)
    {
        /// <summary>
        /// Whether it is brought in if asked for: one that is there already has nothing to bring.
        /// </summary>
        public bool CanBeBroughtIn => Standing == ImportStanding.New || Standing == ImportStanding.InPlaceOfAnother;
    }

    /// <summary>
    /// What bringing a sound of another beatmap set into the one being edited would do.
    /// </summary>
    public enum ImportStanding
    {
        /// <summary>
        /// The set has no sound of its name.
        /// </summary>
        New,

        /// <summary>
        /// The set has the very file, under the same name.
        /// </summary>
        ThereAlready,

        /// <summary>
        /// The set has another sound under its name, which this would be put in place of: brought in only when it is
        /// asked for by itself.
        /// </summary>
        InPlaceOfAnother,

        /// <summary>
        /// Its name is that of the set's song, which nothing is put in place of.
        /// </summary>
        NameOfTheSong,
    }
}
