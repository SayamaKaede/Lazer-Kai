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
using osu.Game.Screens.Edit.Presets;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// What a preset keeps of a shape, and where that shape lands when it is placed again.
    /// </summary>
    [TestFixture]
    public class PlacementPresetTest
    {
        /// <summary>
        /// The whole shape survives the trip to disk and back. The anchors and the length a slider was drawn to are
        /// what make it the shape it is; without either it comes back as a different one.
        /// </summary>
        [Test]
        public void TestAShapeSurvivesBeingWrittenDown()
        {
            var original = curvedSlider();
            original.Path.ExpectedDistance.Value = 123.4;

            var read = PlacementPreset.From(new HitObject[] { original }).GetObjects();

            Assert.That(read.Count, Is.EqualTo(1));

            var slider = (Slider)read[0];

            Assert.That(slider.Position, Is.EqualTo(original.Position));
            Assert.That(slider.Path.ExpectedDistance.Value, Is.EqualTo(123.4));
            Assert.That(slider.Path.ControlPoints.Select(p => p.Position),
                Is.EqualTo(original.Path.ControlPoints.Select(p => p.Position)));
            Assert.That(slider.Path.ControlPoints.Select(p => p.Type),
                Is.EqualTo(original.Path.ControlPoints.Select(p => p.Type)));
            Assert.That(slider.RepeatCount, Is.EqualTo(original.RepeatCount));
        }

        /// <summary>
        /// The list is a file on disk, so it can come back damaged or edited by hand. One entry nobody can read is
        /// not worth taking the whole list down with.
        /// </summary>
        [Test]
        public void TestAPresetThatCannotBeReadComesBackEmpty()
        {
            var damaged = new PlacementPreset { Content = @"{ this is not what was written }" };

            Assert.That(damaged.GetObjects(), Is.Empty);
        }

        /// <summary>
        /// A slider's position is its head, so the area it covers is nowhere near centred on that point. Centring on
        /// the head would put most of a curve off to one side of where it was asked for.
        /// </summary>
        [Test]
        public void TestTheShapeIsCentred_NotItsFirstObject()
        {
            var slider = curvedSlider();
            var objects = new List<HitObject> { slider };

            var centre = new Vector2(256, 192);

            PlacementPresetPlacement.LayOut(objects, 1000, centre);

            var extent = PlacementPresetPlacement.Extent(objects);

            Assert.That(extent.Centre.X, Is.EqualTo(centre.X).Within(0.01f));
            Assert.That(extent.Centre.Y, Is.EqualTo(centre.Y).Within(0.01f));
            Assert.That(slider.Position, Is.Not.EqualTo(centre), "the head landed on the centre, so the curve did not");
        }

        /// <summary>
        /// Several objects move as one. Laying each out on its own would pull the shape apart.
        /// </summary>
        [Test]
        public void TestSeveralObjectsKeepTheirArrangement()
        {
            var first = new HitCircle { StartTime = 500, Position = new Vector2(100, 100) };
            var second = new HitCircle { StartTime = 700, Position = new Vector2(140, 180) };
            var objects = new List<HitObject> { first, second };

            PlacementPresetPlacement.LayOut(objects, 2000, new Vector2(256, 192));

            Assert.That(second.Position - first.Position, Is.EqualTo(new Vector2(40, 80)));
            Assert.That(second.StartTime - first.StartTime, Is.EqualTo(200));
            Assert.That(first.StartTime, Is.EqualTo(2000), "the earliest object is the one put at the asked-for time");
        }

        /// <summary>
        /// The sounds and the combo belong to wherever the shape is being put, not to the shape.
        /// </summary>
        [Test]
        public void TestTheReadingIsLeftBehind()
        {
            var slider = curvedSlider();
            slider.NewCombo = true;
            slider.Samples = new List<HitSampleInfo>
            {
                new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT),
                new HitSampleInfo(HitSampleInfo.HIT_CLAP, HitSampleInfo.BANK_DRUM),
            };

            PlacementPresetPlacement.StripReading(new HitObject[] { slider });

            Assert.That(slider.NewCombo, Is.False);
            Assert.That(slider.Samples.Select(s => s.Name), Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));
        }

        /// <summary>
        /// The velocity is the composer's to decide, so the same shape lasts as long as one drawn by hand in the
        /// same place -- which is not as long as it lasted where it was recorded.
        /// </summary>
        [Test]
        public void TestTheVelocityDecidesHowLongTheShapeLasts()
        {
            var slider = curvedSlider();
            slider.Path.ExpectedDistance.Value = 200;

            double durationAt(double velocity)
            {
                slider.SliderVelocityMultiplier = velocity;
                slider.ApplyDefaults(controlPoints(), new BeatmapDifficulty());
                return slider.Duration;
            }

            double atOne = durationAt(1);
            double atTwo = durationAt(2);

            Assert.That(atTwo, Is.EqualTo(atOne / 2).Within(0.01));
        }

        /// <summary>
        /// The tempo does not decide it. A shape recorded in one part of a song and placed in a faster part covers
        /// the same number of beats, which is what makes a preset worth keeping across a whole map.
        /// </summary>
        [Test]
        public void TestTheTempoDoesNotDecideHowManyBeatsTheShapeCovers()
        {
            var slider = curvedSlider();
            slider.Path.ExpectedDistance.Value = 200;

            double beatsAt(double beatLength)
            {
                slider.ApplyDefaults(controlPoints(beatLength), new BeatmapDifficulty());
                return slider.Duration / beatLength;
            }

            Assert.That(beatsAt(250), Is.EqualTo(beatsAt(500)).Within(0.0001));
        }

        private static ControlPointInfo controlPoints(double beatLength = 500)
        {
            var info = new ControlPointInfo();
            info.Add(0, new TimingControlPoint { BeatLength = beatLength });
            return info;
        }

        /// <remarks>
        /// Defaults are applied because that is the state an object taken out of the editor is in: the node samples
        /// a slider reports do not exist until they have been.
        /// </remarks>
        private static Slider curvedSlider()
        {
            var slider = new Slider
            {
                StartTime = 1234,
                Position = new Vector2(50, 60),
                RepeatCount = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.PERFECT_CURVE),
                    new PathControlPoint(new Vector2(100, -80)),
                    new PathControlPoint(new Vector2(200, 0)),
                }),
            };

            slider.ApplyDefaults(controlPoints(), new BeatmapDifficulty());

            return slider;
        }
    }
}
