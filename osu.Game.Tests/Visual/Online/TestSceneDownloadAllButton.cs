// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osu.Game.Overlays.Profile.Sections.Beatmaps;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Online
{
    /// <summary>
    /// A profile listing's "download all" fires on a hold and never on a click.
    /// </summary>
    public partial class TestSceneDownloadAllButton : OsuManualInputManagerTestScene
    {
        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Pink);

        private DownloadAllButton button = null!;
        private int fired;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("put a button up", () =>
            {
                fired = 0;
                Child = button = new DownloadAllButton
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Action = () => fired++,
                };
            });
            AddStep("move onto it", () => InputManager.MoveMouseTo(button));
        }

        [Test]
        public void TestAClickIsNotEnough()
        {
            double releasedAt = 0;

            AddStep("click", () =>
            {
                InputManager.Click(MouseButton.Left);
                releasedAt = Time.Current;
            });
            AddUntilStep("well past the hold time", () => Time.Current, () => Is.GreaterThan(releasedAt + HoldToConfirmContainer.DANGEROUS_HOLD_ACTIVATION_DELAY + 300));

            AddAssert("it did not fire", () => fired, () => Is.Zero);
        }

        [Test]
        public void TestHoldingFiresItOnce()
        {
            AddStep("press", () => InputManager.PressButton(MouseButton.Left));
            AddUntilStep("it fired", () => fired, () => Is.EqualTo(1));

            AddWaitStep("keep holding a moment", 5);
            AddAssert("still only once", () => fired, () => Is.EqualTo(1));

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        /// <summary>
        /// A finished run leaves it ready to be held again, for a listing that has grown since.
        /// </summary>
        [Test]
        public void TestItCanBeHeldAgainAfterwards()
        {
            AddStep("press", () => InputManager.PressButton(MouseButton.Left));
            AddUntilStep("it fired", () => fired, () => Is.EqualTo(1));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("press again", () => InputManager.PressButton(MouseButton.Left));
            AddUntilStep("it fired again", () => fired, () => Is.EqualTo(2));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [Test]
        public void TestNothingWhileDisabled()
        {
            double pressedAt = 0;

            AddStep("disable it", () => button.Enabled.Value = false);
            AddStep("press", () =>
            {
                InputManager.PressButton(MouseButton.Left);
                pressedAt = Time.Current;
            });
            AddUntilStep("well past the hold time", () => Time.Current, () => Is.GreaterThan(pressedAt + HoldToConfirmContainer.DANGEROUS_HOLD_ACTIVATION_DELAY + 300));

            AddAssert("it did not fire", () => fired, () => Is.Zero);
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        /// <summary>
        /// Sliding off while holding takes it back, the way a dialog's dangerous button does.
        /// </summary>
        [Test]
        public void TestSlidingOffTakesItBack()
        {
            double movedAt = 0;

            AddStep("press", () => InputManager.PressButton(MouseButton.Left));
            AddStep("slide off", () =>
            {
                InputManager.MoveMouseTo(button.ScreenSpaceDrawQuad.TopLeft - new osuTK.Vector2(50));
                movedAt = Time.Current;
            });
            AddUntilStep("well past the hold time", () => Time.Current, () => Is.GreaterThan(movedAt + HoldToConfirmContainer.DANGEROUS_HOLD_ACTIVATION_DELAY + 300));

            AddAssert("it did not fire", () => fired, () => Is.Zero);
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }
    }
}
