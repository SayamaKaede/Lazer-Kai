// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Screens.Menu;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.UserInterface
{
    /// <summary>
    /// Dragging the title screen's logo round turns it, it keeps turning once let go, and a spin is not a click.
    /// </summary>
    public partial class TestSceneSpinningTheLogo : OsuManualInputManagerTestScene
    {
        private OsuLogo logo = null!;
        private int clicks;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("add logo", () =>
            {
                clicks = 0;
                Child = logo = new OsuLogo
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Action = () =>
                    {
                        clicks++;
                        return true;
                    },
                };
            });
        }

        [Test]
        public void TestDraggingRoundTurnsItAndItKeepsTurning()
        {
            float rotationOnRelease = 0;

            Vector2 sizeBefore = Vector2.Zero;

            // the part holding the coloured circle, unscaled: hovering scales the whole logo up on purpose.
            AddStep("note the logo's size", () => sizeBefore = logo.LogoElements.DrawSize);
            AddStep("press at the edge", () =>
            {
                InputManager.MoveMouseTo(pointAt(0));
                InputManager.PressButton(MouseButton.Left);
            });

            // moved in one step and let go in the same one, as a flick is: a cursor held still before letting go stops the logo.
            AddStep("flick a quarter turn round and let go", () =>
            {
                for (int angle = 5; angle <= 90; angle += 5)
                    InputManager.MoveMouseTo(pointAt(angle));

                InputManager.ReleaseButton(MouseButton.Left);
            });
            AddAssert("turned clockwise", () => rotation(), () => Is.GreaterThan(45));
            // a turned square takes more room than an upright one; the coloured circle behind must not grow with it.
            AddAssert("the logo has not grown", () => logo.LogoElements.DrawSize, () => Is.EqualTo(sizeBefore));
            AddStep("note where it is", () => rotationOnRelease = rotation());
            AddUntilStep("keeps turning", () => rotation(), () => Is.Not.EqualTo(rotationOnRelease));
            AddAssert("not taken as a click", () => clicks, () => Is.Zero);
        }

        [Test]
        public void TestAClickStillClicks()
        {
            AddStep("click the logo", () =>
            {
                InputManager.MoveMouseTo(logo);
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("clicked", () => clicks, () => Is.EqualTo(1));
            AddAssert("not turned", () => rotation(), () => Is.Zero);
        }

        /// <summary>A point near the logo's edge, <paramref name="degrees"/> clockwise from the right.</summary>
        private Vector2 pointAt(float degrees)
        {
            var quad = logoSprite.ScreenSpaceDrawQuad;
            float radius = quad.Width * 0.4f;
            float radians = MathF.PI * degrees / 180;
            return quad.Centre + radius * new Vector2(MathF.Cos(radians), MathF.Sin(radians));
        }

        private Sprite logoSprite => logo.ChildrenOfType<Sprite>().Last(s => s.Texture != null && s.Alpha > 0);

        private float rotation() => logoSprite.Rotation;
    }
}
