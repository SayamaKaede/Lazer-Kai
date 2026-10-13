// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using osu.Game.Audio;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The name a sound file of a beatmap set is played as a hitsound by: the sample set, which of its sounds, and the
    /// number of the custom set.
    /// </summary>
    /// <remarks>
    /// The names are of one form, so a file is named by choosing the three rather than by typing (2026-10-13).
    /// </remarks>
    /// <param name="Bank">The sample set: one of <see cref="BANKS"/>.</param>
    /// <param name="Sound">Which of the sample set's sounds: one of <see cref="SOUNDS"/>.</param>
    /// <param name="Index">The custom set, from 1 on. The first is the name with no number.</param>
    public readonly record struct HitsoundFileName(string Bank, string Sound, int Index)
    {
        public static readonly string[] BANKS = HitSampleInfo.ALL_BANKS;

        /// <summary>
        /// What an object is hit with, and what a slider sounds as while it is followed.
        /// </summary>
        public static readonly string[] SOUNDS =
        {
            HitSampleInfo.HIT_NORMAL,
            HitSampleInfo.HIT_WHISTLE,
            HitSampleInfo.HIT_FINISH,
            HitSampleInfo.HIT_CLAP,
            @"slidertick",
            @"sliderslide",
            @"sliderwhistle",
        };

        /// <summary>
        /// The name of the file, being a file of <paramref name="extension"/>.
        /// </summary>
        public string WithExtension(string extension) => $@"{Bank}-{Sound}{(Index >= 2 ? Index.ToString(CultureInfo.InvariantCulture) : string.Empty)}{extension}";

        /// <summary>
        /// The name <paramref name="filename"/> has in the custom set <paramref name="by"/> on from its own. A name
        /// that is of no custom set is as it was.
        /// </summary>
        /// <remarks>
        /// For sounds brought into a beatmap set that has sounds of the same custom sets already, which are not to be
        /// put anything in place of: brought in as the sets after them, they are beside them instead.
        /// </remarks>
        public static string Shifted(string filename, int by) =>
            by != 0 && Of(filename) is HitsoundFileName name && name.Index + by >= 1
                ? (name with { Index = name.Index + by }).WithExtension(Path.GetExtension(filename))
                : filename;

        /// <summary>
        /// What <paramref name="filename"/> is played as, or null if it is no name of the form.
        /// </summary>
        public static HitsoundFileName? Of(string filename)
        {
            // found beside the difficulties only.
            if (filename.Contains('/') || filename.Contains('\\'))
                return null;

            if (!BeatmapSetSounds.IsSound(filename))
                return null;

            string name = Path.GetFileNameWithoutExtension(filename).ToLowerInvariant();

            foreach (string bank in BANKS)
            {
                foreach (string sound in SOUNDS)
                {
                    string prefix = $@"{bank}-{sound}";

                    if (!name.StartsWith(prefix, StringComparison.Ordinal))
                        continue;

                    string number = name[prefix.Length..];

                    if (number.Length == 0)
                        return new HitsoundFileName(bank, sound, 1);

                    // written as a number is, or it is not the name stable looks for.
                    if (number[0] != '0' && number.All(char.IsAsciiDigit) && int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out int index) && index >= 2)
                        return new HitsoundFileName(bank, sound, index);

                    return null;
                }
            }

            return null;
        }
    }
}
