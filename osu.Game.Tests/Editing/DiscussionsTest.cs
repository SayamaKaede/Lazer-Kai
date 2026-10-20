// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Edit.Discussion;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// How a discussion post reads in the editor.
    /// </summary>
    [TestFixture]
    public class DiscussionsTest
    {
        /// <summary>
        /// A review is kept by the website as the blocks it was written in, and read out as raw text it is a wall of
        /// brackets. Its paragraphs are what was written; the problems it raised are threads of their own and are listed
        /// as such.
        /// </summary>
        [Test]
        public void TestAReviewReadsAsItsParagraphs()
        {
            const string blocks = @"[{""type"":""paragraph"",""text"":""Overall fine.""},{""type"":""embed"",""discussion_id"":12},{""type"":""paragraph"",""text"":""Good luck!""}]";

            Assert.That(Discussions.StartingText(starting("review", blocks)), Is.EqualTo("Overall fine.\nGood luck!"));
        }

        [Test]
        public void TestAReviewThatIsNotBlocksReadsAsWritten()
        {
            Assert.That(Discussions.StartingText(starting("review", "[just text]")), Is.EqualTo("[just text]"));
        }

        [Test]
        public void TestAnythingButAReviewReadsAsWritten()
        {
            const string text = @"00:12:345 (1,2) - [{""type"":""paragraph""}] is not a review";

            Assert.That(Discussions.StartingText(starting("problem", text)), Is.EqualTo(text));
        }

        /// <summary>
        /// The tabs divide the threads the way the website's do (2026-10-20 request).
        /// </summary>
        [TestCase(DiscussionFilter.Mine, new long[] { 1, 6 })]
        [TestCase(DiscussionFilter.Notes, new long[] { 6 })]
        [TestCase(DiscussionFilter.Resolved, new long[] { 2 })]
        [TestCase(DiscussionFilter.Pending, new long[] { 1, 3 })]
        [TestCase(DiscussionFilter.Praises, new long[] { 4, 5 })]
        [TestCase(DiscussionFilter.All, new long[] { 1, 2, 3, 4, 5, 6 })]
        public void TestTabs(DiscussionFilter filter, long[] expected)
        {
            var threads = new[]
            {
                thread(1, "problem", user: 7),
                thread(2, "suggestion", resolved: true),
                thread(3, "suggestion"),
                thread(4, "praise"),
                thread(5, "hype"),
                thread(6, "mapper_note", user: 7),
            };

            Assert.That(threads.Where(t => Discussions.Matches(t, filter, 7)).Select(t => t.Id), Is.EqualTo(expected));
        }

        [Test]
        public void TestSortingByUpdatedPutsTheLastChangedFirst()
        {
            var start = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

            var threads = new[]
            {
                thread(1, "problem", updated: start.AddDays(3)),
                thread(2, "problem", updated: start.AddDays(5)),
                thread(3, "problem", updated: start),
            };

            Assert.That(Discussions.Sort(threads, DiscussionSort.Updated).Select(t => t.Id), Is.EqualTo(new long[] { 2, 1, 3 }));
            Assert.That(Discussions.Sort(threads, DiscussionSort.Default).Select(t => t.Id), Is.EqualTo(new long[] { 1, 2, 3 }));
        }

        private static APIBeatmapsetDiscussion thread(long id, string type, int user = 2, bool resolved = false, DateTimeOffset? updated = null) => new APIBeatmapsetDiscussion
        {
            Id = id,
            MessageType = type,
            UserId = user,
            Resolved = resolved,
            UpdatedAt = updated,
        };

        private static APIBeatmapsetDiscussion starting(string type, string message) => new APIBeatmapsetDiscussion
        {
            MessageType = type,
            StartingPost = new APIBeatmapsetDiscussionPost { Message = message },
        };
    }
}
