// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Tests.Rulesets.Difficulty
{
    [TestFixture]
    public class DifficultyBreakdownTest
    {
        /// <summary>
        /// Every skill the ruleset measures the beatmap with belongs in the curve, once each.
        /// </summary>
        [Test]
        public void TestEachSkillContributes()
        {
            var breakdown = new DifficultyBreakdown(new double[] { 0, 1 }, new Skill[]
            {
                new TestAim(1, 10),
                new TestSpeed(2, 20),
                new TestReading(3, 30),
            });

            Assert.That(breakdown.GetCombinedDifficulties(), Is.EqualTo(new double[] { 6, 60 }));
        }

        /// <summary>
        /// A ruleset may build a second copy of a skill only to take a ratio against it — osu! builds aim a second
        /// time with sliders left out to arrive at its slider factor, taiko builds stamina a second time for single
        /// colours. Those copies are not difficulty of their own, and adding them in counts one skill twice, which
        /// tilts the whole curve towards whatever that skill happens to measure.
        /// </summary>
        [Test]
        public void TestSkillBuiltTwiceCountsOnce()
        {
            var breakdown = new DifficultyBreakdown(new double[] { 0, 1 }, new Skill[]
            {
                new TestAim(1, 10),
                new TestAim(2, 20),
                new TestSpeed(3, 30),
            });

            Assert.That(breakdown.GetCombinedDifficulties(), Is.EqualTo(new double[] { 4, 40 }));
        }

        /// <summary>
        /// The copy kept is the one the ruleset built first, which is the one it treats as the real measurement:
        /// osu! lists aim with sliders ahead of aim without, taiko lists stamina ahead of its single-colour copy.
        /// </summary>
        [Test]
        public void TestTheFirstOfTheCopiesIsTheOneKept()
        {
            var breakdown = new DifficultyBreakdown(new double[] { 0 }, new Skill[]
            {
                new TestAim(7),
                new TestAim(100),
            });

            Assert.That(breakdown.GetCombinedDifficulties(), Is.EqualTo(new double[] { 7 }));
        }

        /// <summary>
        /// A skill is free to stop early on an object it has nothing to say about, so it can report fewer values than
        /// there were objects. The ones it did report still have to land on the right objects.
        /// </summary>
        [Test]
        public void TestSkillReportingFewerValuesThanThereAreObjects()
        {
            var breakdown = new DifficultyBreakdown(new double[] { 0, 1, 2 }, new Skill[]
            {
                new TestAim(1, 2, 3),
                new TestSpeed(10),
            });

            Assert.That(breakdown.GetCombinedDifficulties(), Is.EqualTo(new double[] { 11, 2, 3 }));
        }

        private class TestSkill : Skill
        {
            protected TestSkill(double[] difficulties)
                : base(Array.Empty<Mod>())
            {
                ObjectDifficulties.AddRange(difficulties);
            }

            protected override double ProcessInternal(DifficultyHitObject current) => 0;

            public override double DifficultyValue() => 0;
        }

        private class TestAim : TestSkill
        {
            public TestAim(params double[] difficulties)
                : base(difficulties)
            {
            }
        }

        private class TestSpeed : TestSkill
        {
            public TestSpeed(params double[] difficulties)
                : base(difficulties)
            {
            }
        }

        private class TestReading : TestSkill
        {
            public TestReading(params double[] difficulties)
                : base(difficulties)
            {
            }
        }
    }
}
