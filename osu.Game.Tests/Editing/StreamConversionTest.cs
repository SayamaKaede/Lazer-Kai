// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osuTK;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class StreamConversionTest
    {
        private const double beat_length = 500;

        /// <summary>
        /// A straight slider a beat long, so that where a circle lands can be read off by eye.
        /// </summary>
        private static Slider straightSlider(int repeats = 0)
        {
            var slider = new Slider
            {
                StartTime = 1000,
                Position = new Vector2(100, 100),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                RepeatCount = repeats,
            };

            var controlPoints = new ControlPointInfo();
            controlPoints.Add(0, new TimingControlPoint { BeatLength = beat_length });

            slider.ApplyDefaults(controlPoints, new BeatmapDifficulty { SliderMultiplier = 1 });

            return slider;
        }

        /// <summary>
        /// The times are the beat and nothing else. Everything the panel offers moves positions, because a
        /// stream whose times were warped would be a stream of unsnapped circles.
        /// </summary>
        [TestCase(1)]
        [TestCase(2.5)]
        [TestCase(0.4)]
        public void TestTimesAreNeverWarped(double acceleration)
        {
            var slider = straightSlider();
            double spacing = beat_length / 4;

            var notes = StreamConversion.Plan(slider, spacing, acceleration, Easing.InOutQuint, zigzag: 20);

            Assert.That(notes, Is.Not.Empty);

            for (int i = 0; i < notes.Count; i++)
                Assert.That(notes[i].Time, Is.EqualTo(slider.StartTime + i * spacing).Within(1e-6));
        }

        /// <summary>
        /// Left alone, the circles are evenly spread along the slider — which is what the game's own conversion
        /// does, and what anybody pressing the shortcut without touching anything expects.
        /// </summary>
        [Test]
        public void TestEvenByDefault()
        {
            var notes = StreamConversion.Plan(straightSlider(), beat_length / 4);

            Assert.That(notes.Count, Is.GreaterThan(2));

            float[] gaps = notes.Zip(notes.Skip(1), (a, b) => (b.Position - a.Position).Length).ToArray();

            Assert.That(gaps.Max() - gaps.Min(), Is.LessThan(0.01));
        }

        /// <summary>
        /// Above one, the circles start close together and open out; below one they do the reverse. This is the
        /// control the panel calls acceleration, and it is the one thing that has to read the right way round.
        /// </summary>
        [TestCase(2.0, true)]
        [TestCase(0.5, false)]
        public void TestAccelerationOpensOrCloses(double acceleration, bool opensOut)
        {
            var notes = StreamConversion.Plan(straightSlider(), beat_length / 8, acceleration);

            double first = (notes[1].Position - notes[0].Position).Length;
            double last = (notes[^1].Position - notes[^2].Position).Length;

            Assert.That(last > first, Is.EqualTo(opensOut), $"first {first:0.##}, last {last:0.##}");
        }

        /// <summary>
        /// Both ends stay put whatever is done to the middle: a stream that started somewhere other than the
        /// slider's head would not be a conversion of it.
        /// </summary>
        [TestCase(1.0)]
        [TestCase(3.0)]
        [TestCase(0.3)]
        public void TestTheEndsDoNotMove(double acceleration)
        {
            var slider = straightSlider();
            var notes = StreamConversion.Plan(slider, beat_length / 4, acceleration);

            Assert.That(notes.First().Position, Is.EqualTo(slider.Position));
            Assert.That(notes.Last().Position.X, Is.EqualTo(slider.Position.X + 100).Within(0.01));
        }

        /// <summary>
        /// Zigzag pushes circles off the path, alternating sides in runs.
        /// </summary>
        [TestCase(1)]
        [TestCase(2)]
        public void TestZigzagAlternatesInRuns(int period)
        {
            var slider = straightSlider();

            var straight = StreamConversion.Plan(slider, beat_length / 8);
            var zigzagged = StreamConversion.Plan(slider, beat_length / 8, zigzag: 20, zigzagPeriod: period);

            Assert.That(zigzagged.Count, Is.EqualTo(straight.Count));

            for (int i = 0; i < zigzagged.Count; i++)
            {
                float offset = zigzagged[i].Position.Y - straight[i].Position.Y;
                float expected = (i / period) % 2 == 0 ? 20 : -20;

                Assert.That(offset, Is.EqualTo(expected).Within(0.01), $"circle {i}");
            }
        }

        /// <summary>
        /// The zigzag can open out or close in over the stream, which is the second control under it.
        /// </summary>
        [TestCase(ZigzagShape.Widening, false)]
        [TestCase(ZigzagShape.Narrowing, true)]
        public void TestZigzagShapeOpensOrCloses(ZigzagShape shape, bool startsWide)
        {
            var slider = straightSlider();

            var straight = StreamConversion.Plan(slider, beat_length / 8);
            var shaped = StreamConversion.Plan(slider, beat_length / 8, zigzag: 30, zigzagShape: shape);

            float first = Math.Abs(shaped[0].Position.Y - straight[0].Position.Y);
            float last = Math.Abs(shaped[^1].Position.Y - straight[^1].Position.Y);

            Assert.That(first > last, Is.EqualTo(startsWide), $"first {first:0.##}, last {last:0.##}");
        }

        /// <summary>
        /// And it can bulge in the middle rather than at either end.
        /// </summary>
        [Test]
        public void TestZigzagCanBeWidestInTheMiddle()
        {
            var slider = straightSlider();

            var straight = StreamConversion.Plan(slider, beat_length / 16);
            var shaped = StreamConversion.Plan(slider, beat_length / 16, zigzag: 30, zigzagShape: ZigzagShape.WidestInTheMiddle);

            float[] reach = shaped.Select((n, i) => Math.Abs(n.Position.Y - straight[i].Position.Y)).ToArray();

            Assert.That(reach[reach.Length / 2], Is.GreaterThan(reach[0]));
            Assert.That(reach[reach.Length / 2], Is.GreaterThan(reach[^1]));
        }

        /// <summary>
        /// Curves that overshoot on their way would put circles past the ends of the path, which is off the
        /// slider entirely. Whatever the curve does in the middle, what comes out stays on it.
        /// </summary>
        [TestCase(Easing.InBack)]
        [TestCase(Easing.OutElasticHalf)]
        public void TestOvershootingCurvesStayOnThePath(Easing easing)
        {
            for (double progress = 0; progress <= 1; progress += 0.05)
                Assert.That(StreamConversion.Warp(progress, 1, easing), Is.InRange(0, 1));
        }

        /// <summary>
        /// Nothing is pushed anywhere when the amplitude is zero, so that leaving the control alone leaves the
        /// stream on the slider.
        /// </summary>
        [Test]
        public void TestNoZigzagLeavesThePath()
        {
            var slider = straightSlider();
            var notes = StreamConversion.Plan(slider, beat_length / 4, zigzag: 0);

            Assert.That(notes.All(n => Precision.AlmostEquals(n.Position.Y, slider.Position.Y)), Is.True);
        }

        /// <summary>
        /// A slider that repeats runs its path backwards and forwards, and the circles have to follow it rather
        /// than jump back to the head each time.
        /// </summary>
        [Test]
        public void TestRepeatsRunBackAndForth()
        {
            var slider = straightSlider(repeats: 1);
            var notes = StreamConversion.Plan(slider, beat_length / 4);

            Assert.That(notes.Count, Is.GreaterThan(4));

            // out to the far end and back to the head.
            Assert.That(notes.First().Position.X, Is.EqualTo(slider.Position.X).Within(0.01));
            Assert.That(notes.Max(n => n.Position.X), Is.EqualTo(slider.Position.X + 100).Within(0.01));
            Assert.That(notes.Last().Position.X, Is.EqualTo(slider.Position.X).Within(0.01));
        }

        /// <summary>
        /// A division that does not divide is not a stream of one circle every so often, it is nothing.
        /// </summary>
        [Test]
        public void TestNoSpacingProducesNothing()
        {
            Assert.That(StreamConversion.Plan(straightSlider(), 0), Is.Empty);
        }
    }
}
