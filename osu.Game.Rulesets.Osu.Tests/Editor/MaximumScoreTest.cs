// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// What the status panel reads out as the highest total this difficulty could be worth, on this build's own
    /// scoring and on stable's.
    /// </summary>
    /// <remarks>
    /// The number itself belongs to the simulator score conversion already uses, so what is worth pinning down here is
    /// that it is put together the way stable puts it together — in particular that the mod multiplier is applied,
    /// which is the part that would silently do nothing if it were left out.
    /// </remarks>
    [TestFixture]
    public class MaximumScoreTest
    {
        [Test]
        public void TestABeatmapWithObjectsIsWorthSomething()
        {
            Assert.That(legacyScoreOf(), Is.Not.Null.And.GreaterThan(0));
        }

        /// <remarks>
        /// Hard rock and double time raise the multiplier in stable and easy lowers it, and the multiplier reaching
        /// the total is the whole reason the mods are handed over at all.
        /// </remarks>
        [Test]
        public void TestTheModMultiplierReachesTheTotal()
        {
            long plain = legacyScoreOf()!.Value;

            Assert.That(legacyScoreOf(new OsuModHardRock()), Is.GreaterThan(plain), "hard rock did not raise it");
            Assert.That(legacyScoreOf(new OsuModDoubleTime()), Is.GreaterThan(plain), "double time did not raise it");
            Assert.That(legacyScoreOf(new OsuModEasy()), Is.LessThan(plain), "easy did not lower it");
        }

        /// <remarks>
        /// Every hit in stable is worth its own value plus a share scaled by the combo already held, so twice the
        /// notes come to more than twice the score. A total that only added the hit values up would come out at
        /// exactly double.
        /// </remarks>
        [Test]
        public void TestTheComboBonusGrowsWithTheCombo()
        {
            long forty = legacyScoreOf(objects: 40)!.Value;
            long eighty = legacyScoreOf(objects: 80)!.Value;

            Assert.That(eighty, Is.GreaterThan(forty * 2));
        }

        /// <remarks>
        /// Standardised mode gives a million on every beatmap of nothing but circles, which is exactly why it is not
        /// the number worth reading.
        /// </remarks>
        [Test]
        public void TestStandardisedComesToAMillion()
        {
            Assert.That(scoreOf(ScoringMode.Standardised), Is.EqualTo(1_000_000));
        }

        /// <remarks>
        /// Classic mode scales that million by how much there is to hit, so it differs from beatmap to beatmap and
        /// comes out far larger. That is what the client shows when it is set that way, and what was asked for.
        /// </remarks>
        [Test]
        public void TestClassicScalesWithHowMuchThereIsToHit()
        {
            long forty = scoreOf(ScoringMode.Classic)!.Value;
            long eighty = scoreOf(ScoringMode.Classic, objects: 80)!.Value;

            Assert.That(forty, Is.Not.EqualTo(1_000_000));
            Assert.That(eighty, Is.GreaterThan(forty * 2));
        }

        [Test]
        public void TestTheModMultiplierReachesTheDisplayedTotal()
        {
            Assert.That(scoreOf(ScoringMode.Standardised, new OsuModEasy()), Is.LessThan(1_000_000));
        }

        [Test]
        public void TestNothingToScoreIsWorthNothingOnEitherScoring()
        {
            Assert.That(legacyScoreOf(objects: 0), Is.Null);
            Assert.That(scoreOf(ScoringMode.Classic, objects: 0), Is.Null);
        }

        private static long? scoreOf(ScoringMode mode, params Mod[] mods) => scoreOf(mode, mods, objects: 40);

        private static long? scoreOf(ScoringMode mode, int objects) => scoreOf(mode, [], objects);

        private static long? scoreOf(ScoringMode mode, Mod[] mods, int objects)
        {
            var ruleset = new OsuRuleset();
            var playable = new TestWorkingBeatmap(beatmapOf(objects, ruleset.RulesetInfo)).GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            return EditorDifficultyTracker.MaximumScoreFor(ruleset, playable, mods, mode);
        }

        private static long? legacyScoreOf(params Mod[] mods) => legacyScoreOf(mods, objects: 40);

        private static long? legacyScoreOf(int objects) => legacyScoreOf([], objects);

        private static long? legacyScoreOf(Mod[] mods, int objects)
        {
            var ruleset = new OsuRuleset();
            var beatmap = beatmapOf(objects, ruleset.RulesetInfo);
            var working = new TestWorkingBeatmap(beatmap);
            var playable = working.GetPlayableBeatmap(ruleset.RulesetInfo, mods);

            return EditorDifficultyTracker.MaximumLegacyScoreFor(ruleset, working, playable, mods);
        }

        private static OsuBeatmap beatmapOf(int objects, RulesetInfo ruleset)
        {
            var beatmap = new OsuBeatmap { BeatmapInfo = { Ruleset = ruleset } };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            for (int i = 0; i < objects; i++)
                beatmap.HitObjects.Add(new HitCircle { StartTime = 1000 + i * 500 });

            foreach (var hitObject in beatmap.HitObjects.Cast<HitObject>())
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }
    }
}
