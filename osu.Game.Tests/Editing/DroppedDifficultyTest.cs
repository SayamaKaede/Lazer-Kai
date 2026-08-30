// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class DroppedDifficultyTest
    {
        private const int osu = 0;
        private const int taiko = 1;

        [Test]
        public void TestNameNotInSetIsAdded()
        {
            var set = setWith(("Easy", osu), ("Hard", osu));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[0], "Insane", osu, out var target), Is.EqualTo(DroppedDifficulty.Outcome.Added));
            Assert.That(target, Is.Null);
        }

        [Test]
        public void TestNameInSetIsReplaced()
        {
            var set = setWith(("Easy", osu), ("Hard", osu));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[0], "Hard", osu, out var target), Is.EqualTo(DroppedDifficulty.Outcome.Replaced));
            Assert.That(target, Is.EqualTo(set.Beatmaps[1]));
        }

        [Test]
        public void TestNameOfOpenDifficultyIsAskedAbout()
        {
            var set = setWith(("Easy", osu), ("Hard", osu));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[1], "Hard", osu, out var target), Is.EqualTo(DroppedDifficulty.Outcome.ReplacedCurrent));
            Assert.That(target, Is.EqualTo(set.Beatmaps[1]));
        }

        [Test]
        public void TestMatchIsCaseInsensitive()
        {
            var set = setWith(("Easy", osu), ("Kaede's Extra", osu));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[0], "kaede's extra", osu, out var target), Is.EqualTo(DroppedDifficulty.Outcome.Replaced));
            Assert.That(target, Is.EqualTo(set.Beatmaps[1]));
        }

        /// <summary>
        /// Nothing open means nothing to lose, so a match there is a plain replacement rather than a question.
        /// </summary>
        [Test]
        public void TestNoCurrentDifficultyNeverAsks()
        {
            var set = setWith(("Easy", osu), ("Hard", osu));

            Assert.That(DroppedDifficulty.DecideFor(set, null, "Hard", osu, out _), Is.EqualTo(DroppedDifficulty.Outcome.Replaced));
        }

        /// <summary>
        /// Converted sets carry the same names in more than one mode, and a file only means the one in its own.
        /// </summary>
        [Test]
        public void TestSameNameInAnotherModeIsNotTheOneMeant()
        {
            var set = setWith(("Normal", osu), ("Normal", taiko));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[0], "Normal", taiko, out var target), Is.EqualTo(DroppedDifficulty.Outcome.Replaced));
            Assert.That(target, Is.EqualTo(set.Beatmaps[1]));
        }

        [Test]
        public void TestNameHeldByAnotherModeAloneIsLeftAlone()
        {
            var set = setWith(("Easy", osu), ("Muzukashii", taiko));

            Assert.That(DroppedDifficulty.DecideFor(set, set.Beatmaps[0], "Muzukashii", osu, out var target), Is.EqualTo(DroppedDifficulty.Outcome.NameTaken));
            Assert.That(target, Is.Null);
        }

        private static BeatmapSetInfo setWith(params (string name, int rulesetId)[] difficulties)
        {
            var set = new BeatmapSetInfo();

            foreach ((string name, int rulesetId) in difficulties)
            {
                set.Beatmaps.Add(new BeatmapInfo
                {
                    ID = Guid.NewGuid(),
                    DifficultyName = name,
                    Ruleset = new RulesetInfo { OnlineID = rulesetId },
                });
            }

            return set;
        }
    }
}
