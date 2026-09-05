// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.ComboColours;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The studio panel inside the editor.
    /// </summary>
    public partial class TestSceneComboColourStudio : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Test]
        public void TestPanelOpensFromTheEditMenuAndLeavesTheEditorUsable()
        {
            AddAssert("hidden to begin with", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("open it from the edit menu", openFromMenu);
            AddUntilStep("shown", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            // the editor has to stay usable while a layout is being built.
            AddStep("place an object with the panel open", () => EditorBeatmap.Add(new Objects.HitCircle { StartTime = 4000 }));
            AddAssert("the object went in", () => EditorBeatmap.HitObjects.Any(h => h.StartTime == 4000), () => Is.True);

            AddStep("close it again", openFromMenu);
            AddUntilStep("hidden", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void TestPointsSurviveClosingAndReopening()
        {
            AddStep("open it", openFromMenu);
            AddStep("seek somewhere", () => EditorClock.Seek(12345));
            AddStep("add a point there", () => panel().ChildrenOfType<RoundedButton>()
                                                      .First(b => b.Text.Equals(ComboColourStudioStrings.AddPointAtCurrentTime))
                                                      .TriggerClick());

            AddUntilStep("a row appeared", rowCount, () => Is.EqualTo(1));

            AddStep("close", openFromMenu);
            AddStep("open", openFromMenu);
            AddAssert("the row is still there", rowCount, () => Is.EqualTo(1));
        }

        /// <summary>
        /// Input aimed at the panel must stop there. Whatever sits behind it — the composer, a toolbox, a timeline —
        /// is still in the queue underneath, and would otherwise react to the same click.
        /// </summary>
        [Test]
        public void TestInputDoesNotFallThroughToWhatIsBehind()
        {
            AddStep("open the panel", openFromMenu);
            AddStep("point at the panel", () => InputManager.MoveMouseTo(panel()));

            AddUntilStep("the panel took the pointer", () => InputManager.HoveredDrawables.Contains(panel()), () => Is.True);

            AddAssert("nothing behind it did", () =>
            {
                var queue = InputManager.PositionalInputQueue.ToList();
                int panelPosition = queue.IndexOf(panel());

                // the queue runs front to back, so anything past the panel is behind it on screen.
                return queue.Skip(panelPosition + 1).Any(d => InputManager.HoveredDrawables.Contains(d));
            }, () => Is.False);
        }

        /// <summary>
        /// The bar along the top moves the panel, the way the other two panels the edit menu opens do. The panel
        /// covers part of the playfield, and what a layout is being built against is what is underneath it.
        /// </summary>
        [Test]
        public void TestTheHeaderMovesThePanel()
        {
            Vector2 startPosition = default;
            Vector2 grabPoint = default;

            AddStep("open it from the edit menu", openFromMenu);
            AddUntilStep("shown", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("grab the header", () =>
            {
                startPosition = panel().Position;
                grabPoint = panel().ToScreenSpace(new Vector2(panel().DrawWidth / 2, 20));
                InputManager.MoveMouseTo(grabPoint);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag it", () => InputManager.MoveMouseTo(grabPoint + new Vector2(60, 40)));
            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("the panel moved", () => panel().Position, () => Is.Not.EqualTo(startPosition));
        }

        /// <summary>
        /// Escape closes the panel rather than leaving the editor, the way it does on the other two.
        /// </summary>
        [Test]
        public void TestEscapeClosesThePanel()
        {
            AddStep("open it from the edit menu", openFromMenu);
            AddUntilStep("shown", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("press escape", () => InputManager.Key(Key.Escape));

            AddUntilStep("closed", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));

            // escape leaves the editor when nothing has taken it, so this also says the panel took it.
            AddAssert("and the editor is still open", () => Editor.ChildrenOfType<ComboColourStudioOverlay>().Any(), () => Is.True);
        }

        /// <summary>
        /// The panel is read against the beatmap being composed, and it blocks whatever it covers. Left open on the
        /// design or timing screen it takes their controls with it.
        /// </summary>
        [Test]
        public void TestThePanelClosesWhenAnotherScreenIsOpened()
        {
            AddStep("open it from the edit menu", openFromMenu);
            AddUntilStep("shown", () => panel().State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("go to the timing screen", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddUntilStep("closed", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("go back to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddAssert("and stays closed until asked for again", () => panel().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        private void openFromMenu()
            => Editor.ChildrenOfType<EditorMenuBar>().Single()
                     .Items.Single(i => i.Text.Value.Equals(CommonStrings.MenuBarEdit))
                     .Items.Single(i => i.Text.Value.Equals(ComboColourStudioStrings.MenuItem))
                     .Action.Value?.Invoke();

        private int rowCount() => panel().Points.Count;

        private ComboColourStudioOverlay panel() => Editor.ChildrenOfType<ComboColourStudioOverlay>().Single();
    }
}
