// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Stacked objects moved with the grid on land with their positions on it, which is where stable's grid puts them
    /// and what the beatmap holds.
    /// </summary>
    /// <remarks>
    /// The grid used to take hold of where the earliest of the moved objects is drawn. An object a stack's worth off
    /// where it is stored -- the earlier of two stacked circles -- was then stored that far off the grid, and so was
    /// everything moved with it (2026-10-05).
    /// </remarks>
    [TestFixture]
    public partial class TestSceneStacksOnTheGrid : TestSceneOsuEditor
    {
        private const float spacing = 32;

        private OsuPlayfield playfield = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());
            AddStep("seek to the first timing point", () => EditorClock.Seek(Beatmap.Value.Beatmap.ControlPointInfo.TimingPoints.First().Time));

            AddStep("no distance snap", () => Editor.ChildrenOfType<OsuHitObjectComposer>().Single().DistanceSnapProvider.DistanceSnapToggle.Value = TernaryState.False);
            AddStep("enable the grid", () => InputManager.Key(Key.T));
            AddStep("a grid from the middle, every 32 pixels", () =>
            {
                var grid = Editor.ChildrenOfType<OsuGridToolboxGroup>().Single();

                grid.StartPosition.Value = OsuPlayfield.BASE_SIZE / 2;
                grid.GridLinesRotation.Value = 0;
                grid.GridLineSpacing.Value = spacing;
            });
        }

        [Test]
        public void TestAStackMovedOnTheGridLandsOnIt()
        {
            HitCircle first = null!;
            HitCircle second = null!;

            AddStep("add two stacked circles off the grid", () =>
            {
                EditorBeatmap.Add(first = new HitCircle { StartTime = EditorClock.CurrentTime, Position = new Vector2(230, 300) });
                EditorBeatmap.Add(second = new HitCircle { StartTime = EditorClock.CurrentTime + 100, Position = new Vector2(230, 300) });

                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(new[] { first, second });
            });

            AddUntilStep("the first is stacked", () => first.StackHeight, () => Is.GreaterThan(0));

            AddStep("take hold of the later circle", () =>
            {
                InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(second.StackedPosition));
                InputManager.PressButton(MouseButton.Left);
            });

            // towards the grid point at (320, 256), and a hand still moving over it a pixel at a time.
            for (int i = 1; i <= 5; i++)
            {
                Vector2 towards = Vector2.Lerp(new Vector2(230, 300), new Vector2(321, 257), i / 5f);
                AddStep($"drag ({i})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(towards)));
            }

            for (int i = 0; i <= 2; i++)
            {
                Vector2 over = new Vector2(321 - i, 257);

                AddStep($"over the grid point ({i})", () => InputManager.MoveMouseTo(playfield.GamefieldToScreenSpace(over)));
                AddAssert($"the first is on it while held ({i})", () => first.Position, () => Is.EqualTo(new Vector2(320, 256)));
                AddAssert($"the second is on it while held ({i})", () => second.Position, () => Is.EqualTo(new Vector2(320, 256)));
            }

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));

            AddAssert("they are still stacked", () => first.StackHeight, () => Is.GreaterThan(0));
            AddAssert("the first is on the grid point", () => first.Position, () => Is.EqualTo(new Vector2(320, 256)));
            AddAssert("the second is on the grid point", () => second.Position, () => Is.EqualTo(new Vector2(320, 256)));
        }
    }
}
