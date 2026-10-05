// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
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

        private void keepWholeCoordinates(bool whole)
        {
            AddStep($"whole coordinates: {whole}", () => config.SetValue(OsuSetting.EditorIntegerPositions, whole));
        }

        private HitCircle placedCircle() => EditorBeatmap.HitObjects.OfType<HitCircle>().Last();

        private static bool isWhole(Vector2 position) => position.X % 1 == 0 && position.Y % 1 == 0;
    }
}
