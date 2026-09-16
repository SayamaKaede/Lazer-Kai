// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Screens.Edit.Compose.Components.Timeline;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Where the editor says nightcore's cymbal falls, which has to be where the mod actually plays it.
    /// </summary>
    [TestFixture]
    public class NightcoreCymbalTimesTest
    {
        /// <summary>One bar of 4/4 at 120 BPM.</summary>
        private const double bar = 2000;

        [Test]
        public void TestEveryFourBarsFromTheTimingPoint()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(1000, new TimingControlPoint { BeatLength = 500 });

            double[] times = TimelineNightcoreCymbalDisplay.CymbalTimes(controlPoints, 0, 1000 + bar * 12).ToArray();

            Assert.That(times, Is.EqualTo(new[] { 1000, 1000 + bar * 4, 1000 + bar * 8, 1000 + bar * 12 }));
        }

        [Test]
        public void TestCountingStartsAgainAtTheNextTimingPoint()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(0, new TimingControlPoint { BeatLength = 500 });
            // two bars in, which is not where the first timing point's next cymbal would be.
            controlPoints.Add(bar * 2, new TimingControlPoint { BeatLength = 500 });

            double[] times = TimelineNightcoreCymbalDisplay.CymbalTimes(controlPoints, 0, bar * 6).ToArray();

            Assert.That(times, Is.EqualTo(new[] { 0, bar * 2, bar * 6 }));
        }

        [Test]
        public void TestTimeSignatureDecidesTheLength()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(0, new TimingControlPoint { BeatLength = 500, TimeSignature = TimeSignature.SimpleTriple });

            double[] times = TimelineNightcoreCymbalDisplay.CymbalTimes(controlPoints, 0, 500 * 3 * 4 * 2).ToArray();

            // three beats to the bar, four bars to a cymbal.
            Assert.That(times, Is.EqualTo(new[] { 0, 500 * 3 * 4, 500 * 3 * 4 * 2 }));
        }

        [Test]
        public void TestNoCymbalWhereTheFirstBarLineIsOmitted()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(1000, new TimingControlPoint { BeatLength = 500, OmitFirstBarLine = true });

            double[] times = TimelineNightcoreCymbalDisplay.CymbalTimes(controlPoints, 0, 1000 + bar * 8).ToArray();

            Assert.That(times, Is.EqualTo(new[] { 1000 + bar * 4, 1000 + bar * 8 }));
        }

        [Test]
        public void TestOnlyWithinTheRangeAsked()
        {
            var controlPoints = new ControlPointInfo();
            controlPoints.Add(0, new TimingControlPoint { BeatLength = 500 });

            double[] times = TimelineNightcoreCymbalDisplay.CymbalTimes(controlPoints, bar * 3, bar * 9).ToArray();

            Assert.That(times, Is.EqualTo(new[] { bar * 4, bar * 8 }));
        }
    }
}
