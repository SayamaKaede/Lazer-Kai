// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Net;
using NUnit.Framework;
using osu.Game.Beatmaps;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class BeatmapMetadataLookupFailureTest
    {
        /// <summary>
        /// A lookup that comes back empty makes the beatmap unknown to the game: its online id is thrown away, taking
        /// its ranked status, its leaderboard and the credit for its difficulties with it, and nothing looks it up
        /// again. So only an answer counts as an answer.
        /// </summary>
        [TestCase(HttpStatusCode.NotFound, true)]
        [TestCase(HttpStatusCode.InternalServerError, false)]
        [TestCase(HttpStatusCode.BadGateway, false)]
        [TestCase(HttpStatusCode.ServiceUnavailable, false)]
        [TestCase(HttpStatusCode.TooManyRequests, false)]
        [TestCase(HttpStatusCode.Unauthorized, false)]
        public void TestOnlyNotFoundMeansTheBeatmapIsNotOnline(HttpStatusCode status, bool meansNotOnline)
        {
            Assert.That(APIBeatmapMetadataSource.FailureMeansNotOnline(status), Is.EqualTo(meansNotOnline));
        }

        /// <summary>
        /// A request that never reached the server, or was cut off before a reply, says nothing either.
        /// </summary>
        [Test]
        public void TestNoReplyMeansNothing()
        {
            Assert.That(APIBeatmapMetadataSource.FailureMeansNotOnline(null), Is.False);
        }
    }
}
