// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Edit.Blueprints;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Selecting a lot of a long beatmap, which used to give every selected object a blueprint and its drawable however
    /// far its time was from the screen. With a whole beatmap selected that took seconds and most of every frame after.
    /// </summary>
    /// <remarks>
    /// Only what is on screen has a blueprint now. The rest of the selection is outlined without one, and everything a
    /// selection is used for has to go on reaching all of it.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneLargeSelection : TestSceneOsuEditor
    {
        private const int object_count = 50;

        /// <summary>
        /// Nothing is within a minute of the start, so seeking there leaves the whole selection off screen.
        /// </summary>
        private const double first_time = 60000;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);
            var objects = new List<HitObject>();

            for (int i = 0; i < object_count; i++)
                objects.Add(new HitCircle { StartTime = first_time + i * 2000, Position = new Vector2(40 + i * 37 % 432, 40 + i * 53 % 304) });

            beatmap.HitObjects = objects;
            return beatmap;
        }

        private OsuBlueprintContainer blueprints => Editor.ChildrenOfType<OsuBlueprintContainer>().Single();

        private SelectedObjectOutlines outlines => Editor.ChildrenOfType<SelectedObjectOutlines>().Single();

        private OsuPlayfield playfield => Editor.ChildrenOfType<OsuPlayfield>().First();

        private bool hasBlueprint(HitObject hitObject) => blueprints.SelectionBlueprints.Any(b => ReferenceEquals(b.Item, hitObject));

        [Test]
        public void TestSelectingEverythingOnlyGivesBlueprintsToWhatIsOnScreen()
        {
            AddStep("seek to the first object", () => EditorClock.Seek(first_time));
            AddStep("select everything", () => InputManager.Keys(PlatformAction.SelectAll));

            AddUntilStep("everything is selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(object_count));
            AddUntilStep("the first object has a blueprint", () => hasBlueprint(EditorBeatmap.HitObjects.First()));
            AddAssert("most have none", () => blueprints.SelectionBlueprints.Count, () => Is.LessThan(object_count / 2));
            AddAssert("the rest are outlined", () => outlines.Count + blueprints.SelectionHandler.SelectedBlueprints.Count, () => Is.EqualTo(object_count));
        }

        [Test]
        public void TestSelectionSurvivesSeekingAwayAndBack()
        {
            AddStep("seek to the first object", () => EditorClock.Seek(first_time));
            AddStep("select everything", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            AddUntilStep("the first object has a blueprint", () => hasBlueprint(EditorBeatmap.HitObjects.First()));

            AddStep("seek to the last object", () => EditorClock.Seek(EditorBeatmap.HitObjects.Last().StartTime));
            AddUntilStep("the first object's blueprint is gone", () => !hasBlueprint(EditorBeatmap.HitObjects.First()));
            AddAssert("everything is still selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(object_count));

            AddStep("seek back", () => EditorClock.Seek(first_time));
            AddUntilStep("the first object has a blueprint again", () => hasBlueprint(EditorBeatmap.HitObjects.First()));
            AddAssert("and it shows as selected", () => blueprints.SelectionBlueprints.Single(b => ReferenceEquals(b.Item, EditorBeatmap.HitObjects.First())).IsSelected);
            AddAssert("everything is still selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(object_count));
        }

        [Test]
        public void TestSelectionBoxCoversWhatIsOffScreen()
        {
            AddStep("seek to the first object", () => EditorClock.Seek(first_time));
            AddStep("select everything", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            AddUntilStep("most have no blueprint", () => blueprints.SelectionBlueprints.Count, () => Is.LessThan(object_count / 2));

            AddUntilStep("the box covers every object", () => EditorBeatmap.HitObjects.OfType<OsuHitObject>().All(h =>
                blueprints.ChildrenOfType<SelectionBox>().Single().ScreenSpaceDrawQuad.Contains(playfield.ToScreenSpace(h.Position))));
        }

        [Test]
        public void TestNudgingMovesTheSelectionWithNothingOnScreen()
        {
            Dictionary<OsuHitObject, Vector2> before = null!;

            AddStep("seek to the start", () => EditorClock.Seek(0));
            AddStep("select everything", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            AddUntilStep("nothing has a blueprint", () => blueprints.SelectionBlueprints.Count, () => Is.Zero);

            AddStep("note the positions", () => before = EditorBeatmap.HitObjects.OfType<OsuHitObject>().ToDictionary(h => h, h => h.Position));

            AddStep("nudge right", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Right);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddAssert("everything moved right by one", () => EditorBeatmap.HitObjects.OfType<OsuHitObject>().All(h => h.Position == before[h] + new Vector2(1, 0)));
        }

        /// <summary>
        /// Selecting a pattern, going to another part of the beatmap and dragging the pattern from there
        /// (see <see cref="TestSceneSelectionSurvivesSeekingAway"/>). An outline cannot be dragged, so the object under the
        /// pointer has to have been given a blueprint by the time the button goes down.
        /// </summary>
        [Test]
        public void TestDraggingAnOffScreenObjectMovesTheSelection()
        {
            HitCircle first = null!;
            HitCircle second = null!;
            Vector2 firstBefore = default;
            Vector2 secondBefore = default;

            AddStep("seek to the start", () => EditorClock.Seek(0));
            AddStep("select two objects", () =>
            {
                first = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(0);
                second = EditorBeatmap.HitObjects.OfType<HitCircle>().ElementAt(1);
                firstBefore = first.Position;
                secondBefore = second.Position;

                EditorBeatmap.SelectedHitObjects.AddRange(new HitObject[] { first, second });
            });

            AddUntilStep("both are outlined without blueprints", () => outlines.Count, () => Is.EqualTo(2));
            AddAssert("the first has no blueprint", () => !hasBlueprint(first));

            AddStep("rest the pointer on the first", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(first.Position)));
            AddUntilStep("it has a blueprint to take hold of", () => hasBlueprint(first));

            AddStep("press", () => InputManager.PressButton(MouseButton.Left));

            for (int step = 1; step <= 10; step++)
            {
                int s = step;
                AddStep($"move {s}", () => InputManager.MoveMouseTo(playfield.ToScreenSpace(firstBefore + new Vector2(4 * s, 0))));
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the first moved", () => first.Position.X, () => Is.GreaterThan(firstBefore.X + 10));
            AddAssert("the second moved with it", () => second.Position - secondBefore, () => Is.EqualTo(first.Position - firstBefore));
            AddAssert("both are still selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(2));
        }

        [TearDownSteps]
        public void ReleaseEverything()
        {
            AddStep("release the button", () => InputManager.ReleaseButton(MouseButton.Left));
        }
    }
}
