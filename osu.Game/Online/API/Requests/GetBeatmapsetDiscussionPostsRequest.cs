// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Asks for a page of the replies in one discussion thread, oldest first.
    /// </summary>
    /// <remarks>
    /// Only replies: the post that started the thread comes with the thread, and the website's own notes (resolved,
    /// reopened) are left out the way the website asks for them by default.
    /// </remarks>
    public class GetBeatmapsetDiscussionPostsRequest : APIRequest<APIBeatmapsetDiscussionPosts>
    {
        public readonly long DiscussionId;

        /// <summary>
        /// Which page, from 1. The website documents no cursor for this request, only the page.
        /// </summary>
        public readonly int Page;

        public GetBeatmapsetDiscussionPostsRequest(long discussionId, int page = 1)
        {
            DiscussionId = discussionId;
            Page = page;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter(@"beatmapset_discussion_id", DiscussionId.ToString());
            req.AddParameter(@"sort", @"id_asc");
            req.AddParameter(@"limit", @"50");
            req.AddParameter(@"page", Page.ToString());

            return req;
        }

        protected override string Target => @"beatmapsets/discussions/posts";
    }
}
