// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A page of the replies in one discussion thread, with who wrote them.
    /// </summary>
    public class APIBeatmapsetDiscussionPosts
    {
        [JsonProperty(@"posts")]
        public List<APIBeatmapsetDiscussionPost> Posts { get; set; } = new List<APIBeatmapsetDiscussionPost>();

        [JsonProperty(@"users")]
        public List<APIUser> Users { get; set; } = new List<APIUser>();

        /// <summary>
        /// Null where this was the last page.
        /// </summary>
        [JsonProperty(@"cursor_string")]
        public string? CursorString { get; set; }
    }
}
