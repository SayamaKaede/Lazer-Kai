// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Where the wheel takes the editor while it is playing: from white tick to white tick, as stable's does.
    /// </summary>
    /// <remarks>
    /// The mapper's account of un!stable (2026-10-11): a notch back goes to the white tick before; until the first tick
    /// after a white tick has gone by it is the white tick before that one, and after it, that one.
    /// </remarks>
    [TestFixture]
    public class WhiteTickSeekingTest
    {
        /// <summary>
        /// White ticks every 500 from 0.
        /// </summary>
        private static ControlPointInfo steady()
        {
            var timing = new ControlPointInfo();
            timing.Add(0, new TimingControlPoint { BeatLength = 500 });
            return timing;
        }

        /// <summary>
        /// White ticks every 1000 from 0, then every 500 from 1800: the first section is cut short of its beat at 2000.
        /// </summary>
        private static ControlPointInfo changing()
        {
            var timing = new ControlPointInfo();
            timing.Add(0, new TimingControlPoint { BeatLength = 1000 });
            timing.Add(1800, new TimingControlPoint { BeatLength = 500 });
            return timing;
        }

        // with ticks at quarters of a beat, the first after the white tick at 1000 is at 1125.
        [TestCase(1000, 500, Description = "on a white tick")]
        [TestCase(1100, 500, Description = "before the first tick after it")]
        [TestCase(1125, 500, Description = "on that tick")]
        [TestCase(1130, 1000, Description = "past it")]
        [TestCase(1499, 1000)]
        public void TestBackToTheWhiteTickBefore(double time, double expected)
            => Assert.That(WhiteTickSeeking.Before(steady(), time, 4), Is.EqualTo(expected));

        /// <summary>
        /// The tick that has to go by is the first of whatever the ticks are divided into.
        /// </summary>
        [TestCase(8, 1070, 1000, Description = "eighths: the first tick is at 1062.5")]
        [TestCase(8, 1060, 500)]
        [TestCase(2, 1240, 500, Description = "halves: the first tick is at 1250")]
        [TestCase(2, 1260, 1000)]
        [TestCase(1, 1400, 500, Description = "whole beats: the first tick is the next white tick")]
        public void TestTheFirstTickIsTheDivisors(int divisor, double time, double expected)
            => Assert.That(WhiteTickSeeking.Before(steady(), time, divisor), Is.EqualTo(expected));

        [TestCase(1000, 1500, Description = "on a white tick")]
        [TestCase(1100, 1500)]
        [TestCase(1499.5, 2000, Description = "the tick about to be played is not one to go forward to")]
        public void TestForwardToTheNextWhiteTick(double time, double expected)
            => Assert.That(WhiteTickSeeking.After(steady(), time), Is.EqualTo(expected));

        [Test]
        public void TestAcrossAChangeOfTiming()
        {
            // a new section starts on a white tick of its own.
            Assert.That(WhiteTickSeeking.After(changing(), 1500), Is.EqualTo(1800));
            Assert.That(WhiteTickSeeking.After(changing(), 1800), Is.EqualTo(2300));

            // within the first tick of the new section (125 at quarters), back to the white tick before its start.
            Assert.That(WhiteTickSeeking.Before(changing(), 1900, 4), Is.EqualTo(1000));
            Assert.That(WhiteTickSeeking.Before(changing(), 2000, 4), Is.EqualTo(1800));
        }

        [Test]
        public void TestSeveralAtOnce()
        {
            Assert.That(WhiteTickSeeking.Before(steady(), 1100, 4, 2), Is.EqualTo(0));
            Assert.That(WhiteTickSeeking.After(steady(), 1100, 3), Is.EqualTo(2500));
        }
    }
}
