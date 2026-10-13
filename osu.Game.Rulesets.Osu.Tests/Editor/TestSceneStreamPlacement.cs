// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The Streams tool: a shape drawn the way a slider is drawn, handed straight to the conversion panel.
    /// </summary>
    /// <remarks>
    /// What is covered here is the tool being reachable and what happens once a shape has been handed over.
    /// Driving the drawing gesture itself from a test has not been managed -- neither the right click that finishes
    /// a path nor the drag that draws one gets through in this scene -- so the hand-over is called directly. The
    /// gesture is the slider tool's own and untouched, but that it reaches this tool is not proven here.
    /// </remarks>
    public partial class TestSceneStreamPlacement : TestSceneOsuEditor
    {
        private StreamConversionPanel panel => Editor.ChildrenOfType<StreamConversionPanel>().Single();

        private BindableBeatDivisor editorBeatDivisor => (BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor));

        [SetUpSteps]
        public void StartFromNothing()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());
            AddStep("go somewhere quiet", () => EditorClock.Seek(2000));
        }

        /// <summary>
        /// The tool sits in the toolbox and answers the key after the ones that were already there.
        /// </summary>
        [Test]
        public void TestTheToolIsReachedByTheSixthKey()
        {
            AddStep("press 6", () => InputManager.Key(Key.Number6));

            AddAssert("the Streams tool is in hand",
                () => Editor.ChildrenOfType<ComposeBlueprintContainer>().Single().CurrentTool,
                () => Is.TypeOf<StreamCompositionTool>());
        }

        /// <summary>
        /// The shape never goes into the beatmap -- what is being made is a run of circles. Its anchors are put up
        /// on their own so it can still be pulled about, and go away with the panel.
        /// </summary>
        [Test]
        public void TestTheShapeIsShownAsAnchorsOnly()
        {
            AddStep("hand a shape over", () => panel.Open(shape(), fromTheBeatmap: false));

            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.GreaterThan(1));
            AddAssert("no slider went into the beatmap", () => EditorBeatmap.HitObjects.OfType<Slider>(), () => Is.Empty);
            AddUntilStep("its anchors are up", () => Editor.ChildrenOfType<PathControlPointVisualiser<Slider>>().Any(), () => Is.True);

            AddStep("close it without taking it", () => panel.Hide());

            AddUntilStep("the anchors went away", () => Editor.ChildrenOfType<PathControlPointVisualiser<Slider>>(), () => Is.Empty);
        }

        /// <summary>
        /// Reshaping it rebuilds the stream, so what is watched is what the shape is now.
        /// </summary>
        [Test]
        public void TestReshapingItRebuildsTheStream()
        {
            Slider drawn = null;
            float wasFurthest = 0;

            AddStep("hand a shape over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));

            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.True);
            AddStep("remember how far they reach", () => wasFurthest = furthestCircle());

            AddStep("pull the far anchor out", () =>
            {
                drawn.Path.ControlPoints[^1].Position = new Vector2(400, 0);
                EditorBeatmap.Update(drawn);
            });

            AddUntilStep("they reach further now", () => furthestCircle(), () => Is.GreaterThan(wasFurthest));
        }

        /// <summary>
        /// Dragging the run about carries the shape with it. It did not, so touching an anchor afterwards put the
        /// whole run back where the shape still thought it was.
        /// </summary>
        [Test]
        public void TestDraggingTheRunCarriesTheShape()
        {
            Slider drawn = null;
            Vector2 wasAt = Vector2.Zero;

            AddStep("hand a shape over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.True);

            AddStep("remember where the shape is", () => wasAt = drawn.Position);

            AddStep("drag the run to the right", () =>
            {
                foreach (var circle in EditorBeatmap.HitObjects.OfType<HitCircle>().ToList())
                    circle.Position += new Vector2(50, 0);
            });

            AddUntilStep("the shape came too", () => drawn.Position.X, () => Is.EqualTo(wasAt.X + 50).Within(0.01f));

            // and the same along the timeline: the times are counted from the shape's own start.
            double startedAt = 0;
            AddStep("remember when it starts", () => startedAt = drawn.StartTime);

            AddStep("move the run later", () =>
            {
                // a copy: moving one changes where it sits in the beatmap's own order.
                foreach (var circle in EditorBeatmap.HitObjects.OfType<HitCircle>().ToList())
                    circle.StartTime += 500;
            });

            AddUntilStep("the shape waited too", () => drawn.StartTime, () => Is.EqualTo(startedAt + 500).Within(0.01));

            AddStep("add an anchor", () => drawn.Path.ControlPoints.Insert(1, new PathControlPoint(new Vector2(100, 40))));

            AddUntilStep("the run stayed late", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Min(c => c.StartTime),
                () => Is.EqualTo(startedAt + 500).Within(0.01));

            // and stays there: a rebuild puts the circles where the shape now is, not back where it was.
            AddStep("nudge the count", () => panel.NoteCount.Value += 1);
            AddUntilStep("the run stayed put", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Min(c => c.Position.X),
                () => Is.EqualTo(wasAt.X + 50).Within(0.01f));
        }

        /// <summary>
        /// Changing how often a combo starts shows on the run that is on screen, not only on the next one made.
        /// </summary>
        [Test]
        public void TestTheComboIntervalReachesTheRunOnScreen()
        {
            AddStep("hand a shape over", () => panel.Open(shape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.True);

            AddStep("ask for nine", () => panel.NoteCount.Value = 9);
            AddUntilStep("nine of them", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.EqualTo(9));

            AddAssert("one combo to start with", () => combosStarted(), () => Is.EqualTo(1));

            AddStep("a combo every four", () => panel.NewComboEvery.Value = 4);

            // the numbers are worked out by the beatmap, so they only move if it was told the objects changed.
            AddUntilStep("the numbering restarts", () => combosStarted(), () => Is.GreaterThan(1));
        }

        /// <summary>How many times the combo numbering starts over across the run.</summary>
        private int combosStarted() => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(c => c.IndexInCurrentCombo == 0);

        /// <summary>
        /// A shape that turns a corner is laid down a stretch at a time, and each stretch is told what to do on
        /// its own.
        /// </summary>
        [Test]
        public void TestEachStretchIsSetOnItsOwn()
        {
            AddStep("hand a shape with a corner over", () => panel.Open(shapeThatTurnsACorner(), fromTheBeatmap: false));

            AddUntilStep("two stretches", () => panel.Selector.Count.Value, () => Is.EqualTo(2));

            AddStep("four on the first", () => panel.NoteCount.Value = 4);
            AddStep("look at the second", () => panel.Selector.Current.Value = 1);
            AddStep("seven on the second", () => panel.NoteCount.Value = 7);

            // four plus seven, with the one on the corner counted once rather than twice.
            AddUntilStep("ten circles", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.EqualTo(10));

            AddStep("look at the first again", () => panel.Selector.Current.Value = 0);
            AddAssert("it still says four", () => panel.NoteCount.Value, () => Is.EqualTo(4));
        }

        private Slider shapeThatTurnsACorner()
        {
            var slider = new Slider
            {
                StartTime = EditorClock.CurrentTime,
                Position = new Vector2(100, 100),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(150, 0), PathType.LINEAR),
                    new PathControlPoint(new Vector2(150, 150)),
                }),
            };

            slider.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.Difficulty);
            slider.Path.ExpectedDistance.Value = null;
            return slider;
        }

        /// <summary>
        /// The count is a stretch's own, so the division is shared out between them rather than handed to each.
        /// Handing the whole shape's worth to every stretch made a cornered shape twice as long as it should be,
        /// and put the top of the bar at that doubled number.
        /// </summary>
        [Test]
        public void TestTheDivisionIsSharedOutBetweenTheStretches()
        {
            int total = 0;
            int first = 0;

            AddStep("hand a cornered shape over", () => panel.Open(shapeThatTurnsACorner(), fromTheBeatmap: false));
            AddUntilStep("two stretches", () => panel.Selector.Count.Value, () => Is.EqualTo(2));

            AddStep("count what came out", () =>
            {
                total = EditorBeatmap.HitObjects.OfType<HitCircle>().Count();
                first = panel.NoteCount.Value;
            });

            // the two halves of this shape are the same length, so each holds about half the run -- not all of it.
            AddAssert("the first stretch holds about half", () => first, () => Is.EqualTo(total / 2 + 1).Within(1));

            AddStep("look at the second", () => panel.Selector.Current.Value = 1);
            AddAssert("and so does the second", () => panel.NoteCount.Value, () => Is.EqualTo(total / 2 + 1).Within(1));

            AddAssert("the bar stops at sixteen", () => panel.NoteCount.MaxValue, () => Is.EqualTo(16));
        }

        /// <summary>
        /// Ctrl-click puts an anchor into the shape, and a run deleted out from under the panel comes back.
        /// </summary>
        [Test]
        public void TestTheShapeTakesAnchorsAndKeepsItsRun()
        {
            Slider drawn = null;

            AddStep("hand a shape over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.True);

            AddStep("move over the middle of it", () => moveTo(new Vector2(250, 200)));

            AddStep("ctrl-click", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.Click(MouseButton.Left);
                InputManager.ReleaseKey(Key.LControl);
            });

            AddUntilStep("the shape gained an anchor", () => drawn.Path.ControlPoints.Count, () => Is.EqualTo(3));

            // deleting the run is allowed to happen and then undone, wherever it was done from: the panel puts it
            // back rather than holding the buttons back, which would take deleting an anchor away as well.
            AddStep("take the run away", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.OfType<HitCircle>().ToList()));

            AddUntilStep("it came back", () => EditorBeatmap.HitObjects.OfType<HitCircle>(), () => Is.Not.Empty);
        }

        private void moveTo(Vector2 position) =>
            InputManager.MoveMouseTo(Editor.ChildrenOfType<Playfield>().First().ToScreenSpace(position));

        /// <summary>
        /// A drawn shape was never in the beatmap, so closing the panel without taking the conversion must leave
        /// nothing -- rather than leaving the slider behind.
        /// </summary>
        [Test]
        public void TestAShapeThatWasNeverPlacedLeavesNothingBehind()
        {
            AddStep("hand a shape over", () => panel.Open(shape(), fromTheBeatmap: false));

            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.GreaterThan(1));

            AddStep("close it without taking it", () => panel.Hide());

            AddUntilStep("nothing was left", () => EditorBeatmap.HitObjects, () => Is.Empty);
        }

        private float furthestCircle() => EditorBeatmap.HitObjects.OfType<HitCircle>().Max(c => c.Position.X);

        /// <summary>
        /// A stream runs past the moment being looked at, and the whole of it has to stay on screen: the far end
        /// fading out is exactly the part that needs adjusting.
        /// </summary>
        [Test]
        public void TestEveryCircleStaysOnScreen()
        {
            AddStep("hand a shape over", () => panel.Open(shape(), fromTheBeatmap: false));

            AddUntilStep("circles are shown", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.GreaterThan(1));

            AddStep("look somewhere far away", () => EditorClock.Seek(30000));

            AddAssert("all of them are still drawn", () => drawnCircles(), () => Is.EqualTo(EditorBeatmap.HitObjects.OfType<HitCircle>().Count()));
        }

        /// <summary>
        /// How many circles the playfield is actually drawing. Alive rather than all of them: every object the
        /// beatmap holds is in <c>Objects</c> whether it is on screen or not, which is what made an earlier version
        /// of this pass while the far end of the stream was in fact fading out.
        /// </summary>
        private int drawnCircles() =>
            Editor.ChildrenOfType<Playfield>().First().HitObjectContainer.AliveObjects.Count(o => o.HitObject is HitCircle);

        /// <summary>
        /// A shape being drawn has no division of its own, so it starts from the one the editor is set to.
        /// </summary>
        [Test]
        public void TestItStartsFromTheEditorsDivision()
        {
            AddStep("set the editor to 1/8", () => editorBeatDivisor.Value = 8);
            AddStep("hand a shape over", () => panel.Open(shape(), fromTheBeatmap: false));

            AddAssert("the panel came up at 1/8", () => panel.Divisor.Value, () => Is.EqualTo(8));
        }

        /// <summary>
        /// Converting a slider that is already in the beatmap starts from the editor's division too.
        /// </summary>
        [Test]
        public void TestConvertingAPlacedSliderStartsThereAsWell()
        {
            AddStep("set the editor to 1/8", () => editorBeatDivisor.Value = 8);

            AddStep("convert one that is in the beatmap", () =>
            {
                var slider = shape();
                EditorBeatmap.Add(slider);
                panel.Open(slider);
            });

            AddAssert("the panel came up at 1/8", () => panel.Divisor.Value, () => Is.EqualTo(8));
        }

        /// <summary>
        /// One circle deleted from the run of a converted slider is put back, and that is all. The rest of the run was
        /// forgotten along with it and left in the beatmap, so a whole run more was laid over it at every delete
        /// (2026-10-12).
        /// </summary>
        [Test]
        public void TestOneCircleDeletedFromTheRunIsPutBackAlone()
        {
            int before = 0;

            AddStep("convert one that is in the beatmap", () =>
            {
                var slider = shape();
                EditorBeatmap.Add(slider);
                panel.Open(slider);
            });

            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(2));
            AddStep("count them", () => before = circles().Length);

            AddStep("delete one of them", () => EditorBeatmap.Remove(circles()[1]));

            AddUntilStep("it is put back", () => circles().Length, () => Is.GreaterThanOrEqualTo(before));
            AddAssert("and nothing besides", () => circles().Length, () => Is.EqualTo(before));
        }

        /// <summary>
        /// Changing the zigzag leaves the shape where it is. The circles are put on whole coordinates, and how far that
        /// cut the first of them from where the shape has it was read as the run having been dragged: the shape went up
        /// and to the left by a fraction at every change (2026-10-12).
        /// </summary>
        [Test]
        public void TestChangingTheZigzagLeavesTheShapeWhereItIs()
        {
            Slider drawn = null;
            Vector2 wasAt = Vector2.Zero;

            AddStep("hand a slanted shape over", () => panel.Open(drawn = slantedShape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));
            AddStep("remember where the shape is", () => wasAt = drawn.Position);

            for (int reach = 1; reach <= 8; reach++)
            {
                int to = reach;

                AddStep($"a zigzag of {to}", () => zigzag.Current.Value = to);
                AddWaitStep("let the run follow", 3);
            }

            AddAssert("the shape is where it was", () => drawn.Position, () => Is.EqualTo(wasAt));
        }

        /// <summary>
        /// A run with a zigzag is dragged and reshaped as any other is: the shape goes as far as the run was dragged, and
        /// stays where it is while an anchor of it is pulled.
        /// </summary>
        [Test]
        public void TestAZigzaggedRunIsDraggedAndReshapedAsAnyOther()
        {
            Slider drawn = null;
            Vector2 wasAt = Vector2.Zero;

            AddStep("hand a slanted shape over", () => panel.Open(drawn = slantedShape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));

            AddStep("a zigzag of 7", () => zigzag.Current.Value = 7);
            AddWaitStep("let the run follow", 3);
            AddStep("remember where the shape is", () => wasAt = drawn.Position);

            AddStep("drag the run to the right", () =>
            {
                foreach (var circle in circles())
                    circle.Position += new Vector2(50, 0);
            });

            AddWaitStep("let the shape follow", 3);
            AddAssert("the shape came as far", () => drawn.Position, () => Is.EqualTo(wasAt + new Vector2(50, 0)));

            AddStep("pull the far anchor about", () => drawn.Path.ControlPoints[^1].Position = new Vector2(170, 160));
            AddWaitStep("let the run follow", 3);
            AddAssert("the shape stayed there", () => drawn.Position, () => Is.EqualTo(wasAt + new Vector2(50, 0)));
        }

        /// <summary>
        /// Turning the run turns the shape it is laid along. Only how far the first circle went was taken to the shape,
        /// so the run was laid along it again as it had been, somewhere else (2026-10-12).
        /// </summary>
        [Test]
        public void TestTurningTheRunTurnsTheShape()
        {
            Slider drawn = null;
            Vector2[] wereAt = null;

            // lying flat, from (150, 200) to (350, 200).
            AddStep("hand a shape over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));
            AddStep("remember where the circles are", () => wereAt = circles().Select(c => c.Position).ToArray());

            turnClockwise();

            AddUntilStep("the run stands upright", () => circles().Max(c => c.Position.X) - circles().Min(c => c.Position.X), () => Is.LessThan(1));
            AddAssert("as long as it was", () => circles().Max(c => c.Position.Y) - circles().Min(c => c.Position.Y), () => Is.EqualTo(200).Within(1));
            AddAssert("the shape stands upright too", () => MathF.Abs(drawn.Path.ControlPoints[^1].Position.X), () => Is.LessThan(0.01f));
            AddAssert("as long as it was", () => MathF.Abs(drawn.Path.ControlPoints[^1].Position.Y), () => Is.EqualTo(200).Within(0.01f));

            turnClockwise();
            turnClockwise();
            turnClockwise();

            AddUntilStep("a whole turn brings the shape back", () => drawn.Path.ControlPoints[^1].Position, () => Is.EqualTo(new Vector2(200, 0)));
            AddAssert("to where it was", () => drawn.Position, () => Is.EqualTo(new Vector2(150, 200)));
            AddUntilStep("and the circles with it", () => circles().Select(c => c.Position).ToArray(), () => Is.EqualTo(wereAt));
        }

        private void turnClockwise()
        {
            AddStep("ctrl and >", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Period);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddWaitStep("let the run follow", 3);
        }

        /// <summary>
        /// Flipping the run flips the shape it is laid along.
        /// </summary>
        [Test]
        public void TestFlippingTheRunFlipsTheShape()
        {
            Slider drawn = null;

            // from (150, 100) down and to the right, to (350, 230).
            AddStep("hand a slanted shape over", () => panel.Open(drawn = slantedShape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));

            AddStep("ctrl and h", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.H);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            // over the middle of the grid, which is at 256 across.
            AddUntilStep("the shape runs to the left", () => drawn.Path.ControlPoints[^1].Position, () => Is.EqualTo(new Vector2(-200, 130)));
            AddAssert("from across the middle", () => drawn.Position, () => Is.EqualTo(new Vector2(362, 100)));
            AddUntilStep("and the run with it", () => circles().OrderBy(c => c.StartTime).Last().Position, () => Is.EqualTo(new Vector2(162, 230)));
        }

        /// <summary>
        /// Reversing the run reverses the shape it is laid along, and leaves it when it was. The circles' order was turned
        /// round and the shape's was not, so the first of them was at the last one's time, which was read as the run having
        /// been dragged its whole length along the timeline: it went that much later at every press (2026-10-12).
        /// </summary>
        [Test]
        public void TestReversingTheRunReversesTheShape()
        {
            Slider drawn = null;
            double startedAt = 0;
            Vector2[] wereAt = null;

            // from (150, 100) down and to the right, to (350, 230).
            AddStep("hand a slanted shape over", () => panel.Open(drawn = slantedShape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));

            AddStep("remember when and where the run is", () =>
            {
                startedAt = drawn.StartTime;
                wereAt = inOrder().Select(c => c.Position).ToArray();
            });

            reverse();

            AddAssert("the shape starts when it did", () => drawn.StartTime, () => Is.EqualTo(startedAt));
            AddAssert("and so does the run", () => inOrder().First().StartTime, () => Is.EqualTo(startedAt));
            AddAssert("the shape starts where it ended", () => drawn.Position, () => Is.EqualTo(new Vector2(350, 230)));
            AddUntilStep("and the run goes the other way", () => inOrder().Select(c => c.Position).ToArray(), () => Is.EqualTo(wereAt.Reverse().ToArray()));

            reverse();

            AddUntilStep("reversed again it is as it was", () => inOrder().Select(c => c.Position).ToArray(), () => Is.EqualTo(wereAt));
            AddAssert("when it was", () => inOrder().First().StartTime, () => Is.EqualTo(startedAt));
        }

        /// <summary>
        /// What each stretch was told goes with it when the shape is reversed: its last stretch is its first then.
        /// </summary>
        [Test]
        public void TestReversingTheRunKeepsWhatEachStretchWasTold()
        {
            AddStep("hand a shape with a corner over", () => panel.Open(shapeThatTurnsACorner(), fromTheBeatmap: false));
            AddUntilStep("two stretches", () => panel.Selector.Count.Value, () => Is.EqualTo(2));

            AddStep("four on the first", () => panel.NoteCount.Value = 4);
            AddStep("look at the second", () => panel.Selector.Current.Value = 1);
            AddStep("seven on the second", () => panel.NoteCount.Value = 7);
            AddUntilStep("ten circles", () => circles().Length, () => Is.EqualTo(10));

            reverse();

            AddAssert("the one looked at is the first now, and still says seven", () => (panel.Selector.Current.Value, panel.NoteCount.Value), () => Is.EqualTo((0, 7)));
            AddStep("look at the second", () => panel.Selector.Current.Value = 1);
            AddAssert("which says four", () => panel.NoteCount.Value, () => Is.EqualTo(4));
            AddAssert("ten circles still", () => circles().Length, () => Is.EqualTo(10));
        }

        private HitCircle[] inOrder() => circles().OrderBy(c => c.StartTime).ToArray();

        private void reverse()
        {
            AddStep("ctrl and g", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.G);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddWaitStep("let the run follow", 3);
        }

        /// <summary>
        /// Stretching the run stretches the shape it is laid along.
        /// </summary>
        [Test]
        public void TestStretchingTheRunStretchesTheShape()
        {
            Slider drawn = null;

            // lying flat, from (150, 200) to (350, 200).
            AddStep("hand a shape over", () => panel.Open(drawn = shape(), fromTheBeatmap: false));
            AddUntilStep("circles are shown", () => circles().Length, () => Is.GreaterThan(1));

            AddStep("stretch it to half as wide from its start", () =>
            {
                var handler = Editor.ChildrenOfType<OsuSelectionScaleHandler>().First();

                handler.Begin();
                handler.Update(new Vector2(0.5f, 1), new Vector2(150, 200));
                handler.Commit();
            });

            AddUntilStep("the shape is half as long", () => drawn.Path.ControlPoints[^1].Position, () => Is.EqualTo(new Vector2(100, 0)));
            AddAssert("from where it started", () => drawn.Position, () => Is.EqualTo(new Vector2(150, 200)));
            AddUntilStep("and so is the run", () => furthestCircle(), () => Is.EqualTo(250).Within(1));
        }

        private HitCircle[] circles() => EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray();

        // the two float sliders are acceleration then the zigzag's reach, in that order down the panel.
        private FormSliderBar<float> zigzag => panel.ChildrenOfType<FormSliderBar<float>>().Last();

        /// <summary>
        /// A shape across which the zigzag pushes circles onto fractions: nothing of it lies along an axis.
        /// </summary>
        private Slider slantedShape()
        {
            var slider = new Slider
            {
                StartTime = EditorClock.CurrentTime,
                Position = new Vector2(150, 100),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(200, 130)),
                }),
            };

            slider.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.Difficulty);
            return slider;
        }

        /// <summary>
        /// A slider with its defaults applied, so it has a length in time. Without them its velocity is zero and it
        /// never ends, which is not a shape anything can be laid along.
        /// </summary>
        private Slider shape()
        {
            var slider = new Slider
            {
                StartTime = EditorClock.CurrentTime,
                Position = new Vector2(150, 200),
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.LINEAR),
                    new PathControlPoint(new Vector2(200, 0)),
                }),
            };

            slider.ApplyDefaults(EditorBeatmap.ControlPointInfo, EditorBeatmap.Difficulty);
            return slider;
        }
    }
}
