// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// Sorts a beatmapset's discussion into what the editor shows of it, for the one difficulty being edited.
    /// </summary>
    /// <remarks>
    /// Other difficulties' threads are left out altogether: their times are about objects this one does not have, and
    /// going to them here would select the wrong ones.
    /// </remarks>
    public static class Discussions
    {
        private const string review = @"review";

        /// <summary>
        /// What was raised at a time in this difficulty, in the order the song plays it.
        /// </summary>
        public static IEnumerable<APIBeatmapsetDiscussion> Timeline(IEnumerable<APIBeatmapsetDiscussion> all, int beatmapId)
            => all.Where(d => d.BeatmapId == beatmapId && d.Timestamp != null).OrderBy(d => d.Timestamp).ThenBy(d => d.Id);

        /// <summary>
        /// What was raised about this difficulty as a whole, oldest first.
        /// </summary>
        public static IEnumerable<APIBeatmapsetDiscussion> ThisDifficulty(IEnumerable<APIBeatmapsetDiscussion> all, int beatmapId)
            => all.Where(d => d.BeatmapId == beatmapId && d.Timestamp == null).OrderBy(d => d.Id);

        /// <summary>
        /// What was raised about every difficulty at once, oldest first.
        /// </summary>
        public static IEnumerable<APIBeatmapsetDiscussion> WholeSet(IEnumerable<APIBeatmapsetDiscussion> all)
            => all.Where(d => d.BeatmapId == null).OrderBy(d => d.Id);

        /// <summary>
        /// The post that started a thread, as it reads on the website.
        /// </summary>
        /// <remarks>
        /// A review is kept as the blocks it was written in. Its paragraphs are what was written, and the problems it
        /// raised are threads of their own, listed with the rest. Anything that does not read as those blocks is shown
        /// as it was sent rather than not at all.
        /// </remarks>
        public static string StartingText(APIBeatmapsetDiscussion discussion)
        {
            string message = discussion.StartingPost?.Message ?? string.Empty;

            if (discussion.MessageType != review)
                return message;

            try
            {
                var paragraphs = JArray.Parse(message)
                                       .OfType<JObject>()
                                       .Where(block => (string?)block[@"type"] == @"paragraph")
                                       .Select(block => (string?)block[@"text"] ?? string.Empty)
                                       .ToArray();

                return paragraphs.Length > 0 ? string.Join('\n', paragraphs) : message;
            }
            catch (JsonException)
            {
                return message;
            }
        }
    }
}
