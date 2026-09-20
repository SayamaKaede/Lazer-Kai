// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    [TestFixture]
    public partial class TestSceneObjectObjectSnap : TestSceneOsuEditor
    {
        private OsuPlayfield playfield;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        [Resolved]
        private OsuConfigManager config { get; set; }

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());
            AddStep("seek to first control point", () => EditorClock.Seek(Beatmap.Value.Beatmap.ControlPointInfo.TimingPoints.First().Time));

            // whole coordinates are on, as they are for anyone who has not turned them off: what a snap lands on is
            // left exactly where the snap put it, so everything below holds either way.
            AddStep("keep whole coordinates", () => config.SetValue(OsuSetting.EditorIntegerPositions, true));
        }

        /// <summary>
        /// Dragging a circle onto a slider's end has to leave the two stacked. Stacking is worked out from the
        /// positions the beatmap holds, so the circle has to land on the end the slider stores rather than the end it
        /// is drawn at: the two are a stack's worth apart as soon as anything there is stacked.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestACircleDraggedOntoASliderEndStacks(bool sliderIsStacked)
        {
            HitCircle circle = null!;
            Slider slider = null!;

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider, and a circle away from it", () =>
            {
                if (sliderIsStacked)
                {
                    // something for the slider to stack onto, so the end it is drawn at is no longer the end it
                    // stores -- which is the state the report came from.
                    EditorBeatmap.Add(new HitCircle { StartTime = EditorClock.CurrentTime - 500, Position = new Vector2(156, 192) });
                }

                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(156, 192),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(100, 0)),
                    })
                });

                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(256, 300) });
            });

            AddStep("take hold of the circle", () =>
            {
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 300)));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 280))));
            AddStep("drag it onto the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 192))));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the circle is where the slider stores its end", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(3));
            AddUntilStep("the two are stacked", () => circle.StackHeight, () => Is.Not.EqualTo(0));
        }

        /// <summary>
        /// A stacked slider's head, dragged by its anchor onto another object, has to land where that object is
        /// stored rather than where it is drawn.
        /// </summary>
        /// <remarks>
        /// The anchors sit on the drawn slider, which carries its stack offset, while the position the beatmap holds
        /// does not. Dragging the head of a slider that is itself stacked therefore lands it a stack's worth away
        /// from what it was aimed at -- close enough to look right, too far for the two to stack, which is under
        /// three pixels.
        /// </remarks>
        [Test]
        public void TestAStackedSlidersHeadCanBeDraggedOntoAnotherObject()
        {
            HitCircle target = null!;
            Slider slider = null!;

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a stacked slider, and something to aim at", () =>
            {
                EditorBeatmap.Add(target = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(150, 100) });

                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime + 250,
                    Position = new Vector2(300, 250),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(100, 0)),
                    })
                });

                // the circle on top of its head is what gives the slider a stack of its own: the earlier object is
                // the one that moves.
                EditorBeatmap.Add(new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(300, 250) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddUntilStep("the slider is stacked", () => slider.StackHeight, () => Is.Not.EqualTo(0));

            AddStep("take hold of its head anchor", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<PathControlPointPiece<Slider>>().First());
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(250, 200))));
            AddStep("drag it onto the circle", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(150, 100))));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the head landed on what it was aimed at", () => Vector2.Distance(slider.Position, target.Position), () => Is.LessThan(3));
        }

        /// <summary>
        /// A circle held over a slider's end stays there while the hand still moves. Forming the stack moves what is
        /// drawn by a stack's worth, and a drag reads the drawn position, so the circle measures as having jumped:
        /// the snap is lost, the stack comes off, the drawing goes back, and it takes hold again -- over and over,
        /// with the cursor barely moving at all.
        /// </summary>
        [Test]
        public void TestACircleHeldOverASliderEndStaysStacked()
        {
            HitCircle circle = null!;
            Slider slider = null!;

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider, and a circle away from it", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(156, 192),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(100, 0)),
                    })
                });

                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(256, 300) });
            });

            AddStep("take hold of the circle", () =>
            {
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 300)));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 280))));
            AddStep("drag it onto the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 192))));
            AddUntilStep("the two are stacked", () => circle.StackHeight, () => Is.Not.EqualTo(0));

            // the hand is still moving, a pixel at a time, nowhere near far enough to mean letting go.
            for (int i = 1; i <= 4; i++)
            {
                float x = 256 + i;

                AddStep($"move a pixel ({i})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(x, 192))));
                AddAssert($"still on the end ({i})", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(3));
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestHitCircleSnapsToOtherHitCircle(bool distanceSnapEnabled)
        {
            AddStep("move mouse to centre", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            if (!distanceSnapEnabled)
                AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("enter placement mode", () => InputManager.Key(Key.Number2));

            AddStep("place first object", () => InputManager.Click(MouseButton.Left));

            AddStep("move mouse slightly", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 0.01f, 0)));

            AddStep("place second object", () => InputManager.Click(MouseButton.Left));

            AddAssert("both objects at same location", () =>
            {
                var objects = EditorBeatmap.HitObjects;

                var first = (OsuHitObject)objects.First();
                var second = (OsuHitObject)objects.Last();

                return Precision.AlmostEquals(first.EndPosition, second.Position);
            });
        }

        [Test]
        public void TestHitCircleSnapsToSliderEnd()
        {
            AddStep("move mouse to centre", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("enter slider placement mode", () => InputManager.Key(Key.Number3));

            AddStep("start slider placement", () => InputManager.Click(MouseButton.Left));

            AddStep("move to place end", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 0.225f, 0)));

            AddStep("end slider placement", () => InputManager.Click(MouseButton.Right));

            AddStep("seek to slider end", () =>
            {
                var slider = (Slider)EditorBeatmap.HitObjects.Single();
                EditorClock.Seek(slider.EndTime);
            });

            AddStep("enter circle placement mode", () => InputManager.Key(Key.Number2));

            AddStep("move mouse slightly", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 0.205f, 0)));

            AddStep("place second object", () => InputManager.Click(MouseButton.Left));

            AddAssert("circle is at slider's end", () =>
            {
                var objects = EditorBeatmap.HitObjects;

                var first = (Slider)objects.First();
                var second = (OsuHitObject)objects.Last();

                return Precision.AlmostEquals(first.EndPosition, second.Position);
            });
        }

        [Test]
        public void TestSecondCircleInSelectionAlsoSnaps()
        {
            // the objects here are placed a fifth of the play area apart and then dragged back to within the snap's
            // own radius of six pixels. Whole coordinates move each of them by up to half a pixel, which is enough to
            // put the second one just outside that radius -- the geometry, not the snapping, is what cannot take it.
            AddStep("let positions carry a fraction", () => config.SetValue(OsuSetting.EditorIntegerPositions, false));

            AddStep("move mouse to centre", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre));

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("enter placement mode", () => InputManager.Key(Key.Number2));

            AddStep("place first object", () => InputManager.Click(MouseButton.Left));

            AddStep("increment time", () => EditorClock.SeekForward(true));

            AddStep("move mouse right", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 0.2f, 0)));
            AddStep("place second object", () => InputManager.Click(MouseButton.Left));

            AddStep("increment time", () => EditorClock.SeekForward(true));

            AddStep("move mouse down", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(0, playfield.ScreenSpaceDrawQuad.Width * 0.2f)));
            AddStep("place third object", () => InputManager.Click(MouseButton.Left));

            AddStep("enter selection mode", () => InputManager.Key(Key.Number1));

            AddStep("select objects 2 and 3", () =>
            {
                // add selection backwards to test non-sequential time ordering
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects[2]);
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects[1]);
            });

            AddStep("begin drag", () => InputManager.PressButton(MouseButton.Left));

            AddStep("move mouse slightly off centre", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * 0.01f, 0)));

            AddAssert("object 3 snapped to 1", () =>
            {
                var objects = EditorBeatmap.HitObjects;

                var first = (OsuHitObject)objects.First();
                var third = (OsuHitObject)objects.Last();

                return Precision.AlmostEquals(first.EndPosition, third.Position);
            });

            AddStep("move mouse slightly off centre", () => InputManager.MoveMouseTo(playfield.ScreenSpaceDrawQuad.Centre + new Vector2(playfield.ScreenSpaceDrawQuad.Width * -0.21f, playfield.ScreenSpaceDrawQuad.Width * 0.205f)));

            AddAssert("object 2 snapped to 1", () =>
            {
                var objects = EditorBeatmap.HitObjects;

                var first = (OsuHitObject)objects.First();
                var second = (OsuHitObject)objects.ElementAt(1);


                return Precision.AlmostEquals(first.EndPosition, second.Position);
            });

            AddStep("end drag", () => InputManager.ReleaseButton(MouseButton.Left));
        }
    }
}
