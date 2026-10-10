// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Tests.Visual.UserInterface
{
    /// <summary>
    /// That the sliders a number is set with take the reduced drag threshold, when the user has asked for it.
    /// </summary>
    /// <remarks>
    /// Waiting for the mouse to travel ten pixels before the handle follows it makes a change of a step or two
    /// impossible to drag (2026-10-11). The threshold itself is applied by the game's input manager, which a test scene
    /// replaces, so what is checked here is that a slider under the pointer asks for it.
    /// </remarks>
    public partial class TestSceneSliderBarFineDrag : OsuManualInputManagerTestScene
    {
        [Cached]
        private OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Purple);

        private RoundedSliderBar<float> rounded = null!;
        private ShearedSliderBar<float> sheared = null!;
        private FormSliderBar<float> form = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create the sliders", () => Child = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Width = 400,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 30),
                Children = new Drawable[]
                {
                    rounded = new RoundedSliderBar<float> { RelativeSizeAxes = Axes.X, Current = number() },
                    sheared = new ShearedSliderBar<float> { RelativeSizeAxes = Axes.X, Current = number() },
                    form = new FormSliderBar<float> { Caption = @"A number", Current = number() },
                }
            });
        }

        [Test]
        public void TestASliderUnderThePointerAsksForIt()
        {
            assertAsksForIt("the rounded slider", () => rounded);
            assertAsksForIt("the sheared slider", () => sheared);
            assertAsksForIt("the form's slider", () => form.ChildrenOfType<FormSliderBar<float>.InnerSlider>().Single());
        }

        [Test]
        public void TestNothingAsksForItAwayFromASlider()
        {
            AddStep("move off the sliders", () => InputManager.MoveMouseTo(rounded.ScreenSpaceDrawQuad.TopLeft - new Vector2(100)));
            AddAssert("nothing asks for it", () => InputManager.PositionalInputQueue.OfType<IFineDragArea>(), () => Is.Empty);
        }

        private void assertAsksForIt(string name, System.Func<Drawable> slider)
        {
            AddStep($"hover {name}", () => InputManager.MoveMouseTo(slider()));
            AddAssert($"{name} asks for it", () => InputManager.PositionalInputQueue.OfType<IFineDragArea>().Contains(slider() as IFineDragArea), () => Is.True);
        }

        private static BindableFloat number() => new BindableFloat(5) { MinValue = 0, MaxValue = 10, Precision = 0.1f };
    }
}
