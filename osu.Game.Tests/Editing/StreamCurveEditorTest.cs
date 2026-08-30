// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Osu.Edit;
using osuTK;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The curve behind the hand-drawn alternative to picking an easing.
    /// </summary>
    [TestFixture]
    public class StreamCurveEditorTest
    {
        /// <summary>
        /// Both ends are fixed however the points are dragged: a stream that started somewhere other than the
        /// slider's head would not be a conversion of it.
        /// </summary>
        [Test]
        public void TestTheEndsAreFixed([Values(0f, 0.5f, 1f)] float x, [Values(0f, 0.5f, 1f)] float y)
        {
            var read = StreamCurve.Build(new[] { new Vector2(x, y) });

            Assert.That(read(0), Is.EqualTo(0).Within(1e-3));
            Assert.That(read(1), Is.EqualTo(1).Within(1e-3));
        }

        /// <summary>
        /// With nothing put on it the curve is the straight line, which is the even stream the panel opens on.
        /// </summary>
        [Test]
        public void TestNoPointsIsStraight()
        {
            var read = StreamCurve.Build(Array.Empty<Vector2>());

            for (double x = 0; x <= 1; x += 0.1)
                Assert.That(read(x), Is.EqualTo(x).Within(1e-3));
        }

        /// <summary>
        /// A point is somewhere the curve goes, not somewhere it leans towards. Dragging one and having the line
        /// stop short of it is a control that argues with the person using it.
        /// </summary>
        [TestCase(0.25f, 0.75f)]
        [TestCase(0.5f, 0.2f)]
        [TestCase(0.8f, 0.9f)]
        public void TestItPassesThroughThePoints(float x, float y)
        {
            var read = StreamCurve.Build(new[] { new Vector2(x, y) });

            Assert.That(read(x), Is.EqualTo(y).Within(1e-2));
        }

        /// <summary>
        /// And through all of them when there are several, which is the point of being able to add more.
        /// </summary>
        [Test]
        public void TestItPassesThroughSeveralPoints()
        {
            var points = new[] { new Vector2(0.2f, 0.6f), new Vector2(0.5f, 0.3f), new Vector2(0.75f, 0.85f) };
            var read = StreamCurve.Build(points);

            foreach (var point in points)
                Assert.That(read(point.X), Is.EqualTo(point.Y).Within(1e-2), $"at {point.X}");
        }

        /// <summary>
        /// The order they were added in does not matter — what matters is where they are.
        /// </summary>
        [Test]
        public void TestOrderDoesNotMatter()
        {
            var points = new[] { new Vector2(0.7f, 0.2f), new Vector2(0.2f, 0.8f) };

            var forwards = StreamCurve.Build(points);
            var backwards = StreamCurve.Build(points.Reverse().ToArray());

            for (double x = 0; x <= 1; x += 0.05)
                Assert.That(forwards(x), Is.EqualTo(backwards(x)).Within(1e-6), $"at {x:0.##}");
        }

        /// <summary>
        /// Whatever is drawn, the curve never leaves the square — a value past either end would place a circle
        /// off the slider.
        /// </summary>
        [Test]
        public void TestItStaysInTheSquare()
        {
            var read = StreamCurve.Build(new[] { new Vector2(0.1f, 1f), new Vector2(0.2f, 0f), new Vector2(0.9f, 1f) });

            for (double x = 0; x <= 1; x += 0.02)
                Assert.That(read(x), Is.InRange(0, 1));
        }

        /// <summary>
        /// Points dropped outside the square are brought back into it rather than bending the curve off the
        /// slider.
        /// </summary>
        [Test]
        public void TestPointsAreKeptInTheSquare()
        {
            var walked = StreamCurve.WithEnds(new[] { new Vector2(-2, 5), new Vector2(3, -4) });

            Assert.That(walked.All(p => p.X >= 0 && p.X <= 1 && p.Y >= 0 && p.Y <= 1), Is.True);
        }
    }
}
