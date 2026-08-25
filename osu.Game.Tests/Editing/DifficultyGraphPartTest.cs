// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DifficultyGraphPartTest
    {
        /// <summary>
        /// The setting is how densely the graph is drawn, so twice the detail is twice the columns.
        /// </summary>
        [TestCase(0.5, 200)]
        [TestCase(1, 400)]
        [TestCase(2, 800)]
        public void TestDetailScalesTheColumnCount(double detail, int expected)
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(detail), Is.EqualTo(expected));
        }

        /// <summary>
        /// Full detail leaves the graph drawn with the four hundred columns it always was.
        /// </summary>
        [Test]
        public void TestFullDetailIsWhereTheGraphAlwaysWas()
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(DifficultyGraphPart.DEFAULT_DETAIL), Is.EqualTo(400));
        }

        /// <summary>
        /// A column per pixel is already more than can be told apart, and every column is a drawable, so the count
        /// stops rather than following the setting up forever.
        /// </summary>
        [Test]
        public void TestTheCountStopsSomewhere()
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(100), Is.EqualTo(DifficultyGraphPart.MAX_COLUMNS));
        }

        /// <summary>
        /// Enough columns are always drawn for the curve to read as a shape rather than as a handful of blocks.
        /// </summary>
        [TestCase(0.0001)]
        [TestCase(0)]
        [TestCase(-1)]
        public void TestTooLittleDetailIsRefused(double detail)
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(detail), Is.EqualTo(DifficultyGraphPart.MIN_COLUMNS));
        }
    }
}
