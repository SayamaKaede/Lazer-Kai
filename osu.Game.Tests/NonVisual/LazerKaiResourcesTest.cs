// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.IO;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// That this fork's own artwork is embedded under the name the game asks for it by, and that the game can read
    /// the shape of the cog off it.
    /// </summary>
    /// <remarks>
    /// A drawing that is not found leaves an empty sprite and says nothing, and one whose hole cannot be found
    /// leaves 改 somewhere other than in it. Neither complains.
    /// </remarks>
    [TestFixture]
    public class LazerKaiResourcesTest
    {
        [TestCase(@"kai")]
        [TestCase(@"main")]
        public void TestArtworkIsThere(string name)
        {
            var store = LazerKaiArtwork.CreateStore(null);

            Assert.That(store.Get($@"{name}.png"), Is.Not.Null.And.Length.GreaterThan(0));
        }

        [Test]
        public void TestTheCogsHoleCanBeFound()
        {
            var measurements = LazerKaiMark.Measure(LazerKaiArtwork.CreateStore(null).Get(@"main.png"));

            Assert.That(measurements.Found);

            // the hole is a good part of the cog, and the cog is a good part of the drawing, so anything tiny here
            // means some other gap was mistaken for it.
            Assert.That(measurements.HoleWidth, Is.GreaterThan(0.1f).And.LessThan(0.9f));

            // and it is somewhere inside the picture rather than against an edge.
            Assert.That(measurements.HoleCentre.X, Is.GreaterThan(0.15f).And.LessThan(0.85f));
            Assert.That(measurements.HoleCentre.Y, Is.GreaterThan(0.15f).And.LessThan(0.85f));

            Assert.That(measurements.Aspect, Is.GreaterThan(0.1f).And.LessThan(10f));
        }

        /// <remarks>
        /// Reachable by ordinary means: a replacement drawing is whatever somebody dropped in the folder.
        /// </remarks>
        [Test]
        public void TestNonsenseDrawsNothingRatherThanThrowing()
        {
            Assert.That(LazerKaiMark.Measure(null).Found, Is.False);
            Assert.That(LazerKaiMark.Measure(new byte[] { 1, 2, 3, 4 }).Found, Is.False);
        }

        /// <summary>
        /// That a drawing dropped into the folder is used instead of the one inside the build, which is the whole
        /// reason the folder exists.
        /// </summary>
        [Test]
        public void TestADroppedInDrawingWins()
        {
            using (var host = new TemporaryNativeStorage(nameof(TestADroppedInDrawingWins)))
            {
                byte[] embedded = LazerKaiArtwork.CreateStore(null).Get(@"main.png");
                byte[] replacement = { 0x2A, 0x2A, 0x2A, 0x2A };

                using (var file = host.GetStorageForDirectory(LazerKaiArtwork.DIRECTORY).CreateFileSafely(@"main.png"))
                    file.Write(replacement, 0, replacement.Length);

                var store = LazerKaiArtwork.CreateStore(host);

                Assert.That(store.Get(@"main.png"), Is.EqualTo(replacement));
                Assert.That(store.Get(@"main.png"), Is.Not.EqualTo(embedded));

                // and the one nobody replaced still comes out of the build.
                Assert.That(store.Get(@"kai.png"), Is.EqualTo(LazerKaiArtwork.CreateStore(null).Get(@"kai.png")));
            }
        }
    }
}
