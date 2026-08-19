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
using osu.Game.Screens.Edit.ComboColours;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Tests.Beatmaps;

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

        private void openFromMenu()
            => Editor.ChildrenOfType<EditorMenuBar>().Single()
                     .Items.Single(i => i.Text.Value.Equals(CommonStrings.MenuBarEdit))
                     .Items.Single(i => i.Text.Value.Equals(ComboColourStudioStrings.ComboColourStudio))
                     .Action.Value?.Invoke();

        private int rowCount() => panel().Points.Count;

        private ComboColourStudioOverlay panel() => Editor.ChildrenOfType<ComboColourStudioOverlay>().Single();
    }
}
