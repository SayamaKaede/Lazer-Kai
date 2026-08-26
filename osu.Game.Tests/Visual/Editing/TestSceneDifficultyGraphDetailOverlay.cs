// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.DifficultyGraph;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    [TestFixture]
    public partial class TestSceneDifficultyGraphDetailOverlay : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// Opening it at all is most of the test. A screen is a pile of containers sizing themselves against one
        /// another, and getting that wrong throws on the update thread rather than looking slightly off — which is
        /// the whole editor gone, from a menu item.
        /// </summary>
        [Test]
        public void TestOpensAndDrawsTheBeatmap()
        {
            AddStep("open the detail screen", () => overlay().Show());

            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("the curve is drawn", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);
            AddUntilStep("the skills are drawn", () => overlay().ChildrenOfType<SkillRow>().Any(), () => Is.True);
        }

        /// <summary>
        /// The rows fold away, and folding them is not allowed to take the screen with it.
        /// </summary>
        [Test]
        public void TestTheSkillRowsFold()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("the skills are drawn", () => overlay().ChildrenOfType<SkillRow>().Any(), () => Is.True);

            // the rows are folded away by hiding what holds them, so the rows themselves are untouched — what has to
            // be true is that nothing of them reaches the screen, and that the room they took goes with them.
            AddStep("fold them away", () => config.SetValue(OsuSetting.EditorDifficultyGraphOverlayShowSkills, false));
            AddUntilStep("they are out of sight", () => overlay().ChildrenOfType<SkillRow>().All(r => !r.Parent!.IsPresent), () => Is.True);

            AddStep("unfold them", () => config.SetValue(OsuSetting.EditorDifficultyGraphOverlayShowSkills, true));
            AddUntilStep("they are back", () => overlay().ChildrenOfType<SkillRow>().All(r => r.Parent!.IsPresent), () => Is.True);
        }

        /// <summary>
        /// Closing and opening again is the ordinary way this is used, and the second open rebuilds everything the
        /// first one built.
        /// </summary>
        [Test]
        public void TestItSurvivesBeingOpenedTwice()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("the curve is drawn", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);

            AddStep("close it", () => overlay().Hide());
            AddStep("open it again", () => overlay().Show());

            AddUntilStep("the curve is drawn again", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);
        }

        /// <summary>
        /// A screen with no way out is worse than no screen. Escape has to close it and leave the editor where it
        /// was, rather than falling through to the editor's own back action and walking out to song select.
        /// </summary>
        [Test]
        public void TestEscapeClosesItRatherThanTheEditor()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("it closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the editor is still here", () => Editor.IsCurrentScreen(), () => Is.True);
        }

        /// <summary>
        /// The close button is the other way out, and the one a mouse reaches for.
        /// </summary>
        [Test]
        public void TestTheCloseButtonCloses()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("click the close button", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<IconButton>().Single());
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("it closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        /// <summary>
        /// The bar at the bottom is deliberately left out of the shade: scrubbing while reading the graph is worth
        /// keeping, and nothing there edits the beatmap.
        /// </summary>
        [Test]
        public void TestTheBottomBarIsStillReachable()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddAssert("the shade stops short of the bottom", () =>
            {
                // the shade fills the overlay, and the overlay holds itself off the bottom by the height of the bar.
                var shade = overlay().ChildrenOfType<Box>().First();

                return shade.ScreenSpaceDrawQuad.BottomLeft.Y < overlay().ScreenSpaceDrawQuad.BottomLeft.Y - 1;
            }, () => Is.True);
        }

        private DifficultyGraphDetailOverlay overlay() => Editor.ChildrenOfType<DifficultyGraphDetailOverlay>().Single();
    }
}
