// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace osu.Game.Online.API.Requests.Responses
{
    /// <summary>
    /// A page of a beatmapset's discussion threads, with who started them.
    /// </summary>
    public class APIBeatmapsetDiscussions
    {
        [JsonProperty(@"discussions")]
        public List<APIBeatmapsetDiscussion> Discussions { get; set; } = new List<APIBeatmapsetDiscussion>();

        /// <summary>
        /// The issues raised in the reviews among <see cref="Discussions"/>: threads of their own, which the website
        /// hands out apart from the page and never among it.
        /// </summary>
        [JsonProperty(@"included_discussions")]
        public List<APIBeatmapsetDiscussion> IncludedDiscussions { get; set; } = new List<APIBeatmapsetDiscussion>();

        [JsonProperty(@"users")]
        public List<APIUser> Users { get; set; } = new List<APIUser>();

        /// <summary>
        /// Null where this was the last page.
        /// </summary>
        [JsonProperty(@"cursor_string")]
        public string? CursorString { get; set; }
    }
}
