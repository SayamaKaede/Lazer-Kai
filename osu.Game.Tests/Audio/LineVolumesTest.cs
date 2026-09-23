// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Audio;

namespace osu.Game.Tests.Audio
{
    /// <summary>
    /// The green lines' volumes as gameplay reads them: the last line at or before a time, as stable does.
    /// </summary>
    [TestFixture]
    public class LineVolumesTest
    {
        private static readonly (double, int)[] lines = { (1000, 60), (2000, 20), (2000, 30), (3000, 10) };

        [TestCase(500, 100)]
        [TestCase(1000, 60)]
        [TestCase(1999, 60)]
        [TestCase(2000, 30)]
        [TestCase(2500, 30)]
        [TestCase(5000, 10)]
        public void TestTheLineInEffect(double time, int volume)
        {
            Assert.That(new LineVolumes(() => true, lines).VolumeAt(time), Is.EqualTo(volume));
        }

        [Test]
        public void TestEnabledFollowsTheSetting()
        {
            bool setting = false;
            var volumes = new LineVolumes(() => setting, lines);

            Assert.That(volumes.Enabled, Is.False);

            setting = true;
            Assert.That(volumes.Enabled, Is.True);
        }

        [Test]
        public void TestNoLinesIsNothingToFollow()
        {
            Assert.That(new LineVolumes(() => true, Array.Empty<(double, int)>()).Enabled, Is.False);
        }
    }
}
