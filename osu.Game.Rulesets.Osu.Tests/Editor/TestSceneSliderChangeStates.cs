// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    public partial class TestSceneSliderChangeStates : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset, false);

        [TestCase(SplineType.Catmull)]
        [TestCase(SplineType.BSpline)]
        [TestCase(SplineType.Linear)]
        [TestCase(SplineType.PerfectCurve)]
        public void TestSliderRetainsCurveTypes(SplineType splineType)
        {
            Slider? slider = null;
            PathType pathType = new PathType(splineType);

            AddStep("add slider", () => EditorBeatmap.Add(slider = new Slider
            {
                StartTime = 500,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, pathType),
                    new PathControlPoint(new Vector2(200, 0), pathType),
                })
            }));
            AddAssert("slider has correct spline type", () => ((Slider)EditorBeatmap.HitObjects[0]).Path.ControlPoints.All(p => p.Type == pathType));
            AddStep("remove object", () => EditorBeatmap.Remove(slider));
            AddAssert("slider removed", () => EditorBeatmap.HitObjects.Count == 0);
            addUndoSteps();
            AddAssert("slider not removed", () => EditorBeatmap.HitObjects.Count == 1);
            AddAssert("slider has correct spline type", () => ((Slider)EditorBeatmap.HitObjects[0]).Path.ControlPoints.All(p => p.Type == pathType));
        }

        /// <summary>
        /// Undoing in the middle of dragging an anchor throws the drag away, and leaves the change before it alone.
        /// </summary>
        [Test]
        public void TestUndoInTheMiddleOfDraggingAnAnchorPutsTheShapeBack()
        {
            AddStep("add slider", () => EditorBeatmap.Add(new Slider
            {
                StartTime = 500,
                Position = new Vector2(150, 200),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(200, 0)),
                })
            }));
            AddStep("add a circle after it", () => EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(300, 300) }));
            AddStep("seek to the slider", () => EditorClock.Seek(500));
            AddStep("select the slider", () => EditorBeatmap.SelectedHitObjects.Add(slider));

            AddStep("start dragging its end", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<PathControlPointPiece<Slider>>().Single(p => p.ControlPoint == slider.Path.ControlPoints[1]));
                InputManager.PressButton(MouseButton.Left);
                InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position + new Vector2(0, 80));
            });
            AddAssert("the end is moving", () => slider.Path.ControlPoints[1].Position, () => Is.Not.EqualTo(new Vector2(200, 0)));

            AddStep("press ctrl+z", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddAssert("the end is back", () => slider.Path.ControlPoints[1].Position, () => Is.EqualTo(new Vector2(200, 0)));
            AddAssert("the circle is still there", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(2));

            AddStep("keep moving the mouse", () => InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position + new Vector2(60, 0)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("the end stays back", () => slider.Path.ControlPoints[1].Position, () => Is.EqualTo(new Vector2(200, 0)));
        }

        private Slider slider => EditorBeatmap.HitObjects.OfType<Slider>().Single();

        private void addUndoSteps() => AddStep("undo", () => Editor.Undo());
    }
}
