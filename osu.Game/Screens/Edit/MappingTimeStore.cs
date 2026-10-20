// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// What a <see cref="MappingSpan"/> is a stretch of.
    /// </summary>
    public enum MappingSpanKind
    {
        /// <summary>
        /// Changing one difficulty in the editor, counted only around the changes made.
        /// </summary>
        Edit,

        /// <summary>
        /// Having one difficulty open in the editor, whether anything was changed or not.
        /// </summary>
        Open,

        /// <summary>
        /// The game running. Carries no beatmap.
        /// </summary>
        Game,
    }

    /// <summary>
    /// A stretch of time spent on one difficulty in the editor, or with the game running.
    /// </summary>
    /// <remarks>
    /// The names are kept as they were while it was being mapped, so a stretch still reads as what it was after the
    /// beatmap is renamed or deleted.
    /// </remarks>
    public record MappingSpan(
        Guid ID,
        Guid BeatmapID,
        Guid BeatmapSetID,
        string Artist,
        string Title,
        string DifficultyName,
        DateTimeOffset Start,
        DateTimeOffset End,
        MappingSpanKind Kind = MappingSpanKind.Edit);

    /// <summary>
    /// Where the time spent mapping is kept: one file, every stretch in the order it began.
    /// </summary>
    public class MappingTimeStore
    {
        public const string FILENAME = "mapping-time.json";

        private readonly Storage storage;

        public MappingTimeStore(Storage storage)
        {
            this.storage = storage;
        }

        public IReadOnlyList<MappingSpan> Load()
        {
            if (!storage.Exists(FILENAME))
                return Array.Empty<MappingSpan>();

            try
            {
                using (var stream = storage.GetStream(FILENAME))
                using (var reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<List<MappingSpan>>(reader.ReadToEnd())?
                                      .Where(s => s.End > s.Start)
                                      .ToArray()
                           ?? Array.Empty<MappingSpan>();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read the time spent mapping");
                return Array.Empty<MappingSpan>();
            }
        }

        /// <summary>
        /// Keeps <paramref name="span"/>, in place of the one with its <see cref="MappingSpan.ID"/> if there is one already.
        /// </summary>
        public void Save(MappingSpan span)
        {
            var spans = Load().ToList();
            int existing = spans.FindIndex(s => s.ID == span.ID);

            if (existing >= 0)
                spans[existing] = span;
            else
                spans.Add(span);

            try
            {
                using (var stream = storage.CreateFileSafely(FILENAME))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(spans, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the time spent mapping");
            }
        }
    }
}
