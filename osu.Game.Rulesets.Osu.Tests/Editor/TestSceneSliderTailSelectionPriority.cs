// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// That an object which is already selected can still be picked up where a slider's tail sits over it.
    /// </summary>
    /// <remarks>
    /// The marker that drags a slider's end is drawn over whatever is behind that end and takes the mouse for
    /// itself, so a circle under there could be seen and selected but never grabbed -- every attempt caught the
    /// slider. What is selected is what the user has already said they are working on.
    ///
    /// The marker only takes the mouse while its own slider is selected -- measured, not assumed; with the slider
    /// unselected its blueprint is not on screen and nothing on it is in the input queue at all. So the case where
    /// this bites is a selection holding both: the slider and something under its tail.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneSliderTailSelectionPriority : TestSceneOsuEditor
    {
        private static readonly Vector2 tail_position = new Vector2(300, 200);

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset, false);

        private Slider slider = null!;
        private HitCircle circle = null!;

        private SliderEndDragMarker marker => Editor.ChildrenOfType<SliderEndDragMarker>().Single();

        /// <summary>
        /// A point actually on the marker's arc. The middle of its bounding box is inside the circle the arc traces,
        /// which is nowhere near the stroke -- probing there answers a question nobody asked.
        /// </summary>
        private Vector2 markerPoint => marker.ToScreenSpace(marker.PositionInBoundingBox(marker.Vertices[marker.Vertices.Count / 2]));

        [SetUpSteps]
        public void SetUpTailSteps()
        {
            // the circle goes in first so that the slider's blueprint sits over it, which is the way round the
            // problem happens. Added the other way, the circle is already on top and takes the mouse anyway.
            AddStep("lay a circle, then a slider over it", () =>
            {
                EditorBeatmap.Clear();

                EditorBeatmap.Add(circle = new HitCircle { StartTime = 900, Position = Vector2.Zero });

                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = 500,
                    Position = tail_position - new Vector2(150, 0),
                    Path = new SliderPath
                    {
                        ControlPoints =
                        {
                            new PathControlPoint(),
                            new PathControlPoint(new Vector2(150, 0)),
                        },
                    },
                });
            });

            // partway through the slider, so its body is actually on screen: the blueprint turns positional input
            // away entirely while the slider is not drawn, and then nothing on it can be grabbed either way.
            AddStep("seek to where both are", () => EditorClock.Seek(600));
            AddStep("select the slider", () => EditorBeatmap.SelectedHitObjects.Add(slider));
            AddUntilStep("the marker is up", () => Editor.ChildrenOfType<SliderEndDragMarker>().Any());

            // the arc traces the rim of the tail circle rather than sitting on its middle, so where it actually
            // lands is measured rather than worked out from the slider's own position.
            AddStep("move the circle under the marker", () =>
            {
                circle.Position = Editor.ChildrenOfType<OsuPlayfield>().First().ToLocalSpace(markerPoint);
                EditorBeatmap.Update(circle);
            });

            AddStep("put the cursor on the tail", () => InputManager.MoveMouseTo(markerPoint));
        }

        /// <remarks>
        /// The slider is what the marker is for, so a selection holding only that does not push it aside.
        /// </remarks>
        [Test]
        public void TestTheMarkerKeepsTheTailWhenOnlyItsSliderIsSelected()
        {
            AddAssert("the marker has the cursor", () => InputManager.PositionalInputQueue.Contains(marker));
        }

        [Test]
        public void TestTheMarkerStandsAsideForSomethingElseSelectedUnderIt()
        {
            alsoSelectTheCircle();

            AddAssert("the marker lets the cursor through", () => InputManager.PositionalInputQueue.Contains(marker), () => Is.False);
        }

        [Test]
        public void TestDraggingThereMovesTheSelectionAndLeavesTheSliderItsLength()
        {
            double lengthBefore = 0;
            Vector2 positionBefore = Vector2.Zero;

            alsoSelectTheCircle();

            AddStep("remember where things are", () =>
            {
                lengthBefore = slider.Path.Distance;
                positionBefore = circle.Position;
            });

            AddStep("take hold at the tail", () =>
            {
                InputManager.MoveMouseTo(markerPoint);
                InputManager.PressButton(MouseButton.Left);
            });

            // dragged back along the slider rather than away from it, and in steps rather than in one jump. Pulling
            // straight off the end moves the length by nothing, and a single jump lands before anything can take
            // hold -- either way both outcomes look the same and the test proves nothing.
            AddStep("drag back along the slider", () =>
            {
                var from = markerPoint;

                for (int step = 1; step <= 10; step++)
                    InputManager.MoveMouseTo(from - new Vector2(8 * step, 0));
            });

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the selection came with it", () => circle.Position, () => Is.Not.EqualTo(positionBefore));
            AddAssert("the slider is the length it was", () => slider.Path.Distance, () => Is.EqualTo(lengthBefore).Within(0.01));
        }

        private void alsoSelectTheCircle() => AddStep("select the circle as well", () => EditorBeatmap.SelectedHitObjects.Add(circle));
    }
}
