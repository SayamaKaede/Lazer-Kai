// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The saved file's red and green lines, drawn across the timeline.
    /// </summary>
    public partial class TestSceneTimelineSavedLines : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private SavedTimingLines savedLines => Editor.ChildrenOfType<SavedTimingLines>().Single();

        private TimelineSavedLineDisplay.LinePiece[] pieces =>
            Editor.ChildrenOfType<TimelineSavedLineDisplay>().SelectMany(d => d.ChildrenOfType<TimelineSavedLineDisplay.LinePiece>()).ToArray();

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("lines hidden again", () => config.SetValue(OsuSetting.EditorTimelineShowSavedLines, false));
            AddStep("lazer's timeline again", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer));
        }

        [Test]
        public void TestTheFilesLinesAreDrawnWhereTheyStand()
        {
            AddStep("stable's timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddStep("show the lines", () => config.SetValue(OsuSetting.EditorTimelineShowSavedLines, true));
            AddStep("a file with a red and a green line", () => savedLines.Update(fileWith("1000,500,4,2,1,60,1,0", "1500,-50,4,3,2,5,0,1")));
            AddStep("look at the start", () => EditorClock.Seek(1200));

            AddUntilStep("both are drawn", () => pieces.Select(p => (p.Line.Time, p.Line.Uninherited)), () => Is.EquivalentTo(new[] { (1000d, true), (1500d, false) }));
            AddAssert("the green one says what it holds",
                () => pieces.Single(p => !p.Line.Uninherited).TooltipText.ToString(),
                () => Does.Contain("SV x2").And.Contain("Drum #2").And.Contain("5%").And.Contain("Kiai"));
        }

        [Test]
        public void TestNothingIsDrawnWhileHidden()
        {
            AddStep("stable's timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddStep("a file with a red line", () => savedLines.Update(fileWith("1000,500,4,2,1,60,1,0")));
            AddStep("look at it", () => EditorClock.Seek(1000));
            AddWaitStep("give it time", 5);

            AddAssert("nothing drawn", () => pieces, () => Is.Empty);
        }

        /// <summary>
        /// Lazer's own timeline runs its ticks the full height, where a red line could not be told from a half-beat tick.
        /// </summary>
        [TestCase(EditorTimelineStyle.Lazer, false)]
        [TestCase(EditorTimelineStyle.Stable, true)]
        [TestCase(EditorTimelineStyle.LazerKai, true)]
        public void TestDrawnOnlyWhereTheTicksStayDown(EditorTimelineStyle style, bool drawn)
        {
            AddStep($"{style} timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, style));
            AddStep("show the lines", () => config.SetValue(OsuSetting.EditorTimelineShowSavedLines, true));
            AddStep("a file with a red line", () => savedLines.Update(fileWith("1000,500,4,2,1,60,1,0")));
            AddStep("look at it", () => EditorClock.Seek(1000));
            AddWaitStep("give it time", 5);

            AddAssert(drawn ? "drawn" : "not drawn", () => pieces.Length, () => Is.EqualTo(drawn ? 1 : 0));
        }

        [Test]
        public void TestSavingPutsTheWrittenLinesUp()
        {
            AddStep("show the lines", () => config.SetValue(OsuSetting.EditorTimelineShowSavedLines, true));
            AddStep("no lines yet", () => savedLines.Lines.Clear());

            AddStep("save", () => InputManager.Keys(PlatformAction.Save));

            AddUntilStep("the saved red line is there", () => savedLines.Lines.Any(l => l.Uninherited));
        }

        private static byte[] fileWith(params string[] timingPoints) =>
            System.Text.Encoding.UTF8.GetBytes("osu file format v14\r\n\r\n[TimingPoints]\r\n" + string.Join("\r\n", timingPoints) + "\r\n");
    }
}
