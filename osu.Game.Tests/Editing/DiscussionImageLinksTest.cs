// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Discussion;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Which links in a discussion post are images to show under it (2026-10-19 request).
    /// </summary>
    [TestFixture]
    public class DiscussionImageLinksTest
    {
        /// <summary>
        /// The markdown for an image is only there to say "this is an image", and read as text it is brackets around a
        /// link. It is taken out, and the image shown instead.
        /// </summary>
        [Test]
        public void TestAMarkdownImageIsTakenOutOfTheText()
        {
            var (text, images) = DiscussionImageLinks.Extract("00:12:345 (1) - like this\n![](https://example.com/shot.png)");

            Assert.That(text, Is.EqualTo("00:12:345 (1) - like this"));
            Assert.That(images, Is.EqualTo(new[] { "https://example.com/shot.png" }));
        }

        /// <summary>
        /// A link to an image file is left in the text, where it can still be opened in the browser.
        /// </summary>
        [Test]
        public void TestALinkToAnImageFileStaysInTheText()
        {
            const string post = "see https://cdn.example.com/a/shot.JPG?ex=1 here";

            var (text, images) = DiscussionImageLinks.Extract(post);

            Assert.That(text, Is.EqualTo(post));
            Assert.That(images, Is.EqualTo(new[] { "https://cdn.example.com/a/shot.JPG?ex=1" }));
        }

        [TestCase("https://imgur.com/AbC123", "https://i.imgur.com/AbC123.png")]
        [TestCase("https://m.imgur.com/AbC123", "https://i.imgur.com/AbC123.png")]
        // the address Gyazo's own oEmbed gives for a page (2026-10-19).
        [TestCase("https://gyazo.com/0123456789abcdef0123456789abcdef", "https://i.gyazo.com/0123456789abcdef0123456789abcdef.png")]
        // a screenshot shared with Shift+F12, which stable still links over http (2026-10-19 report).
        [TestCase("http://osu.ppy.sh/ss/17259458/4ce7", "https://osu.ppy.sh/ss/17259458/4ce7")]
        [TestCase("https://osu.ppy.sh/ss/17259458", "https://osu.ppy.sh/ss/17259458")]
        public void TestAPageForOneImageIsThatImage(string page, string image)
        {
            Assert.That(DiscussionImageLinks.Extract($"look: {page}").Images, Is.EqualTo(new[] { image }));
        }

        /// <summary>
        /// An album is a page of several images, which cannot be told from its address.
        /// </summary>
        [TestCase("https://imgur.com/a/AbC123")]
        [TestCase("https://imgur.com/gallery/AbC123")]
        [TestCase("https://osu.ppy.sh/beatmapsets/1#osu/2")]
        [TestCase("https://example.com/notes.txt")]
        public void TestAnythingElseIsNotAnImage(string link)
        {
            Assert.That(DiscussionImageLinks.Extract($"look: {link}").Images, Is.Empty);
        }

        [Test]
        public void TestAMarkdownImageOfAnImgurPageIsThatImage()
        {
            Assert.That(DiscussionImageLinks.Extract("![](https://imgur.com/AbC123)").Images, Is.EqualTo(new[] { "https://i.imgur.com/AbC123.png" }));
        }

        [Test]
        public void TestTheFullStopAfterALinkIsNotPartOfIt()
        {
            Assert.That(DiscussionImageLinks.Extract("It looks like https://example.com/shot.png.").Images, Is.EqualTo(new[] { "https://example.com/shot.png" }));
        }

        [Test]
        public void TestImagesAreInTheOrderTheyWereWrittenAndOnlyOnce()
        {
            var (_, images) = DiscussionImageLinks.Extract("https://imgur.com/First1 then ![](https://example.com/second.png) then https://imgur.com/First1 again");

            Assert.That(images, Is.EqualTo(new[] { "https://i.imgur.com/First1.png", "https://example.com/second.png" }));
        }
    }
}
