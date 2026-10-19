// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Discussion;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Where each image in the discussion is asked for.
    /// </summary>
    [TestFixture]
    public class DiscussionImageStoreTest
    {
        private const string website = "https://osu.ppy.sh";

        /// <summary>
        /// The website's media proxy answers 404 for every imgur image, which imgur itself serves (2026-10-19, read in
        /// the game's network log and asked of both by hand). So imgur is asked directly, as the player chose.
        /// </summary>
        [Test]
        public void TestImgurIsAskedDirectly()
        {
            Assert.That(DiscussionImageStore.SourceOf("https://i.imgur.com/1QD2MSg.jpeg", website),
                Is.EqualTo(new ImageSource("https://i.imgur.com/1QD2MSg.jpeg", true)));
        }

        /// <summary>
        /// The website's own images, such as a screenshot shared with Shift+F12, need no proxy to be fetched.
        /// </summary>
        [Test]
        public void TestTheWebsitesOwnAreAskedOfTheWebsite()
        {
            Assert.That(DiscussionImageStore.SourceOf("https://osu.ppy.sh/ss/17259458/4ce7", website),
                Is.EqualTo(new ImageSource("https://osu.ppy.sh/ss/17259458/4ce7", false)));
        }

        [Test]
        public void TestAnythingElseIsAskedThroughTheProxy()
        {
            Assert.That(DiscussionImageStore.SourceOf("https://example.com/a b.png?x=1&y=2", website),
                Is.EqualTo(new ImageSource("https://osu.ppy.sh/media-url?url=https%3A%2F%2Fexample.com%2Fa%20b.png%3Fx%3D1%26y%3D2", false)));
        }
    }
}
