// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osuTK;
using osuTK.Input;
using static osu.Game.Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneTimelineHitObjectBlueprint : TimelineTestScene
    {
        public override Drawable CreateTestComponent() => new TimelineBlueprintContainer(Composer);

        [Test]
        public void TestContextMenu()
        {
            TimelineHitObjectBlueprint blueprint;

            AddStep("add object", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new HitCircle { StartTime = 3000 });
            });

            AddStep("click object", () =>
            {
                blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().Single();
                InputManager.MoveMouseTo(blueprint);
                InputManager.Click(MouseButton.Left);
            });

            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddAssert("context menu open", () => this.ChildrenOfType<OsuContextMenu>().Any(m => m.State == MenuState.Open));
        }

        [Test]
        public void TestDisallowZeroDurationObjects()
        {
            DragArea dragArea;

            AddStep("add spinner", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Spinner
                {
                    Position = new Vector2(256, 256),
                    StartTime = 2700,
                    Duration = 500
                });
            });

            AddStep("hold down drag bar", () =>
            {
                // distinguishes between the actual drag bar and its "underlay shadow".
                dragArea = this.ChildrenOfType<DragArea>().Single(bar => bar.HandlePositionalInput);
                InputManager.MoveMouseTo(dragArea);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("try to drag bar past start", () =>
            {
                var blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().Single();
                InputManager.MoveMouseTo(blueprint.SelectionQuad.TopLeft - new Vector2(100, 0));
                InputManager.ReleaseButton(MouseButton.Left);
            });

            AddAssert("object has non-zero duration", () => EditorBeatmap.HitObjects.OfType<IHasDuration>().Single().Duration > 0);
        }

        [Test]
        public void TestDisallowRepeatsOnZeroDurationObjects()
        {
            DragArea dragArea;

            AddStep("add zero length slider", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(new Slider
                {
                    Position = new Vector2(256, 256),
                    StartTime = 2700
                });
            });

            AddStep("hold down drag bar", () =>
            {
                // distinguishes between the actual drag bar and its "underlay shadow".
                dragArea = this.ChildrenOfType<DragArea>().Single(bar => bar.HandlePositionalInput);
                InputManager.MoveMouseTo(dragArea);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("try to extend drag bar", () =>
            {
                var blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().Single();
                InputManager.MoveMouseTo(blueprint.SelectionQuad.TopLeft + new Vector2(100, 0));
            });

            AddStep("release button", () => InputManager.PressButton(MouseButton.Left));

            AddAssert("object has zero repeats", () => EditorBeatmap.HitObjects.OfType<IHasRepeats>().Single().RepeatCount == 0);
        }

        /// <summary>
        /// Holding Shift while dragging a slider's end changes its velocity to fit. Dragged back to its head or before,
        /// there is no length left to fit into, and the velocity was being divided by nothing or less and left pinned to
        /// its highest or lowest (2026-11-09).
        /// </summary>
        [Test]
        public void TestShiftDraggingASliderEndBehindItsHeadLeavesItsVelocity()
        {
            Slider slider = null!;
            DragArea dragArea;

            AddStep("add slider", () =>
            {
                EditorBeatmap.Clear();
                EditorBeatmap.Add(slider = new Slider
                {
                    Position = new Vector2(256, 256),
                    StartTime = 2700,
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) }),
                });
            });

            AddStep("hold down drag bar with shift", () =>
            {
                dragArea = this.ChildrenOfType<DragArea>().Single(bar => bar.HandlePositionalInput);
                InputManager.MoveMouseTo(dragArea);
                InputManager.PressKey(Key.LShift);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag end behind the head", () =>
            {
                var blueprint = this.ChildrenOfType<TimelineHitObjectBlueprint>().Single();
                InputManager.MoveMouseTo(blueprint.SelectionQuad.TopLeft - new Vector2(100, 0));
            });

            AddStep("release", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                InputManager.ReleaseKey(Key.LShift);
            });

            AddAssert("velocity is as it was", () => slider.SliderVelocityMultiplier, () => Is.EqualTo(1));
        }
    }
}
