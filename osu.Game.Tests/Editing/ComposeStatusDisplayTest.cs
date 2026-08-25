// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class ComposeStatusDisplayTest
    {
        /// <summary>
        /// Milliseconds are shown because that is the unit the editor works in — every timestamp a mapper reads,
        /// writes or is sent in a modding post carries them.
        /// </summary>
        [TestCase(0, "0:00.000")]
        [TestCase(1_000, "0:01.000")]
        [TestCase(65_000, "1:05.000")]
        [TestCase(83_456, "1:23.456")]
        [TestCase(599_999, "9:59.999")]
        public void TestDrainTimeReadsAsMinutesSecondsAndMilliseconds(double milliseconds, string expected)
        {
            Assert.That(ComposeStatusDisplay.DrainTimeReadout(milliseconds), Is.EqualTo(expected));
        }

        /// <summary>
        /// Marathon beatmaps run past the hour. Formatting through a time span's minute component would start the
        /// count again at zero there and report an hour-long beatmap as a short one.
        /// </summary>
        [TestCase(3_600_000, "60:00.000")]
        [TestCase(3_661_001, "61:01.001")]
        public void TestDrainTimePastAnHourKeepsCountingMinutes(double milliseconds, string expected)
        {
            Assert.That(ComposeStatusDisplay.DrainTimeReadout(milliseconds), Is.EqualTo(expected));
        }

        /// <summary>
        /// A beatmap whose breaks cover more than its objects would drain for a negative length, which is not a thing
        /// to put on screen.
        /// </summary>
        [Test]
        public void TestNegativeDrainTimeReadsAsNothing()
        {
            Assert.That(ComposeStatusDisplay.DrainTimeReadout(-1_000), Is.EqualTo("0:00.000"));
        }

        /// <summary>
        /// Seconds and milliseconds are always padded, so the row does not jump about as the numbers cross a digit.
        /// </summary>
        [TestCase(9_007, "0:09.007")]
        [TestCase(9_070, "0:09.070")]
        public void TestSecondsAndMillisecondsAreAlwaysPadded(double milliseconds, string expected)
        {
            Assert.That(ComposeStatusDisplay.DrainTimeReadout(milliseconds), Is.EqualTo(expected));
        }

        /// <summary>
        /// A drain length is a measurement rather than a countdown, so a fraction of a millisecond is dropped rather
        /// than rounded up into a millisecond that has not passed.
        /// </summary>
        [Test]
        public void TestPartOfAMillisecondIsDropped()
        {
            Assert.That(ComposeStatusDisplay.DrainTimeReadout(1_999.9), Is.EqualTo("0:01.999"));
        }
    }
}
