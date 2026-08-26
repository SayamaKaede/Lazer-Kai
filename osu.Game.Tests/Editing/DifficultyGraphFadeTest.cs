// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Screens.Edit.DifficultyGraph;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DifficultyGraphFadeTest
    {
        private static bool[] nothing(int columns) => new bool[columns];

        /// <summary>
        /// A column with something measured in it is itself, at full height.
        /// </summary>
        [Test]
        public void TestAMeasuredColumnIsLeftAlone()
        {
            var fade = DifficultyGraphFade.Across(new[] { true, true }, nothing(2));

            Assert.That(fade[0].Source, Is.Zero);
            Assert.That(fade[0].Factor, Is.EqualTo(1));
            Assert.That(fade[1].Source, Is.EqualTo(1));
            Assert.That(fade[1].Factor, Is.EqualTo(1));
        }

        /// <summary>
        /// A stretch with nothing measured in it winds down from whatever came before rather than dropping out, so a
        /// long slider reads as something still going on instead of as a hole in the beatmap.
        /// </summary>
        /// <remarks>
        /// Counted in columns rather than in seconds. The graph is drawn with the same number of columns whatever the
        /// beatmap, so a fade measured in time would trail across half a short beatmap and be invisible on a long one.
        /// </remarks>
        [Test]
        public void TestAGapWindsDownFromWhatCameBefore()
        {
            var fade = DifficultyGraphFade.Across(new[] { true, false, false }, nothing(3));

            Assert.That(fade[1].Source, Is.Zero);
            Assert.That(fade[2].Source, Is.Zero);
            Assert.That(fade[1].Factor, Is.GreaterThan(fade[2].Factor));
            Assert.That(fade[1].Factor, Is.LessThan(1));
        }

        /// <summary>
        /// A tenth left after the agreed number of columns, whatever that number is set to.
        /// </summary>
        [Test]
        public void TestATenthIsLeftAtTheEndOfTheFade()
        {
            bool[] measured = new bool[DifficultyGraphFade.FADE_COLUMNS + 1];
            measured[0] = true;

            var fade = DifficultyGraphFade.Across(measured, nothing(measured.Length));

            Assert.That(fade[DifficultyGraphFade.FADE_COLUMNS].Factor, Is.EqualTo(0.1).Within(1e-9));
        }

        /// <summary>
        /// Nothing has been measured yet at the start of a beatmap, and there is nothing to wind down from.
        /// </summary>
        [Test]
        public void TestThereIsNothingToFadeFromBeforeTheFirstObject()
        {
            var fade = DifficultyGraphFade.Across(new[] { false, false, true }, nothing(3));

            Assert.That(fade[0].Source, Is.EqualTo(-1));
            Assert.That(fade[1].Source, Is.EqualTo(-1));
        }

        /// <summary>
        /// A break is drawn as the empty stretch it is. The bar along the bottom of the editor already says where the
        /// breaks are, and leaving them blank is what tells them apart from a slider being held.
        /// </summary>
        [Test]
        public void TestABreakStaysEmpty()
        {
            var fade = DifficultyGraphFade.Across(new[] { true, false, false }, new[] { false, true, false });

            Assert.That(fade[1].Source, Is.EqualTo(-1));
        }

        /// <summary>
        /// And the beatmap starts again after one, rather than picking the fade back up where it left off.
        /// </summary>
        [Test]
        public void TestTheFadeDoesNotResumeAfterABreak()
        {
            var fade = DifficultyGraphFade.Across(new[] { true, false, false }, new[] { false, true, false });

            Assert.That(fade[2].Source, Is.EqualTo(-1));
        }
    }
}
