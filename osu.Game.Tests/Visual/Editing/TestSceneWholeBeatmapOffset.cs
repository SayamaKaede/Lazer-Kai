// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.ComboColours;
using osu.Game.Screens.Edit.GreenLines;
using osu.Game.Screens.Edit.Timing;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Moving the whole beatmap in time: everything it has at a time goes by the same amount, and what was just off a tick
    /// is put on it.
    /// </summary>
    /// <remarks>
    /// Renatus [Hard], kept in stable's format, so its lines are the file's: four red lines, the first at 956.
    /// </remarks>
    public partial class TestSceneWholeBeatmapOffset : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                var working = Game.BeatmapManager.GetWorkingBeatmap(importedSet!.Value.Beatmaps.Single(b => b.Path == @"Soleily - Renatus (Gamu) [Hard].osu"));

                keepBesideTheBeatmap(working);
                return working;
            };

            base.SetUpSteps();

            // counted apart from what the tests then do: this beatmap's own share of what is just off a tick.
            // (the circle at 78262, whose tick is at 78263.69.)
            AddStep("put what is just off a tick on it", () => move(0));
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestEverythingGoesByTheSameAmount()
        {
            double[] redLines = null!;
            double[] greenLines = null!;
            double[] timingPoints = null!;
            double[] effectPoints = null!;
            double[] starts = null!;
            double[] ends = null!;
            double[] breaks = null!;
            int[] bookmarks = null!;
            WholeBeatmapOffset.Result result = default;

            AddStep("a bookmark and a preview point", () =>
            {
                EditorBeatmap.Bookmarks.Add(5000);
                EditorBeatmap.PreviewTime.Value = 60000;
            });

            AddStep("remember where everything is", () =>
            {
                redLines = timingLines.Lines.Where(l => l.Uninherited).Select(l => l.Time).ToArray();
                greenLines = timingLines.Lines.Where(l => !l.Uninherited).Select(l => l.Time).ToArray();
                timingPoints = EditorBeatmap.ControlPointInfo.TimingPoints.Select(t => t.Time).ToArray();
                effectPoints = EditorBeatmap.ControlPointInfo.EffectPoints.Select(e => e.Time).ToArray();
                starts = EditorBeatmap.HitObjects.Select(h => h.StartTime).ToArray();
                ends = EditorBeatmap.HitObjects.Select(h => h.GetEndTime()).ToArray();
                breaks = EditorBeatmap.Breaks.SelectMany(b => new[] { b.StartTime, b.EndTime }).ToArray();
                bookmarks = EditorBeatmap.Bookmarks.ToArray();
            });

            AddStep("move it 7 ms later", () => result = move(7));

            AddAssert("there are lines of both kinds", () => redLines.Length == 4 && greenLines.Length > 0 && effectPoints.Length > 0);
            AddAssert("nothing had to be put on a tick", () => result, () => Is.EqualTo(new WholeBeatmapOffset.Result(0, 0)));

            AddAssert("the red lines went", () => timingLines.Lines.Where(l => l.Uninherited).Select(l => l.Time), () => Is.EqualTo(later(redLines, 7)));
            AddAssert("the green lines went", () => timingLines.Lines.Where(l => !l.Uninherited).Select(l => l.Time), () => Is.EqualTo(later(greenLines, 7)));
            AddAssert("the timing points went", () => EditorBeatmap.ControlPointInfo.TimingPoints.Select(t => t.Time), () => Is.EqualTo(later(timingPoints, 7)));
            AddAssert("the kiai went", () => EditorBeatmap.ControlPointInfo.EffectPoints.Select(e => e.Time), () => Is.EqualTo(later(effectPoints, 7)));
            AddAssert("the objects went", () => EditorBeatmap.HitObjects.Select(h => h.StartTime), () => Is.EqualTo(later(starts, 7)).Within(0.001));
            AddAssert("as long as they were", () => EditorBeatmap.HitObjects.Select(h => h.GetEndTime()), () => Is.EqualTo(later(ends, 7)).Within(0.001));
            AddAssert("the breaks went", () => EditorBeatmap.Breaks.SelectMany(b => new[] { b.StartTime, b.EndTime }), () => Is.EqualTo(later(breaks, 7)).Within(0.001));
            AddAssert("the bookmarks went", () => EditorBeatmap.Bookmarks.ToArray(), () => Is.EqualTo(bookmarks.Select(b => b + 7).ToArray()));
            AddAssert("the one put in among them", () => EditorBeatmap.Bookmarks, () => Does.Contain(5007));
            AddAssert("the preview point went", () => EditorBeatmap.PreviewTime.Value, () => Is.EqualTo(60007));
        }

        [Test]
        public void TestItGoesEarlierForANegativeNumber()
        {
            double firstRedLine = 0;
            double firstObject = 0;

            AddStep("remember where it starts", () =>
            {
                firstRedLine = timingLines.Lines.First(l => l.Uninherited).Time;
                firstObject = EditorBeatmap.HitObjects.First().StartTime;
            });

            AddStep("move it 12 ms earlier", () => move(-12));

            AddAssert("the first red line went", () => timingLines.Lines.First(l => l.Uninherited).Time, () => Is.EqualTo(firstRedLine - 12));
            AddAssert("the first object went", () => EditorBeatmap.HitObjects.First().StartTime, () => Is.EqualTo(firstObject - 12).Within(0.001));
        }

        /// <summary>
        /// An object a millisecond or so off a tick is put on it. One further off than that was put there, and stays.
        /// </summary>
        [Test]
        public void TestAnObjectJustOffATickIsPutOnIt()
        {
            HitObject justOff = null!;
            HitObject wellOff = null!;
            double tick = 0;
            double wasWellOff = 0;
            WholeBeatmapOffset.Result result = default;

            AddStep("put one object just off its tick, and another well off", () =>
            {
                var circles = EditorBeatmap.HitObjects.OfType<HitCircle>().Take(2).ToArray();

                justOff = circles[0];
                wellOff = circles[1];

                tick = EditorBeatmap.ControlPointInfo.GetClosestSnappedTime(justOff.StartTime);

                justOff.StartTime = tick + 1.5;
                wellOff.StartTime = wasWellOff = EditorBeatmap.ControlPointInfo.GetClosestSnappedTime(wellOff.StartTime) + 5;
            });

            AddStep("move nothing", () => result = move(0));

            AddAssert("the one just off is on its tick", () => justOff.StartTime, () => Is.EqualTo(tick).Within(0.001));
            AddAssert("the one well off stays", () => wellOff.StartTime, () => Is.EqualTo(wasWellOff));
            AddAssert("one object is said to have been", () => result.Objects, () => Is.EqualTo(1));
        }

        /// <summary>
        /// A green line a millisecond after the slider it was meant for says nothing of that slider's velocity. Put on
        /// the tick, it does, and the slider is given the velocity as it would be read from the file.
        /// </summary>
        [Test]
        public void TestAGreenLineJustOffATickIsPutOnIt()
        {
            Slider slider = null!;
            double tick = 0;
            WholeBeatmapOffset.Result result = default;

            AddStep("a line just after a slider, and one well off any tick", () =>
            {
                slider = EditorBeatmap.HitObjects.OfType<Slider>().First(s => s.SliderVelocityMultiplier != 2);
                tick = EditorBeatmap.ControlPointInfo.GetClosestSnappedTime(slider.StartTime);

                addLine(FormattableString.Invariant($"{Math.Floor(tick) + 1},-50,4,2,0,60,0,0"));
                addLine(FormattableString.Invariant($"{Math.Floor(tick) + 7},-25,4,2,0,60,0,0"));
            });

            AddStep("move nothing", () => result = move(0));

            AddAssert("the line just off is on the tick", () => editorLines(), () => Does.Contain(FormattableString.Invariant($"{Math.Floor(tick)},-50,4,2,0,60,0,0")));
            AddAssert("the slider under it goes at what it says", () => slider.SliderVelocityMultiplier, () => Is.EqualTo(2));
            AddAssert("the line well off stays", () => editorLines(), () => Does.Contain(FormattableString.Invariant($"{Math.Floor(tick) + 7},-25,4,2,0,60,0,0")));
            AddAssert("one line is said to have been", () => result.Lines, () => Is.EqualTo(1));
        }

        [Test]
        public void TestItIsOneStepToUndo()
        {
            string[] lines = null!;
            double[] starts = null!;

            AddStep("remember where everything is", () =>
            {
                lines = editorLines();
                starts = EditorBeatmap.HitObjects.Select(h => h.StartTime).ToArray();
            });

            AddStep("an object just off its tick, as a part of what is undone", () =>
            {
                EditorBeatmap.BeginChange();
                starts[0] = EditorBeatmap.HitObjects.First().StartTime += 1.5;
                EditorBeatmap.EndChange();
            });

            AddStep("move it 7 ms later", () => move(7));
            AddAssert("it went", () => editorLines(), () => Is.Not.EqualTo(lines));

            AddStep("undo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));

            AddAssert("the lines are back", () => editorLines(), () => Is.EqualTo(lines));
            AddAssert("and the objects", () => EditorBeatmap.HitObjects.Select(h => h.StartTime), () => Is.EqualTo(starts).Within(0.001));
        }

        [Test]
        public void TestWhatIsTypedInTheWindowIsApplied()
        {
            double firstRedLine = 0;

            AddStep("remember where it starts", () => firstRedLine = timingLines.Lines.First(l => l.Uninherited).Time);

            AddStep("ask for it from the editor", () => Editor.AdjustWholeBeatmapOffset());
            AddUntilStep("the window is up", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));

            AddStep("type something that is not a number", () => overlay.ChildrenOfType<FormTextBox>().Single().Current.Value = "soon");
            AddAssert("it cannot be applied", () => applyButton.Enabled.Value, () => Is.False);

            AddStep("type -12", () => overlay.ChildrenOfType<FormTextBox>().Single().Current.Value = "-12");
            AddStep("apply", () => applyButton.TriggerClick());

            AddUntilStep("the window is closed", () => overlay.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("the first red line went 12 ms earlier", () => timingLines.Lines.First(l => l.Uninherited).Time, () => Is.EqualTo(firstRedLine - 12));
            AddUntilStep("what was done is said", () => Game.ChildrenOfType<SimpleNotification>().Any(n => n.Text.ToString() == "Moved everything by -12 ms. Put on a tick: 0 objects, 0 green lines."));
        }

        /// <summary>
        /// What the green line panel has pending is held open as one step to undo, and put back as it was when it is
        /// cancelled: a move made in the middle of that would be a part of both.
        /// </summary>
        [Test]
        public void TestItWaitsForWhatTheGreenLinePanelHasPending()
        {
            AddStep("open the green line panel", () => panel.Show());

            // in the break, where no line stands for the panel to select in place of putting one in.
            AddStep("go where no line is", () => Editor.ChildrenOfType<EditorClock>().Single().Seek(130000));
            AddStep("put a line in, which is pending", () => panel.AddLine());
            AddAssert("it is pending", () => panel.Pending);

            AddStep("ask for it from the editor", () => Editor.AdjustWholeBeatmapOffset());

            AddUntilStep("it says to settle the lines first", () => Game.ChildrenOfType<SimpleNotification>().Any(n => n.Text.ToString() == "Confirm or cancel the changes to the green lines first."));
            AddAssert("the window stays closed", () => overlay.State.Value, () => Is.EqualTo(Visibility.Hidden));

            AddStep("cancel the line", () => panel.Cancel());
        }

        /// <summary>
        /// A collaboration is divided at the bookmarks, and who has which stretch is kept by the bookmark it opens on. With
        /// every bookmark taken out and put in again somewhere else, nobody had anything.
        /// </summary>
        [Test]
        public void TestWhoHasWhichStretchOfACollaborationIsKept()
        {
            AddAssert("a stretch is given to somebody", () => collabAssignment(), () => Is.EqualTo(new[] { collabKey }));

            AddStep("move it 7 ms later", () => move(7));
            AddAssert("it is still theirs, where it now opens", () => collabAssignment(), () => Is.EqualTo(new[] { collabKey + 7 }));

            AddStep("undo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));
            AddAssert("and where it opened before", () => collabAssignment(), () => Is.EqualTo(new[] { collabKey }));
        }

        /// <summary>
        /// The colour layout is kept beside the beatmap at times of its own. Left where it was, a point would stand after
        /// the combo it was put on once the beatmap goes earlier.
        /// </summary>
        [Test]
        public void TestTheColourLayoutGoesWithTheBeatmap()
        {
            AddAssert("a colour point is laid out", () => colourPoints(), () => Is.EqualTo(new[] { 30000.0 }));

            AddStep("ask for it from the editor", () => Editor.AdjustWholeBeatmapOffset());
            AddUntilStep("the window is up", () => overlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddStep("type -12", () => overlay.ChildrenOfType<FormTextBox>().Single().Current.Value = "-12");
            AddStep("apply", () => applyButton.TriggerClick());

            AddAssert("the point went 12 ms earlier", () => colourPoints(), () => Is.EqualTo(new[] { 29988.0 }));

            AddStep("undo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));
            AddAssert("it is back", () => colourPoints(), () => Is.EqualTo(new[] { 30000.0 }));

            AddStep("redo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(1));
            AddAssert("and earlier again", () => colourPoints(), () => Is.EqualTo(new[] { 29988.0 }));
        }

        /// <summary>
        /// The bookmark a stretch of the collaboration opens on: one in the middle of the beatmap's.
        /// </summary>
        private int collabKey;

        /// <summary>
        /// Puts a collaboration and a colour layout beside the beatmap before it is opened, which is when the editor reads
        /// them.
        /// </summary>
        private void keepBesideTheBeatmap(WorkingBeatmap working)
        {
            var beatmap = new EditorBeatmap(working.GetPlayableBeatmap(working.BeatmapInfo.Ruleset), beatmapInfo: working.BeatmapInfo);
            int[] bookmarks = beatmap.Bookmarks.OrderBy(b => b).ToArray();

            collabKey = bookmarks[bookmarks.Length / 2];

            var collab = new CollabProject();

            collab.Names.Add(@"A");
            collab.Assignment[collabKey] = @"A";

            new CollabProjectStore(Game.Storage).Save(beatmap, collab);

            var colours = new ComboColourProject();

            colours.Points.Add(new ComboColourPoint { Time = 30000 });

            new ComboColourProjectStore(Game.Storage).Save(beatmap, colours);
        }

        private int[] collabAssignment() => new CollabProjectStore(Game.Storage).Load(EditorBeatmap).Assignment.Keys.ToArray();

        private double[] colourPoints() => Editor.ChildrenOfType<ComboColourStudioOverlay>().Single().Points.Select(p => p.Time).ToArray();

        private SavedTimingLines timingLines => Editor.ChildrenOfType<SavedTimingLines>().Single();

        private WholeBeatmapOffsetOverlay overlay => Editor.ChildrenOfType<WholeBeatmapOffsetOverlay>().Single();

        private GreenLinePanel panel => Editor.ChildrenOfType<GreenLinePanel>().Single();

        // the first of the window's two buttons.
        private RoundedButton applyButton => overlay.ChildrenOfType<RoundedButton>().First();

        private string[] editorLines() => timingLines.Lines.Select(l => l.Raw).ToArray();

        private WholeBeatmapOffset.Result move(int milliseconds) => WholeBeatmapOffset.Move(EditorBeatmap, timingLines, milliseconds, true);

        private static double[] later(double[] times, double by) => times.Select(t => t + by).ToArray();

        private void addLine(string raw)
        {
            var line = SavedTimingLines.Parse(raw)!.Value;

            EditorBeatmap.BeginChange();
            timingLines.Lines.Insert(timingLines.Lines.Count(l => l.Time <= line.Time), line);
            EditorBeatmap.EndChange();
        }
    }
}
