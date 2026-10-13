// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// Brings difficulties and sounds of another beatmap set into the one being edited (2026-10-13).
    /// </summary>
    /// <remarks>
    /// In stable this is done by copying files from one beatmap folder to another. Here a set has no folder: its folder
    /// is written out to be opened, for one set at a time, so the files of two sets are never side by side.
    /// </remarks>
    public class BeatmapSetImporter
    {
        private readonly BeatmapManager beatmaps;

        public BeatmapSetImporter(BeatmapManager beatmaps)
        {
            this.beatmaps = beatmaps;
        }

        /// <summary>
        /// What was brought in.
        /// </summary>
        /// <param name="Difficulties">The difficulties, by their names in the set they are now of.</param>
        /// <param name="Sounds">The sounds, by their names.</param>
        public record Imported(IReadOnlyList<string> Difficulties, IReadOnlyList<string> Sounds);

        /// <summary>
        /// Brings <paramref name="difficulties"/> and <paramref name="sounds"/> of <paramref name="from"/> into the set
        /// of <paramref name="into"/>.
        /// </summary>
        /// <param name="into">A difficulty of the set brought into, as it was last saved: the difficulties brought in say
        /// the song, the background and the names as it does.</param>
        /// <param name="from">The set brought from.</param>
        /// <param name="difficulties">Which of its difficulties.</param>
        /// <param name="sounds">Which of its sounds. One that would be put in place of another is.</param>
        public Imported Import(BeatmapInfo into, BeatmapSetInfo from, IEnumerable<BeatmapInfo> difficulties, IEnumerable<ImportableSound> sounds)
        {
            // read afresh, so that what is brought in is added to the set as the game has it now.
            var reference = beatmaps.GetWorkingBeatmap(into, true);
            var set = reference.BeatmapSetInfo;

            var theirs = new BeatmapSetFiles(beatmaps, from);

            var broughtSounds = new List<string>();

            // before the difficulties, which are saved as files of the set and say then what the set is made of.
            foreach (var sound in sounds.Where(s => s.CanBeBroughtIn))
            {
                using (var stream = theirs.GetStream(sound.Name))
                {
                    if (stream == null)
                        continue;

                    BeatmapSetSounds.Put(beatmaps, set, sound.Name, stream);
                }

                broughtSounds.Add(sound.Name);
            }

            var broughtDifficulties = new List<string>();

            string? path = reference.BeatmapInfo.Path;
            byte[]? like = path == null ? null : new BeatmapSetFiles(beatmaps, set).Get(path);

            if (like != null)
            {
                foreach (var difficulty in difficulties)
                {
                    byte[]? file = difficulty.Path == null ? null : theirs.Get(difficulty.Path);

                    if (file == null)
                        continue;

                    string name = NamingUtils.GetNextBestName(set.Beatmaps.Select(b => b.DifficultyName), difficulty.DifficultyName);
                    string written = ImportedDifficultyWriter.AsOneOf(Encoding.UTF8.GetString(file), Encoding.UTF8.GetString(like), name);

                    broughtDifficulties.Add(beatmaps.AddDifficultyFromFile(set, reference, Encoding.UTF8.GetBytes(written)).BeatmapInfo.DifficultyName);
                }
            }

            return new Imported(broughtDifficulties, broughtSounds);
        }
    }
}
