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

            // whole coordinates are on, as they are for anyone who has not turned them off. What a snap lands on is
            // rounded once a drag or a placement is over (2026-10-05), which moves it under a pixel; the tests below
            // that compare positions exactly turn them off.
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

        /// <summary>
        /// Two circles already stacked on each other, taken together onto a slider's end, land on the end the slider
        /// stores, both of them, wherever the hand comes to rest over it (2026-10-05). The earlier of the two is drawn a
        /// stack's worth up and to the left, and whichever of the two reached the end first used to decide where they
        /// landed: one way they were on the end, the other a stack's worth off it, looking just as right.
        /// </summary>
        [TestCase(false)]
        [TestCase(true)]
        public void TestTwoStackedCirclesDraggedOntoASliderEndLandOnIt(bool endOnAFraction)
        {
            HitCircle first = null!;
            HitCircle second = null!;
            Slider slider = null!;
            Vector2[] drawnWhileHeld = null!;

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider, and two stacked circles away from it", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = endOnAFraction ? new Vector2(156.59f, 191.76f) : new Vector2(156, 192),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(100, 0)),
                    })
                });

                EditorBeatmap.Add(first = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(256, 300) });
                EditorBeatmap.Add(second = new HitCircle { StartTime = EditorClock.CurrentTime + 600, Position = new Vector2(256, 300) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new[] { first, second });
            });

            AddUntilStep("the circles are stacked", () => first.StackHeight, () => Is.Not.EqualTo(0));

            AddStep("take hold of the later circle", () =>
            {
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 300)));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(256, 280))));

            // the later circle aimed at the end, then the hand still moving a pixel at a time over it.
            for (int i = 0; i <= 4; i++)
            {
                float x = 256 + i;

                AddStep($"over the end ({i})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(x, 192))));
                AddAssert($"the first is on the end ({i})", () => Vector2.Distance(slider.EndPosition, first.Position), () => Is.LessThan(3));
                AddAssert($"the second is on the end ({i})", () => Vector2.Distance(slider.EndPosition, second.Position), () => Is.LessThan(3));
            }

            // where stable puts them when they are stacked on the end there: the fraction cut off, as measured on
            // 2026-10-05 -- (416.59, 195.76) became (416, 195) -- so that stacking them again in stable moves nothing.
            // Already while they are held, so that letting go does not move them (2026-10-05).
            Vector2 whereStableHasThem = endOnAFraction ? new Vector2(256, 191) : new Vector2(256, 192);

            AddUntilStep("both are stacked on the end while held", () => new[] { first.StackHeight, second.StackHeight }, () => Has.All.LessThan(0));
            AddAssert("the first is where stable has it while held", () => first.Position, () => Is.EqualTo(whereStableHasThem));
            AddAssert("the second is where stable has it while held", () => second.Position, () => Is.EqualTo(whereStableHasThem));
            AddStep("note where they are drawn", () => drawnWhileHeld = new[] { first.StackedPosition, second.StackedPosition });

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddUntilStep("both are stacked on the end", () => new[] { first.StackHeight, second.StackHeight }, () => Has.All.LessThan(0));
            AddAssert("the first is drawn where it was held", () => first.StackedPosition, () => Is.EqualTo(drawnWhileHeld[0]));
            AddAssert("the second is drawn where it was held", () => second.StackedPosition, () => Is.EqualTo(drawnWhileHeld[1]));
            AddAssert("the first is where stable has it", () => first.Position, () => Is.EqualTo(whereStableHasThem));
            AddAssert("the second is where stable has it", () => second.Position, () => Is.EqualTo(whereStableHasThem));
        }

        /// <summary>
        /// A slider taken by its body until its end is on a circle, then the circle taken back onto the end, leaves
        /// the circle where it was (2026-10-05). The slider's head used to be cut to a whole number on its own, which
        /// put the end under a pixel short of the circle; the circle, snapped back onto it, was then cut a pixel over,
        /// and going back and forth walked them a pixel at a time.
        /// </summary>
        [Test]
        public void TestASliderEndBroughtOntoACircleKeepsItWhereItIs()
        {
            HitCircle circle = null!;
            Slider slider = null!;

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider whose end is a fraction off its head, and a circle after it", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(100, 100),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(50.59f, 30.76f)),
                    })
                });

                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(300, 250) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            // by the middle of its body: the head of a selected slider is an anchor, which would change its shape.
            Vector2 middle = default;

            AddStep("take hold of the slider's body", () =>
            {
                middle = slider.Path.PositionAt(0.5);
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + middle));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + middle + new Vector2(20, 20))));
            // near enough for the snap to take it the rest of the way, as a hand does; exactly on it is not a snap at all.
            AddStep("bring its end near the circle", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(301.3f, 249.2f) - slider.Path.PositionAt(1) + middle)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the end is on the circle", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(3));
            AddAssert("the head is on a whole coordinate", () => slider.Position.X % 1 == 0 && slider.Position.Y % 1 == 0);

            AddStep("select the circle", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(circle);
            });

            AddStep("take hold of the circle", () =>
            {
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(circle.StackedPosition));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("take it off the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(circle.StackedPosition + new Vector2(30, 30))));
            AddStep("bring it back onto the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.EndPosition)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the circle is where it was", () => circle.Position, () => Is.EqualTo(new Vector2(300, 250)));
        }

        /// <summary>
        /// A slider held with its end a little up and to the left of a circle stays put, however the hand trembles
        /// (2026-10-05). The snap takes the end onto the circle, which then stacks on it and is drawn a stack's worth
        /// down and to the right; measured only from where it is drawn, the circle was out of reach, so the snap let
        /// go, the stack came off, the circle was drawn back, and the snap took hold again -- on every movement, as
        /// fast as the input came in.
        /// </summary>
        [Test]
        public void TestASliderEndHeldNearACircleDoesNotShake()
        {
            HitCircle circle = null!;
            Slider slider = null!;
            Vector2 middle = default;
            var heldAt = new System.Collections.Generic.List<Vector2>();

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider and a circle after it", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(100, 100),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(60, 40)),
                    })
                });

                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(300, 250) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            AddStep("take hold of the slider's body", () =>
            {
                middle = slider.Path.PositionAt(0.5);
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + middle));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start the drag", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + middle + new Vector2(20, 20))));

            // the end aimed two and a half pixels up and to the left of the circle: in reach of where the circle is
            // stored, out of reach of where it is drawn once it stacks.
            for (int i = 0; i < 8; i++)
            {
                float tremble = i % 2 == 0 ? 0.1f : -0.1f;

                AddStep($"hold the end near the circle ({i})", () =>
                    InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(new Vector2(297.5f + tremble, 247.5f) - slider.Path.PositionAt(1) + middle)));
                AddStep($"note where the slider is ({i})", () => heldAt.Add(slider.Position));
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the slider stayed in one place", () => heldAt.Distinct().Count(), () => Is.EqualTo(1));
            AddAssert("its end is on the circle", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(3));
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
