// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The part of a stretch that falls on one day, in local time.
    /// </summary>
    public record MappingDayPiece(MappingSpan Span, DateTime From, DateTime To)
    {
        public TimeSpan Length => To - From;
    }

    /// <summary>
    /// The time one difficulty was changed on a day, and the time it was open in the editor.
    /// </summary>
    public record MappingDifficultyTime(Guid BeatmapID, string DifficultyName, TimeSpan Total, TimeSpan Open);

    /// <summary>
    /// The time a beatmap set was changed on a day and the time it was open, and how both split between its difficulties.
    /// </summary>
    public record MappingSetTime(Guid BeatmapSetID, string Artist, string Title, TimeSpan Total, TimeSpan Open, IReadOnlyList<MappingDifficultyTime> Difficulties);

    /// <summary>
    /// The time spent mapping, by the day it was spent on.
    /// </summary>
    /// <remarks>
    /// A day runs from midnight to midnight where the player is. A stretch that crosses midnight is split there,
    /// so each day counts only its own part.
    ///
    /// The time a difficulty was open counts only on a day it was also changed: one opened just to look at is left
    /// out of the day altogether (2026-10-20, chosen by the user).
    /// </remarks>
    public class MappingTimeSummary
    {
        private readonly Dictionary<DateTime, List<MappingDayPiece>> days = new Dictionary<DateTime, List<MappingDayPiece>>();

        public MappingTimeSummary(IEnumerable<MappingSpan> spans, TimeZoneInfo zone)
        {
            foreach (var span in spans)
            {
                var start = TimeZoneInfo.ConvertTime(span.Start, zone).DateTime;
                var end = TimeZoneInfo.ConvertTime(span.End, zone).DateTime;

                for (var day = start.Date; day < end; day = day.AddDays(1))
                {
                    var from = start > day ? start : day;
                    var to = end < day.AddDays(1) ? end : day.AddDays(1);

                    if (to <= from)
                        continue;

                    if (!days.TryGetValue(day, out var pieces))
                        days[day] = pieces = new List<MappingDayPiece>();

                    pieces.Add(new MappingDayPiece(span, from, to));
                }
            }

            foreach (var pieces in days.Values)
                pieces.Sort((a, b) => a.From.CompareTo(b.From));
        }

        /// <summary>
        /// The parts of every stretch of changes on <paramref name="day"/>, earliest first.
        /// </summary>
        public IReadOnlyList<MappingDayPiece> PiecesOn(DateTime day) => piecesOn(day, MappingSpanKind.Edit).ToArray();

        /// <summary>
        /// The time spent changing beatmaps on <paramref name="day"/>.
        /// </summary>
        public TimeSpan TotalOn(DateTime day) => sum(piecesOn(day, MappingSpanKind.Edit));

        /// <summary>
        /// The time the difficulties changed on <paramref name="day"/> were open in the editor that day.
        /// </summary>
        public TimeSpan OpenOn(DateTime day) => sum(openPiecesOfEditedOn(day));

        /// <summary>
        /// The time the game was running on <paramref name="day"/>.
        /// </summary>
        public TimeSpan GameOn(DateTime day) => sum(piecesOn(day, MappingSpanKind.Game));

        public TimeSpan TotalIn(int year, int month) =>
            sum(days.Where(d => d.Key.Year == year && d.Key.Month == month).SelectMany(d => d.Value).Where(p => p.Span.Kind == MappingSpanKind.Edit));

        /// <summary>
        /// The sets changed on <paramref name="day"/>, the most time first, each named as it was last changed.
        /// </summary>
        public IReadOnlyList<MappingSetTime> SetsOn(DateTime day)
        {
            var open = openPiecesOfEditedOn(day).ToArray();

            return piecesOn(day, MappingSpanKind.Edit)
                   .GroupBy(p => p.Span.BeatmapSetID)
                   .Select(set =>
                   {
                       var latest = set.Last().Span;

                       var difficulties = set.GroupBy(p => p.Span.BeatmapID)
                                             .Select(d => new MappingDifficultyTime(d.Key, d.Last().Span.DifficultyName, sum(d),
                                                 sum(open.Where(p => p.Span.BeatmapID == d.Key))))
                                             .OrderByDescending(d => d.Total)
                                             .ToArray();

                       return new MappingSetTime(set.Key, latest.Artist, latest.Title, sum(set),
                           sum(open.Where(p => difficulties.Any(d => d.BeatmapID == p.Span.BeatmapID))), difficulties);
                   })
                   .OrderByDescending(s => s.Total)
                   .ToArray();
        }

        private IEnumerable<MappingDayPiece> piecesOn(DateTime day, MappingSpanKind kind) =>
            days.TryGetValue(day.Date, out var pieces) ? pieces.Where(p => p.Span.Kind == kind) : Enumerable.Empty<MappingDayPiece>();

        private IEnumerable<MappingDayPiece> openPiecesOfEditedOn(DateTime day)
        {
            var edited = piecesOn(day, MappingSpanKind.Edit).Select(p => p.Span.BeatmapID).ToHashSet();
            return piecesOn(day, MappingSpanKind.Open).Where(p => edited.Contains(p.Span.BeatmapID));
        }

        private static TimeSpan sum(IEnumerable<MappingDayPiece> pieces) =>
            pieces.Aggregate(TimeSpan.Zero, (total, p) => total + p.Length);
    }
}
