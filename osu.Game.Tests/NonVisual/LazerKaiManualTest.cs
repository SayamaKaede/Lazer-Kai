// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Game.IO;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// That the manual is looked for under the name it is shipped as, and that its absence is reported rather than
    /// guessed at.
    /// </summary>
    /// <remarks>
    /// The settings row hides itself on a null, so a name that does not line up would look exactly like a build
    /// that never had a manual copied into it -- which is what every build made straight out of the repository is.
    /// </remarks>
    [TestFixture]
    public class LazerKaiManualTest
    {
        private string directory = null!;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(directory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        [Test]
        public void TestFoundWhereItShips()
        {
            string expected = Path.Combine(directory, LazerKaiManual.FILENAME);
            File.WriteAllText(expected, @"<html></html>");

            Assert.That(LazerKaiManual.Find(directory), Is.EqualTo(expected));
        }

        [Test]
        public void TestNotThere()
        {
            Assert.That(LazerKaiManual.Find(directory), Is.Null);
        }

        [Test]
        public void TestAnotherHtmlFileIsNotTheManual()
        {
            File.WriteAllText(Path.Combine(directory, @"index.html"), @"<html></html>");

            Assert.That(LazerKaiManual.Find(directory), Is.Null);
        }

        [TestCase(null)]
        [TestCase(@"")]
        public void TestNowhereToLook(string? nowhere)
        {
            Assert.That(LazerKaiManual.Find(nowhere), Is.Null);
        }

        [Test]
        public void TestGuideFoundWhereItShips()
        {
            string expected = Path.Combine(directory, LazerKaiManual.GUIDE_FILENAME);
            File.WriteAllText(expected, @"<html></html>");

            Assert.That(LazerKaiManual.FindGuide(directory), Is.EqualTo(expected));
        }

        [Test]
        public void TestTheManualIsNotTheGuide()
        {
            File.WriteAllText(Path.Combine(directory, LazerKaiManual.FILENAME), @"<html></html>");

            Assert.That(LazerKaiManual.FindGuide(directory), Is.Null);
            Assert.That(LazerKaiManual.Find(directory), Is.Not.Null);
        }

        [TestCase(null)]
        [TestCase(@"")]
        public void TestNowhereToLookForTheGuide(string? nowhere)
        {
            Assert.That(LazerKaiManual.FindGuide(nowhere), Is.Null);
        }
    }
}
