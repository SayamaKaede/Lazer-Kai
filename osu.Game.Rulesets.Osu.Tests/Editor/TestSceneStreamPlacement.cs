// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
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
