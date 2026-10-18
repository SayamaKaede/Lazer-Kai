// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Events;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Game.Input.Bindings;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The keys for the editor's playback speed (2026-10-18 request), which step through the speeds the bottom bar
    /// offers rather than to speeds of their own.
    /// </summary>
    public partial class TestSceneEditorPlaybackSpeedBinding : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private double speed => Editor.ChildrenOfType<PlaybackControl.PlaybackTabControl>().Single().Current.Value;

        [Test]
        public void TestStepsDownAndBackUp()
        {
            AddAssert("at full speed", () => speed, () => Is.EqualTo(1));

            press(GlobalAction.EditorDecreasePlaybackSpeed);
            AddAssert("at 75%", () => speed, () => Is.EqualTo(0.75));

            press(GlobalAction.EditorDecreasePlaybackSpeed);
            AddAssert("at 50%", () => speed, () => Is.EqualTo(0.5));

            press(GlobalAction.EditorIncreasePlaybackSpeed);
            AddAssert("back at 75%", () => speed, () => Is.EqualTo(0.75));
        }

        [Test]
        public void TestStopsAtEitherEnd()
        {
            for (int i = 0; i < 6; i++)
                press(GlobalAction.EditorIncreasePlaybackSpeed);
            AddAssert("at the fastest", () => speed, () => Is.EqualTo(1.5));

            for (int i = 0; i < 6; i++)
                press(GlobalAction.EditorDecreasePlaybackSpeed);
            AddAssert("at the slowest", () => speed, () => Is.EqualTo(0.25));
        }

        /// <summary>
        /// The speed is taken away while timing, and the keys must not hand it back.
        /// </summary>
        [Test]
        public void TestLeavesTheSpeedAloneWhileTiming()
        {
            AddStep("go to timing", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddUntilStep("at full speed", () => speed, () => Is.EqualTo(1));

            press(GlobalAction.EditorDecreasePlaybackSpeed);
            AddAssert("still at full speed", () => speed, () => Is.EqualTo(1));
        }

        private void press(GlobalAction action) => AddStep($"press {action}",
            () => Editor.OnPressed(new KeyBindingPressEvent<GlobalAction>(new InputState(), action)));
    }
}
