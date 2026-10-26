// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    public partial class TestSceneOsuObjectPlacement : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Test]
        public void TestCannotSelectObjectsMidSpinnerPlacement()
        {
            AddStep("seek to first object", () => EditorClock.Seek(EditorBeatmap.HitObjects.First().StartTime));
            AddStep("select spinner placement tool", () => InputManager.Key(Key.Number4));
            AddStep("move mouse to composer", () => InputManager.MoveMouseTo(this.ChildrenOfType<OsuHitObjectComposer>().Single()));
            AddStep("click left mouse", () => InputManager.Click(MouseButton.Left));
            AddStep("move mouse to slider", () => InputManager.MoveMouseTo(this.ChildrenOfType<SliderSelectionBlueprint>()
                                                                               .Single(b => b.HitObject == EditorBeatmap.HitObjects[0])
                                                                               .ScreenSpaceSelectionPoint));
            AddStep("click left mouse", () => InputManager.Click(MouseButton.Left));
            AddAssert("placement still ongoing", () => EditorBeatmap.PlacementObject, () => Is.Not.Null);
        }

        /// <summary>
        /// As in stable, undoing while a slider is still being drawn throws the unfinished slider away and leaves the
        /// change before it alone.
        /// </summary>
        [Test]
        public void TestUndoMidSliderPlacementDiscardsTheSlider()
        {
            int countAfterCircle = 0;

            AddStep("seek past the last object", () => EditorClock.Seek(EditorBeatmap.HitObjects.Last().GetEndTime() + 2000));
            AddStep("choose the circle tool", () => InputManager.Key(Key.Number2));
            AddStep("place a circle", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(100, 100)));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("the circle is placed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(h => h.Position == new Vector2(100, 100)));
            AddStep("remember the count", () => countAfterCircle = EditorBeatmap.HitObjects.Count);

            AddStep("seek a bit further", () => EditorClock.Seek(EditorClock.CurrentTime + 1000));
            AddStep("choose the slider tool", () => InputManager.Key(Key.Number3));
            AddStep("start a slider", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(300, 200)));
                InputManager.Click(MouseButton.Left);
            });
            AddStep("add an anchor", () =>
            {
                InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(400, 250)));
                InputManager.Click(MouseButton.Left);
            });
            AddStep("move on", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(450, 300))));
            AddAssert("the slider is being drawn", () => placement, () => Is.EqualTo(PlacementBlueprint.PlacementState.Active));

            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddAssert("the slider is gone", () => placement, () => Is.EqualTo(PlacementBlueprint.PlacementState.Waiting));
            AddAssert("the circle is still there", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(countAfterCircle));
            AddAssert("no slider was placed", () => EditorBeatmap.HitObjects.OfType<Slider>().Any(s => s.Position == new Vector2(300, 200)), () => Is.False);
        }

        private OsuPlayfield playfield => Editor.ChildrenOfType<OsuPlayfield>().First();

        private PlacementBlueprint.PlacementState? placement => Editor.ChildrenOfType<ComposeBlueprintContainer>().Single().CurrentPlacement?.PlacementActive;
    }
}
