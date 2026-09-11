// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Edit;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Catch.Tests.Editor
{
    /// <summary>
    /// Dragging an object sends one movement per mouse event, and a mouse sends many of those a frame.
    /// </summary>
    /// <remarks>
    /// Each movement used to go through a change of its own, and a change ending runs the whole beatmap processing there
    /// and then — so a drag reprocessed the beatmap once per mouse event rather than once per frame, and the editor
    /// crawled while anything was being dragged (measured 2026-09-11 on a real beatmap: 3942 reprocessings in one drag).
    /// osu!'s selection handler already keeps the same flow out of its movement, for the same reason.
    /// </remarks>
    public partial class TestSceneCatchDragProcessing : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new CatchRuleset();

        private Fruit fruit = null!;
        private Vector2 grabbedAt;
        private int reprocessed;

        [SetUpSteps]
        public void SetUpDrag()
        {
            // a test that fails partway never reaches its teardown, which leaves the button held. Pressing a held button
            // presses nothing, so the next test's drag would never start and it would fail for no reason of its own.
            AddStep("nothing held", () => InputManager.ReleaseButton(MouseButton.Left));
            AddStep("seek to a fruit in the middle", () =>
            {
                fruit = EditorBeatmap.HitObjects.OfType<Fruit>().Skip(EditorBeatmap.HitObjects.OfType<Fruit>().Count() / 2).First();
                EditorClock.Seek(fruit.StartTime);
            });
            AddUntilStep("wait for seek", () => !EditorClock.IsSeeking);
            AddStep("select it", () => EditorBeatmap.SelectedHitObjects.Add(fruit));
            AddUntilStep("its blueprint is showing", () => blueprint() != null);
            AddStep("grab it", () =>
            {
                grabbedAt = blueprint()!.ScreenSpaceSelectionPoint;
                InputManager.MoveMouseTo(grabbedAt);
                InputManager.PressButton(MouseButton.Left);
            });
            AddStep("start counting reprocessings", () =>
            {
                reprocessed = 0;
                EditorBeatmap.BeatmapReprocessed += countReprocessing;
            });
        }

        [TearDownSteps]
        public void TearDownDrag()
        {
            AddStep("stop counting", () => EditorBeatmap.BeatmapReprocessed -= countReprocessing);
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
        }

        [Test]
        public void TestManyMovementsInAFrameReprocessOnce()
        {
            float originalX = 0;

            AddStep("ten movements at once", () =>
            {
                originalX = fruit.OriginalX;

                for (int i = 1; i <= 10; i++)
                    InputManager.MoveMouseTo(grabbedAt + new Vector2(i * 3, 0));
            });
            AddWaitStep("let them be handled", 3);

            AddAssert("the fruit moved", () => fruit.OriginalX, () => Is.Not.EqualTo(originalX));
            AddAssert("the beatmap was reprocessed once, not per movement", () => reprocessed, () => Is.InRange(1, 2));
        }

        /// <summary>
        /// A drag is still one step of undo, however it gets processed along the way.
        /// </summary>
        [Test]
        public void TestUndoTakesTheWholeDragBack()
        {
            float originalX = 0;

            AddStep("remember where it was", () => originalX = fruit.OriginalX);

            AddStep("drag it along", () =>
            {
                for (int i = 1; i <= 5; i++)
                    InputManager.MoveMouseTo(grabbedAt + new Vector2(i * 6, 0));
            });
            AddWaitStep("let the drag be handled", 3);

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
            AddUntilStep("it moved", () => fruit.OriginalX, () => Is.Not.EqualTo(originalX));

            AddStep("undo", () => Editor.Undo());
            AddUntilStep("it is back where it was", () => currentFruit().OriginalX, () => Is.EqualTo(originalX).Within(0.01f));
        }

        private void countReprocessing() => reprocessed++;

        private HitObjectSelectionBlueprint? blueprint() => Editor.ChildrenOfType<HitObjectSelectionBlueprint>().FirstOrDefault(b => b.IsSelected && b.IsPresent);

        /// <summary>
        /// Undo puts back a copy of the beatmap, so the fruit is found again by its time.
        /// </summary>
        private Fruit currentFruit() => EditorBeatmap.HitObjects.OfType<Fruit>().First(f => Math.Abs(f.StartTime - fruit.StartTime) < 1);
    }
}
