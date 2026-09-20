// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The shape a slider is placed with, which the setting takes from the one chosen for new anchors.
    /// </summary>
    /// <remarks>
    /// A slider being placed is shaped by how many points the segment holds: two make a straight line, three an arc,
    /// more a bezier. Alt and a number override that for the segment being drawn, but it has to be pressed for every
    /// slider, which is no way to work in one style for a whole map. While the setting is on, the shape chosen for
    /// new anchors is what a slider is placed with as well, and ctrl+1 to ctrl+4 reach a slider already being drawn.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneAnchorTypeForPlacement : PlacementBlueprintTestScene
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected sealed override Ruleset CreateRuleset() => new OsuRuleset();

        protected override DrawableHitObject CreateHitObject(HitObject hitObject) => new DrawableSlider((Slider)hitObject);
        protected override HitObjectPlacementBlueprint CreateBlueprint() => new SliderPlacementBlueprint();

        [SetUp]
        public void Setup() => Schedule(() =>
        {
            HitObjectContainer.Clear();
            ResetPlacement();
        });

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("back to the stock answers", () =>
            {
                config.SetValue(OsuSetting.EditorSliderAnchorTypeForPlacement, SliderPlacementAnchorType.Automatic);
                config.SetValue(OsuSetting.EditorSliderAnchorType, SliderAnchorType.PerfectCurve);
            });
        }

        /// <summary>
        /// Two points make a straight line on their own, so a bezier here can only have come from the setting.
        /// </summary>
        [Test]
        public void TestASliderIsPlacedWithTheChosenShape()
        {
            setShape(SliderPlacementAnchorType.Bezier);

            placeTwoPointSlider();

            assertFirstPointIs(PathType.BEZIER);
        }

        /// <summary>
        /// Automatic is what the editor did before the setting existed: the shape follows the number of points.
        /// </summary>
        [Test]
        public void TestAutomaticLeavesItToTheNumberOfPoints()
        {
            setShape(SliderPlacementAnchorType.Automatic);

            placeTwoPointSlider();

            assertFirstPointIs(PathType.LINEAR);
        }

        /// <summary>
        /// The shape is read every frame, so changing it -- from the menu, mid-slider -- reaches what is being drawn.
        /// </summary>
        [Test]
        public void TestTheShapeReachesASliderBeingPlaced()
        {
            setShape(SliderPlacementAnchorType.Bezier);

            AddStep("move to the start", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(200))));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddStep("move away", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(400, 200))));

            AddStep("ask for catmull", () => config.SetValue(OsuSetting.EditorSliderAnchorTypeForPlacement, SliderPlacementAnchorType.Catmull));
            AddUntilStep("the slider being drawn is catmull", () => firstPointOfTheBlueprint(), () => Is.EqualTo(PathType.CATMULL));

            AddStep("finish it", () => InputManager.Click(MouseButton.Right));

            assertFirstPointIs(PathType.CATMULL);
        }

        /// <summary>
        /// Alt and a number have the last word, for the segment being drawn.
        /// </summary>
        [Test]
        public void TestAltStillOverridesTheShape()
        {
            setShape(SliderPlacementAnchorType.Bezier);

            AddStep("move to the start", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(200))));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddStep("move away", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(400, 200))));

            AddStep("alt+1 for a straight line", () =>
            {
                InputManager.PressKey(Key.AltLeft);
                InputManager.Key(Key.Number1);
                InputManager.ReleaseKey(Key.AltLeft);
            });

            AddStep("finish it", () => InputManager.Click(MouseButton.Right));

            assertFirstPointIs(PathType.LINEAR);
        }

        /// <summary>
        /// A part divided off with a red anchor is the user's own, so it takes the shape an added anchor makes rather
        /// than the one the slider was placed as -- a perfect curve cannot hold a division in stable either.
        /// </summary>
        [Test]
        public void TestAPartDividedOffTakesTheShapeAnAddedAnchorMakes()
        {
            setShape(SliderPlacementAnchorType.PerfectCurve);
            AddStep("added anchors make beziers", () => config.SetValue(OsuSetting.EditorSliderAnchorType, SliderAnchorType.Bezier));

            AddStep("move to the start", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(150, 200))));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddStep("move along", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(250, 150))));
            AddStep("divide it with a red anchor", () => InputManager.Key(Key.S));
            AddStep("move on", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(350, 250))));
            AddStep("finish it", () => InputManager.Click(MouseButton.Right));

            AddAssert("the slider starts as a perfect curve", () => placedSlider()?.Path.ControlPoints[0].Type, () => Is.EqualTo(PathType.PERFECT_CURVE));
            AddAssert("the part divided off is a bezier", () => dividingPointOf(placedSlider()!)?.Type, () => Is.EqualTo(PathType.BEZIER));
        }

        /// <summary>
        /// With the shape left automatic nothing is forced anywhere, which is the editor as it was.
        /// </summary>
        [Test]
        public void TestAutomaticLeavesADividedPathAlone()
        {
            setShape(SliderPlacementAnchorType.Automatic);
            AddStep("added anchors make beziers", () => config.SetValue(OsuSetting.EditorSliderAnchorType, SliderAnchorType.Bezier));

            AddStep("move to the start", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(150, 200))));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddStep("move along", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(250, 150))));
            AddStep("divide it with a red anchor", () => InputManager.Key(Key.S));
            AddStep("move on", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(350, 250))));
            AddStep("finish it", () => InputManager.Click(MouseButton.Right));

            AddAssert("the part divided off follows the number of points", () => dividingPointOf(placedSlider()!)?.Type, () => Is.EqualTo(PathType.LINEAR));
        }

        private static PathControlPoint? dividingPointOf(Slider slider) =>
            slider.Path.ControlPoints.Skip(1).FirstOrDefault(p => p.Type != null);

        private void setShape(SliderPlacementAnchorType shape)
        {
            AddStep($"place with {shape}", () => config.SetValue(OsuSetting.EditorSliderAnchorTypeForPlacement, shape));
        }

        private void placeTwoPointSlider()
        {
            AddStep("move to the start", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(200))));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddStep("move away", () => InputManager.MoveMouseTo(InputManager.ToScreenSpace(new Vector2(400, 200))));
            AddStep("finish it", () => InputManager.Click(MouseButton.Right));
        }

        private PathType? firstPointOfTheBlueprint() =>
            this.ChildrenOfType<SliderPlacementBlueprint>().Single().HitObject.Path.ControlPoints[0].Type;

        private void assertFirstPointIs(PathType type) =>
            AddAssert($"the slider is {type}", () => placedSlider()?.Path.ControlPoints[0].Type, () => Is.EqualTo(type));

        private Slider? placedSlider() => HitObjectContainer.Count > 0 ? ((DrawableSlider)HitObjectContainer[0]).HitObject : null;
    }
}
