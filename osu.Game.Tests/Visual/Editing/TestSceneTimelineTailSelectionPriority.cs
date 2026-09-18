// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Input;
using static osu.Game.Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// That an object already selected can be dragged along the timeline where a slider's tail sits over it.
    /// </summary>
    /// <remarks>
    /// The handle that lengthens a slider covers its end, and a circle landing on that end is behind it. Clicking
    /// there took hold of the slider and dragging added a repeat, when what was wanted was to slide the circle the
    /// user had already picked out a little earlier or later -- only the sliver of it poking out could be grabbed.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneTimelineTailSelectionPriority : TimelineTestScene
    {
        public override Drawable CreateTestComponent() => new TimelineBlueprintContainer(Composer);

        private Slider slider = null!;
        private HitCircle circle = null!;

        // the shadow drawn under the handle answers nothing, so the one that does is the handle itself.
        private DragArea handle => this.ChildrenOfType<DragArea>().Single(d => d.HandlePositionalInput);

        [SetUpSteps]
        public void SetUpTailSteps()
        {
            AddStep("lay a circle on a slider's tail", () =>
            {
                EditorBeatmap.Clear();

                EditorBeatmap.Add(slider = new Slider
                {
                    Position = new Vector2(256, 256),
                    StartTime = 2700,
                    Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]),
                });

                EditorBeatmap.Add(circle = new HitCircle { Position = new Vector2(100, 100), StartTime = slider.GetEndTime() });
            });

            AddUntilStep("the handle is up", () => this.ChildrenOfType<DragArea>().Any(d => d.HandlePositionalInput));
            AddStep("put the cursor on the handle", () => InputManager.MoveMouseTo(handle));
        }

        [Test]
        public void TestTheHandleKeepsTheEndWhenNothingElseIsSelected()
        {
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());

            AddAssert("the handle has the cursor", () => InputManager.PositionalInputQueue.Contains(handle));
        }

        /// <remarks>
        /// The slider is what the handle is for, so a selection holding only that does not push it aside.
        /// </remarks>
        [Test]
        public void TestTheHandleKeepsTheEndWhenOnlyItsSliderIsSelected()
        {
            AddStep("select the slider", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddAssert("the handle has the cursor", () => InputManager.PositionalInputQueue.Contains(handle));
        }

        /// <remarks>
        /// Several objects selected are only ever being moved, so a slider's end among them is somewhere to take hold of
        /// the selection rather than to add a repeat.
        /// </remarks>
        [Test]
        public void TestTheHandleStandsAsideWhenItsSliderIsSelectedWithTheCircle()
        {
            AddStep("select the slider and the circle", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new HitObject[] { slider, circle });
            });

            AddAssert("the handle lets the cursor through", () => InputManager.PositionalInputQueue.Contains(handle), () => Is.False);
        }

        /// <remarks>
        /// Nothing selected is under the handle here, so it is being one of several selected that moves it aside.
        /// </remarks>
        [Test]
        public void TestTheHandleStandsAsideWhenItsSliderIsOneOfSeveralSelected()
        {
            moveTheCircleAwayAndSelectBoth();

            AddAssert("the handle lets the cursor through", () => InputManager.PositionalInputQueue.Contains(handle), () => Is.False);
        }

        [Test]
        public void TestDraggingTheTailOfOneOfSeveralSelectedMovesThemRatherThanAddingARepeat()
        {
            double sliderStartedAt = 0;
            double circleStartedAt = 0;
            int repeatsBefore = 0;

            moveTheCircleAwayAndSelectBoth();

            AddStep("remember where things are", () =>
            {
                sliderStartedAt = slider.StartTime;
                circleStartedAt = circle.StartTime;
                repeatsBefore = slider.RepeatCount;
            });

            AddStep("take hold on the tail", () =>
            {
                InputManager.MoveMouseTo(handle);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag along the timeline", () =>
            {
                var from = handle.ScreenSpaceDrawQuad.Centre;

                for (int step = 1; step <= 10; step++)
                    InputManager.MoveMouseTo(from + new Vector2(6 * step, 0));
            });

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the slider moved", () => slider.StartTime, () => Is.Not.EqualTo(sliderStartedAt));
            AddAssert("the circle moved with it", () => circle.StartTime - circleStartedAt, () => Is.EqualTo(slider.StartTime - sliderStartedAt).Within(0.001));
            AddAssert("the slider gained no repeat", () => slider.RepeatCount, () => Is.EqualTo(repeatsBefore));
        }

        [Test]
        public void TestTheHandleStandsAsideForTheSelectedCircleUnderIt()
        {
            selectTheCircle();

            AddAssert("the handle lets the cursor through", () => InputManager.PositionalInputQueue.Contains(handle), () => Is.False);
        }

        [Test]
        public void TestDraggingThereMovesTheCircleRatherThanAddingARepeat()
        {
            double startedAt = 0;
            int repeatsBefore = 0;

            selectTheCircle();

            AddStep("remember where things are", () =>
            {
                startedAt = circle.StartTime;
                repeatsBefore = slider.RepeatCount;
            });

            AddStep("take hold on the tail", () =>
            {
                InputManager.MoveMouseTo(handle);
                InputManager.PressButton(MouseButton.Left);
            });

            // along the timeline, and in steps: a single jump lands before anything can take hold, and a drag across
            // the timeline rather than along it moves neither of the two things this is telling apart.
            AddStep("drag along the timeline", () =>
            {
                var from = handle.ScreenSpaceDrawQuad.Centre;

                for (int step = 1; step <= 10; step++)
                    InputManager.MoveMouseTo(from + new Vector2(6 * step, 0));
            });

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the circle moved", () => circle.StartTime, () => Is.Not.EqualTo(startedAt));
            AddAssert("the slider gained no repeat", () => slider.RepeatCount, () => Is.EqualTo(repeatsBefore));
        }

        private void selectTheCircle() => AddStep("select the circle", () =>
        {
            EditorBeatmap.SelectedHitObjects.Clear();
            EditorBeatmap.SelectedHitObjects.Add(circle);
        });

        private void moveTheCircleAwayAndSelectBoth()
        {
            AddStep("move the circle off the tail", () =>
            {
                circle.StartTime = slider.StartTime - 500;
                EditorBeatmap.Update(circle);
            });

            AddStep("select the slider and the circle", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new HitObject[] { slider, circle });
            });

            AddStep("put the cursor back on the handle", () => InputManager.MoveMouseTo(handle));
        }
    }
}
