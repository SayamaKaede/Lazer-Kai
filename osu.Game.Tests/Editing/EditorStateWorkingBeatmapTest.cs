// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Screens.Edit.DifficultyGraph;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The difficulty graph works out the beatmap as it stands in the editor, in whichever ruleset it is.
    /// </summary>
    [TestFixture]
    public class EditorStateWorkingBeatmapTest
    {
        private static readonly Ruleset[] rulesets = { new OsuRuleset(), new TaikoRuleset(), new CatchRuleset(), new ManiaRuleset() };

        /// <summary>
        /// Reported through the error reports on 2026-09-25: opening a mania beatmap, the graph could not be worked out,
        /// its calculator wanting a mania beatmap (for its columns) and being handed a plain one.
        /// </summary>
        [TestCaseSource(nameof(rulesets))]
        public void TestTheGraphCanBeWorkedOut(Ruleset ruleset)
        {
            // what the editor holds: the beatmap made playable in the ruleset it is edited in.
            var editorState = new TestWorkingBeatmap(new TestBeatmap(ruleset.RulesetInfo)).GetPlayableBeatmap(ruleset.RulesetInfo);

            var working = new EditorStateWorkingBeatmap(editorState);

            Assert.DoesNotThrow(() => ruleset.CreateDifficultyCalculator(working).CalculateBreakdown(Array.Empty<Mod>()));
        }
    }
}
