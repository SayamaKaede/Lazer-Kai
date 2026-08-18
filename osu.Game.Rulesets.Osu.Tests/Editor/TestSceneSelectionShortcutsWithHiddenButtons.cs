// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Hiding the buttons that sit on a selection must not take the keyboard shortcuts with them.
    /// </summary>
    /// <remarks>
    /// They ran through the button objects, so with the buttons gone there was nothing left to call.
    /// </remarks>
    public partial class TestSceneSelectionShortcutsWithHiddenButtons : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private Vector2[] positionsBefore = null!;

        [TestCase(Key.Period, Description = "rotate clockwise")]
        [TestCase(Key.Comma, Description = "rotate counter-clockwise")]
        [TestCase(Key.G, Description = "reverse")]
        public void TestShortcutStillActsWithButtonsHidden(Key key)
        {
            AddStep("hide the selection buttons", () => config.SetValue(OsuSetting.EditorHideSelectionButtons, true));

            selectFirstThreeObjects();
            press(key);
            AddAssert("selection changed", () => currentPositions().SequenceEqual(positionsBefore), () => Is.False);
        }

        [TestCase(Key.Period)]
        [TestCase(Key.Comma)]
        [TestCase(Key.G)]
        public void TestShortcutStillActsWithButtonsShown(Key key)
        {
            AddStep("show the selection buttons", () => config.SetValue(OsuSetting.EditorHideSelectionButtons, false));

            selectFirstThreeObjects();
            press(key);
            AddAssert("selection changed", () => currentPositions().SequenceEqual(positionsBefore), () => Is.False);
        }

        private void selectFirstThreeObjects()
        {
            AddStep("select first three objects", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects.Take(3));
                positionsBefore = currentPositions();
            });
        }

        private void press(Key key) => AddStep($"press ctrl+{key}", () =>
        {
            InputManager.PressKey(Key.ControlLeft);
            InputManager.Key(key);
            InputManager.ReleaseKey(Key.ControlLeft);
        });

        private Vector2[] currentPositions() => EditorBeatmap.HitObjects.OfType<OsuHitObject>().Take(3).Select(o => o.Position).ToArray();
    }
}
