// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DrainTimeTest
    {
        /// <summary>
        /// With nothing to leave out, draining runs from the first object to the end of the last.
        /// </summary>
        [Test]
        public void TestABeatmapWithoutBreaksDrainsFromEndToEnd()
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(new TestObject { StartTime = 1_000 });
            beatmap.HitObjects.Add(new TestDurationObject { StartTime = 4_000, Duration = 500 });

            Assert.That(ComposeStatusDisplay.DrainTimeFor(beatmap), Is.EqualTo(3_500));
        }

        /// <summary>
        /// A break is written into the beatmap with a gap held before and after it, so the stored period is shorter
        /// than the stretch where nothing is actually played. What a mapper is judged on is the stretch, so that is
        /// what comes off — the same number Mapping Verifier reports, rather than the shorter one the file states.
        /// </summary>
        [Test]
        public void TestABreakTakesOffTheWholeGapAroundIt()
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(new TestObject { StartTime = 0 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 1_000 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 2_000 });
            beatmap.Breaks.Add(new BreakPeriod(1_200, 1_800));

            // the stored period is 600ms; the gap it sits in is the whole 1000ms between the objects around it.
            Assert.That(ComposeStatusDisplay.DrainTimeFor(beatmap), Is.EqualTo(1_000));
        }

        /// <summary>
        /// The gap is measured from where the object before the break finishes, not from where it starts, so a
        /// slider running into a break is played rather than rested through.
        /// </summary>
        [Test]
        public void TestTheGapStartsWhereTheObjectBeforeItEnds()
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(new TestObject { StartTime = 0 });
            beatmap.HitObjects.Add(new TestDurationObject { StartTime = 500, Duration = 500 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 2_000 });
            beatmap.Breaks.Add(new BreakPeriod(1_200, 1_800));

            // the slider finishes at 1000, so the gap is 1000 long rather than 1500.
            Assert.That(ComposeStatusDisplay.DrainTimeFor(beatmap), Is.EqualTo(1_000));
        }

        /// <summary>
        /// Every break comes off, not only the first.
        /// </summary>
        [Test]
        public void TestEveryBreakComesOff()
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(new TestObject { StartTime = 0 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 1_000 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 2_000 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 3_000 });
            beatmap.Breaks.Add(new BreakPeriod(200, 800));
            beatmap.Breaks.Add(new BreakPeriod(2_200, 2_800));

            Assert.That(ComposeStatusDisplay.DrainTimeFor(beatmap), Is.EqualTo(1_000));
        }

        /// <summary>
        /// A break with no beatmap on one side of it is not a gap between two objects, and is left as the file
        /// states it rather than measured against something that is not there.
        /// </summary>
        [Test]
        public void TestABreakHangingOffTheEndUsesWhatTheFileSays()
        {
            var beatmap = new Beatmap();
            beatmap.HitObjects.Add(new TestObject { StartTime = 0 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 1_000 });
            beatmap.Breaks.Add(new BreakPeriod(2_000, 2_600));

            Assert.That(ComposeStatusDisplay.DrainTimeFor(beatmap), Is.EqualTo(400));
        }

        /// <summary>
        /// Nothing to drain through, and nothing to say about it.
        /// </summary>
        [Test]
        public void TestAnEmptyBeatmapDrainsForNoTime()
        {
            Assert.That(ComposeStatusDisplay.DrainTimeFor(new Beatmap()), Is.Zero);
        }

        private class TestObject : HitObject
        {
        }

        private class TestDurationObject : HitObject, IHasDuration
        {
            public double Duration { get; set; }

            public double EndTime => StartTime + Duration;
        }
    }
}
