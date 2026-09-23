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
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Tests.Resources;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A selected slider shows a point on every tick of the beat divisor inside it, saying the volume there, and setting
    /// one puts a green line on that tick.
    /// </summary>
    /// <remarks>
    /// Renatus [Hard]: the slider at 2274 lasts 329.67ms, a quarter of a beat is 82.42ms, and the red line at 956 says 60
    /// until a green line at 20736.
    /// </remarks>
    public partial class TestSceneLineVolumeTicks : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                return Game.BeatmapManager.GetWorkingBeatmap(importedSet!.Value.Beatmaps.Single(b => b.Path == @"Soleily - Renatus (Gamu) [Hard].osu"));
            };

            base.SetUpSteps();

            AddStep("quarter beats", () => ((BindableBeatDivisor)Editor.Dependencies.Get(typeof(BindableBeatDivisor))).SetArbitraryDivisor(4));
            AddStep("look at the slider at 2274", () => ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(2274));
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        private Slider slider => (Slider)EditorBeatmap.HitObjects.Single(h => h.StartTime == 2274);

        private LineVolumePointPiece[] ticks => Editor.ChildrenOfType<LineVolumePointPiece>().Where(p => p.HitObject == slider && p.IsPresent).ToArray();

        [Test]
        public void TestNothingShowsUntilSelected()
        {
            AddWaitStep("give it time", 5);
            AddAssert("no ticks", () => ticks, () => Is.Empty);
        }

        [Test]
        public void TestASelectedSliderShowsItsTicks()
        {
            AddStep("select it", () => EditorBeatmap.SelectedHitObjects.Add(slider));

            // on the beat from the red line: the slider is written at 2274 but the beat falls at 2274.68, so its quarters
            // do at 2357.1, 2439.5 and 2521.9. The one halfway is where the slider's own point is, so it is left to that.
            AddUntilStep("a tick a quarter in and three quarters in", () => ticks.Select(t => (int)t.TickTime), () => Is.EqualTo(new[] { 2357, 2521 }));
            AddAssert("each says the volume there", () => ticks.Select(t => t.Volume), () => Is.EqualTo(new[] { 60, 60 }));
        }

        [Test]
        public void TestSettingATickPutsAGreenLineThere()
        {
            AddStep("select it", () => EditorBeatmap.SelectedHitObjects.Add(slider));
            AddUntilStep("ticks show", () => ticks.Length, () => Is.EqualTo(2));

            AddStep("click the first tick", () =>
            {
                InputManager.MoveMouseTo(ticks.First());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("popover is open", () => Editor.ChildrenOfType<LineVolumePointPiece.LineVolumePopover>().Any(p => p.IsPresent));
            AddStep("set 30", () => Editor.ChildrenOfType<LineVolumePointPiece.LineVolumePopover>().Single().ChildrenOfType<SamplePointPiece.VolumeControl>().Single().Current.Value = 30);

            AddAssert("a green line on the tick", () => Editor.ChildrenOfType<SavedTimingLines>().Single().Lines.Select(l => l.Raw), () => Does.Contain("2357,-100,4,2,0,30,0,0"));
            AddAssert("the tail after it reads 30", () => slider.NodeSamples[1].First().Volume, () => Is.EqualTo(30));
            AddAssert("the head before it is still 60", () => slider.NodeSamples[0].First().Volume, () => Is.EqualTo(60));
            AddUntilStep("the tick says 30", () => ticks.First().Volume, () => Is.EqualTo(30));
        }
    }
}
