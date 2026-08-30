// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The panel that turns a slider into a stream.
    /// </summary>
    [TestFixture]
    public partial class TestSceneStreamConversionPanel : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private Slider slider = null!;

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("put a slider in", () =>
            {
                EditorBeatmap.Clear();

                EditorBeatmap.Add(slider = new Slider
                {
                    StartTime = 3000,
                    Position = new Vector2(200, 200),
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
                });
            });

            AddStep("select it", () => EditorBeatmap.SelectedHitObjects.Add(slider));
        }

        /// <summary>
        /// Opening it at all is most of the test. A panel is a pile of containers sizing themselves against one
        /// another, and getting that wrong throws on the update thread rather than looking slightly off.
        /// </summary>
        [Test]
        public void TestItOpensAndShowsTheStream()
        {
            open();

            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);
            AddAssert("the slider is out of the way", () => EditorBeatmap.HitObjects.Contains(slider), () => Is.False);
        }

        /// <summary>
        /// The settings change the stream that is already on screen, rather than being read once at the end.
        /// </summary>
        [Test]
        public void TestChangingTheDivisionChangesTheStream()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            int before = 0;
            AddStep("count them", () => before = circles().Length);

            // the left chevron of the editor's own divisor control, which is what a mapper would reach for.
            AddStep("coarsen the division", () => panel().ChildrenOfType<BeatDivisorControl.ChevronButton>().First().TriggerClick());
            AddUntilStep("there are fewer", () => circles().Length, () => Is.LessThan(before));
        }

        /// <summary>
        /// Taking the change swaps the slider for the stream. That is the whole point of it.
        /// </summary>
        [Test]
        public void TestCommitReplacesTheSlider()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            AddStep("take it", () => panel().ChildrenOfType<RoundedButton>().Single().TriggerClick());

            AddUntilStep("the panel closed", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the slider is gone", () => EditorBeatmap.HitObjects.Contains(slider), () => Is.False);
            AddAssert("the circles stayed", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.True);
        }

        /// <summary>
        /// Closing without taking it leaves the beatmap as it was — a preview that could not be backed out of
        /// would be a conversion with extra steps.
        /// </summary>
        [Test]
        public void TestClosingPutsEverythingBack()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            AddStep("close it", () => panel().Hide());

            AddUntilStep("the circles are gone", () => EditorBeatmap.HitObjects.OfType<HitCircle>().Any(), () => Is.False);
            AddAssert("the slider is back", () => EditorBeatmap.HitObjects.Contains(slider), () => Is.True);
        }

        /// <summary>
        /// The drawn curve is an alternative to the list of easings, not something on top of it — one of the two
        /// is on screen at a time, and whichever it is drives the stream.
        /// </summary>
        [Test]
        public void TestTheDrawnCurveReplacesTheEasing()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            AddAssert("the list is showing", () => panel().ChildrenOfType<FormEnumDropdown<StreamEasing>>().Single().Alpha, () => Is.EqualTo(1));
            AddAssert("the curve is not", () => editor().Alpha, () => Is.EqualTo(0));

            AddStep("switch to drawing it", () => panel().ChildrenOfType<FormCheckBox>().First().Current.Value = true);

            AddUntilStep("the curve is showing", () => editor().Alpha, () => Is.EqualTo(1));
            AddAssert("the list is not", () => panel().ChildrenOfType<FormEnumDropdown<StreamEasing>>().Single().Alpha, () => Is.EqualTo(0));

            float[] before = null!;
            AddStep("note where the circles are", () => before = circles().Select(c => c.Position.X).ToArray());

            AddStep("bend the curve", () => editor().Points.Add(new Vector2(0.25f, 0.85f)));

            AddUntilStep("the stream moved with it",
                () => circles().Select(c => c.Position.X).ToArray(), () => Is.Not.EqualTo(before));
        }

        /// <summary>
        /// Acceleration goes away with the list of easings. It is part of the same reckoning, and a drawn curve
        /// says everything it was for — left on screen it would be a slider that moves and changes nothing.
        /// </summary>
        [Test]
        public void TestAccelerationGoesWithTheEasing()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            AddAssert("acceleration is showing", () => acceleration().Alpha, () => Is.EqualTo(1));

            AddStep("switch to drawing it", () => panel().ChildrenOfType<FormCheckBox>().First().Current.Value = true);
            AddUntilStep("it went with the list", () => acceleration().Alpha, () => Is.EqualTo(0));

            AddStep("switch back", () => panel().ChildrenOfType<FormCheckBox>().First().Current.Value = false);
            AddUntilStep("and came back with it", () => acceleration().Alpha, () => Is.EqualTo(1));
        }

        /// <summary>
        /// The two settings under the zigzag are dimmed while it reaches nowhere, since neither does anything
        /// until it does.
        /// </summary>
        [Test]
        public void TestTheZigzagSettingsDimWhenItReachesNowhere()
        {
            open();
            AddUntilStep("circles appeared", circles, () => Is.Not.Empty);

            AddAssert("they are dimmed", () => zigzagExtras().All(d => d.Alpha < 1), () => Is.True);

            AddStep("give it some reach", () => panel().ChildrenOfType<FormSliderBar<float>>().Last().Current.Value = 30);
            AddUntilStep("they woke up", () => zigzagExtras().All(d => d.Alpha == 1), () => Is.True);

            AddStep("take it away again", () => panel().ChildrenOfType<FormSliderBar<float>>().Last().Current.Value = 0);
            AddUntilStep("they dimmed again", () => zigzagExtras().All(d => d.Alpha < 1), () => Is.True);
        }

        /// <summary>
        /// The back action closes the panel rather than reaching the editor behind it, where it would walk out
        /// of the beatmap.
        /// </summary>
        [Test]
        public void TestBackClosesItRatherThanTheEditor()
        {
            open();
            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("it closed", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the editor is still here", () => ((IScreen)Editor).IsCurrentScreen(), () => Is.True);
            AddAssert("and the slider is back", () => EditorBeatmap.HitObjects.Contains(slider), () => Is.True);
        }

        /// <summary>
        /// The container the window sits in is the size of the editor, so that the window can be dragged
        /// anywhere in it. Only the window takes the mouse — taking it everywhere would leave the playfield
        /// and the timeline dead while the panel is open, with nothing on screen to say why.
        /// </summary>
        [Test]
        public void TestOnlyTheWindowTakesTheMouse()
        {
            open();
            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddAssert("the window takes it", () => panel().ReceivePositionalInputAt(probe().ScreenSpaceDrawQuad.Centre), () => Is.True);

            AddAssert("the rest of the screen does not", () =>
            {
                var whole = panel().ScreenSpaceDrawQuad;
                var window = probe().Parent!.Parent!.Parent!.ScreenSpaceDrawQuad;

                // a point far from the window, on the opposite side of the editor.
                var away = new Vector2(whole.TopLeft.X + 20, whole.BottomLeft.Y - 20);

                return !window.Contains(away) && !panel().ReceivePositionalInputAt(away);
            }, () => Is.True);
        }

        /// <summary>
        /// A point follows the mouse the whole way, not for a frame.
        /// </summary>
        /// <remarks>
        /// Rebuilding the handles whenever the points changed threw away the one being held as soon as it moved,
        /// which ended the drag at once: the point could be picked up but barely shifted.
        /// </remarks>
        [Test]
        public void TestAPointCanBeDraggedAcross()
        {
            open();
            AddStep("switch to drawing it", () => panel().ChildrenOfType<FormCheckBox>().First().Current.Value = true);
            AddStep("put a point down", () => editor().Points.Add(new Vector2(0.3f, 0.3f)));

            AddUntilStep("its handle is there", () => editor().ChildrenOfType<CircularContainer>().Any(), () => Is.True);

            AddStep("take hold of it", () =>
            {
                InputManager.MoveMouseTo(editor().ChildrenOfType<CircularContainer>().First());
                InputManager.PressButton(MouseButton.Left);
            });

            // a handful of small movements rather than one jump, because that is what a drag is — and because
            // one jump lands before anything has had a chance to take the handle away, which is what went
            // wrong. A test that moves the mouse once passes either way and proves nothing.
            AddStep("drag it right across", () =>
            {
                var box = editor().ScreenSpaceDrawQuad;

                for (int step = 1; step <= 10; step++)
                {
                    float across = 0.3f + (0.8f - 0.3f) * step / 10f;
                    float down = 0.3f + (0.2f - 0.3f) * step / 10f;

                    InputManager.MoveMouseTo(new Vector2(box.TopLeft.X + box.Width * across,
                        box.TopLeft.Y + box.Height * (1 - down)));
                }
            });

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("it went where it was taken", () => editor().Points.Single().X, () => Is.EqualTo(0.8f).Within(0.05f));
        }

        /// <summary>
        /// It opens fully on screen. Placing it against the bottom edge means knowing how tall it is, which is
        /// not settled until it has been laid out — reading that too early put it half off the screen.
        /// </summary>
        [Test]
        public void TestItOpensFullyOnScreen()
        {
            open();
            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddUntilStep("all of it is within the editor", () =>
            {
                var window = probe().Parent!.Parent!.Parent!.ScreenSpaceDrawQuad;
                var screen = panel().ScreenSpaceDrawQuad;

                return window.TopLeft.X >= screen.TopLeft.X - 1 && window.TopLeft.Y >= screen.TopLeft.Y - 1
                       && window.BottomRight.X <= screen.BottomRight.X + 1 && window.BottomRight.Y <= screen.BottomRight.Y + 1;
            }, () => Is.True);
        }

        /// <summary>
        /// Where it was left is where it comes back, so that moving it off a stream is not undone by the next one.
        /// </summary>
        [Test]
        public void TestItReopensWhereItWasLeft()
        {
            open();
            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            Vector2 moved = Vector2.Zero;

            AddStep("take hold of it", () =>
            {
                InputManager.MoveMouseTo(panel().ChildrenOfType<OsuSpriteText>().First());
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("shove it", () => InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position + new Vector2(150, 120)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            // read on a later frame than the drag: the move is applied when the event is handled, not when the
            // mouse is told to move.
            AddStep("note where it ended up", () => moved = probe().ScreenSpaceDrawQuad.TopLeft);

            AddStep("close it", () => panel().Hide());
            AddStep("select the slider again", () => EditorBeatmap.SelectedHitObjects.Add(slider));
            open();

            AddUntilStep("it came back where it was", () => probe().ScreenSpaceDrawQuad.TopLeft, () => Is.EqualTo(moved));
        }

        /// <summary>
        /// It can be moved off the stream it is about, which is why it is a window rather than a popover.
        /// </summary>
        [Test]
        public void TestItCanBeDragged()
        {
            open();
            AddUntilStep("the panel is showing", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            Vector2 before = Vector2.Zero;

            // the button at the bottom of the panel stands in for the panel: it moves with it, and unlike the
            // panel itself it is not the full-screen container everything is hung from.
            AddStep("take hold of the title", () =>
            {
                before = probe().ScreenSpaceDrawQuad.TopLeft;

                InputManager.MoveMouseTo(panel().ChildrenOfType<OsuSpriteText>().First());
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag it away", () => InputManager.MoveMouseTo(InputManager.CurrentState.Mouse.Position - new Vector2(120, 90)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("it moved", () => probe().ScreenSpaceDrawQuad.TopLeft, () => Is.Not.EqualTo(before));
        }

        private void open() => AddStep("press the shortcut", () =>
        {
            InputManager.PressKey(Key.ControlLeft);
            InputManager.PressKey(Key.ShiftLeft);
            InputManager.Key(Key.F);
            InputManager.ReleaseKey(Key.ShiftLeft);
            InputManager.ReleaseKey(Key.ControlLeft);
        });

        private StreamConversionPanel panel() => Editor.ChildrenOfType<StreamConversionPanel>().Single();

        private RoundedButton probe() => panel().ChildrenOfType<RoundedButton>().Single();

        private StreamCurveEditor editor() => panel().ChildrenOfType<StreamCurveEditor>().Single();

        // the two float sliders are acceleration then the zigzag's reach, in that order down the panel.
        private FormSliderBar<float> acceleration() => panel().ChildrenOfType<FormSliderBar<float>>().First();

        private Drawable[] zigzagExtras() => new Drawable[]
        {
            panel().ChildrenOfType<FormSliderBar<int>>().Single(),
            panel().ChildrenOfType<FormEnumDropdown<ZigzagShape>>().Single(),
        };

        private HitCircle[] circles() => EditorBeatmap.HitObjects.OfType<HitCircle>().ToArray();
    }
}
