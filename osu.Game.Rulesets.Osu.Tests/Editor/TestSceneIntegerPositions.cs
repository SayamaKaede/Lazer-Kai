// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Components;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Objects kept on whole coordinates, which is what the beatmap is read as outside this editor.
    /// </summary>
    /// <remarks>
    /// The game holds a position as a fraction of a pixel and writes it out that way, while stable and the website
    /// read a whole number. A position half a pixel off therefore lands somewhere else as soon as the beatmap leaves
    /// the editor, which moves objects onto stacks they were never meant to be on.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneIntegerPositions : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private OsuPlayfield playfield = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());
            AddStep("seek to the first timing point", () => EditorClock.Seek(Beatmap.Value.Beatmap.ControlPointInfo.TimingPoints.First().Time));
        }

        [TearDownSteps]
        public void PutTheSettingBack()
        {
            AddStep("back to the stock answer", () => config.SetValue(OsuSetting.EditorIntegerPositions, true));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestPlacingACircle(bool wholeCoordinates)
        {
            keepWholeCoordinates(wholeCoordinates);

            // 1 is the select tool, so the circle is 2.
            AddStep("pick the circle tool", () => InputManager.Key(Key.Number2));
            AddStep("move half a pixel off", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(128.5f, 192.5f))));
            AddStep("place it", () => InputManager.Click(MouseButton.Left));

            AddAssert("the position is as the setting asks", () => isWhole(placedCircle().Position), () => Is.EqualTo(wholeCoordinates));
        }

        /// <summary>
        /// Nudging by a whole pixel leaves a position that was already a fraction off still a fraction off, so the
        /// rounding has to happen to the position rather than to what is added to it.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TestNudgingAnObjectThatIsAlreadyOff(bool wholeCoordinates)
        {
            HitCircle circle = null!;

            keepWholeCoordinates(wholeCoordinates);

            AddStep("add a circle half a pixel off", () =>
            {
                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(128.5f, 192.5f) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(circle);
            });

            AddStep("nudge it right", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.Key(Key.Right);
                InputManager.ReleaseKey(Key.LControl);
            });

            AddAssert("the position is as the setting asks", () => isWhole(circle.Position), () => Is.EqualTo(wholeCoordinates));
        }

        /// <summary>
        /// A turn puts objects anywhere, so they are rounded on every step of it, from where they were before it:
        /// what is shown while turning is where it is left (2026-10-05).
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TestRotatingASelection(bool wholeCoordinates)
        {
            HitCircle circle = null!;

            keepWholeCoordinates(wholeCoordinates);

            AddStep("add two circles", () =>
            {
                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(100, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(300, 100) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects);
            });

            AddStep("turn it a little", () =>
            {
                var rotation = Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single();

                rotation.Begin();
                rotation.Update(23);
            });

            AddAssert("the position is as the setting asks while turning", () => isWhole(circle.Position), () => Is.EqualTo(wholeCoordinates));

            Vector2 whileTurning = default;
            AddStep("note where it is", () => whileTurning = circle.Position);
            AddStep("settle the turn", () => Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single().Commit());

            AddAssert("the position is as the setting asks", () => isWhole(circle.Position), () => Is.EqualTo(wholeCoordinates));
            AddAssert("settling it moved nothing", () => circle.Position, () => Is.EqualTo(whileTurning));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestScalingASelection(bool wholeCoordinates)
        {
            HitCircle circle = null!;

            keepWholeCoordinates(wholeCoordinates);

            AddStep("add two circles", () =>
            {
                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(100, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(300, 100) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects);
            });

            AddStep("stretch it a little", () =>
            {
                var scaling = Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single();

                scaling.Begin();
                // a factor that lands the far circle on a fraction: 100 + 200 * 1.007 is 301.4.
                scaling.Update(new Vector2(1.007f), new Vector2(100, 100));
            });

            AddAssert("the position is as the setting asks while stretching", () => isWhole(EditorBeatmap.HitObjects.OfType<HitCircle>().Last().Position), () => Is.EqualTo(wholeCoordinates));

            Vector2 whileStretching = default;
            AddStep("note where it is", () => whileStretching = EditorBeatmap.HitObjects.OfType<HitCircle>().Last().Position);
            AddStep("settle the stretch", () => Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single().Commit());

            AddAssert("the position is as the setting asks", () => isWhole(EditorBeatmap.HitObjects.OfType<HitCircle>().Last().Position), () => Is.EqualTo(wholeCoordinates));
            AddAssert("settling it moved nothing", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Last().Position, () => Is.EqualTo(whileStretching));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestFlippingASelection(bool wholeCoordinates)
        {
            HitCircle circle = null!;

            keepWholeCoordinates(wholeCoordinates);

            AddStep("add a circle half a pixel off", () =>
            {
                EditorBeatmap.Add(circle = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(100.5f, 100.5f) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(circle);
            });

            AddStep("flip it across", () =>
            {
                InputManager.PressKey(Key.LControl);
                InputManager.Key(Key.H);
                InputManager.ReleaseKey(Key.LControl);
            });

            AddAssert("the position is as the setting asks", () => isWhole(circle.Position), () => Is.EqualTo(wholeCoordinates));
        }

        /// <summary>
        /// Holding a circle on a slider's end, with whole coordinates asked for. The end of a slider placed before
        /// the setting existed sits on a fraction of a pixel, so rounding pulls the circle off the spot the snap just
        /// put it on, the snap takes hold again on the next frame, and the circle shakes between the two. Once let go
        /// it is rounded all the same (2026-10-05): a fraction left there is written as a whole number anyway, and the
        /// rounding moves it under a pixel, well inside the three that keep it stacked on the end.
        /// </summary>
        [Test]
        public void TestHoldingACircleOnAFractionalSliderEnd()
        {
            HitCircle circle = null!;
            Slider slider = null!;

            keepWholeCoordinates(true);

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider ending on a fraction, and a circle away from it", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(156.3f, 192.4f),
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
            AddStep("drag it onto the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.EndPosition)));

            AddAssert("it is on the end", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(1));

            // the hand is still on it, moving a fraction of a pixel at a time, nowhere near letting go.
            for (int i = 1; i <= 4; i++)
            {
                float offset = i * 0.3f;

                AddStep($"move a fraction of a pixel ({i})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.EndPosition + new Vector2(offset, 0))));
                AddAssert($"still on the end ({i})", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(1));
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("it is on a whole coordinate", () => isWhole(circle.Position));
            AddAssert("still on the end", () => Vector2.Distance(slider.EndPosition, circle.Position), () => Is.LessThan(1));
            AddUntilStep("stacked on it", () => circle.StackHeight, () => Is.Not.EqualTo(0));
        }

        /// <summary>
        /// A circle placed on a slider's end that sits on a fraction is put on a whole coordinate, as one placed
        /// anywhere else is, and on the one stable puts it on: the fraction cut off rather than rounded (2026-10-05).
        /// </summary>
        [Test]
        public void TestPlacingACircleOnAFractionalSliderEnd()
        {
            Slider slider = null!;

            keepWholeCoordinates(true);

            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider ending on a fraction", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(156.59f, 191.76f),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.LINEAR),
                        new PathControlPoint(new Vector2(100, 0)),
                    })
                });
            });

            AddStep("seek past it", () => EditorClock.Seek(slider.GetEndTime() + 250));
            AddStep("enter circle placement", () => InputManager.Key(Key.Number2));
            AddStep("aim at the end", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.EndPosition + new Vector2(1, 0))));
            AddStep("place", () => InputManager.Click(MouseButton.Left));

            AddAssert("a circle was placed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.EqualTo(1));
            AddAssert("it is on a whole coordinate", () => isWhole(placedCircle().Position));
            AddAssert("it is on the end", () => Vector2.Distance(slider.EndPosition, placedCircle().Position), () => Is.LessThan(1.5f));
            AddAssert("it is where stable puts it", () => placedCircle().Position, () => Is.EqualTo(new Vector2(256, 191)));
        }

        /// <summary>
        /// A circle placed with distance snap is on a whole coordinate (2026-10-05). The snap puts it on a ring around
        /// the circle before it, which is almost never a whole number, and what a snap decided used to be left as it was.
        /// </summary>
        [Test]
        public void TestPlacingACircleWithDistanceSnap()
        {
            keepWholeCoordinates(true);

            AddStep("add a circle to measure from", () => EditorBeatmap.Add(new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(200, 200) }));
            AddStep("move a beat on", () => EditorClock.SeekForward(true));
            AddStep("pick the circle tool", () => InputManager.Key(Key.Number2));

            // off at an angle, so the ring is crossed away from any whole number.
            AddStep("aim off at an angle", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(new Vector2(263.3f, 237.1f))));
            AddStep("place it", () => InputManager.Click(MouseButton.Left));

            AddAssert("a second circle was placed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.EqualTo(2));
            AddAssert("it is on a whole coordinate", () => isWhole(placedCircle().Position));
        }

        /// <summary>
        /// The circles a polygon is generated with are on whole coordinates (2026-10-05). They are worked out around a
        /// circle, which lands on fractions.
        /// </summary>
        [Test]
        public void TestGeneratingAPolygon()
        {
            keepWholeCoordinates(true);

            AddStep("open the polygon generator", () => Editor.ChildrenOfType<EditorToolButton>().Single(b => b.Text.ToString() == "Polygon").TriggerClick());
            AddUntilStep("circles were generated", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Count(), () => Is.GreaterThan(0));
            AddAssert("they are on whole coordinates", () => EditorBeatmap.HitObjects.OfType<HitCircle>().All(c => isWhole(c.Position)));
            AddStep("close it", () => InputManager.Key(Key.Escape));
        }

        /// <summary>
        /// Moving a selection by numbers puts it on whole coordinates, as one piece (2026-10-05). The numbers are whole,
        /// but a selection that was already a fraction off stayed that far off.
        /// </summary>
        [Test]
        public void TestMovingBySetNumbers()
        {
            HitCircle first = null!;
            HitCircle second = null!;

            keepWholeCoordinates(true);

            AddStep("add two circles a fraction off", () =>
            {
                EditorBeatmap.Add(first = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(100.6f, 100.4f) });
                EditorBeatmap.Add(second = new HitCircle { StartTime = EditorClock.CurrentTime + 500, Position = new Vector2(200.6f, 150.4f) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new[] { first, second });
            });

            AddStep("open the move popover", () => Editor.ChildrenOfType<EditorToolButton>().Single(b => b.Text.ToString() == "Move").TriggerClick());
            AddUntilStep("it is open", () => Editor.ChildrenOfType<PreciseMovementPopover>().Any());
            AddStep("move by ten across", () => Editor.ChildrenOfType<PreciseMovementPopover>().Single().ChildrenOfType<FormSliderBar<float>>().First().Current.Value = 10);

            AddAssert("the first is on a whole coordinate", () => isWhole(first.Position));
            AddAssert("the second is on a whole coordinate", () => isWhole(second.Position));
            AddAssert("they are as far apart as they were", () => second.Position - first.Position, () => Is.EqualTo(new Vector2(100, 50)));
            AddStep("close it", () => InputManager.Key(Key.Escape));
        }

        /// <summary>
        /// A slider's anchors are kept on whole coordinates too, as stable holds them (2026-10-05). They used to be
        /// left as fractions, which the save wrote as whole numbers, so the path -- and the end of it, and whatever was
        /// stacked on that end -- moved once the beatmap was opened again. Each way of changing them is tried in turn,
        /// and the anchors are looked at as where they are, not as offsets from the head.
        /// </summary>
        [TestCase(AnchorChange.DraggingAnAnchor)]
        [TestCase(AnchorChange.DraggingTheHead)]
        [TestCase(AnchorChange.AddingAnAnchor)]
        [TestCase(AnchorChange.Turning)]
        [TestCase(AnchorChange.Stretching)]
        [TestCase(AnchorChange.Flipping)]
        [TestCase(AnchorChange.Reversing)]
        public void TestASlidersAnchorsStayWhole(AnchorChange change)
        {
            Slider slider = null!;

            keepWholeCoordinates(true);

            // a case that failed with the button held leaves it held into the next.
            AddStep("let go of everything", () => InputManager.ReleaseButton(MouseButton.Left));
            AddStep("disable distance snap", () => InputManager.Key(Key.Q));

            AddStep("add a slider", () =>
            {
                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = EditorClock.CurrentTime,
                    Position = new Vector2(150, 150),
                    Path = new SliderPath(new[]
                    {
                        new PathControlPoint(new Vector2(0), PathType.BEZIER),
                        new PathControlPoint(new Vector2(60, -40)),
                        new PathControlPoint(new Vector2(120, 10)),
                    })
                });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(slider);
            });

            switch (change)
            {
                case AnchorChange.DraggingAnAnchor:
                case AnchorChange.DraggingTheHead:
                    int index = change == AnchorChange.DraggingTheHead ? 0 : 1;

                    AddStep("take hold of the anchor", () =>
                    {
                        InputManager.MoveMouseTo(Editor.ChildrenOfType<PathControlPointPiece<Slider>>().ElementAt(index));
                        InputManager.PressButton(MouseButton.Left);
                    });
                    AddStep("move it a fraction of a pixel's worth off", () =>
                        InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + slider.Path.ControlPoints[index].Position + new Vector2(10.4f, 7.3f))));
                    AddAssert("the anchors are whole while held", () => anchorsAreWhole(slider));
                    AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
                    AddAssert("the anchor moved", () => slider.Position + slider.Path.ControlPoints[index].Position,
                        () => Is.Not.EqualTo(new Vector2(150, 150) + (index == 0 ? Vector2.Zero : new Vector2(60, -40))));
                    break;

                case AnchorChange.AddingAnAnchor:
                    AddStep("ctrl-click on the body", () =>
                    {
                        InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(slider.Position + slider.Path.PositionAt(0.3) + new Vector2(0.4f, 0.3f)));
                        InputManager.PressKey(Key.ControlLeft);
                        InputManager.Click(MouseButton.Left);
                        InputManager.ReleaseKey(Key.ControlLeft);
                    });
                    AddAssert("an anchor was added", () => slider.Path.ControlPoints.Count, () => Is.EqualTo(4));
                    break;

                case AnchorChange.Turning:
                    AddStep("turn it", () =>
                    {
                        var rotation = Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single();
                        rotation.Begin();
                        rotation.Update(23);
                    });
                    AddAssert("the anchors are whole while turning", () => anchorsAreWhole(slider));
                    AddStep("settle it", () => Editor.ChildrenOfType<OsuSelectionRotationHandler>().Single().Commit());
                    break;

                case AnchorChange.Stretching:
                    AddStep("stretch it", () =>
                    {
                        var scaling = Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single();
                        scaling.Begin();
                        scaling.Update(new Vector2(1.07f), new Vector2(150, 150));
                    });
                    AddAssert("the anchors are whole while stretching", () => anchorsAreWhole(slider));
                    AddStep("settle it", () => Editor.ChildrenOfType<OsuSelectionScaleHandler>().Single().Commit());
                    break;

                case AnchorChange.Flipping:
                    // across the selection, whose middle is a fraction off a whole number for this path.
                    AddStep("flip it", () => Editor.ChildrenOfType<OsuSelectionHandler>().Single().HandleFlip(Direction.Vertical, false));
                    break;

                case AnchorChange.Reversing:
                    AddStep("shorten it to a fraction of its path", () => slider.Path.ExpectedDistance.Value = slider.Path.CalculatedDistance - 7.3);
                    AddStep("reverse it", () => Editor.ChildrenOfType<OsuSelectionHandler>().Single().HandleReverse());
                    break;
            }

            AddAssert("the anchors are whole", () => anchorsAreWhole(slider));
        }

        public enum AnchorChange
        {
            DraggingAnAnchor,
            DraggingTheHead,
            AddingAnAnchor,
            Turning,
            Stretching,
            Flipping,
            Reversing,
        }

        private static bool anchorsAreWhole(Slider slider) =>
            isWhole(slider.Position) && slider.Path.ControlPoints.All(point => isWhole(slider.Position + point.Position));

        private void keepWholeCoordinates(bool whole)
        {
            AddStep($"whole coordinates: {whole}", () => config.SetValue(OsuSetting.EditorIntegerPositions, whole));
        }

        private HitCircle placedCircle() => EditorBeatmap.HitObjects.OfType<HitCircle>().Last();

        private static bool isWhole(Vector2 position) => position.X % 1 == 0 && position.Y % 1 == 0;
    }
}
