// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A slider's end being dragged on the timeline goes away before the drag ends: deleted, or scrolled out of the
    /// timeline. The editor goes on as it would after any other drag (2026-10-30).
    /// </summary>
    public partial class TestSceneTimelineDragCutShort : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);
            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });
            return beatmap;
        }

        private Slider slider = null!;

        [Test]
        public void TestDeletingTheSliderWhileItsEndIsDragged()
        {
            AddStep("clear", () => EditorBeatmap.Clear());
            AddStep("add a slider", () => EditorBeatmap.Add(slider = new Slider
            {
                StartTime = 500,
                Position = new Vector2(100),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            }));

            AddStep("take hold of its end", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<TimelineHitObjectBlueprint.DragArea>().Single(d => d.HandlePositionalInput));
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("drag it along", () => InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position + new Vector2(80, 0)));
            AddStep("delete the slider", () => EditorBeatmap.Remove(slider));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("add a circle", () => EditorBeatmap.Add(new HitCircle { StartTime = 2000, Position = new Vector2(200) }));
            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddAssert("the circle is gone again", () => EditorBeatmap.HitObjects.OfType<HitCircle>(), () => Is.Empty);
        }

        [Test]
        public void TestTheTimelineStaysPutAfterwards()
        {
            double time = 0;

            AddStep("clear", () => EditorBeatmap.Clear());
            AddStep("add a slider", () => EditorBeatmap.Add(slider = new Slider
            {
                StartTime = 500,
                Position = new Vector2(100),
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            }));

            AddStep("take hold of its end", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<TimelineHitObjectBlueprint.DragArea>().Single(d => d.HandlePositionalInput));
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("drag it along", () => InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position + new Vector2(80, 0)));
            AddStep("delete the slider", () => EditorBeatmap.Remove(slider));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddStep("rest the cursor at the timeline's right end", () =>
            {
                var timeline = Editor.ChildrenOfType<Timeline>().First();
                InputManager.MoveMouseTo(new Vector2(timeline.ScreenSpaceDrawQuad.TopRight.X - 2, timeline.ScreenSpaceDrawQuad.Centre.Y));
                time = EditorClock.CurrentTime;
            });
            AddWaitStep("wait", 10);
            AddAssert("the timeline has not moved", () => EditorClock.CurrentTime, () => Is.EqualTo(time));
        }
    }
}
