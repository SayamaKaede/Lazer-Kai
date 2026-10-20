// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Turns the changes made in one editor into the stretches of time spent mapping.
    /// </summary>
    /// <remarks>
    /// Only time around a change counts: each one keeps the stretch going for <see cref="IDLE_LIMIT"/> after it, so
    /// time spent looking at a beatmap without touching it, or away from the desk with it open, is never counted.
    /// </remarks>
    public class MappingTimeRecorder
    {
        /// <summary>
        /// How long after a change the time is still counted as mapping.
        /// </summary>
        public static readonly TimeSpan IDLE_LIMIT = TimeSpan.FromMinutes(5);

        /// <summary>
        /// How often a stretch that keeps going is written out, so a crash loses at most this much of it.
        /// </summary>
        public static readonly TimeSpan WRITE_INTERVAL = TimeSpan.FromMinutes(1);

        private readonly MappingTimeStore store;

        private MappingSpan? current;
        private DateTimeOffset lastWrite;

        public MappingTimeRecorder(MappingTimeStore store)
        {
            this.store = store;
        }

        /// <summary>
        /// Counts the time around a change made to <paramref name="beatmap"/> at <paramref name="now"/>.
        /// </summary>
        public void Changed(BeatmapInfo beatmap, DateTimeOffset now)
        {
            var metadata = beatmap.Metadata;

            if (current != null && current.BeatmapID == beatmap.ID && now <= current.End)
            {
                current = current with
                {
                    Artist = metadata.Artist,
                    Title = metadata.Title,
                    DifficultyName = beatmap.DifficultyName,
                    End = now + IDLE_LIMIT,
                };

                if (now - lastWrite >= WRITE_INTERVAL)
                    write(now);

                return;
            }

            // the stretch before was over before this change (or was another difficulty's), so it ends where it stands.
            if (current != null)
                store.Save(current);

            current = new MappingSpan(Guid.NewGuid(), beatmap.ID, beatmap.BeatmapSet?.ID ?? Guid.Empty,
                metadata.Artist, metadata.Title, beatmap.DifficultyName, now, now + IDLE_LIMIT);

            write(now);
        }

        /// <summary>
        /// Ends the stretch at <paramref name="now"/> if it was still going, as the editor is being left.
        /// </summary>
        public void Stop(DateTimeOffset now)
        {
            if (current == null)
                return;

            if (now < current.End)
                current = current with { End = now };

            write(now);
            current = null;
        }

        private void write(DateTimeOffset now)
        {
            store.Save(current!);
            lastWrite = now;
        }
    }
}
