// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Legacy;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Reads what sounds in an osu!mania difficulty hitsounded in stable, from its file: for it to be turned into an osu!
    /// hitsound difficulty as it is (see <see cref="LaneSoundConversion"/>).
    /// </summary>
    /// <remarks>
    /// In stable a note is given a sound by copying one that has it, so the notes say what they sound as themselves:
    /// by their hitsounds and sample sets, or by the name of a file. The lanes say nothing, and none are gone by here.
    /// What is read is as Mapping Tools' Hitsound Studio reads a difficulty's hitsounds in, which is how the user has
    /// turned such difficulties into hitsound difficulties (2026-10-13).
    /// </remarks>
    public static class StableManiaHitsounds
    {
        /// <summary>
        /// What sounds in a difficulty.
        /// </summary>
        /// <param name="Notes">When each sound is heard, the lane of each being its place in <paramref name="Sounds"/>.</param>
        /// <param name="Sounds">The sounds there are.</param>
        /// <param name="Fallback">The hit sound of a circle none of whose sounds is one.</param>
        /// <param name="Volumes">The volume every red and green line says, in the order of the file.</param>
        public record Read(IReadOnlyList<LaneSoundConversion.Note> Notes, IReadOnlyList<LaneSound> Sounds, LaneSound Fallback, IReadOnlyList<(double Time, int Volume)> Volumes);

        /// <summary>
        /// Reads <paramref name="file"/>.
        /// </summary>
        /// <param name="file">The difficulty as stable has it.</param>
        /// <param name="fileOf">The sound file of the beatmap set that stable plays for a name without its extension, by its
        /// name in the set, or null if the set has none.</param>
        public static Read From(string file, Func<string, string?> fileOf)
        {
            string[] lines = file.Replace("\r\n", "\n").Split('\n').Select(l => l.Trim()).ToArray();

            string fallbackBank = bankOf(sampleSetOfTheFile(lines), HitSampleInfo.BANK_NORMAL);

            var timing = section(lines, "[TimingPoints]").Select(l => l.Split(','))
                                                           .Where(f => f.Length > 0 && double.TryParse(f[0], NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                                                           .Select(f => (time: double.Parse(f[0], CultureInfo.InvariantCulture), set: whole(f.ElementAtOrDefault(3)), index: whole(f.ElementAtOrDefault(4))))
                                                           .ToArray();

            var heard = new List<(double time, LaneSound sound)>();

            foreach (string[] fields in section(lines, "[HitObjects]").Select(l => l.Split(',')))
            {
                if (fields.Length < 5 || !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double time)
                                      || !int.TryParse(fields[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int type)
                                      || !int.TryParse(fields[4], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hitSound))
                    continue;

                // a hold note says when it ends before the rest.
                string[] sample = (fields.ElementAtOrDefault(5) ?? string.Empty).Split(':');

                if ((type & (int)LegacyHitObjectType.Hold) > 0)
                    sample = sample.Skip(1).ToArray();

                // the line the note is on, which says what the note does not: a little late is still on it, as it is
                // where the game reads the file.
                var line = timing.LastOrDefault(t => t.time <= time + LegacyBeatmapDecoder.CONTROL_POINT_LENIENCY, timing.FirstOrDefault());

                string normalBank = bankOf(whole(sample.ElementAtOrDefault(0)), bankOf(line.set, fallbackBank));
                string additionBank = bankOf(whole(sample.ElementAtOrDefault(1)), normalBank);

                int index = whole(sample.ElementAtOrDefault(2));

                if (index == 0)
                    index = line.index;

                string filename = string.Join(':', sample.Skip(4)).Trim();

                var sounds = (LegacyHitSoundType)hitSound;

                string[] additions = new[]
                {
                    (LegacyHitSoundType.Whistle, HitSampleInfo.HIT_WHISTLE),
                    (LegacyHitSoundType.Finish, HitSampleInfo.HIT_FINISH),
                    (LegacyHitSoundType.Clap, HitSampleInfo.HIT_CLAP),
                }.Where(a => sounds.HasFlag(a.Item1)).Select(a => a.Item2).ToArray();

                if (filename.Length > 0)
                {
                    // a file is the one sound of its note, in place of the others: it is of the kind the note says
                    // first, and of the sample set a sound of that kind would have been of.
                    string kind = sounds.HasFlag(LegacyHitSoundType.Normal) || additions.Length == 0 ? HitSampleInfo.HIT_NORMAL : additions[0];

                    heard.Add((time, new LaneSound
                    {
                        Bank = kind == HitSampleInfo.HIT_NORMAL ? normalBank : additionBank,
                        Sound = kind,
                        File = fileOf(Path.ChangeExtension(filename, null)) ?? string.Empty,
                    }));

                    continue;
                }

                // in osu!mania a note that has a sound added is not also the hit sound, unless it says so.
                if (sounds.HasFlag(LegacyHitSoundType.Normal) || additions.Length == 0)
                    heard.Add((time, soundOf(normalBank, HitSampleInfo.HIT_NORMAL, index, fileOf)));

                foreach (string addition in additions)
                    heard.Add((time, soundOf(additionBank, addition, index, fileOf)));
            }

            // in the order of their names, as Hitsound Studio has its layers: where two of one kind are heard together,
            // the sample set of the first is the one they both go under.
            var distinct = heard.Select(h => (h.sound.Bank, h.sound.Sound, h.sound.File))
                                .Distinct()
                                .OrderBy(s => Path.GetFileNameWithoutExtension(s.File), StringComparer.Ordinal)
                                .ThenBy(s => $@"{s.Bank}-{s.Sound}", StringComparer.Ordinal)
                                .ToList();

            return new Read(
                heard.Select(h => new LaneSoundConversion.Note(h.time, distinct.IndexOf((h.sound.Bank, h.sound.Sound, h.sound.File)))).ToArray(),
                distinct.Select(s => new LaneSound { Bank = s.Bank, Sound = s.Sound, File = s.File }).ToArray(),
                soundOf(fallbackBank, HitSampleInfo.HIT_NORMAL, 1, fileOf),
                HitsoundDifficultyWriter.VolumesOf(file));
        }

        /// <summary>
        /// A sound of a sample set under a custom index: the file of the beatmap set stable plays for it, or with none,
        /// and under no custom index, the skin's.
        /// </summary>
        private static LaneSound soundOf(string bank, string sound, int index, Func<string, string?> fileOf) => new LaneSound
        {
            Bank = bank,
            Sound = sound,
            File = index <= 0 ? string.Empty : fileOf($@"{bank}-{sound}{(index == 1 ? string.Empty : index.ToString(CultureInfo.InvariantCulture))}") ?? string.Empty,
        };

        private static IEnumerable<string> section(string[] lines, string name) =>
            lines.SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0 && !l.StartsWith("//", StringComparison.Ordinal));

        /// <summary>
        /// The sample set the file says its lines are of where they do not say.
        /// </summary>
        private static int sampleSetOfTheFile(string[] lines)
        {
            string? said = section(lines, "[General]").FirstOrDefault(l => l.StartsWith("SampleSet:", StringComparison.Ordinal))?.Split(':')[1].Trim();

            return Enum.TryParse(said, true, out LegacySampleBank bank) ? (int)bank : 0;
        }

        private static string bankOf(int sampleSet, string otherwise) => (LegacySampleBank)sampleSet switch
        {
            LegacySampleBank.Normal => HitSampleInfo.BANK_NORMAL,
            LegacySampleBank.Soft => HitSampleInfo.BANK_SOFT,
            LegacySampleBank.Drum => HitSampleInfo.BANK_DRUM,
            _ => otherwise,
        };

        private static int whole(string? field) =>
            double.TryParse(field, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? (int)value : 0;
    }
}
