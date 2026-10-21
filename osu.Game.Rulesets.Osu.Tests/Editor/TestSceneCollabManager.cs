// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The collaboration panel inside the editor.
    /// </summary>
    public partial class TestSceneCollabManager : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        /// <summary>
        /// Input aimed at the panel must stop there. The composer, the toolboxes and the timelines are all still in
        /// the queue underneath it, and would otherwise answer the same click.
        /// </summary>
        [Test]
        public void TestInputDoesNotFallThroughToWhatIsBehind()
        {
            AddStep("open the panel", openFromMenu);
            AddStep("point at the panel", () => InputManager.MoveMouseTo(overlay().Panel));

            AddUntilStep("the panel took the pointer", () => InputManager.HoveredDrawables.Contains(overlay().Panel), () => Is.True);

            AddAssert("nothing behind it did", () =>
            {
                var queue = InputManager.PositionalInputQueue.ToList();
                int panelPosition = queue.IndexOf(overlay().Panel);

                // the queue runs front to back, so anything past the panel is behind it on screen.
                return queue.Skip(panelPosition + 1).Any(d => InputManager.HoveredDrawables.Contains(d));
            }, () => Is.False);
        }

        /// <summary>
        /// The panel thins out while the beatmap behind it is being looked at, and comes back when it is worked in
        /// again -- from anywhere on it, the bar it is dragged by included.
        /// </summary>
        [Test]
        public void TestThePanelComesBackWhenClickedAnywhere()
        {
            AddStep("open the panel", openFromMenu);
            AddUntilStep("solid to begin with", () => overlay().Panel.Alpha, () => Is.EqualTo(1).Within(0.01f));

            AddStep("click on the beatmap behind it", () => clickAt(new Vector2(overlay().Panel.ScreenSpaceDrawQuad.TopLeft.X - 100,
                overlay().Panel.ScreenSpaceDrawQuad.Centre.Y)));
            AddUntilStep("the panel thinned out", () => overlay().Panel.Alpha, () => Is.LessThan(0.5f));

            AddStep("click the bar it is dragged by", () => clickAt(overlay().Panel.ScreenSpaceDrawQuad.TopLeft + new Vector2(20, 20)));
            AddUntilStep("the panel came back", () => overlay().Panel.Alpha, () => Is.EqualTo(1).Within(0.01f));
        }

        /// <summary>
        /// The list says who has what, but not where any of it is. Pointing at a row is how the two are put together.
        /// </summary>
        [Test]
        public void TestPointingAtARowLightsThatStretchOnTheTimeline()
        {
            int bookmark = 0;

            AddStep("divide the beatmap in two", () =>
            {
                double first = EditorBeatmap.HitObjects.Min(h => h.StartTime);
                double last = EditorBeatmap.HitObjects.Max(h => h.GetEndTime());

                // the stretches are between bookmarks (2026-10-18), so the beatmap's two ends are marked as well.
                EditorBeatmap.Bookmarks.AddRange(new[] { (int)first, bookmark = (int)((first + last) / 2), (int)last });
            });

            AddStep("open the panel", openFromMenu);
            AddUntilStep("both stretches listed", () => overlay().ChildrenOfType<CollabBlockRow>().Count(), () => Is.EqualTo(2));

            AddStep("point at the second one", () => InputManager.MoveMouseTo(overlay().ChildrenOfType<CollabBlockRow>().Last()));

            AddUntilStep("the timeline lit up", () => bands().Count(), () => Is.EqualTo(1));
            AddAssert("from where that stretch begins", () => bands().Single().X, () => Is.EqualTo(bookmark).Within(1));

            AddStep("point away from the list", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<EditorMenuBar>().Single()));
            AddUntilStep("the light went out", () => bands().Count(), () => Is.Zero);
        }

        /// <summary>
        /// The other way round: the timeline is where a split is looked at against the song, and pointing at a moment
        /// there has to say whose moment it is.
        /// </summary>
        [Test]
        public void TestPointingAtTheTimelineLightsTheRowThatHoldsIt()
        {
            AddStep("divide the beatmap in two", () =>
            {
                double first = EditorBeatmap.HitObjects.Min(h => h.StartTime);
                double last = EditorBeatmap.HitObjects.Max(h => h.GetEndTime());

                EditorBeatmap.Bookmarks.AddRange(new[] { (int)first, (int)((first + last) / 2), (int)last });
            });

            // the strip belongs to the editor, not to this panel: it answers the pointer only while a split is open.
            AddStep("point near the end of the song", pointAtTheLatterHalf);
            AddWaitStep("let it answer if it is going to", 5);
            AddAssert("the strip says nothing", () => part().PointedAt, () => Is.Null);

            AddStep("open the panel", openFromMenu);
            AddUntilStep("both stretches listed", () => overlay().ChildrenOfType<CollabBlockRow>().Count(), () => Is.EqualTo(2));

            AddStep("point there again", pointAtTheLatterHalf);
            AddUntilStep("the second stretch is named", () => overlay().ChildrenOfType<CollabBlockRow>().Last().Lit, () => Is.True);

            AddStep("close the panel", openFromMenu);
            AddUntilStep("the strip goes back to being a strip", () => litRows(), () => Is.Zero);
        }

        /// <summary>
        /// Handing a stretch over is what the panel is for, and it goes through a list drawn outside the panel that
        /// has to open, be picked from, and be remembered.
        /// </summary>
        [Test]
        public void TestAStretchIsGivenToSomebodyFromTheList()
        {
            AddStep("divide the beatmap in two", () =>
            {
                double first = EditorBeatmap.HitObjects.Min(h => h.StartTime);
                double last = EditorBeatmap.HitObjects.Max(h => h.GetEndTime());

                EditorBeatmap.Bookmarks.AddRange(new[] { (int)first, (int)((first + last) / 2), (int)last });
            });

            AddStep("open the panel", openFromMenu);
            AddUntilStep("both stretches listed", () => overlay().ChildrenOfType<CollabBlockRow>().Count(), () => Is.EqualTo(2));

            AddStep("add a mapper", () => overlay().ChildrenOfType<RoundedButton>()
                                                   .First(b => b.Text.Equals(CollabStrings.AddCollaborator))
                                                   .TriggerClick());

            AddStep("open the list on the second stretch", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<CollabBlockRow>().Last().ChildrenOfType<OsuAnimatedButton>().Last());
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("the list opened", () => overlay().ChildrenOfType<OsuPopover>().Any(), () => Is.True);

            AddStep("pick them", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<OsuPopover>().Single().ChildrenOfType<OsuAnimatedButton>().Last());
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("the stretch is theirs", () => overlay().ChildrenOfType<CollabBlockRow>().Last()
                                                                .ChildrenOfType<OsuSpriteText>().Any(t => t.Text.ToString() == "A"), () => Is.True);
        }

        private void pointAtTheLatterHalf()
        {
            var strip = Editor.ChildrenOfType<SummaryTimeline>().Single().ScreenSpaceDrawQuad;

            InputManager.MoveMouseTo(strip.Centre + new Vector2(strip.Width * 0.4f, 0));
        }

        /// <summary>
        /// Asked to, the strip keeps saying whose stretch the song is in, and keeps saying it after the panel is
        /// closed. What it is for is a recording of the beatmap playing through with the split readable as it goes.
        /// </summary>
        [Test]
        public void TestTheStripCanFollowThePlayheadAfterThePanelIsClosed()
        {
            AddStep("divide the beatmap in two", () =>
            {
                double first = EditorBeatmap.HitObjects.Min(h => h.StartTime);
                double last = EditorBeatmap.HitObjects.Max(h => h.GetEndTime());

                EditorBeatmap.Bookmarks.AddRange(new[] { (int)first, (int)((first + last) / 2), (int)last });
            });

            AddStep("open the panel", openFromMenu);
            AddUntilStep("both stretches listed", () => overlay().ChildrenOfType<CollabBlockRow>().Count(), () => Is.EqualTo(2));

            AddStep("add a mapper", () => overlay().ChildrenOfType<RoundedButton>()
                                                   .First(b => b.Text.Equals(CollabStrings.AddCollaborator))
                                                   .TriggerClick());

            AddStep("open the list on the first stretch", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<CollabBlockRow>().First().ChildrenOfType<OsuAnimatedButton>().Last());
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("the list opened", () => overlay().ChildrenOfType<OsuPopover>().Any(), () => Is.True);

            AddStep("give it to them", () =>
            {
                InputManager.MoveMouseTo(overlay().ChildrenOfType<OsuPopover>().Single().ChildrenOfType<OsuAnimatedButton>().Last());
                InputManager.Click(MouseButton.Left);
            });

            // by whether a stretch has an owner rather than by whose name it is: the panel keeps its split on disk,
            // so which names exist depends on what the tests before this one did.
            AddUntilStep("a stretch has an owner", () => highlight().Bands.Value.Length, () => Is.GreaterThan(0));

            AddStep("go where that stretch is", () => EditorClock.Seek(highlight().Bands.Value.First().Start + 1));

            // nothing until it is asked for: the strip is the editor's, not this panel's.
            AddWaitStep("let it answer if it is going to", 5);
            AddAssert("the strip says nothing yet", () => followingBand(), () => Is.Zero);

            AddStep("ask it to follow", () => overlay().ChildrenOfType<OsuCheckbox>().Single().Current.Value = true);
            AddUntilStep("the stretch under the playhead lit up", () => followingBand(), () => Is.GreaterThan(0));

            AddStep("close the panel", openFromMenu);
            AddUntilStep("the panel is closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("it is still lit", () => followingBand(), () => Is.GreaterThan(0));

            AddStep("open the panel again", openFromMenu);
            AddStep("stop asking", () => overlay().ChildrenOfType<OsuCheckbox>().Single().Current.Value = false);
            AddUntilStep("the light went out", () => followingBand(), () => Is.Zero);
        }

        /// <summary>
        /// The panel is read against the beatmap being composed, and it blocks whatever it covers. Left open on the
        /// design or timing screen it takes their controls with it.
        /// </summary>
        [Test]
        public void TestThePanelClosesWhenAnotherScreenIsOpened()
        {
            AddStep("open the panel", openFromMenu);
            AddUntilStep("shown", () => overlay().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("go to the timing screen", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddUntilStep("closed", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("go back to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddAssert("and stays closed until asked for again", () => overlay().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        private float followingBand() => Editor.ChildrenOfType<CollabPlayheadPart>().Single().ChildrenOfType<Box>().Single().Alpha;

        private CollabHighlight highlight() => (CollabHighlight)Editor.Dependencies.Get(typeof(CollabHighlight));

        private int litRows() => overlay().ChildrenOfType<CollabBlockRow>().Count(r => r.Lit);

        private CollabPart part() => Editor.ChildrenOfType<CollabPart>().Single();

        private IEnumerable<Box> bands() => part().ChildrenOfType<Box>();

        private void clickAt(Vector2 screenSpacePosition)
        {
            InputManager.MoveMouseTo(screenSpacePosition);
            InputManager.Click(MouseButton.Left);
        }

        private void openFromMenu()
            => Editor.ChildrenOfType<EditorMenuBar>().Single()
                     .Items.Single(i => i.Text.Value.Equals(CommonStrings.MenuBarEdit))
                     .Items.Single(i => i.Text.Value.Equals(CollabStrings.MenuItem))
                     .Action.Value?.Invoke();

        private CollabOverlay overlay() => Editor.ChildrenOfType<CollabOverlay>().Single();
    }
}
