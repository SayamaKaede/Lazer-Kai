// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Screens.Edit.DifficultyGraph;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DifficultyGraphDetailOverlayTest
    {
        private static readonly double[] one_second_apart = { 0, 1_000, 2_000, 3_000 };

        /// <summary>
        /// The column stands for the hardest thing in it, the same way the timeline's curve does, so the two are the
        /// same shape and the bands are that shape taken apart.
        /// </summary>
        [Test]
        public void TestAColumnHoldsItsHardestObject()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                one_second_apart,
                new[]
                {
                    new double[] { 1, 9, 0, 0 },
                    new double[] { 1, 1, 0, 0 },
                },
                trackLength: 4_000,
                columns: 2);

            // both of the first two objects land in the first column; the harder one is the one shown.
            Assert.That(columns[0].Total, Is.EqualTo(10));
            Assert.That(columns[0].Skills, Is.EqualTo(new double[] { 9, 1 }));
        }

        /// <summary>
        /// The bands are a breakdown of the column, so they have to come to its height.
        /// </summary>
        [Test]
        public void TestTheBandsAddUpToTheColumn()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                one_second_apart,
                new[]
                {
                    new double[] { 5, 2, 8, 1 },
                    new double[] { 3, 7, 1, 4 },
                    new double[] { 2, 2, 2, 2 },
                },
                trackLength: 4_000,
                columns: 4);

            foreach (var column in columns)
                Assert.That(column.Skills.Sum(), Is.EqualTo(column.Total).Within(1e-9));
        }

        /// <summary>
        /// Nothing has been played yet before the first object, and there is nothing to wind down from.
        /// </summary>
        [Test]
        public void TestTheRunUpToTheFirstObjectStaysEmpty()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                new double[] { 3_000 },
                new[] { new double[] { 4 } },
                trackLength: 4_000,
                columns: 4);

            Assert.That(columns[0].Total, Is.Zero);
            Assert.That(columns[1].Total, Is.Zero);
            Assert.That(columns[2].Total, Is.Zero);
            Assert.That(columns[3].Total, Is.EqualTo(4));
        }

        /// <summary>
        /// A skill is free to stop early on an object it has nothing to say about, and reports fewer values than
        /// there were objects when it does. It contributes nothing to those rather than taking another skill's value.
        /// </summary>
        [Test]
        public void TestASkillThatStoppedEarlyContributesNothing()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                one_second_apart,
                new[]
                {
                    new double[] { 1, 1, 1, 1 },
                    new double[] { 5 },
                },
                trackLength: 4_000,
                columns: 4);

            Assert.That(columns[0].Skills, Is.EqualTo(new double[] { 1, 5 }));
            Assert.That(columns[3].Skills, Is.EqualTo(new double[] { 1, 0 }));
        }

        /// <summary>
        /// A stretch with nothing measured in it winds down from what came before rather than dropping out, so a
        /// long slider reads as something still going on instead of as a hole in the beatmap.
        /// </summary>
        [Test]
        public void TestAStretchWithNothingMeasuredWindsDown()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                new double[] { 0 },
                new[] { new double[] { 4 } },
                trackLength: 4_000,
                columns: 4);

            Assert.That(columns[0].Total, Is.EqualTo(4));
            Assert.That(columns[0].Measured, Is.True);

            Assert.That(columns[1].Measured, Is.False);
            Assert.That(columns[1].Total, Is.LessThan(columns[0].Total).And.GreaterThan(0));
            Assert.That(columns[2].Total, Is.LessThan(columns[1].Total).And.GreaterThan(0));
        }

        /// <summary>
        /// What the difficulty was made of stays readable while it winds down, rather than the bands sliding around
        /// as the stack shrinks.
        /// </summary>
        [Test]
        public void TestTheWholeStackFadesTogether()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                new double[] { 0 },
                new[] { new double[] { 3 }, new double[] { 1 } },
                trackLength: 4_000,
                columns: 4);

            Assert.That(columns[1].Skills[0] / columns[1].Skills[1], Is.EqualTo(3).Within(1e-9));
        }

        /// <summary>
        /// A break is drawn as the empty stretch it is — the bar along the bottom of the editor already says where
        /// the breaks are, and leaving them blank is what tells them apart from a slider being held.
        /// </summary>
        [Test]
        public void TestABreakStaysEmpty()
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                new double[] { 0 },
                new[] { new double[] { 4 } },
                trackLength: 4_000,
                columns: 4,
                breaks: new[] { (1_000d, 2_900d) });

            Assert.That(columns[1].Total, Is.Zero);
            Assert.That(columns[2].Total, Is.Zero);
        }

        /// <summary>
        /// The track length is read from the audio, and an editor opened on a beatmap whose track has not loaded
        /// hands back nothing rather than a division by zero.
        /// </summary>
        [TestCase(0)]
        [TestCase(-1)]
        public void TestNoTrackMeansNoColumns(double trackLength)
        {
            var columns = DifficultyGraphDetailOverlay.ColumnsFor(
                one_second_apart,
                new[] { new double[] { 1, 1, 1, 1 } },
                trackLength,
                columns: 4);

            Assert.That(columns, Is.Empty);
        }
    }
}
