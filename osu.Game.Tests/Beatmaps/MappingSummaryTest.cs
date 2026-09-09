// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Select;
using osuTK;

namespace osu.Game.Tests.Beatmaps
{
    [TestFixture]
    public class MappingSummaryTest
    {
        [Test]
        public void TestDensityIsCountedOverWhatThereIsToPlay()
        {
            // five objects, the first at two seconds in and the last at six, is five objects over four seconds.
            var beatmap = beatmapWith(circleAt(2000), circleAt(3000), circleAt(4000), circleAt(5000), circleAt(6000));

            Assert.That(MappingSummary.Read(beatmap).Density, Is.EqualTo(1.25).Within(0.001));
        }

        [Test]
        public void TestEveryDivisorInUseIsReported()
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(250), circleAt(500), circleAt(1000));

            Assert.That(MappingSummary.Read(beatmap).Divisors, Is.EqualTo(new[] { 1, 2, 4 }));
        }

        /// <remarks>
        /// 1031 is a good way off every divisor of a beat of 1000 -- the finest of them lands on 1062.5 -- so it is
        /// snapped to nothing rather than being called the nearest thing to it.
        /// </remarks>
        [Test]
        public void TestAnObjectOnNoDivisorIsCalledUnsnappedRatherThanGivenTheClosestOne()
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(1031));

            var summary = MappingSummary.Read(beatmap);

            Assert.That(summary.UnsnappedObjects, Is.EqualTo(1));
            Assert.That(summary.Divisors, Is.EqualTo(new[] { 1 }));
        }

        /// <summary>
        /// That this counts what the editor marks, rather than answering the same question its own way.
        /// </summary>
        /// <remarks>
        /// Song select and the editor disagreeing about how many objects in a beatmap are off the beat would leave
        /// the reader with no way to tell which of the two to believe.
        /// </remarks>
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        public void TestOffTheBeatIsMeasuredTheWayTheEditorMarksIt(double outBy, int expected)
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(1000 + outBy));

            Assert.That(UnsnapMarker.TOLERATED_MS, Is.EqualTo(2), "this fixture is written around the editor's own allowance");
            Assert.That(MappingSummary.Read(beatmap).UnsnappedObjects, Is.EqualTo(expected));
        }

        /// <remarks>
        /// The editor marks the end of an object as well as its start, and its repeats along with them, so counting
        /// objects rather than moments would let a perfectly placed head hide an end that is nowhere near the beat.
        /// A spinner is used because its end is written down rather than worked out from a path and a velocity.
        /// </remarks>
        [Test]
        public void TestAnEndOffTheBeatIsCountedEvenWhereTheStartIsOnIt()
        {
            var beatmap = beatmapWith(new Spinner { StartTime = 0, EndTime = 1031 });

            Assert.That(MappingSummary.Read(beatmap).UnsnappedObjects, Is.EqualTo(1));
        }

        [Test]
        public void TestKiaiLeftOpenRunsToTheLastObject()
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(4000));

            beatmap.ControlPointInfo.Add(1000, new EffectControlPoint { KiaiMode = true });

            Assert.That(MappingSummary.Read(beatmap).KiaiTime, Is.EqualTo(3000));
        }

        [Test]
        public void TestKiaiStopsWhereItIsTurnedOff()
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(4000));

            beatmap.ControlPointInfo.Add(1000, new EffectControlPoint { KiaiMode = true });
            beatmap.ControlPointInfo.Add(2500, new EffectControlPoint { KiaiMode = false });

            Assert.That(MappingSummary.Read(beatmap).KiaiTime, Is.EqualTo(1500));
        }

        [Test]
        public void TestTheVolumeIsReadOffTheObjectsRatherThanTheControlPoints()
        {
            var quiet = new HitCircle { StartTime = 0, Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 30) } };
            var loud = new HitCircle { StartTime = 1000, Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 85) } };

            var summary = MappingSummary.Read(beatmapWith(quiet, loud));

            Assert.That(summary.LowestVolume, Is.EqualTo(30));
            Assert.That(summary.HighestVolume, Is.EqualTo(85));
        }

        [Test]
        public void TestBreaksAreAddedUp()
        {
            var beatmap = beatmapWith(circleAt(0), circleAt(10000));

            beatmap.Breaks.Add(new BreakPeriod(1000, 3000));
            beatmap.Breaks.Add(new BreakPeriod(5000, 6500));

            Assert.That(MappingSummary.Read(beatmap).BreakTime, Is.EqualTo(3500));
        }

        private static HitCircle circleAt(double time) => new HitCircle { StartTime = time };

        private static IBeatmap beatmapWith(params HitObject[] objects)
        {
            var beatmap = new Beatmap
            {
                HitObjects = new List<HitObject>(objects),
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 1000 });

            return beatmap;
        }
    }
}
