// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Resources;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A beatmap kept in stable's format: the lines a save will put in for velocities are shown as they come about, and
    /// sliders are placed at the velocity of the green line at the playhead, as in stable (2026-09-25).
    /// </summary>
    public partial class TestSceneVelocityOfStableFiles : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private SavedTimingLines savedLines => Editor.ChildrenOfType<SavedTimingLines>().Single();

        private EditorClock clock => Editor.ChildrenOfType<EditorClock>().First();

        private OsuSliderVelocityToolboxGroup toolbox =>Editor.ChildrenOfType<OsuSliderVelocityToolboxGroup>().Single();

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                return Game.BeatmapManager.GetWorkingBeatmap(importedSet!.Value.Beatmaps.Single(b => b.Path == @"Soleily - Renatus (Gamu) [Hard].osu"));
            };

            base.SetUpSteps();

            // the beatmap's own sliders there would take lines of their own at 1x, in effect wherever they stand.
            AddStep("nothing between 29000 and 50000", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.Where(h => h.StartTime > 29000 && h.StartTime < 50000).ToList()));

            AddStep("green lines at 2x from 30000 and 0.5x from 40000", () =>
            {
                addLine("30000,-50,4,2,0,60,0,0");
                addLine("40000,-200,4,2,0,60,0,0");
            });
        }

        public override void TearDownSteps()
        {
            AddStep("settings back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestTheVelocityIsTheGreenLinesAtThePlayhead()
        {
            AddStep("to 35000", () => clock.Seek(35000));
            AddUntilStep("2x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(2).Within(0.001));

            // before the beatmap's own line at 43153.
            AddStep("to 41000", () => clock.Seek(41000));
            AddUntilStep("0.5x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(0.5).Within(0.001));
        }

        /// <summary>
        /// One chosen by hand stands while the same line is in effect, and gives way once another is.
        /// </summary>
        [Test]
        public void TestOneChosenByHandStandsUntilAnotherLine()
        {
            AddStep("to 35000", () => clock.Seek(35000));
            AddUntilStep("2x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(2).Within(0.001));

            AddStep("choose 1.2x", () => toolbox.AdjustVelocity(-0.8));
            AddStep("on to 36000, the same line", () => clock.Seek(36000));
            AddWaitStep("give it time", 5);
            AddAssert("still 1.2x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(1.2).Within(0.001));

            AddStep("to 41000, another line", () => clock.Seek(41000));
            AddUntilStep("0.5x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(0.5).Within(0.001));
        }

        [Test]
        public void TestTheLineASaveWillPutInIsShownAtOnce()
        {
            AddStep("a 1.2x slider at 35000.5", () => EditorBeatmap.Add(new Slider
            {
                StartTime = 35000.5,
                Position = new Vector2(100, 200),
                SliderVelocityMultiplier = 1.2,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            }));

            AddUntilStep("a line at 35000 shown", () => savedLines.Shown.Any(l => l.Time == 35000 && System.Math.Abs(l.SliderVelocity - 1.2) < 0.001));
            AddAssert("not yet in the file's", () => savedLines.Lines.Any(l => l.Time == 35000), () => Is.False);

            // what the next slider is placed at, from the playhead on.
            AddStep("to 36000", () => clock.Seek(36000));
            AddUntilStep("1.2x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(1.2).Within(0.001));
        }

        /// <summary>
        /// Reported on 2026-09-25 with a recording: sliders placed one after another at 1.5x under a 0.9x line, and an
        /// undo turned the ones left to 0.9x.
        /// </summary>
        [Test]
        public void TestUndoingOneSliderLeavesTheOthersVelocity()
        {
            Slider first = null!;

            AddStep("a 0.9x line from 30000", () => addLine("30000,-111.11111111111111,4,2,0,60,0,0"));
            AddStep("to 30500", () => clock.Seek(30500));

            AddStep("a 1.5x slider at 31000", () => EditorBeatmap.Add(first = slider(31000, 1.5)));
            AddStep("to 31500", () => clock.Seek(31500));
            AddStep("a 1.5x slider at 32000", () => EditorBeatmap.Add(slider(32000, 1.5)));
            AddStep("to 32500", () => clock.Seek(32500));
            AddWaitStep("give it time", 5);

            AddStep("undo", () => InputManager.Keys(osu.Framework.Input.PlatformAction.Undo));
            AddWaitStep("give it time", 5);

            AddAssert("one slider left there", () => EditorBeatmap.HitObjects.OfType<Slider>().Count(s => s.StartTime > 29000 && s.StartTime < 50000), () => Is.EqualTo(1));
            AddAssert("still 1.5x", () => EditorBeatmap.HitObjects.OfType<Slider>().Single(s => s.StartTime == first.StartTime).SliderVelocityMultiplier, () => Is.EqualTo(1.5));
        }

        /// <summary>
        /// The same, placed with the slider tool the way the recording shows it.
        /// </summary>
        [Test]
        public void TestUndoingOnePlacedSliderLeavesTheOthersVelocity()
        {
            AddStep("a 0.9x line from 30000", () => addLine("30000,-111.11111111111111,4,2,0,60,0,0"));
            AddStep("to 30500", () => clock.Seek(30500));
            AddUntilStep("0.9x", () => toolbox.SliderVelocity.Value, () => Is.EqualTo(0.9).Within(0.001));
            AddStep("choose 1.5x", () => toolbox.AdjustVelocity(0.6));

            AddStep("slider tool", () => InputManager.Key(osuTK.Input.Key.Number3));
            place();
            AddStep("to 31500", () => clock.Seek(31500));
            place();
            AddStep("to 32500", () => clock.Seek(32500));
            place();

            AddAssert("three at 1.5x", () => placed().Select(s => s.SliderVelocityMultiplier), () => Is.EqualTo(new[] { 1.5, 1.5, 1.5 }));

            AddStep("undo", () => InputManager.Keys(osu.Framework.Input.PlatformAction.Undo));
            AddWaitStep("give it time", 5);

            AddAssert("two left at 1.5x", () => placed().Select(s => s.SliderVelocityMultiplier), () => Is.EqualTo(new[] { 1.5, 1.5 }));
        }

        /// <summary>
        /// The green line panel lists the lines a save will put in as well, as the timeline shows them (2026-09-25, with
        /// a recording: they showed on the timeline and not in the panel until saved).
        /// </summary>
        [Test]
        public void TestThePanelListsTheLinesASaveWillPutIn()
        {
            AddStep("a 0.9x line from 30000", () => addLine("30000,-111.11111111111111,4,2,0,60,0,0"));
            AddStep("a 1.5x slider at 31000", () => EditorBeatmap.Add(slider(31000, 1.5)));

            openPanelAt(30500);

            AddUntilStep("listed, to be put in", () => rows().SingleOrDefault(r => r.Line.Time == 31000)?.ToBePutIn, () => Is.True);
            AddAssert("the lines before are not", () => rows().Where(r => r.Line.Time == 30000).Select(r => r.ToBePutIn), () => Is.All.False);
        }

        /// <summary>
        /// Changing one of them takes them all in, and no slider's velocity changes: taking the one alone would put it
        /// in effect over the next slider, whose own line was still to come.
        /// </summary>
        [Test]
        public void TestChangingOneOfThemTakesThemAllIn()
        {
            AddStep("a 0.9x line from 30000", () => addLine("30000,-111.11111111111111,4,2,0,60,0,0"));
            AddStep("a 1.5x slider at 31000", () => EditorBeatmap.Add(slider(31000, 1.5)));
            AddStep("a 0.9x slider at 33000", () => EditorBeatmap.Add(slider(33000, 0.9)));

            openPanelAt(30500);

            AddUntilStep("both listed, to be put in", () => rows().Count(r => r.ToBePutIn), () => Is.EqualTo(2));
            AddStep("select the one at 31000", () => rows().Single(r => r.Line.Time == 31000).TriggerClick());
            AddStep("its volume to 50", () => panel.Volume.Value = 50);

            AddAssert("both in the lines now", () => savedLines.Lines.Count(l => l.Time == 31000 || l.Time == 33000), () => Is.EqualTo(2));
            AddAssert("the one changed", () => savedLines.Lines.Single(l => l.Time == 31000).Volume, () => Is.EqualTo(50));
            AddAssert("the sliders as they were", () => placed().Select(s => s.SliderVelocityMultiplier), () => Is.EqualTo(new[] { 1.5, 0.9 }));
        }

        private osu.Game.Screens.Edit.GreenLines.GreenLinePanel panel => Editor.ChildrenOfType<osu.Game.Screens.Edit.GreenLines.GreenLinePanel>().Single();

        private osu.Game.Screens.Edit.GreenLines.GreenLinePanel.LineRow[] rows() => panel.ChildrenOfType<osu.Game.Screens.Edit.GreenLines.GreenLinePanel.LineRow>().ToArray();

        private void openPanelAt(double time)
        {
            AddStep($"to {time}", () => clock.Seek(time));
            AddStep("press F6", () => InputManager.Key(osuTK.Input.Key.F6));
            AddUntilStep("panel is open", () => panel.State.Value, () => Is.EqualTo(osu.Framework.Graphics.Containers.Visibility.Visible));
        }

        private Slider[] placed() => EditorBeatmap.HitObjects.OfType<Slider>().Where(s => s.StartTime > 29000 && s.StartTime < 50000).ToArray();

        private void place()
        {
            AddStep("to the middle", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<osu.Game.Rulesets.UI.Playfield>().First().ScreenSpaceDrawQuad.Centre));
            AddStep("start", () => InputManager.Click(osuTK.Input.MouseButton.Left));
            AddStep("to the right", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<osu.Game.Rulesets.UI.Playfield>().First().ScreenSpaceDrawQuad.Centre + new Vector2(150, 0)));
            AddStep("end", () => InputManager.Click(osuTK.Input.MouseButton.Right));
        }

        private static Slider slider(double time, double velocity) => new Slider
        {
            StartTime = time,
            Position = new Vector2(100, 200),
            SliderVelocityMultiplier = velocity,
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
        };

        private void addLine(string raw)
        {
            var line = SavedTimingLines.Parse(raw)!.Value;
            int index = savedLines.Lines.Count(l => l.Time <= line.Time);
            savedLines.Lines.Insert(index, line);
        }
    }
}
