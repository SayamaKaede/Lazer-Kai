// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A post in a beatmapset's discussion: the one that starts a thread, or a reply to it.
    /// </summary>
    public class APIBeatmapsetDiscussionPost
    {
        [JsonProperty(@"id")]
        public long Id { get; set; }

        [JsonProperty(@"user_id")]
        public int UserId { get; set; }

        [JsonProperty(@"message")]
        public string Message { get; set; } = string.Empty;
    }
}
