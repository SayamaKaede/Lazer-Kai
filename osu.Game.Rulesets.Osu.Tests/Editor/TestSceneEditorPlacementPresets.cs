// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Screens.Edit.Presets;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The placement preset list, and what the digits do while it is open.
    /// </summary>
    public partial class TestSceneEditorPlacementPresets : TestSceneOsuEditor
    {
        private PlacementPresetOverlay overlay => Editor.ChildrenOfType<PlacementPresetOverlay>().Single();

        [SetUpSteps]
        public void StartFromNothing()
        {
            AddStep("clear the beatmap", () => EditorBeatmap.Clear());

            // not just anywhere: nothing can be placed before the first timing point, so a click that should have
            // put a circle down does nothing and an assertion about being blocked passes for the wrong reason.
            AddStep("go to the first timing point",
                () => EditorClock.Seek(EditorBeatmap.ControlPointInfo.TimingPoints.First().Time));
            AddStep("empty the list", () =>
            {
                overlay.VisibleList.Presets.Clear();
                overlay.Hide();
            });
        }

        /// <summary>
        /// The digits are the list's while it is open. They are also the tool shortcuts, which sit underneath, and
        /// a number pressed while choosing a shape must not change what is in hand instead.
        /// </summary>
        [Test]
        public void TestTheDigitsDoNotReachTheToolsWhileTheListIsOpen()
        {
            AddStep("take the slider tool", () => InputManager.Key(Key.Number3));

            AddAssert("the slider tool is in hand", () => currentTool, () => Is.TypeOf<SliderCompositionTool>());

            AddStep("open the list", () => overlay.Show());
            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddAssert("the slider tool is still in hand", () => currentTool, () => Is.TypeOf<SliderCompositionTool>());

            // the first build did this by holding focus, and in the editor focus is often elsewhere, so the digits
            // went to the tools anyway. Keeping the list out of the focus chain is the point.
            AddAssert("and not because the list holds focus", () => overlay.HasFocus, () => Is.False);

            AddAssert("the tools can still be clicked", () => toolButtons.All(b => b.Enabled.Value), () => Is.True);

            AddStep("close the list", () => overlay.Hide());
            AddStep("press 2", () => InputManager.Key(Key.Number2));

            AddAssert("the tool changed once the list was closed", () => currentTool, () => Is.Not.TypeOf<SliderCompositionTool>());
        }

        /// <summary>
        /// Only the digits. Which shape to reach for is decided while listening to the part it is going into, so
        /// the song has to keep playing with the list open.
        /// </summary>
        [Test]
        public void TestTheSongStillPlaysWhileTheListIsOpen()
        {
            AddStep("open the list", () => overlay.Show());
            AddStep("press space", () => InputManager.Key(Key.Space));

            AddUntilStep("the song is playing", () => EditorClock.IsRunning);

            AddStep("press space", () => InputManager.Key(Key.Space));

            AddUntilStep("the song stopped", () => !EditorClock.IsRunning);
        }

        /// <summary>
        /// A shape goes down in the middle of the playfield, whatever it looked like where it was kept from.
        /// </summary>
        [Test]
        public void TestAKeptShapeIsPlacedInTheMiddle()
        {
            AddStep("put a circle in a corner", () =>
            {
                EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(20, 20) });
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.Single());
            });

            AddStep("open the list", () => overlay.Show());
            AddStep("keep the selection", () => overlay.KeepSelection());
            AddStep("give it the first digit", () => overlay.VisibleList.Assign(overlay.VisibleList.Presets.Single(), 1));

            AddStep("clear the beatmap", () => EditorBeatmap.Clear());
            AddStep("go somewhere else", () => EditorClock.Seek(6000));

            AddStep("press 1", () => InputManager.Key(Key.Number1));

            AddAssert("a circle arrived", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(1));

            AddAssert("it is in the middle", () => ((IHasPosition)EditorBeatmap.HitObjects.Single()).Position,
                () => Is.EqualTo(OsuPlayfield.BASE_SIZE / 2));

            AddAssert("it is at the playhead, not where it was kept from",
                () => EditorBeatmap.HitObjects.Single().StartTime, () => Is.Not.EqualTo(3000));
        }

        /// <summary>
        /// What arrives is left selected, so whatever is done next is done to the shape that was just placed.
        /// </summary>
        [Test]
        public void TestWhatArrivesIsSelected()
        {
            AddStep("keep a circle", () =>
            {
                EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(20, 20) });
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.Single());
                overlay.Show();
                overlay.KeepSelection();
                overlay.VisibleList.Assign(overlay.VisibleList.Presets.Single(), 1);
                EditorBeatmap.Clear();
            });

            AddStep("press 1", () => InputManager.Key(Key.Number1));

            AddAssert("the new object is selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(1));
        }

        /// <summary>
        /// Escape closes the list rather than leaving the editor, which is what it would otherwise reach.
        /// </summary>
        [Test]
        public void TestEscapeClosesTheList()
        {
            AddStep("open the list", () => overlay.Show());
            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("the list closed", () => overlay.State.Value == Framework.Graphics.Containers.Visibility.Hidden);
        }

        /// <summary>
        /// The bar along the top moves the panel, the way the collab panel's does. The list covers part of the
        /// playfield, and what it is being read against is what is underneath it.
        /// </summary>
        [Test]
        public void TestTheHeaderMovesThePanel()
        {
            Vector2 startPosition = default;
            Vector2 grabPoint = default;

            AddStep("open the list", () => overlay.Show());

            AddStep("grab the header", () =>
            {
                startPosition = overlay.Position;
                grabPoint = overlay.ToScreenSpace(new Vector2(overlay.DrawWidth / 2, 20));
                InputManager.MoveMouseTo(grabPoint);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag it", () => InputManager.MoveMouseTo(grabPoint + new Vector2(60, 40)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the panel moved", () => overlay.Position, () => Is.Not.EqualTo(startPosition));
        }

        /// <summary>
        /// A click that lands on the panel stops there. Without this the tool in hand answers it as well, and a
        /// circle appears behind the list that was being read.
        /// </summary>
        [Test]
        public void TestAClickOnThePanelDoesNotReachTheBeatmap()
        {
            Vector2 point = default;

            AddStep("take the circle tool", () => InputManager.Key(Key.Number2));

            AddStep("work out a spot the list will cover", () =>
            {
                overlay.Show();
                point = overlay.ToScreenSpace(new Vector2(overlay.DrawWidth - 40, overlay.DrawHeight / 2));
                overlay.Hide();
            });

            AddUntilStep("wait for it to go", () => !overlay.IsPresent);

            // established first: an assertion that nothing was placed proves nothing unless something would have
            // been.
            AddStep("move there", () => InputManager.MoveMouseTo(point));
            AddStep("click with the list closed", () => InputManager.Click(MouseButton.Left));

            AddAssert("a circle was placed", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(1));

            AddStep("clear the beatmap", () => EditorBeatmap.Clear());
            AddStep("open the list", () => overlay.Show());

            AddStep("move there again", () => InputManager.MoveMouseTo(point));
            AddStep("click the same spot", () => InputManager.Click(MouseButton.Left));

            AddAssert("nothing was placed this time", () => EditorBeatmap.HitObjects.Count, () => Is.Zero);
        }

        private object currentTool => Editor.ChildrenOfType<ComposeBlueprintContainer>().Single().CurrentTool;

        private IEnumerable<HitObjectCompositionToolButton> toolButtons => Editor.ChildrenOfType<HitObjectCompositionToolButton>();
    }
}
