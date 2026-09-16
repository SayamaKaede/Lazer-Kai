// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.Timing;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The marks for nightcore's cymbal, which belong to the timing screen only.
    /// </summary>
    public partial class TestSceneNightcoreCymbalMarks : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// The marks of one screen's timeline. A screen that is not being shown still holds its own, and asking a piece
        /// whether it is present does not take its parents into account, so each screen is asked separately.
        /// </summary>
        private TimelineNightcoreCymbalDisplay? displayOf<TScreen>() where TScreen : EditorScreen
            => Editor.ChildrenOfType<TScreen>().SingleOrDefault()?.ChildrenOfType<TimelineNightcoreCymbalDisplay>().SingleOrDefault();

        private int marksOn<TScreen>() where TScreen : EditorScreen
        {
            var display = displayOf<TScreen>();

            return display?.Alpha > 0 ? display.ChildrenOfType<TimelineNightcoreCymbalDisplay.CymbalPiece>().Count() : 0;
        }

        [SetUpSteps]
        public void SetUpMarks()
        {
            AddStep("turn the marks on", () => config.SetValue(OsuSetting.EditorTimelineShowNightcoreCymbals, true));
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddStep("go to timing", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddStep("seek to the first timing point", () => EditorClock.Seek(EditorBeatmap.ControlPointInfo.TimingPoints.First().Time));
            AddUntilStep("both timelines are loaded", () => displayOf<TimingScreen>() != null && displayOf<ComposeScreen>() != null);
        }

        [Test]
        public void TestOnlyOnTheTimingScreen()
        {
            AddUntilStep("the timing screen marks the cymbal", () => marksOn<TimingScreen>(), () => Is.GreaterThan(0));
            AddAssert("the compose screen marks nothing", () => marksOn<ComposeScreen>(), () => Is.Zero);
        }

        [Test]
        public void TestNothingWhileTheSettingIsOff()
        {
            AddUntilStep("the timing screen marks the cymbal", () => marksOn<TimingScreen>(), () => Is.GreaterThan(0));

            AddStep("turn the marks off", () => config.SetValue(OsuSetting.EditorTimelineShowNightcoreCymbals, false));
            AddUntilStep("nothing is marked", () => marksOn<TimingScreen>(), () => Is.Zero);
        }

        protected override void Dispose(bool isDisposing)
        {
            config?.SetValue(OsuSetting.EditorTimelineShowNightcoreCymbals, false);
            base.Dispose(isDisposing);
        }
    }
}
