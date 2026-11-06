// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Where a stream's circles land along a shape.
    /// </summary>
    [TestFixture]
    public class StreamConversionTest
    {
        /// <summary>
        /// Evenly spaced along the path, whatever the path does. A shape that turns a corner was piling circles up
        /// at the turn and leaving gaps along the straights.
        /// </summary>
        [Test]
        public void TestTheyAreEvenlySpacedAroundACorner()
        {
            var slider = sliderThatTurnsACorner();

            var plan = StreamConversion.Plan(slider, 100);

            Assert.That(plan.Count, Is.GreaterThan(4));

            double[] gaps = plan.Zip(plan.Skip(1), (a, b) => (double)(b.Position - a.Position).Length).ToArray();

            Assert.That(gaps.Max() / gaps.Min(), Is.LessThan(1.5),
                $"the gaps between circles run from {gaps.Min():N1} to {gaps.Max():N1}");
        }

        /// <summary>
        /// A stream covers the shape that was drawn, and no more. A slider carries a length of its own, snapped
        /// from its velocity when it was placed, and that length is kept while its anchors move -- so a shape
        /// pulled shorter was still having circles laid past the end of it.
        /// </summary>
        [Test]
        public void TestItCoversWhatWasDrawnRatherThanTheSlidersOwnLength()
        {
            var slider = sliderThatTurnsACorner();

            slider.Path.ExpectedDistance.Value = slider.Path.CalculatedDistance * 2;
            Assert.That(slider.Path.Distance, Is.Not.EqualTo(slider.Path.CalculatedDistance).Within(1));

            // what the panel does with a shape that was drawn rather than placed.
            slider.Path.ExpectedDistance.Value = null;

            Assert.That(slider.Path.Distance, Is.EqualTo(slider.Path.CalculatedDistance).Within(0.01));

            var plan = StreamConversion.Plan(slider, 100);
            double[] gaps = plan.Zip(plan.Skip(1), (a, b) => (double)(b.Position - a.Position).Length).ToArray();

            Assert.That(gaps.Max() / gaps.Min(), Is.LessThan(1.5));
        }

        /// <summary>
        /// A shape that turns a corner has a circle on the corner. Laid straight down the path, none of them lands
        /// on the turn: the run steps over the tip and comes out the far side, which is a gap on one side and a
        /// short step on the other.
        /// </summary>
        [Test]
        public void TestACircleLandsOnTheCorner()
        {
            var slider = sliderThatTurnsACorner();
            var corner = slider.Position + slider.Path.PositionAt(StreamConversion.CornersOf(slider).Single());

            var stretches = new[] { new StreamConversion.Stretch(5), new StreamConversion.Stretch(5) };
            var plan = StreamConversion.PlanStretches(slider, 100, stretches);

            Assert.That(plan.Count, Is.EqualTo(9), "five each side, with the one on the corner shared between them");
            Assert.That(plan.Any(n => (n.Position - corner).Length < 0.01f), Is.True, "nothing landed on the corner");
        }

        /// <summary>
        /// The times stay on the beat however the stretches divide the shape up.
        /// </summary>
        [Test]
        public void TestTheTimesAreStillTheBeat()
        {
            var slider = sliderThatTurnsACorner();

            var plan = StreamConversion.PlanStretches(slider, 100,
                new[] { new StreamConversion.Stretch(4), new StreamConversion.Stretch(7) });

            Assert.That(plan.Select(n => n.Time), Is.EqualTo(Enumerable.Range(0, 10).Select(i => 1000 + i * 100.0)));
        }

        /// <summary>
        /// A slider with a repeat turns its corner once on the way out and once on the way back, and turns round at
        /// its end in between. A circle lands on all three. The corners were taken as how far along the whole slider
        /// they are rather than along its path, so the first stretch ran past the corner, and everything after it was
        /// laid somewhere along the way back instead (2026-11-04).
        /// </summary>
        [Test]
        public void TestARepeatedSliderHasACircleOnEveryTurn()
        {
            var slider = sliderThatTurnsACorner(repeats: 1, firstRun: 100);
            var corner = slider.Position + new Vector2(100, 0);
            var end = slider.Position + new Vector2(100, 200);

            var stretches = Enumerable.Range(0, StreamConversion.CornersOf(slider).Count + 1)
                                      .Select(_ => new StreamConversion.Stretch(4))
                                      .ToArray();
            var plan = StreamConversion.PlanStretches(slider, 100, stretches);

            Assert.That(plan.Count(n => (n.Position - corner).Length < 0.01f), Is.EqualTo(2), "the corner, there and back");
            Assert.That(plan.Count(n => (n.Position - end).Length < 0.01f), Is.EqualTo(1), "where it turns round");
            Assert.That((plan[^1].Position - slider.Position).Length, Is.LessThan(0.01f), "back where it started");
        }

        /// <summary>
        /// Two straight runs meeting at a right angle, which is what a red anchor makes.
        /// </summary>
        private Slider sliderThatTurnsACorner(int repeats = 0, float firstRun = 200)
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Position = Vector2.Zero,
                RepeatCount = repeats,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(firstRun, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(firstRun, 200)),
                }),
            };

            var controlPoints = new ControlPointInfo();
            controlPoints.Add(0, new TimingControlPoint { BeatLength = 500 });

            slider.ApplyDefaults(controlPoints, new BeatmapDifficulty());

            return slider;
        }
    }
}
