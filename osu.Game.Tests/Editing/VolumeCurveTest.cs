// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osuTK;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// What the volume graph is drawn from.
    /// </summary>
    [TestFixture]
    public class VolumeCurveTest
    {
        [Test]
        public void TestEachChangeIsAPointOfItsOwn()
        {
            var points = VolumeCurve.Read(beatmapWith(circleAt(0, 100), circleAt(1000, 40), circleAt(2000, 70)));

            Assert.That(points.Select(p => (p.Time, p.Volume)), Is.EqualTo(new[] { (0d, 100), (1000d, 40), (2000d, 70) }));
        }

        /// <remarks>
        /// A beatmap that writes the same volume onto every object would otherwise carry thousands of points that
        /// draw nothing.
        /// </remarks>
        [Test]
        public void TestObjectsThatChangeNothingAreLeftOut()
        {
            var points = VolumeCurve.Read(beatmapWith(circleAt(0, 60), circleAt(1000, 60), circleAt(2000, 60), circleAt(3000, 20)));

            Assert.That(points.Select(p => (p.Time, p.Volume)), Is.EqualTo(new[] { (0d, 60), (3000d, 20) }));
        }

        /// <remarks>
        /// The loudest of an object's samples, not an average: they play together, so the loudest is what is heard.
        /// </remarks>
        [Test]
        public void TestTheLoudestSampleOnAnObjectIsTheOneTaken()
        {
            var circle = new HitCircle
            {
                StartTime = 0,
                Samples = new List<HitSampleInfo>
                {
                    new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 20),
                    new HitSampleInfo(HitSampleInfo.HIT_CLAP, volume: 90),
                },
            };

            Assert.That(VolumeCurve.Read(beatmapWith(circle)).Single().Volume, Is.EqualTo(90));
        }

        /// <remarks>
        /// A slider that fades out over its length is exactly the thing this graph is for, and its head, repeats and
        /// tail each carry their own volume.
        /// </remarks>
        [Test]
        public void TestASlidersOwnNodesAreEachAPoint()
        {
            var slider = new Slider
            {
                StartTime = 0,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 80) },
                NodeSamples = new List<IList<HitSampleInfo>>
                {
                    new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 80) },
                    new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: 15) },
                },
            };

            var beatmap = beatmapWith(slider);

            slider.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            var points = VolumeCurve.Read(beatmap);

            Assert.That(points.Select(p => p.Volume), Is.EqualTo(new[] { 80, 15 }));
            Assert.That(points[1].Time, Is.EqualTo(slider.EndTime).Within(0.01));
        }

        [Test]
        public void TestAnEmptyBeatmapHasNothingToDraw()
        {
            Assert.That(VolumeCurve.Read(beatmapWith()), Is.Empty);
        }

        /// <summary>
        /// That a moment lands where the playhead puts it.
        /// </summary>
        /// <remarks>
        /// Laid out against the song rather than against the stretch the objects cover. Measured from the first
        /// object instead, the curve is stretched across the timeline and drifts further from the playhead the
        /// further along it goes -- which is what it did, and it was reported from a beatmap where a change three
        /// quarters of the way through was drawn a long way off it.
        /// </remarks>
        [TestCase(0, 0)]
        [TestCase(30000, 300)]
        [TestCase(90000, 900)]
        public void TestAMomentIsPlacedAgainstTheLengthOfTheSong(double time, float expectedX)
        {
            var points = new[] { new VolumeCurve.Point(time, 50) };

            var traced = VolumeCurve.Trace(points, time, trackLength: 120000, size: new Vector2(1200, 40));

            Assert.That(traced.Single().X, Is.EqualTo(expectedX).Within(0.01f));
        }

        /// <remarks>
        /// The curve is drawn where the objects are, so a beatmap starting a minute in leaves the first minute of
        /// the timeline empty rather than beginning at its left edge.
        /// </remarks>
        [Test]
        public void TestAnOpeningTheObjectsDoNotCoverIsLeftEmpty()
        {
            var points = new[] { new VolumeCurve.Point(60000, 50) };

            var traced = VolumeCurve.Trace(points, 60000, trackLength: 120000, size: new Vector2(1200, 40));

            Assert.That(traced.Single().X, Is.EqualTo(600).Within(0.01f));
        }

        [Test]
        public void TestTheVolumeIsHeldAcrossAndThenStepped()
        {
            var points = new[] { new VolumeCurve.Point(0, 100), new VolumeCurve.Point(50000, 0) };

            var traced = VolumeCurve.Trace(points, 50000, trackLength: 100000, size: new Vector2(1000, 100));

            // out at full volume, across to the moment it changes at that same height, then straight down to nothing.
            Assert.That(traced.Select(v => v.X), Is.EqualTo(new[] { 0f, 500f, 500f }));
            Assert.That(traced[0].Y, Is.EqualTo(traced[1].Y));
            Assert.That(traced[2].Y, Is.GreaterThan(traced[1].Y));
        }

        [Test]
        public void TestLoudSitsAboveQuiet()
        {
            var size = new Vector2(100, 50);

            float loud = VolumeCurve.Trace(new[] { new VolumeCurve.Point(0, 100) }, 0, 1000, size).Single().Y;
            float quiet = VolumeCurve.Trace(new[] { new VolumeCurve.Point(0, 0) }, 0, 1000, size).Single().Y;

            Assert.That(loud, Is.LessThan(quiet));
            Assert.That(loud, Is.GreaterThanOrEqualTo(0));
            Assert.That(quiet, Is.LessThanOrEqualTo(size.Y));
        }

        [Test]
        public void TestNothingIsDrawnBeforeTheTrackIsKnown()
        {
            var points = new[] { new VolumeCurve.Point(0, 50) };

            Assert.That(VolumeCurve.Trace(points, 0, trackLength: 0, size: new Vector2(100, 50)), Is.Empty);
            Assert.That(VolumeCurve.Trace(points, 0, trackLength: 1000, size: Vector2.Zero), Is.Empty);
        }

        private static HitCircle circleAt(double time, int volume) => new HitCircle
        {
            StartTime = time,
            Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, volume: volume) },
        };

        private static IBeatmap beatmapWith(params HitObject[] objects)
        {
            var beatmap = new Beatmap { HitObjects = new List<HitObject>(objects) };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 1000 });

            return beatmap;
        }
    }
}
