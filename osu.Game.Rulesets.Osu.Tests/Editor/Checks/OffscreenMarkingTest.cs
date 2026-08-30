// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor.Checks
{
    /// <summary>
    /// What the editor marks as offscreen against what beatmap verification calls offscreen.
    /// </summary>
    /// <remarks>
    /// The marking exists so that a mapper does not have to run the verifier to find this out, which only works
    /// if the two agree. They did not: the marker measured a slider from the curve its control points describe,
    /// while the body is drawn along that curve only as far as the slider's length says — and past the end of the
    /// curve it carries on, with none of that being looked at.
    /// </remarks>
    [TestFixture]
    public class OffscreenMarkingTest
    {
        [Test]
        public void TestACircleWellInsideIsNotMarked()
        {
            Assert.That(CheckOffscreenObjects.IsOffscreen(circleAt(new Vector2(256, 192))), Is.False);
        }

        [Test]
        public void TestACircleOverTheTopIsMarked()
        {
            Assert.That(CheckOffscreenObjects.IsOffscreen(circleAt(new Vector2(256, -60))), Is.True);
        }

        /// <summary>
        /// The case that was reported: both ends sit comfortably on screen and the body does not.
        /// </summary>
        [Test]
        public void TestASliderWhoseBodyLeavesTheTopIsMarked()
        {
            var slider = sliderThrough(new Vector2(200, 200), new Vector2(256, -400), new Vector2(312, 200));

            Assert.That(slider.StackedPosition.Y, Is.GreaterThan(CheckOffscreenObjects.MIN_Y), @"the head is meant to be on screen");
            Assert.That(slider.StackedEndPosition.Y, Is.GreaterThan(CheckOffscreenObjects.MIN_Y), @"and so is the tail");

            Assert.That(CheckOffscreenObjects.IsOffscreen(slider), Is.True);
        }

        [Test]
        public void TestASliderEntirelyInsideIsNotMarked()
        {
            Assert.That(CheckOffscreenObjects.IsOffscreen(sliderThrough(new Vector2(200, 200), new Vector2(256, 150), new Vector2(312, 200))), Is.False);
        }

        /// <summary>
        /// A slider dragged out longer than the anchors that describe it — placing two anchors and pulling the
        /// length out is how most sliders get made — carries on past the last anchor in the direction it was
        /// heading. Everything past that point is body that the anchors say nothing about.
        /// </summary>
        [Test]
        public void TestASliderStretchedPastItsAnchorsIsMarked()
        {
            var slider = stretchedSlider(new Vector2(256, 200), new Vector2(256, 150), length: 400);

            Assert.That(CheckOffscreenObjects.IsOffscreen(slider), Is.True);
        }

        [Test]
        public void TestASliderStretchedButStillInsideIsNotMarked()
        {
            Assert.That(CheckOffscreenObjects.IsOffscreen(stretchedSlider(new Vector2(256, 300), new Vector2(256, 280), length: 100)), Is.False);
        }

        [TestCase(256, 200, 256, 150, 400)]
        [TestCase(256, 300, 256, 280, 100)]
        [TestCase(100, 200, 140, 200, 500)]
        [TestCase(400, 200, 360, 200, 500)]
        [TestCase(256, 100, 256, 140, 400)]
        public void TestAStretchedSliderAgreesWithTheVerifier(float x, float y, float towardsX, float towardsY, double length)
        {
            var slider = stretchedSlider(new Vector2(x, y), new Vector2(towardsX, towardsY), length);

            Assert.That(CheckOffscreenObjects.IsOffscreen(slider), Is.EqualTo(verifierSaysOffscreen(slider)));
        }

        /// <summary>
        /// The slider actually reported, taken from the beatmap it happened in.
        /// </summary>
        /// <remarks>
        /// <c>169,133,133138,6,0,P|195:-137|-9:42,2,700,...</c> — a perfect curve arcing up out of the play area
        /// and back down, twice. Written out here so that whatever is wrong is being asked about the real thing
        /// rather than something that merely resembles it.
        /// </remarks>
        [Test]
        public void TestTheReportedSlider()
        {
            var slider = reportedSlider();

            Assert.That(verifierSaysOffscreen(slider), Is.True, @"the verifier is meant to call this offscreen");
            Assert.That(CheckOffscreenObjects.IsOffscreen(slider), Is.True, @"and so is the marking");
        }

        /// <summary>
        /// The marking and the verifier answering the same, whatever is put in front of them. This is the property
        /// the marking is for; the cases above are only the ones somebody thought to write down.
        /// </summary>
        [TestCase(200, 200, 256, -400, 312, 200)]
        [TestCase(200, 200, 256, 150, 312, 200)]
        [TestCase(60, 60, 20, 20, 60, 120)]
        [TestCase(450, 300, 600, 340, 450, 380)]
        [TestCase(100, 40, 300, -200, 500, 40)]
        [TestCase(256, 350, 256, 700, 256, 350)]
        [TestCase(30, 192, -300, 192, 30, 192)]
        public void TestTheMarkingAgreesWithTheVerifier(float x1, float y1, float x2, float y2, float x3, float y3)
        {
            var slider = sliderThrough(new Vector2(x1, y1), new Vector2(x2, y2), new Vector2(x3, y3));

            Assert.That(CheckOffscreenObjects.IsOffscreen(slider), Is.EqualTo(verifierSaysOffscreen(slider)));
        }

        private static bool verifierSaysOffscreen(HitObject hitObject)
        {
            IBeatmap beatmap = new Beatmap<HitObject> { HitObjects = new List<HitObject> { hitObject } };

            return new CheckOffscreenObjects().Run(new BeatmapVerifierContext(beatmap, new TestWorkingBeatmap(beatmap))).Any();
        }

        private static Slider reportedSlider()
        {
            var head = new Vector2(169, 133);

            var slider = new Slider
            {
                StartTime = 133138,
                Position = head,
                // the control points are written in the file as absolute positions; the path holds them as
                // offsets from the head.
                Path = new SliderPath(PathType.PERFECT_CURVE, new[]
                {
                    Vector2.Zero,
                    new Vector2(195, -137) - head,
                    new Vector2(-9, 42) - head,
                }, 700),
                RepeatCount = 2,
            };

            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }

        private static HitCircle circleAt(Vector2 position)
        {
            var circle = new HitCircle { Position = position };
            circle.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());
            return circle;
        }

        /// <summary>
        /// A straight slider from <paramref name="start"/> heading towards <paramref name="towards"/>, but as
        /// long as <paramref name="length"/> — so the body runs past the last anchor.
        /// </summary>
        private static Slider stretchedSlider(Vector2 start, Vector2 towards, double length)
        {
            var slider = new Slider
            {
                Position = start,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, towards - start }, length),
            };

            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }

        private static Slider sliderThrough(Vector2 start, Vector2 middle, Vector2 end)
        {
            var slider = new Slider
            {
                Position = start,
                Path = new SliderPath(PathType.PERFECT_CURVE, new[]
                {
                    Vector2.Zero,
                    middle - start,
                    end - start,
                }),
            };

            slider.ApplyDefaults(new ControlPointInfo(), new BeatmapDifficulty());

            return slider;
        }
    }
}
