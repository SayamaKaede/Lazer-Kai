// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.UI;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Right clicking the playfield where there is nothing, which turns new combo on and off the way the toolbox
    /// button does.
    /// </summary>
    /// <remarks>
    /// The reach for it is with a placement tool held, mid-pattern, which is also when right click means quick delete
    /// — so what is worth pinning down is that having a tool other than select does not take it away.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneOsuEditorRightClickNewCombo : TestSceneOsuEditor
    {
        private OsuPlayfield playfield = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private OsuHitObjectComposer composer => Editor.ChildrenOfType<OsuHitObjectComposer>().Single();

        private TernaryState newCombo => composer.SelectionNewComboState!.Value;

        [TearDownSteps]
        public void TearDownSwapSteps()
        {
            AddStep("unswap the buttons", () => config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse).SetDefault());
        }

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());

            // far enough past the end that nothing is under the cursor, including the fork's grace period for
            // selecting an object shortly after it finishes.
            AddStep("seek past the last object",
                () => EditorClock.Seek(EditorBeatmap.HitObjects.Last().GetEndTime() + 5000));
        }

        [TestCase(Key.Number1)] // select
        [TestCase(Key.Number2)] // circle
        [TestCase(Key.Number3)] // slider
        public void TestRightClickOnNothingTogglesNewCombo(Key tool)
        {
            AddStep("pick the tool", () => InputManager.Key(tool));
            AddStep("move onto nothing", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            TernaryState before = TernaryState.False;
            AddStep("remember where it was", () => before = newCombo);

            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddAssert("new combo flipped", () => newCombo, () => Is.Not.EqualTo(before));

            AddStep("right click again", () => InputManager.Click(MouseButton.Right));
            AddAssert("new combo flipped back", () => newCombo, () => Is.EqualTo(before));
        }

        /// <remarks>
        /// The swap exists so that finishing a slider stays under the same finger, and it is applied at the input
        /// source — so with it on, the button the hand reaches for arrives as middle. New combo has to follow the
        /// hand rather than the name, or turning the setting on takes the toggle away without saying so.
        /// </remarks>
        [Test]
        public void TestNewComboStaysOnThePhysicalRightButtonWhenSwapped()
        {
            AddStep("swap the buttons", () => config.SetValue(OsuSetting.SwapRightAndMiddleMouse, true));
            AddStep("move onto nothing", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            TernaryState before = TernaryState.False;
            AddStep("remember where it was", () => before = newCombo);

            AddStep("press what the right button now sends", () => InputManager.Click(MouseButton.Middle));
            AddAssert("new combo flipped", () => newCombo, () => Is.Not.EqualTo(before));
        }

        [Test]
        public void TestTheOtherButtonIsNotTheToggleWhenSwapped()
        {
            AddStep("swap the buttons", () => config.SetValue(OsuSetting.SwapRightAndMiddleMouse, true));
            AddStep("move onto nothing", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            TernaryState before = TernaryState.False;
            AddStep("remember where it was", () => before = newCombo);

            AddStep("press what the wheel now sends", () => InputManager.Click(MouseButton.Right));
            AddAssert("new combo did not move", () => newCombo, () => Is.EqualTo(before));
        }
    }
}
