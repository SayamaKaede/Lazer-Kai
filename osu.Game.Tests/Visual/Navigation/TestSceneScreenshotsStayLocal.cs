// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Input.Bindings;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Navigation
{
    /// <summary>
    /// Upstream uploads a screenshot to the official servers on shift-F12. This build is a modified client, so its
    /// screenshots stay on the player's machine and the key takes an ordinary one, as stable's habit expects something.
    /// </summary>
    public partial class TestSceneScreenshotsStayLocal : OsuGameTestScene
    {
        [Test]
        public void TestShiftF12TakesAnOrdinaryScreenshot()
        {
            AddStep("hold shift-F12", () =>
            {
                InputManager.PressKey(Key.LShift);
                InputManager.PressKey(Key.F12);
            });
            AddAssert("an ordinary screenshot is asked for", () => globalActions.PressedActions, () => Is.EquivalentTo(new[] { GlobalAction.TakeScreenshot }));
            AddStep("let go", () =>
            {
                InputManager.ReleaseKey(Key.F12);
                InputManager.ReleaseKey(Key.LShift);
            });
        }

        [Test]
        public void TestTheUploadIsNotTaken()
        {
            // a build that had the upload stored its binding, which stays in the database of whoever ran it.
            AddAssert("the upload is not taken", () => Game.ChildrenOfType<ScreenshotManager>().Single()
                                                           .OnPressed(new KeyBindingPressEvent<GlobalAction>(InputManager.CurrentState, GlobalAction.TakeAndUploadScreeshot)),
                () => Is.False);
        }

        private GlobalActionContainer globalActions => Game.ChildrenOfType<GlobalActionContainer>().First();
    }
}
