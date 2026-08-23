// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation.Osu;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.RadioButtons;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Turning new combo on while a tool is selected has to reach the object about to be placed, whichever tool it is.
    /// </summary>
    [TestFixture]
    public partial class TestSceneNewComboDuringPlacement : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private OsuHitObjectComposer composer => Editor.ChildrenOfType<OsuHitObjectComposer>().Single();

        private ComposeBlueprintContainer blueprints => Editor.ChildrenOfType<ComposeBlueprintContainer>().Single();

        [SetUpSteps]
        public void SetUpComboSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("composer is up", () => Editor.ChildrenOfType<OsuHitObjectComposer>().Any());
        }

        private void selectTool(string name) => AddStep($"select the {name} tool", () =>
            composer.ChildrenOfType<EditorRadioButton>().First(b => b.Text.ToString() == name).TriggerClick());

        private bool placementHasNewCombo() =>
            blueprints.CurrentHitObjectPlacement?.HitObject is IHasComboInformation combo && combo.NewCombo;

        [Test]
        public void TestNewComboReachesACircleAboutToBePlaced()
        {
            selectTool(OsuEditorStrings.HitCircleTool.ToString());

            AddStep("turn new combo on", () => composer.SelectionNewComboState.Value = TernaryState.True);
            AddUntilStep("the circle takes it", placementHasNewCombo);
        }

        /// <remarks>
        /// The same thing with the slider tool did nothing, and the only way to start a slider on a new combo was to
        /// press the button before picking the tool.
        /// </remarks>
        [Test]
        public void TestNewComboReachesASliderAboutToBePlaced()
        {
            selectTool(OsuEditorStrings.SliderTool.ToString());


            AddStep("turn new combo on", () => composer.SelectionNewComboState.Value = TernaryState.True);
            AddUntilStep("the slider takes it", placementHasNewCombo);
        }

        [Test]
        public void TestNewComboSetBeforePickingTheToolStillApplies()
        {
            AddStep("turn new combo on", () => composer.SelectionNewComboState.Value = TernaryState.True);

            selectTool(OsuEditorStrings.SliderTool.ToString());

            AddUntilStep("the slider takes it", placementHasNewCombo);
        }
    }
}
