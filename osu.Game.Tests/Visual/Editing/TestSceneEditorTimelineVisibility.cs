// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Timelines.Summary;

namespace osu.Game.Tests.Visual.Editing
{
    [TestFixture]
    public partial class TestSceneEditorTimelineVisibility : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// Turning the timeline off is asking for the beatmap rather than the instruments around it, and the strip
        /// along the bottom is as much of the instruments as the one along the top.
        /// </summary>
        [Test]
        public void TestTurningTheTimelineOffTakesBothOfThem()
        {
            AddStep("turn the timeline on", () => config.SetValue(OsuSetting.EditorShowTimeline, true));
            AddUntilStep("the bottom one is showing", () => summaryTimeline().IsPresent, () => Is.True);

            AddStep("turn it off", () => config.SetValue(OsuSetting.EditorShowTimeline, false));
            AddUntilStep("the bottom one went with it", () => summaryTimeline().IsPresent, () => Is.False);

            AddStep("turn it back on", () => config.SetValue(OsuSetting.EditorShowTimeline, true));
            AddUntilStep("it is back", () => summaryTimeline().IsPresent, () => Is.True);
        }

        /// <summary>
        /// Only the timeline goes. Playback, the clock and the test button are how the editor is operated, and a
        /// mapper who wanted a clear view of the beatmap did not ask to lose them.
        /// </summary>
        [Test]
        public void TestTheRestOfTheBarStays()
        {
            AddStep("turn the timeline off", () => config.SetValue(OsuSetting.EditorShowTimeline, false));
            AddUntilStep("the timeline went", () => summaryTimeline().IsPresent, () => Is.False);

            AddAssert("the test button is still there", () => Editor.ChildrenOfType<TestGameplayButton>().Single().IsPresent, () => Is.True);
            AddAssert("playback is still there", () => Editor.ChildrenOfType<PlaybackControl>().Single().IsPresent, () => Is.True);
        }

        /// <summary>
        /// The reason for turning the timeline off in the first place: a slider whose tail or anchors run under the
        /// bar cannot be dragged, because the bar takes the mouse before the beatmap does. Hidden, its share of the
        /// bar has to be a window onto what is behind rather than part of the bar.
        /// </summary>
        [Test]
        public void TestTheHiddenTimelineStopsTakingTheMouse()
        {
            AddStep("turn the timeline on", () => config.SetValue(OsuSetting.EditorShowTimeline, true));
            AddUntilStep("the bar takes the mouse there", () => barTakesTheMouseOverTheTimeline(), () => Is.True);

            AddStep("turn it off", () => config.SetValue(OsuSetting.EditorShowTimeline, false));
            AddUntilStep("it lets go", () => barTakesTheMouseOverTheTimeline(), () => Is.False);
        }

        /// <summary>
        /// The rest of the bar keeps taking it. Clicking a button there must not fall through and start dragging the
        /// beatmap behind it.
        /// </summary>
        [Test]
        public void TestTheRestOfTheBarKeepsTakingTheMouse()
        {
            AddStep("turn the timeline off", () => config.SetValue(OsuSetting.EditorShowTimeline, false));
            AddUntilStep("the timeline let go", () => barTakesTheMouseOverTheTimeline(), () => Is.False);

            AddAssert("playback still takes it", () =>
            {
                var bar = Editor.ChildrenOfType<BottomBar>().Single();
                var playback = Editor.ChildrenOfType<PlaybackControl>().Single();

                return bar.ReceivePositionalInputAt(playback.ScreenSpaceDrawQuad.Centre);
            }, () => Is.True);
        }

        private bool barTakesTheMouseOverTheTimeline()
        {
            var bar = Editor.ChildrenOfType<BottomBar>().Single();

            return bar.ReceivePositionalInputAt(summaryTimeline().ScreenSpaceDrawQuad.Centre);
        }

        private SummaryTimeline summaryTimeline() => Editor.ChildrenOfType<SummaryTimeline>().Single();
    }
}
