// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using osu.Game.Skinning;
using osu.Game.Storyboards;

namespace osu.Game.Beatmaps.Formats
{
    /// <summary>
    /// Keeps a beatmap that came from stable in stable's own format while it is edited here, so that what was not
    /// edited is saved exactly as it was read.
    /// </summary>
    /// <remarks>
    /// Reading a stable beatmap turns its green lines into sounds carried by each object, and writing it out turns
    /// them back into lines of this game's own choosing. What is heard stays the same, but the file does not: the
    /// objects stop following the green lines, the lines move to where the objects are, and a mapper who carries on
    /// in stable finds the hitsounding rearranged. So the file as it was read is held on to, alongside what this game
    /// makes of it, and a save that changes nothing this game can see writes the file back untouched.
    ///
    /// The comparison is made between two encodings by this game's own encoder — of the beatmap as it was opened, and
    /// as it is now — so that anything the encoder rewrites of its own accord shows up identically on both sides and
    /// is not mistaken for an edit.
    /// </remarks>
    public class StableFileKeeper
    {
        /// <summary>
        /// The file as it now stands on disk.
        /// </summary>
        private byte[] file;

        /// <summary>
        /// What this game's encoder made of the beatmap when <see cref="file"/> was read or last written.
        /// </summary>
        private string baseline;

        /// <summary>
        /// The red lines and kiai as they stood then, so that the save knows to write edits to them into the lines.
        /// </summary>
        private IReadOnlyList<string> timing;

        public StableFileKeeper(byte[] file, IBeatmap beatmap, ISkin? skin, Storyboard? storyboard)
        {
            this.file = file;
            baseline = Encode(beatmap, skin, storyboard);
            timing = timingOf(beatmap);
        }

        /// <summary>
        /// Works out what to save.
        /// </summary>
        /// <param name="beatmap">The beatmap being saved.</param>
        /// <param name="skin">Its skin, as a save encodes it.</param>
        /// <param name="storyboard">Its storyboard, as a save encodes it.</param>
        /// <param name="timingLines">The file's red and green lines as they were edited, one line of text each; null if they were not.</param>
        /// <param name="current">What this game's encoder makes of the beatmap now, for <see cref="Saved"/>.</param>
        /// <returns>The file to save, or null if the beatmap has changed in a way that cannot be kept in stable's format.</returns>
        public byte[]? Write(IBeatmap beatmap, ISkin? skin, Storyboard? storyboard, IReadOnlyList<string>? timingLines, out string current)
        {
            current = Encode(beatmap, skin, storyboard);
            LastFailure = null;

            bool timingChanged = !timingOf(beatmap).SequenceEqual(timing);

            if (current == baseline && timingLines == null && !timingChanged)
                return file;

            var merger = new StableFileMerger(file, baseline, current, beatmap, timingLines, timingChanged);
            byte[]? merged = merger.Merge();

            LastFailure = merger.Failure;
            return merged;
        }

        /// <summary>
        /// Why the last <see cref="Write"/> could not keep the beatmap in stable's format, if it could not.
        /// </summary>
        public string? LastFailure { get; private set; }

        /// <summary>
        /// The red lines and kiai of a beatmap, one entry each, in a form that compares as text.
        /// </summary>
        private static IReadOnlyList<string> timingOf(IBeatmap beatmap)
        {
            var info = beatmap.ControlPointInfo;

            return info.TimingPoints.Select(t => FormattableString.Invariant($"T {t.Time} {t.BeatLength} {t.TimeSignature.Numerator} {t.OmitFirstBarLine}"))
                       .Concat(info.EffectPoints.Select(e => FormattableString.Invariant($"E {e.Time} {e.KiaiMode} {e.ScrollSpeed}")))
                       .ToList();
        }

        /// <summary>
        /// The format version the kept file states.
        /// </summary>
        public int Version => FormatVersion(file) ?? LegacyBeatmapEncoder.STABLE_VERSION;

        /// <summary>
        /// Takes note of a save made from <see cref="Write"/>, so that the next one is compared against it.
        /// </summary>
        public void Saved(byte[] written, string current, IBeatmap beatmap)
        {
            file = written;
            baseline = current;
            timing = timingOf(beatmap);
        }

        /// <summary>
        /// Whether a file is in stable's format: one that states a format version no later than the one stable writes.
        /// </summary>
        public static bool IsStableFile(byte[] file)
        {
            int? version = FormatVersion(file);
            return version != null && version <= LegacyBeatmapEncoder.STABLE_VERSION;
        }

        /// <summary>
        /// What this game's encoder makes of a beatmap: the same as a save would write, for comparison.
        /// </summary>
        public static string Encode(IBeatmap beatmap, ISkin? skin, Storyboard? storyboard)
        {
            var builder = new StringBuilder();

            using (var writer = new StringWriter(builder))
                new LegacyBeatmapEncoder(beatmap, skin, storyboard).Encode(writer);

            return builder.ToString();
        }

        /// <summary>
        /// The format version a file states on its first line, if it states one.
        /// </summary>
        public static int? FormatVersion(byte[] file)
        {
            using (var reader = new StreamReader(new MemoryStream(file), Encoding.UTF8, true))
            {
                string? line;

                while ((line = reader.ReadLine()) != null)
                {
                    line = line.Trim();

                    if (line.Length == 0)
                        continue;

                    const string header = "osu file format v";

                    if (!line.StartsWith(header, StringComparison.Ordinal))
                        return null;

                    return int.TryParse(line.AsSpan(header.Length), out int version) ? version : null;
                }
            }

            return null;
        }
    }
}
