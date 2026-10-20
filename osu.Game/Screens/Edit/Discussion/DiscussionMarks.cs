// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// What the discussion panel and the timeline along the bottom share: where the threads listed are, and which one
    /// was clicked there (2026-10-20 request).
    /// </summary>
    /// <remarks>
    /// The timeline shows what the panel's tab and user lists, not everything, so that picking the pending tab leaves
    /// on the timeline only what is still waiting. Nothing is there until the panel has read the discussion; it stays
    /// once the panel is closed.
    /// </remarks>
    public class DiscussionMarks
    {
        /// <summary>
        /// The threads at a time in this difficulty that the panel lists.
        /// </summary>
        public readonly Bindable<APIBeatmapsetDiscussion[]> Timeline = new Bindable<APIBeatmapsetDiscussion[]>(Array.Empty<APIBeatmapsetDiscussion>());

        /// <summary>
        /// Raised when a thread is clicked on the timeline, so the panel can bring it into view.
        /// </summary>
        public event Action<APIBeatmapsetDiscussion>? Picked;

        public void Pick(APIBeatmapsetDiscussion discussion) => Picked?.Invoke(discussion);
    }
}
