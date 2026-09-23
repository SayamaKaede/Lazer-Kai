// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Edit.GreenLines;
using osu.Game.Tests.Resources;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The green line panel, opened with the key that shows what is playing everywhere else, on a beatmap kept in stable's
    /// format.
    /// </summary>
    /// <remarks>
    /// Renatus [Hard]: a red line at 956 with volume 60, then green lines at 20736 (65) and 22054 (70). Between 956 and
    /// 20736 nothing changes the volume.
    /// </remarks>
    public partial class TestSceneGreenLinePanel : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestTheKeyOpensThePanelRatherThanWhatIsPlaying()
        {
            AddStep("press F6", () => InputManager.Key(Key.F6));

            AddUntilStep("panel is open", () => panel.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("what is playing is not", () => Game.ChildrenOfType<NowPlayingOverlay>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("press F6 again", () => InputManager.Key(Key.F6));

            AddUntilStep("panel is closed", () => panel.State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        [Test]
        public void TestAnAddedLineCarriesOnFromTheOneBefore()
        {
            openAt(2109);
            AddStep("add a line", () => panel.AddLine());

            AddAssert("it is there, after the red line", () => editorLines().Take(2), () => Is.EqualTo(new[] { "956,329.67032967033,4,2,0,60,1,0", "2109,-100,4,2,0,60,0,0" }));
            AddAssert("and it is the one being edited", () => panel.Selected?.Time, () => Is.EqualTo(2109));
        }

        [Test]
        public void TestALinesVolumeGoesToTheObjectsUnderIt()
        {
            openAt(2109);
            AddStep("add a line", () => panel.AddLine());
            AddStep("turn it down to 30", () => panel.Volume.Value = 30);

            AddAssert("the circle at it is 30", () => objectAt(2109).Samples.First().Volume, () => Is.EqualTo(30));
            AddAssert("the slider after it is 30 all through", () => slider(2274).Samples.Concat(slider(2274).NodeSamples.SelectMany(n => n)).Select(s => s.Volume).Distinct(),
                () => Is.EqualTo(new[] { 30 }));
            AddAssert("the circle before it is still 60", () => objectAt(1285).Samples.First().Volume, () => Is.EqualTo(60));
            AddAssert("the slider at the next line is still 65", () => slider(20736).Samples.First().Volume, () => Is.EqualTo(65));

            AddStep("confirm", () => panel.Confirm());
            saveAndWait();

            AddAssert("the file has the line and nothing else new", () => addedTimingLines(), () => Is.EqualTo(new[] { "2109,-100,4,2,0,30,0,0" }));
        }

        [Test]
        public void TestALinesVelocityGoesToTheSlidersUnderIt()
        {
            openAt(2109);
            AddStep("add a line", () => panel.AddLine());
            AddStep("double its velocity", () => panel.SliderVelocity.Value = 2);

            AddAssert("the slider after it is twice as fast", () => slider(2274).SliderVelocityMultiplier, () => Is.EqualTo(2).Within(0.001));
            AddAssert("the slider before it is not", () => slider(1450).SliderVelocityMultiplier, () => Is.EqualTo(1).Within(0.001));

            AddStep("confirm", () => panel.Confirm());
            saveAndWait();

            AddAssert("the file has the line and nothing else new", () => addedTimingLines(), () => Is.EqualTo(new[] { "2109,-50,4,2,0,60,0,0" }));
        }

        [Test]
        public void TestRemovingALinePutsBackWhatCameBefore()
        {
            openAt(20736);
            AddAssert("the line at 20736 is the one being edited", () => panel.Selected?.Raw, () => Is.EqualTo("20736,-100,4,2,0,65,0,0"));
            AddStep("remove it", () => panel.RemoveLine());

            AddAssert("it is gone", () => editorLines(), () => Has.None.StartsWith("20736,"));
            AddAssert("the slider at it is back to the red line's 60", () => slider(20736).Samples.First().Volume, () => Is.EqualTo(60));

            AddStep("confirm", () => panel.Confirm());
            saveAndWait();

            AddAssert("the file has lost just that line", () => removedAndAddedTimingLines(), () => Is.EqualTo((new[] { "20736,-100,4,2,0,65,0,0" }, System.Array.Empty<string>())));
        }

        /// <summary>
        /// Opened, the panel picks a line the way stable's does: one standing there, else a green line a few
        /// milliseconds either side, else the last one before.
        /// </summary>
        [TestCase(20736, "20736,-100,4,2,0,65,0,0")]
        [TestCase(20733, "20736,-100,4,2,0,65,0,0")]
        [TestCase(21065, "20736,-100,4,2,0,65,0,0")]
        [TestCase(1000, "956,329.67032967033,4,2,0,60,1,0")]
        public void TestOpeningPicksALineTheWayStableDoes(double time, string picked)
        {
            openAt(time);

            AddAssert("that line is selected", () => panel.Selected?.Raw, () => Is.EqualTo(picked));
            AddAssert("and marked on the timeline", () => Editor.ChildrenOfType<SavedTimingLines>().Single().Selected.Value?.Raw, () => Is.EqualTo(picked));
        }

        /// <summary>
        /// The selection stays where it was put while the playhead moves, and a change goes to it.
        /// </summary>
        [Test]
        public void TestMovingThePlayheadKeepsTheSelection()
        {
            openAt(20736);
            AddStep("go past the sliders", () => clock.Seek(21800));
            AddAssert("the line at 20736 is still selected", () => panel.Selected?.Raw, () => Is.EqualTo("20736,-100,4,2,0,65,0,0"));

            AddStep("turn it down to 30", () => panel.Volume.Value = 30);

            AddAssert("that line changed", () => editorLines(), () => Does.Contain("20736,-100,4,2,0,30,0,0"));
            AddAssert("no line was put in", () => editorLines(), () => Has.None.StartsWith("21800,"));
        }

        /// <summary>
        /// A velocity cannot go on a red line, so setting one with a red line selected puts in a green line beside it.
        /// </summary>
        [Test]
        public void TestAVelocityAtARedLineGoesOnAGreenLineBesideIt()
        {
            openAt(1000);
            AddStep("double the velocity", () => panel.SliderVelocity.Value = 2);

            AddAssert("a green line beside the red one", () => editorLines().Take(2), () => Is.EqualTo(new[] { "956,329.67032967033,4,2,0,60,1,0", "956,-50,4,2,0,60,0,0" }));
            AddAssert("and it is the one selected", () => panel.Selected?.Raw, () => Is.EqualTo("956,-50,4,2,0,60,0,0"));
        }

        [Test]
        public void TestCancellingPutsEverythingBack()
        {
            openAt(2109);
            AddStep("turn it down to 30", () => panel.Volume.Value = 30);
            AddAssert("the circle is 30 while it is pending", () => objectAt(2109).Samples.First().Volume, () => Is.EqualTo(30));

            AddStep("cancel", () => panel.Cancel());

            AddAssert("the lines are the file's", () => editorLines(), () => Is.EqualTo(section(originalText(), "TimingPoints")));
            AddAssert("the circle is 60 again", () => objectAt(2109).Samples.First().Volume, () => Is.EqualTo(60));
            AddAssert("nothing pending", () => panel.Pending, () => Is.False);
        }

        [TestCase(Key.Escape)]
        [TestCase(Key.F6)]
        public void TestClosingPutsEverythingBack(Key key)
        {
            openAt(2274);
            AddStep("double the velocity", () => panel.SliderVelocity.Value = 2);
            AddAssert("the slider is faster while it is pending", () => slider(2274).SliderVelocityMultiplier, () => Is.EqualTo(2).Within(0.001));

            AddStep($"press {key}", () => InputManager.Key(key));

            AddUntilStep("panel is closed", () => panel.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the lines are the file's", () => editorLines(), () => Is.EqualTo(section(originalText(), "TimingPoints")));
            AddAssert("the slider is as it was", () => slider(2274).SliderVelocityMultiplier, () => Is.EqualTo(1).Within(0.001));
        }

        [Test]
        public void TestWhatIsConfirmedIsOneStepToUndo()
        {
            openAt(2109);
            AddStep("turn it down to 30", () => panel.Volume.Value = 30);
            AddStep("turn it down to 20", () => panel.Volume.Value = 20);
            AddStep("go to the slider", () => clock.Seek(2274));
            AddStep("double its velocity", () => panel.SliderVelocity.Value = 2);
            AddStep("confirm", () => panel.Confirm());

            AddStep("undo once", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));

            AddAssert("the lines are the file's", () => editorLines(), () => Is.EqualTo(section(originalText(), "TimingPoints")));
            AddAssert("the circle is 60 again", () => objectAt(2109).Samples.First().Volume, () => Is.EqualTo(60));
            AddAssert("the slider is as it was", () => slider(2274).SliderVelocityMultiplier, () => Is.EqualTo(1).Within(0.001));
        }

        /// <summary>
        /// The volume is set the way the velocity is, by a bar stepping outwards from its middle: 1, 5, 10 and 25.
        /// </summary>
        [Test]
        public void TestTheVolumeStepsOutwardsLikeTheVelocity()
        {
            openAt(2109);
            AddStep("add a line", () => panel.AddLine());

            clickVolumeBar(0.45f);
            AddAssert("the outermost step up is 25", () => panel.Volume.Value, () => Is.EqualTo(85));

            clickVolumeBar(-0.45f);
            AddAssert("the outermost step down is 25", () => panel.Volume.Value, () => Is.EqualTo(60));

            clickVolumeBar(0.05f);
            AddAssert("the innermost step is 1", () => panel.Volume.Value, () => Is.EqualTo(61));

            AddAssert("the line says so", () => editorLines(), () => Does.Contain("2109,-100,4,2,0,61,0,0"));
        }

        /// <summary>
        /// Clicks the volume bar at a distance from its middle, as a share of its width.
        /// </summary>
        private void clickVolumeBar(float fromMiddle) => AddStep($"click the volume bar at {fromMiddle}", () =>
        {
            var bar = panel.ChildrenOfType<osu.Game.Screens.Edit.Timing.DiscreteAdjustmentControl<int>>().Single().ScreenSpaceDrawQuad;
            InputManager.MoveMouseTo(bar.Centre + new osuTK.Vector2((bar.TopRight.X - bar.TopLeft.X) * fromMiddle, 0));
            InputManager.Click(MouseButton.Left);
        });

        /// <summary>
        /// Stable's "use current time": the selected line moves to the playhead, and the objects follow where it now is.
        /// </summary>
        [Test]
        public void TestUsingTheCurrentTimeMovesTheLine()
        {
            openAt(20736);
            AddStep("go to the next slider", () => clock.Seek(21065));
            AddStep("use the current time", () => panel.UseCurrentTime());

            AddAssert("the line is at the playhead", () => editorLines(), () => Does.Contain("21065,-100,4,2,0,65,0,0"));
            AddAssert("and not where it was", () => editorLines(), () => Has.None.StartsWith("20736,"));
            AddAssert("and still selected", () => panel.Selected?.Time, () => Is.EqualTo(21065));
            AddAssert("the slider it left is the red line's 60", () => slider(20736).Samples.First().Volume, () => Is.EqualTo(60));
            AddAssert("the one it came to is still 65", () => slider(21065).Samples.First().Volume, () => Is.EqualTo(65));
        }

        /// <summary>
        /// Kiai goes on the selected line alone, as in stable: from it to the next line.
        /// </summary>
        [Test]
        public void TestKiaiGoesOnTheSelectedLine()
        {
            openAt(20736);
            AddStep("turn kiai on", () => panel.Kiai.Value = true);

            AddAssert("the line says so", () => editorLines(), () => Does.Contain("20736,-100,4,2,0,65,0,1"));
            AddAssert("kiai is on from it", () => EditorBeatmap.ControlPointInfo.EffectPointAt(21000).KiaiMode, () => Is.True);
            AddAssert("and off from the next line", () => EditorBeatmap.ControlPointInfo.EffectPointAt(22100).KiaiMode, () => Is.False);

            AddStep("confirm", () => panel.Confirm());
            saveAndWait();

            AddAssert("the file has just that line changed", () => removedAndAddedTimingLines(),
                () => Is.EqualTo((new[] { "20736,-100,4,2,0,65,0,0" }, new[] { "20736,-100,4,2,0,65,0,1" })));
        }

        [Test]
        public void TestCancellingPutsKiaiBack()
        {
            openAt(20736);
            AddStep("turn kiai on", () => panel.Kiai.Value = true);
            AddStep("cancel", () => panel.Cancel());

            AddAssert("the line is as it was", () => editorLines(), () => Does.Contain("20736,-100,4,2,0,65,0,0"));
            AddAssert("kiai is off there again", () => EditorBeatmap.ControlPointInfo.EffectPointAt(21000).KiaiMode, () => Is.False);
        }

        /// <summary>
        /// The selected line's stretch, from it to the next line, lights up on the timeline at the bottom.
        /// </summary>
        [Test]
        public void TestTheSelectedLinesStretchLightsUpBelow()
        {
            openAt(20736);

            AddUntilStep("the stretch from 20736 to 22054 is lit", () => litStretches(), () => Is.EqualTo(new[] { (20736d, 22054d) }));

            AddStep("close", () => InputManager.Key(Key.Escape));
            AddUntilStep("nothing is lit", () => litStretches(), () => Is.Empty);
        }

        private (double, double)[] litStretches() => Editor.ChildrenOfType<osu.Game.Screens.Edit.Components.Timelines.Summary.Parts.GreenLineRangePart.Band>()
                                                          .Where(b => b.LifetimeEnd == double.MaxValue && b.IsPresent)
                                                          .Select(b => ((double)b.X, (double)(b.X + b.Width)))
                                                          .ToArray();

        [Test]
        public void TestARedLineIsNotMovedHere()
        {
            openAt(1000);
            AddStep("go on", () => clock.Seek(2109));
            AddStep("use the current time", () => panel.UseCurrentTime());

            AddAssert("the red line is where it was", () => editorLines().First(), () => Is.EqualTo("956,329.67032967033,4,2,0,60,1,0"));
        }

        [Test]
        public void TestTheSelectedLineIsScrolledToWhenOpened()
        {
            openAt(119966);

            AddUntilStep("its row is in view", () => selectedRowInView(), () => Is.True);
        }

        [Test]
        public void TestTheSelectedLineIsScrolledToWhenOpenedAgain()
        {
            openAt(2109);
            AddStep("add a line", () => panel.AddLine());
            AddStep("turn it down to 30", () => panel.Volume.Value = 30);
            AddStep("confirm", () => panel.Confirm());
            AddStep("go to the red line", () => clock.Seek(956));
            AddStep("close", () => InputManager.Key(Key.F6));

            // straight away, before it has faded out.
            openAt(185570);

            AddAssert("the line there is selected", () => panel.Selected?.Time, () => Is.EqualTo(185570));
            AddUntilStep("its row is in view", () => selectedRowInView(), () => Is.True);
        }

        [Test]
        public void TestAnAddedLineIsScrolledTo()
        {
            openAt(2109);

            AddStep("go to 119800", () => clock.Seek(119800));
            AddStep("add a line", () => panel.AddLine());

            AddUntilStep("the new line's row is in view", () => selectedRowInView(), () => Is.True);
        }

        /// <summary>
        /// Whether the selected line's row is in the middle third of the list's visible area, where it is scrolled to
        /// rather than just into view. The list's start and end are left out, where there is no scrolling it further.
        /// </summary>
        private bool selectedRowInView()
        {
            var scroll = panel.ChildrenOfType<osu.Game.Graphics.Containers.OsuScrollContainer>().Single();
            var row = panel.ChildrenOfType<GreenLinePanel.LineRow>().SingleOrDefault(r => r.Line == panel.Selected);

            if (row == null)
                return false;

            var visible = scroll.ScreenSpaceDrawQuad.AABBFloat;
            float middle = row.ScreenSpaceDrawQuad.AABBFloat.Centre.Y;

            return middle >= visible.Top + visible.Height / 3 && middle <= visible.Bottom - visible.Height / 3;
        }

        [Test]
        public void TestALineInTheListIsGoneToWhenClicked()
        {
            openAt(2109);
            AddStep("click the line at 22054", () =>
            {
                var row = panel.ChildrenOfType<GreenLinePanel.LineRow>().Single(r => r.Line.Time == 22054);
                row.TriggerClick();
            });

            AddUntilStep("the editor is at it", () => clock.CurrentTime, () => Is.EqualTo(22054).Within(1));
            AddUntilStep("and it is the one being edited", () => panel.Selected?.Raw, () => Is.EqualTo("22054,-100,4,2,0,70,0,0"));
        }

        [Test]
        public void TestARedLineIsNotRemovedHere()
        {
            openAt(1000);
            AddAssert("the red line is the one in effect", () => panel.Selected?.Uninherited, () => Is.True);
            AddStep("try to remove it", () => panel.RemoveLine());

            AddAssert("it is still there", () => editorLines().First(), () => Is.EqualTo("956,329.67032967033,4,2,0,60,1,0"));
        }

        /// <summary>
        /// With the green lines to say what a slider's body plays, its head is set on its own.
        /// </summary>
        [Test]
        public void TestTheHeadAloneChangesWhileLinesCanBeEdited()
        {
            AddStep("seek to the slider at 2274", () => clock.Seek(2274));
            AddStep("click its head's sound", () =>
            {
                InputManager.MoveMouseTo(Editor.ChildrenOfType<NodeSamplePointPiece>().First(p => p.HitObject.StartTime == 2274 && p.NodeIndex == 0));
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("popover is open", () => Editor.ChildrenOfType<SamplePointPiece.SampleEditPopover>().Any(p => p.IsPresent));
            AddStep("set 30", () => Editor.ChildrenOfType<SamplePointPiece.SampleEditPopover>().Single().ChildrenOfType<SamplePointPiece.VolumeControl>().Single().Current.Value = 30);

            AddAssert("the head is 30", () => slider(2274).NodeSamples[0].First().Volume, () => Is.EqualTo(30));
            AddAssert("the body is still 60", () => slider(2274).Samples.First().Volume, () => Is.EqualTo(60));
        }

        [Test]
        public void TestNothingCanBeEditedWithoutCompatibility()
        {
            AddStep("compatibility off", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, false));
            ReloadEditorToSameBeatmap();

            openAt(2109);
            AddAssert("it says it cannot", () => panel.Editable, () => Is.False);
            AddStep("try to add a line", () => panel.AddLine());

            AddAssert("nothing added", () => editorLines(), () => Has.None.StartsWith("2109,"));
            AddAssert("what is playing is still not shown", () => Game.ChildrenOfType<NowPlayingOverlay>().Single().State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        private GreenLinePanel panel => Editor.ChildrenOfType<GreenLinePanel>().Single();

        private EditorClock clock => (EditorClock)Editor.Dependencies.Get(typeof(EditorClock));

        private void openAt(double time)
        {
            AddStep($"seek to {time}", () => clock.Seek(time));
            AddStep("press F6", () => InputManager.Key(Key.F6));
            AddUntilStep("panel is open", () => panel.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        private string[] editorLines() => Editor.ChildrenOfType<SavedTimingLines>().Single().Lines.Select(l => l.Raw).ToArray();

        private HitObject objectAt(double time) => EditorBeatmap.HitObjects.Single(h => h.StartTime == time);

        private Slider slider(double time) => (Slider)objectAt(time);

        private void saveAndWait()
        {
            string before = string.Empty;

            AddStep("note the hash", () => before = EditorBeatmap.BeatmapInfo.MD5Hash);
            SaveEditor();
            AddUntilStep("saved", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.Not.EqualTo(before));
        }

        private string[] addedTimingLines()
        {
            var (removed, added) = removedAndAddedTimingLines();
            return removed.Length > 0 ? new[] { "an original line was lost" } : added;
        }

        private (string[] Removed, string[] Added) removedAndAddedTimingLines()
        {
            var original = section(originalText(), "TimingPoints");
            var saved = section(savedText(), "TimingPoints");

            return (original.Where(l => !saved.Contains(l)).ToArray(), saved.Where(l => !original.Contains(l)).ToArray());
        }

        private static string[] section(string text, string name)
        {
            var lines = text.Split("\r\n");
            int start = System.Array.IndexOf(lines, $"[{name}]");
            return lines.Skip(start + 1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();
        }

        private string savedText() => Game.Realm.Run(r =>
        {
            var beatmap = r.Find<BeatmapInfo>(imported.ID)!;
            var file = beatmap.BeatmapSet!.Files.Single(f => f.File.Hash == beatmap.Hash);

            using (var stream = Game.Storage.GetStorageForDirectory(@"files").GetStream(file.File.GetStoragePath()))
                return Encoding.UTF8.GetString(stream.ReadAllRemainingBytesToArray());
        });

        private static string originalText()
        {
            using (var archive = new ZipArchive(TestResources.GetTestBeatmapStream()))
            using (var stream = archive.GetEntry(difficulty_file)!.Open())
                return Encoding.UTF8.GetString(stream.ReadAllRemainingBytesToArray());
        }
    }
}
