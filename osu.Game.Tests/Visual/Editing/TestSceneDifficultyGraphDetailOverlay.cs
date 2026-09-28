// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.DifficultyGraph;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    [TestFixture]
    public partial class TestSceneDifficultyGraphDetailOverlay : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// Opening it at all is most of the test. A screen is a pile of containers sizing themselves against one
        /// another, and getting that wrong throws on the update thread rather than looking slightly off — which is
        /// the whole editor gone, from a menu item.
        /// </summary>
        [Test]
        public void TestOpensAndDrawsTheBeatmap()
        {
            AddStep("open the detail screen", () => overlay().Show());

            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("the curve is drawn", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);
            AddUntilStep("the skills are drawn", () => overlay().ChildrenOfType<SkillRow>().Any(), () => Is.True);
        }

        /// <summary>
        /// The rows fold away, and folding them is not allowed to take the screen with it.
        /// </summary>
        [Test]
        public void TestTheSkillRowsFold()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("the skills are drawn", () => overlay().ChildrenOfType<SkillRow>().Any(), () => Is.True);

            // the rows are folded away by hiding what holds them, so the rows themselves are untouched — what has to
            // be true is that nothing of them reaches the screen, and that the room they took goes with them.
            AddStep("fold them away", () => config.SetValue(OsuSetting.EditorDifficultyGraphOverlayShowSkills, false));
            AddUntilStep("they are out of sight", () => overlay().ChildrenOfType<SkillRow>().All(r => !r.Parent!.IsPresent), () => Is.True);

            AddStep("unfold them", () => config.SetValue(OsuSetting.EditorDifficultyGraphOverlayShowSkills, true));
            AddUntilStep("they are back", () => overlay().ChildrenOfType<SkillRow>().All(r => r.Parent!.IsPresent), () => Is.True);
        }

        /// <summary>
        /// Closing and opening again is the ordinary way this is used, and the second open rebuilds everything the
        /// first one built.
        /// </summary>
        [Test]
        public void TestItSurvivesBeingOpenedTwice()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("the curve is drawn", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);

            AddStep("close it", () => overlay().Hide());
            AddStep("open it again", () => overlay().Show());

            AddUntilStep("the curve is drawn again", () => overlay().ChildrenOfType<StackedGraph>().Single().ChildrenOfType<Box>().Any(), () => Is.True);
        }

        /// <summary>
        /// A screen with no way out is worse than no screen. Escape has to close it and leave the editor where it
        /// was, rather than falling through to the editor's own back action and walking out to song select.
        /// </summary>
        [Test]
        public void TestEscapeClosesItRatherThanTheEditor()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("it closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the editor is still here", () => Editor.IsCurrentScreen(), () => Is.True);
        }

        /// <summary>
        /// The close button is the other way out, and the one a mouse reaches for.
        /// </summary>
        [Test]
        public void TestTheCloseButtonCloses()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("click the close button", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<IconButton>().Single());
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("it closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        /// <summary>
        /// The bar at the bottom is deliberately left out of the shade: scrubbing while reading the graph is worth
        /// keeping, and nothing there edits the beatmap.
        /// </summary>
        [Test]
        public void TestTheBottomBarIsStillReachable()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is showing", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddAssert("the shade stops short of the bottom", () =>
            {
                // the shade fills the overlay, and the overlay holds itself off the bottom by the height of the bar.
                var shade = overlay().ChildrenOfType<Box>().First();

                return shade.ScreenSpaceDrawQuad.BottomLeft.Y < overlay().ScreenSpaceDrawQuad.BottomLeft.Y - 1;
            }, () => Is.True);
        }

        /// <summary>
        /// The readout has to describe the moment the cursor is actually over. Reading a graph means putting the
        /// pointer on a peak and asking what it is made of, and an answer taken from somewhere else is worse than no
        /// answer at all — nothing on screen says it is wrong.
        /// </summary>
        [TestCase(0.25f)]
        [TestCase(0.5f)]
        [TestCase(0.8f)]
        public void TestTheReadoutFollowsTheCursor(float across)
        {
            AddStep("open the detail screen", () => overlay().Show());
            waitForTheBeatmapToBeWorkedOut();

            AddStep($"point at {across:P0} across", () =>
            {
                var quad = graph().ScreenSpaceDrawQuad;
                InputManager.MoveMouseTo(new Vector2(quad.TopLeft.X + quad.Width * across, quad.Centre.Y));
            });

            AddUntilStep("it read somewhere", () => graph().HoveredTime.Value, () => Is.Not.Null);

            AddAssert("it read the moment under the cursor",
                () => graph().HoveredTime.Value!.Value,
                () => Is.EqualTo(Beatmap.Value.Track.Length * across).Within(Beatmap.Value.Track.Length * 0.01));
        }

        /// <summary>
        /// And clicking has to land where the cursor is, for the same reason and by the same reckoning.
        /// </summary>
        [TestCase(0.3f)]
        [TestCase(0.6f)]
        public void TestClickingSeeksToWhereTheCursorIs(float across)
        {
            AddStep("open the detail screen", () => overlay().Show());
            waitForTheBeatmapToBeWorkedOut();

            AddStep($"click {across:P0} across", () =>
            {
                var quad = graph().ScreenSpaceDrawQuad;
                InputManager.MoveMouseTo(new Vector2(quad.TopLeft.X + quad.Width * across, quad.Centre.Y));
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("playback went there",
                () => EditorClock.CurrentTime,
                () => Is.EqualTo(Beatmap.Value.Track.Length * across).Within(Beatmap.Value.Track.Length * 0.01));
        }

        /// <summary>
        /// The readout across the top describes where playback is, without being asked. It follows the clock rather
        /// than the pointer, so it is still right after seeking with a hand nowhere near the graph.
        /// </summary>
        [Test]
        public void TestTheTopReadoutFollowsPlayback()
        {
            AddStep("open the detail screen", () => overlay().Show());
            waitForTheBeatmapToBeWorkedOut();

            AddStep("seek to a third in", () => EditorClock.Seek(Beatmap.Value.Track.Length / 3));
            AddUntilStep("it says so", () => readoutTime(), () => Is.EqualTo((Beatmap.Value.Track.Length / 3).ToEditorFormattedString()));

            AddStep("seek to two thirds in", () => EditorClock.Seek(Beatmap.Value.Track.Length * 2 / 3));
            AddUntilStep("it moved with it", () => readoutTime(), () => Is.EqualTo((Beatmap.Value.Track.Length * 2 / 3).ToEditorFormattedString()));
        }

        /// <summary>
        /// And it says what the beatmap is made of there, not only when.
        /// </summary>
        [Test]
        public void TestTheTopReadoutNamesEverySkill()
        {
            AddStep("open the detail screen", () => overlay().Show());
            waitForTheBeatmapToBeWorkedOut();

            AddAssert("every skill is named", () => overlay().ChildrenOfType<PlayheadReadout>().Single()
                                                            .ChildrenOfType<OsuSpriteText>().Select(t => t.Text.ToString()),
                () => Does.Contain("Aim").And.Contains("Speed"));
        }

        /// <summary>
        /// The screen can be made see-through so the beatmap behind it can be looked at without closing it. Showing
        /// and hiding still runs from nothing to whatever was set, rather than to solid.
        /// </summary>
        [Test]
        public void TestTheScreenCanBeSeenThrough()
        {
            AddStep("open the detail screen", () => overlay().Show());
            AddUntilStep("it is solid", () => shade().Alpha, () => Is.EqualTo(1));

            AddStep("half it", () => config.SetValue(OsuSetting.EditorDifficultyGraphOverlayOpacity, 0.5f));
            AddUntilStep("it is see-through", () => shade().Alpha, () => Is.EqualTo(0.5f).Within(0.01f));

            AddStep("close it", () => overlay().Hide());
            AddStep("open it again", () => overlay().Show());
            AddUntilStep("it came back see-through", () => shade().Alpha, () => Is.EqualTo(0.5f).Within(0.01f));

            AddAssert("and still takes the mouse", () => overlay().ReceivePositionalInputAt(overlay().ScreenSpaceDrawQuad.Centre), () => Is.True);
        }

        /// <summary>
        /// The graph has boxes of its own — its background — before anything is worked out, so they say nothing about
        /// whether the beatmap has been. The rows come with the first thing drawn from it. Waiting on the boxes let the
        /// tests read the readout ahead of the calculation, which held only as long as the calculation was quick.
        /// </summary>
        private void waitForTheBeatmapToBeWorkedOut() =>
            AddUntilStep("the beatmap is worked out", () => overlay().ChildrenOfType<SkillRow>().Any(), () => Is.True);

        private Drawable shade() => overlay().ChildrenOfType<Box>().First().Parent!;

        private string readoutTime() => overlay().ChildrenOfType<PlayheadReadout>().Single()
                                                 .ChildrenOfType<OsuSpriteText>().First().Text.ToString();

        private StackedGraph graph() => overlay().ChildrenOfType<StackedGraph>().Single();

        private DifficultyGraphDetailOverlay overlay() => Editor.ChildrenOfType<DifficultyGraphDetailOverlay>().Single();
    }
}
