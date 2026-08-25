// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DifficultyGraphPartTest
    {
        private const double minute = 60_000;

        /// <summary>
        /// A column stands for a fixed stretch of the song, so a longer beatmap is drawn with more of them rather
        /// than with the same number spread thinner. Drawn across a width that does not grow, a fixed count means a
        /// marathon's columns each swallow seconds of the song and the shape of it goes flat.
        /// </summary>
        [Test]
        public void TestALongerBeatmapIsDrawnWithMoreColumns()
        {
            int shortMap = DifficultyGraphPart.ColumnCountFor(2 * minute, 375);
            int longMap = DifficultyGraphPart.ColumnCountFor(6 * minute, 375);

            Assert.That(longMap, Is.GreaterThan(shortMap));
        }

        /// <summary>
        /// The setting is how long one column stands for, so halving it draws twice as many.
        /// </summary>
        [Test]
        public void TestAFinerSettingDrawsMoreColumns()
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(3 * minute, 250), Is.EqualTo(720));
            Assert.That(DifficultyGraphPart.ColumnCountFor(3 * minute, 500), Is.EqualTo(360));
        }

        /// <summary>
        /// A column per pixel is already more than can be told apart, and every column is a drawable. Past that the
        /// count stops rather than growing with the song.
        /// </summary>
        [Test]
        public void TestTheCountStopsSomewhere()
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(60 * minute, 50), Is.EqualTo(DifficultyGraphPart.MAX_COLUMNS));
        }

        /// <summary>
        /// A beatmap only a few seconds long still gets enough columns to read as a shape rather than as one block.
        /// </summary>
        [TestCase(0)]
        [TestCase(1_000)]
        [TestCase(4_000)]
        public void TestAVeryShortBeatmapStillGetsAShape(double trackLength)
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(trackLength, 375), Is.EqualTo(DifficultyGraphPart.MIN_COLUMNS));
        }

        /// <summary>
        /// Nothing should ask for columns of no length, but dividing by it would hand back a count that cannot be
        /// drawn, and the graph is not worth a crash.
        /// </summary>
        [TestCase(0)]
        [TestCase(-1)]
        public void TestAColumnOfNoLengthIsRefused(double columnLength)
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(3 * minute, columnLength), Is.EqualTo(DifficultyGraphPart.MIN_COLUMNS));
        }

        /// <summary>
        /// The default keeps a beatmap of ordinary length looking the way it did before the count could be changed.
        /// </summary>
        [Test]
        public void TestTheDefaultLeavesAnOrdinaryBeatmapWhereItWas()
        {
            Assert.That(DifficultyGraphPart.ColumnCountFor(2.5 * minute, DifficultyGraphPart.DEFAULT_COLUMN_LENGTH), Is.EqualTo(400));
        }
    }
}
