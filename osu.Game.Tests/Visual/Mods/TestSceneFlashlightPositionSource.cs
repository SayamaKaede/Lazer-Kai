// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osuTK;

namespace osu.Game.Tests.Visual.Mods
{
    /// <summary>
    /// Whether something other than the mouse can lead the lit area, which is what the editor's mod preview does
    /// with autoplay's cursor.
    /// </summary>
    public partial class TestSceneFlashlightPositionSource : OsuTestScene
    {
        private const float marker_x = 60;
        private const float marker_y = 40;

        private TestFlashlight flashlight = null!;
        private Drawable marker = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("build a flashlight", () =>
            {
                var mod = new TestFlashlightMod();
                var tracker = new ModFlashlight<HitObject>.PlayfieldDrawInfoTracker();

                flashlight = (TestFlashlight)mod.MakeFlashlight();
                flashlight.RelativeSizeAxes = Axes.Both;
                flashlight.PlayfieldDrawInfoTracker = tracker;

                Child = new Container
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(300),
                    Children = new[]
                    {
                        tracker,
                        flashlight,
                        // sits in the same space as the flashlight, so where it is in that space is what the
                        // flashlight is expected to arrive at.
                        marker = new Box
                        {
                            Origin = Anchor.Centre,
                            Position = new Vector2(marker_x, marker_y),
                            Size = new Vector2(4),
                        },
                    },
                };
            });
        }

        [Test]
        public void TestTheSourceLeadsTheFlashlight()
        {
            AddStep("lead it by the marker", () => flashlight.ScreenSpacePositionSource = () => marker.ScreenSpaceDrawQuad.Centre);

            AddUntilStep("it arrives at the marker", () => Precision.AlmostEquals(flashlight.Position, new Vector2(marker_x, marker_y), 0.5f));
        }

        [Test]
        public void TestNothingLeadsItWithoutASource()
        {
            AddAssert("it has not moved", () => flashlight.Position, () => Is.EqualTo(Vector2.Zero));

            AddStep("lead it by nothing at all", () => flashlight.ScreenSpacePositionSource = () => null);
            AddWaitStep("let it run", 5);

            AddAssert("it still has not moved", () => flashlight.Position, () => Is.EqualTo(Vector2.Zero));
        }

        private partial class TestFlashlightMod : ModFlashlight<HitObject>
        {
            public override BindableFloat SizeMultiplier { get; } = new BindableFloat(1);

            public override BindableBool ComboBasedSize { get; } = new BindableBool();

            public override float DefaultFlashlightSize => 100;

            public Flashlight MakeFlashlight() => CreateFlashlight();

            protected override Flashlight CreateFlashlight() => new TestFlashlight(this);
        }

        private partial class TestFlashlight : ModFlashlight<HitObject>.Flashlight
        {
            public TestFlashlight(ModFlashlight modFlashlight)
                : base(modFlashlight)
            {
            }

            public new Vector2 Position => FlashlightPosition;

            protected override void UpdateFlashlightSize(float size) => FlashlightSize = new Vector2(0, size);

            protected override string FragmentShader => "CircularFlashlight";
        }
    }
}
