// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Updater;

namespace osu.Game.Tests.Updater
{
    [TestFixture]
    public class LazerKaiReleaseTest
    {
        /// <summary>
        /// The feed is edited by hand, and this is what is actually being served today — trailing comma and all.
        /// </summary>
        [Test]
        public void TestReadsTheFeedAsItIsWritten()
        {
            var release = LazerKaiRelease.FromJson(@"{
  ""version"": ""20260820.0"",
  ""url"": ""https://drive.google.com/file/d/1BkMCzPOVPFPL4inyyEjH_Kf8PHw0vkND/view?usp=sharing"",
}");

            Assert.That(release, Is.Not.Null);
            Assert.That(release!.Version, Is.EqualTo("20260820.0"));
            Assert.That(release.Url, Does.StartWith("https://drive.google.com/"));
            Assert.That(release.Notes, Is.Null);
        }

        [TestCase("20260820.1", "20260820.0", true)]
        [TestCase("20260821.0", "20260820.3", true)]
        [TestCase("20260820.0", "20260820.0", false)]
        [TestCase("20260820.0", "20260820.1", false)]
        [TestCase("20260819.9", "20260820.0", false)]
        public void TestComparesDayThenRevision(string feed, string running, bool newer)
        {
            Assert.That(new LazerKaiRelease { Version = feed }.IsNewerThan(running), Is.EqualTo(newer));
        }

        /// <summary>
        /// A portable build is stamped `20260820 861133`, which is nobody's to update, and a feed that has been typed
        /// wrong must not start telling everyone there is a new release.
        /// </summary>
        [TestCase("20260820 861133")]
        [TestCase("local debug")]
        [TestCase("")]
        public void TestAnythingNotShapedLikeAReleaseIsIgnored(string running)
        {
            Assert.That(new LazerKaiRelease { Version = "20260820.1" }.IsNewerThan(running), Is.False);
            Assert.That(new LazerKaiRelease { Version = running }.IsNewerThan("20260820.0"), Is.False);
        }
    }
}
