// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Covers what adding a slider anchor does to the path's control point types.
    /// </summary>
    /// <remarks>
    /// The rule has to hold for every shape a path can already be in, which is tedious to confirm by dragging things
    /// around in the editor. Each case here states the path before, where the anchor lands, and the types expected
    /// after.
    ///
    /// Nothing is ever split: in stable a slider is one shape, and the only anchors dividing a path are the ones the
    /// user makes. What an added anchor does is give the part of the path it lands in the shape asked for.
    ///
    /// Straight parts are the exception, being divided at every corner already: only the straight piece the anchor lands
    /// in is shaped, and the pieces either side stay as they were (2026-10-07).
    /// </remarks>
    [TestFixture]
    public class SliderAnchorInsertionTest
    {
        /// <summary>
        /// A straight two point path, asked for an arc: three points is exactly what an arc holds.
        /// </summary>
        [Test]
        public void TestStraightPathBecomesAnArc()
        {
            var points = pathOf(PathType.LINEAR, null);

            insertAt(points, 1, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.PERFECT_CURVE, null, null }));
        }

        /// <summary>
        /// The arc that came of that, added to again. A fourth point is more than an arc can hold, so the part becomes
        /// a bezier rather than being split to keep an arc somewhere in it.
        /// </summary>
        [Test]
        public void TestArcWithAFourthPointBecomesABezier()
        {
            var points = pathOf(PathType.PERFECT_CURVE, null, null);

            insertAt(points, 2, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.BEZIER, null, null, null }));
        }

        /// <summary>
        /// A bezier, which is what the fork used to leave alone and the user watched stay a bezier when they wanted a
        /// curve out of it.
        /// </summary>
        [Test]
        public void TestBezierBecomesAnArcWhereItFits()
        {
            var points = pathOf(PathType.BEZIER, null);

            insertAt(points, 1, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.PERFECT_CURVE, null, null }));
        }

        /// <summary>
        /// Whatever shape is asked for is what the part becomes, arcs aside.
        /// </summary>
        [Test]
        public void TestTheShapeAskedForIsWhatItBecomes()
        {
            var straight = pathOf(PathType.BEZIER, null, null);
            insertAt(straight, 1, PathType.LINEAR);
            Assert.That(typesOf(straight), Is.EqualTo(new PathType?[] { PathType.LINEAR, null, null, null }));

            var bezier = pathOf(PathType.CATMULL, null, null);
            insertAt(bezier, 1, PathType.BEZIER);
            Assert.That(typesOf(bezier), Is.EqualTo(new PathType?[] { PathType.BEZIER, null, null, null }));

            var catmull = pathOf(PathType.BEZIER, null, null);
            insertAt(catmull, 1, PathType.CATMULL);
            Assert.That(typesOf(catmull), Is.EqualTo(new PathType?[] { PathType.CATMULL, null, null, null }));
        }

        /// <summary>
        /// A straight path with a corner, added to between the corner and its end: the user's case (2026-10-07). The line
        /// up to the corner stays as it was, and the arc runs from the corner.
        /// </summary>
        [Test]
        public void TestOnlyTheStraightPieceItLandedInIsShaped()
        {
            var points = pathOf(PathType.LINEAR, null, null);

            insertAt(points, 2, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.PERFECT_CURVE, null, null }));
        }

        /// <summary>
        /// Landing in a straight piece with more of them after it, those go on being straight from the corner the shaped
        /// piece ends at — whatever shape is asked for.
        /// </summary>
        [Test]
        public void TestTheStraightPiecesAfterItStayStraight()
        {
            var first = pathOf(PathType.LINEAR, null, null);
            insertAt(first, 1, PathType.PERFECT_CURVE);
            Assert.That(typesOf(first), Is.EqualTo(new PathType?[] { PathType.PERFECT_CURVE, null, PathType.LINEAR, null }));

            var middle = pathOf(PathType.LINEAR, null, null, null);
            insertAt(middle, 2, PathType.BEZIER);
            Assert.That(typesOf(middle), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.BEZIER, null, PathType.LINEAR, null }));

            var catmull = pathOf(PathType.LINEAR, null, null, null);
            insertAt(catmull, 2, PathType.CATMULL);
            Assert.That(typesOf(catmull), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.CATMULL, null, PathType.LINEAR, null }));
        }

        /// <summary>
        /// The last straight piece before a part of another shape: the corner it ends at already starts that part, and
        /// is left as it is.
        /// </summary>
        [Test]
        public void TestTheStraightPieceBeforeAnotherShape()
        {
            var points = pathOf(PathType.LINEAR, null, PathType.BEZIER, null, null);

            insertAt(points, 2, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.PERFECT_CURVE, null, PathType.BEZIER, null, null }));
        }

        /// <summary>
        /// Asked for a straight line, a straight piece is already one: nothing changes.
        /// </summary>
        [Test]
        public void TestAStraightPieceAskedToBeStraight()
        {
            var points = pathOf(PathType.LINEAR, null, null);

            insertAt(points, 2, PathType.LINEAR);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.LINEAR, null, null, null }));
        }

        /// <summary>
        /// Only the part the anchor landed in is shaped. Whatever the user divided the path into stays divided the
        /// same way, and the parts either side keep their own shapes.
        /// </summary>
        [Test]
        public void TestTheOtherPartsOfThePathAreLeftAlone()
        {
            var points = pathOf(PathType.LINEAR, null, PathType.BEZIER, PathType.CATMULL, null);

            insertAt(points, 3, PathType.PERFECT_CURVE);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[]
            {
                PathType.LINEAR, null, PathType.PERFECT_CURVE, null, PathType.CATMULL, null
            }));
        }

        /// <summary>
        /// Adding an anchor never divides the path: the anchor itself carries no type, and no other anchor gains one.
        /// </summary>
        [Test]
        public void TestNothingIsSplit()
        {
            var points = pathOf(PathType.BEZIER, null, null, null);

            insertAt(points, 2, PathType.PERFECT_CURVE);

            Assert.That(points[2].Type, Is.Null, "the anchor that was added");
            Assert.That(points.Count(p => p.Type != null), Is.EqualTo(1), "anchors dividing the path");
        }

        private static List<PathControlPoint> pathOf(params PathType?[] types) =>
            types.Select((type, i) => new PathControlPoint(new Vector2(i * 50, 0), type)).ToList();

        private static void insertAt(List<PathControlPoint> points, int index, PathType type)
        {
            points.Insert(index, new PathControlPoint(new Vector2(index * 50 - 25, 25)));
            SliderAnchorInsertion.ShapeAroundInsertion(points, index, type);
        }

        private static PathType?[] typesOf(IEnumerable<PathControlPoint> points) => points.Select(p => p.Type).ToArray();
    }
}
