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
    /// </remarks>
    [TestFixture]
    public class SliderAnchorInsertionTest
    {
        /// <summary>
        /// A straight two-point path. Adding an anchor between them should bend it.
        /// </summary>
        [Test]
        public void TestStraightPathBends()
        {
            var points = pathOf((PathType.LINEAR, true), (null, false));

            insertAt(points, 1);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.PERFECT_CURVE, null, null }));
        }

        /// <summary>
        /// A straight path carrying an inherited anchor, added to past that anchor.
        /// </summary>
        /// <remarks>
        /// The type in effect sits further back than the anchor immediately before the insertion, so the arc has to
        /// start at that nearer anchor while the earlier stretch stays straight.
        /// </remarks>
        [Test]
        public void TestStraightPathWithInheritedAnchorBends()
        {
            var points = pathOf((PathType.LINEAR, true), (null, false), (null, false));

            insertAt(points, 2);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.LINEAR, PathType.PERFECT_CURVE, null, null }));
        }

        /// <summary>
        /// Adding into the middle of a straight path, with more of it following.
        /// </summary>
        [Test]
        public void TestStraightPathKeepsItsRemainderStraight()
        {
            var points = pathOf((PathType.LINEAR, true), (null, false), (null, false), (null, false));

            insertAt(points, 1);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[]
            {
                PathType.PERFECT_CURVE, null, PathType.LINEAR, null, null
            }));
        }

        /// <summary>
        /// A bezier the user shaped on purpose is left alone.
        /// </summary>
        [Test]
        public void TestBezierIsUntouched()
        {
            var points = pathOf((PathType.BEZIER, true), (null, false), (null, false), (null, false));

            insertAt(points, 2);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.BEZIER, null, null, null, null }));
        }

        /// <summary>
        /// An existing arc is left alone, and degrades to bezier through the usual validation instead.
        /// </summary>
        [Test]
        public void TestArcIsUntouched()
        {
            var points = pathOf((PathType.PERFECT_CURVE, true), (null, false), (null, false));

            insertAt(points, 2);

            Assert.That(typesOf(points), Is.EqualTo(new PathType?[] { PathType.PERFECT_CURVE, null, null, null }));
        }

        /// <summary>
        /// Only the segment being added to changes; a later segment keeps its own type.
        /// </summary>
        [Test]
        public void TestLaterSegmentIsUntouched()
        {
            var points = pathOf((PathType.LINEAR, true), (null, false), (PathType.BEZIER, false), (null, false));

            insertAt(points, 1);

            // the arc takes the first three points; the anchor closing it resumes the straight run into the bezier,
            // which still starts where it did.
            Assert.That(typesOf(points), Is.EqualTo(new PathType?[]
            {
                PathType.PERFECT_CURVE, null, PathType.LINEAR, PathType.BEZIER, null
            }));
        }

        /// <summary>
        /// Every arc produced must have the three points an arc is allowed, or it silently becomes a bezier.
        /// </summary>
        [Test]
        public void TestProducedArcsAreThreePointsLong()
        {
            var points = pathOf((PathType.LINEAR, true), (null, false), (null, false), (null, false));

            insertAt(points, 2);

            Assert.That(segmentLengthStartingAt(points, PathType.PERFECT_CURVE), Is.EqualTo(3));
        }

        private static List<PathControlPoint> pathOf(params (PathType? type, bool _)[] entries) =>
            entries.Select((e, i) => new PathControlPoint(new Vector2(i * 50, 0), e.type)).ToList();

        private static void insertAt(List<PathControlPoint> points, int index)
        {
            points.Insert(index, new PathControlPoint(new Vector2(index * 50 - 25, 25)));
            SliderAnchorInsertion.MakeArcAroundInsertion(points, index);
        }

        private static PathType?[] typesOf(IEnumerable<PathControlPoint> points) => points.Select(p => p.Type).ToArray();

        /// <summary>
        /// Length of the segment beginning at the first point of the given type, counting the point that closes it.
        /// </summary>
        private static int segmentLengthStartingAt(List<PathControlPoint> points, PathType type)
        {
            int start = points.FindIndex(p => p.Type == type);

            if (start < 0)
                return 0;

            for (int i = start + 1; i < points.Count; i++)
            {
                if (points[i].Type != null)
                    return i - start + 1;
            }

            return points.Count - start;
        }
    }
}
