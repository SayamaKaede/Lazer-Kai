// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

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

        private static APIBeatmapsetDiscussion starting(string type, string message) => new APIBeatmapsetDiscussion
        {
            MessageType = type,
            StartingPost = new APIBeatmapsetDiscussionPost { Message = message },
        };
    }
}
