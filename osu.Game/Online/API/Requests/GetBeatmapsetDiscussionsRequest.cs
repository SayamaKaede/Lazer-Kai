// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.IO.Network;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Online.API.Requests
{
    /// <summary>
    /// Asks for a page of a beatmapset's discussion threads, each with the post that started it.
    /// </summary>
    /// <remarks>
    /// The replies are not included and are asked for one thread at a time, with
    /// <see cref="GetBeatmapsetDiscussionPostsRequest"/>.
    /// </remarks>
    public class GetBeatmapsetDiscussionsRequest : APIRequest<APIBeatmapsetDiscussions>
    {
        public readonly int BeatmapSetId;

        /// <summary>
        /// Which page, from 1.
        /// </summary>
        /// <remarks>
        /// By number rather than by the cursor the response carries: the website's cursor is only the next page's
        /// number, and the replies' request takes nothing else, so both are asked for the same way.
        /// </remarks>
        public readonly int Page;

        public GetBeatmapsetDiscussionsRequest(int beatmapSetId, int page = 1)
        {
            BeatmapSetId = beatmapSetId;
            Page = page;
        }

        protected override WebRequest CreateWebRequest()
        {
            var req = base.CreateWebRequest();

            req.AddParameter(@"beatmapset_id", BeatmapSetId.ToString());
            req.AddParameter(@"limit", @"50");
            req.AddParameter(@"page", Page.ToString());

            return req;
        }

        protected override string Target => @"beatmapsets/discussions";
    }
}
