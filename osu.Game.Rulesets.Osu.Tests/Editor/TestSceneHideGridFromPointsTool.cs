// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Tests.Beatmaps;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Keeping the grid from points tool out of the toolbox, for mappers who never reach for it and keep hitting its
    /// number key by accident.
    /// </summary>
    public partial class TestSceneHideGridFromPointsTool : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestHidingTakesTheButtonAndItsNumberKeyAway()
        {
            setHidden(false);

            AddAssert("the button is there", () => gridButton().IsPresent, () => Is.True);

            press5();
            AddAssert("5 selects the grid tool", () => gridButton().Selected.Value, () => Is.True);

            setHidden(true);

            AddUntilStep("the button went away", () => gridButton().IsPresent, () => Is.False);

            // hiding it while it was in use would otherwise leave the mapper composing with a tool they cannot see.
            AddAssert("the select tool took over", () => selectButton().Selected.Value, () => Is.True);

            press5();
            AddAssert("5 no longer reaches it", () => gridButton().Selected.Value, () => Is.False);
            AddAssert("and did not disturb the select tool", () => selectButton().Selected.Value, () => Is.True);
        }

        /// <summary>
        /// Turning the setting back off has to bring it back without reopening the editor.
        /// </summary>
        [Test]
        public void TestUnhidingBringsItBack()
        {
            setHidden(true);
            AddUntilStep("the button went away", () => gridButton().IsPresent, () => Is.False);

            setHidden(false);
            AddUntilStep("the button is back", () => gridButton().IsPresent, () => Is.True);

            press5();
            AddAssert("5 selects the grid tool again", () => gridButton().Selected.Value, () => Is.True);
        }

        private void setHidden(bool hidden)
            => AddStep(hidden ? "hide the grid tool" : "show the grid tool", () => config.SetValue(OsuSetting.EditorHideGridFromPointsTool, hidden));

        private void press5() => AddStep("press 5", () => InputManager.Key(Key.Number5));

        private HitObjectCompositionToolButton gridButton()
            => Editor.ChildrenOfType<HitObjectCompositionToolButton>().Single(b => b.Tool is GridFromPointsTool);

        private HitObjectCompositionToolButton selectButton()
            => Editor.ChildrenOfType<HitObjectCompositionToolButton>().Single(b => b.Tool is SelectTool);
    }
}
