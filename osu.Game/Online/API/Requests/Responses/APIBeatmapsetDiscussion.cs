// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// One thread of a beatmapset's discussion: what was raised, and on which difficulty and at what time.
    /// </summary>
    public class APIBeatmapsetDiscussion
    {
        [JsonProperty(@"id")]
        public long Id { get; set; }

        /// <summary>
        /// The difficulty this was raised on, or null where it was raised on the whole set.
        /// </summary>
        [JsonProperty(@"beatmap_id")]
        public int? BeatmapId { get; set; }

        /// <summary>
        /// Where in the song this is about, in milliseconds, or null where it is about the difficulty as a whole.
        /// </summary>
        [JsonProperty(@"timestamp")]
        public int? Timestamp { get; set; }

        /// <summary>
        /// One of hype, mapper_note, praise, problem, review and suggestion.
        /// </summary>
        /// <remarks>
        /// Kept as the text the website sends rather than an enum, so that a kind added there later is shown as a
        /// plain post instead of failing the whole list.
        /// </remarks>
        [JsonProperty(@"message_type")]
        public string MessageType { get; set; } = string.Empty;

        [JsonProperty(@"resolved")]
        public bool Resolved { get; set; }

        [JsonProperty(@"user_id")]
        public int UserId { get; set; }

        [JsonProperty(@"starting_post")]
        public APIBeatmapsetDiscussionPost? StartingPost { get; set; }

        /// <summary>
        /// When the thread was last changed: replied to, resolved or reopened, or edited.
        /// </summary>
        [JsonProperty(@"updated_at")]
        public DateTimeOffset? UpdatedAt { get; set; }
    }
}
