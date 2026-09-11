// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// A path finished over the bottom timeline is finished there, rather than dropped for a seek.
    /// </summary>
    /// <remarks>
    /// A slider whose end ran down onto the bottom timeline could not be finished: the timeline took the right click
    /// as a seek, so the path stayed open and the clock jumped to wherever the click had landed.
    /// </remarks>
    public partial class TestSceneBottomTimelineFinishesPlacement : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private OsuPlayfield playfield => Editor.ChildrenOfType<OsuPlayfield>().First();

        private SummaryTimeline bottomTimeline => Editor.ChildrenOfType<SummaryTimeline>().First();

        private double timeBefore;

        [SetUpSteps]
        public void StartFromNothing()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());

            // after the first timing point, which is the only place objects can go at all. A path started before it is
            // never finished, and that reads exactly like the click not arriving -- which is how it was first mistaken.
            AddStep("go somewhere objects can be placed", () => EditorClock.Seek(EditorBeatmap.ControlPointInfo.TimingPoints.First().Time + 500));
        }

        // in teardown rather than at the end of the one test that swaps them, so a failure part way through cannot
        // leave every test after it running with the buttons the other way round.
        [TearDownSteps]
        public void PutTheButtonsBack()
        {
            AddStep("buttons back as they were", () => config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse).SetDefault());
        }

        /// <summary>
        /// The ground everything else stands on: a right click finishes a path at all, in this scene.
        /// </summary>
        [Test]
        public void TestFinishingOverThePlayfield()
        {
            beginSlider();

            AddStep("move within the playfield", () => InputManager.MoveMouseTo(inPlayfield(0.7f, 0.5f)));
            AddStep("right click", () => InputManager.Click(MouseButton.Right));

            AddAssert("a slider went in", () => EditorBeatmap.HitObjects.OfType<Slider>().Count(), () => Is.EqualTo(1));
        }

        [Test]
        public void TestFinishingOverTheBottomTimeline()
        {
            beginSlider();
            moveOntoTheBottomTimeline();

            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddWaitStep("give a seek the time to land", 2);

            AddAssert("a slider went in", () => EditorBeatmap.HitObjects.OfType<Slider>().Count(), () => Is.EqualTo(1));
            AddAssert("the clock stayed where it was", () => EditorClock.CurrentTime, () => Is.EqualTo(timeBefore).Within(1));
        }

        /// <summary>
        /// With the buttons swapped, the physical right button arrives as the middle one, and it is still the one
        /// that finishes a path.
        /// </summary>
        [Test]
        public void TestFinishingWithTheButtonsSwapped()
        {
            AddStep("swap right and middle", () => config.SetValue(OsuSetting.SwapRightAndMiddleMouse, true));

            beginSlider();
            moveOntoTheBottomTimeline();

            AddStep("press what the right button has become", () => InputManager.Click(MouseButton.Middle));
            AddWaitStep("give a seek the time to land", 2);

            AddAssert("a slider went in", () => EditorBeatmap.HitObjects.OfType<Slider>().Count(), () => Is.EqualTo(1));
            AddAssert("the clock stayed where it was", () => EditorClock.CurrentTime, () => Is.EqualTo(timeBefore).Within(1));
        }

        [Test]
        public void TestFinishingAStreamOverTheBottomTimeline()
        {
            AddStep("take the Streams tool", () => InputManager.Key(Key.Number6));
            AddStep("move within the playfield", () => InputManager.MoveMouseTo(inPlayfield(0.3f, 0.5f)));
            AddStep("start the shape", () => InputManager.Click(MouseButton.Left));
            AddUntilStep("a placement is under way", () => EditorBeatmap.PlacementObject.Value, () => Is.Not.Null);

            moveOntoTheBottomTimeline();

            AddStep("right click", () => InputManager.Click(MouseButton.Right));
            AddWaitStep("give a seek the time to land", 2);

            AddUntilStep("the shape went to the panel", () => Editor.ChildrenOfType<StreamConversionPanel>().Single().State.Value,
                () => Is.EqualTo(Visibility.Visible));
            AddAssert("the clock stayed where it was", () => EditorClock.CurrentTime, () => Is.EqualTo(timeBefore).Within(1));
        }

        /// <summary>
        /// Only a path under way takes the click. With a tool in hand and nothing started, the timeline seeks as it
        /// always has.
        /// </summary>
        [Test]
        public void TestRightClickStillSeeksWhenNothingIsBeingPlaced()
        {
            AddStep("take the slider tool", () => InputManager.Key(Key.Number3));
            moveOntoTheBottomTimeline();

            AddStep("right click", () => InputManager.Click(MouseButton.Right));

            AddUntilStep("the clock moved", () => EditorClock.CurrentTime, () => Is.Not.EqualTo(timeBefore).Within(1));
        }

        private Vector2 inPlayfield(float x, float y) => playfield.ToScreenSpace(OsuPlayfield.BASE_SIZE * new Vector2(x, y));

        private void beginSlider()
        {
            AddStep("take the slider tool", () => InputManager.Key(Key.Number3));
            AddStep("move within the playfield", () => InputManager.MoveMouseTo(inPlayfield(0.3f, 0.5f)));
            AddStep("start the slider", () => InputManager.Click(MouseButton.Left));
            AddUntilStep("a placement is under way", () => EditorBeatmap.PlacementObject.Value, () => Is.Not.Null);
        }

        private void moveOntoTheBottomTimeline()
        {
            AddStep("move onto the bottom timeline", () => InputManager.MoveMouseTo(bottomTimeline.ScreenSpaceDrawQuad.Centre));
            AddStep("remember the time", () => timeBefore = EditorClock.CurrentTime);
        }
    }
}
