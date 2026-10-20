// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Counts the whole of a stretch that runs until it is stopped: a difficulty being open in the editor, or the game
    /// running.
    /// </summary>
    /// <remarks>
    /// Set beside <see cref="MappingTimeRecorder"/>, so how much of the time a beatmap was open went on changing it
    /// can be read off. Its owner ticks it every <see cref="MappingTimeRecorder.WRITE_INTERVAL"/> while it runs, so a
    /// crash loses at most that much.
    /// </remarks>
    public class RunningTimeRecorder
    {
        private readonly MappingTimeStore store;

        private MappingSpan span;

        private RunningTimeRecorder(MappingTimeStore store, MappingSpan span)
        {
            this.store = store;
            this.span = span;
        }

        public static RunningTimeRecorder ForGame(MappingTimeStore store, DateTimeOffset now) =>
            new RunningTimeRecorder(store, new MappingSpan(Guid.NewGuid(), Guid.Empty, Guid.Empty, string.Empty, string.Empty, string.Empty, now, now, MappingSpanKind.Game));

        public static RunningTimeRecorder ForBeatmap(MappingTimeStore store, BeatmapInfo beatmap, DateTimeOffset now) =>
            new RunningTimeRecorder(store, new MappingSpan(Guid.NewGuid(), beatmap.ID, beatmap.BeatmapSet?.ID ?? Guid.Empty,
                beatmap.Metadata.Artist, beatmap.Metadata.Title, beatmap.DifficultyName, now, now, MappingSpanKind.Open));

        /// <summary>
        /// Takes the stretch on to <paramref name="now"/> and writes it out.
        /// </summary>
        /// <remarks>
        /// Every time, with how often left to the caller's timer: a second check here only made the writes lag further.
        /// </remarks>
        public void Tick(DateTimeOffset now)
        {
            span = span with { End = now };
            store.Save(span);
        }

        /// <summary>
        /// Ends the stretch at <paramref name="now"/>.
        /// </summary>
        public void Stop(DateTimeOffset now)
        {
            span = span with { End = now };
            store.Save(span);
        }
    }
}
