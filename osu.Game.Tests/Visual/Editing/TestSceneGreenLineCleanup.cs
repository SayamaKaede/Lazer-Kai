// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Removing the green lines that change nothing: ones saying what the line before them does, and ones saying what
    /// the line after them at the same time does.
    /// </summary>
    /// <remarks>
    /// Renatus [Hard]: the red line at 956 says 60 on the soft set, and nothing changes that until 20736. No object is
    /// between 122274 and 140735, around the break. The line at 140735 says 50 on the soft set up to 151285. The line at
    /// 74719 turns kiai off with no object before the next line. The red line at 119637 says 80 on the soft set, and the
    /// one at 161834 says 65.
    /// </remarks>
    public partial class TestSceneGreenLineCleanup : EditorSavingTestScene
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
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestALineSayingWhatTheOneBeforeDoesGoes()
        {
            addLine("2400,-100,4,2,0,60,0,0");
            cleanUp();

            AddAssert("it is gone", () => editorLines(), () => Has.None.StartsWith("2400,"));
        }

        [Test]
        public void TestALineSayingWhatTheOneAfterItAtTheSameTimeDoesGoes()
        {
            addLineJustBefore("20736,-100,4,2,0,65,0,0");
            cleanUp();

            AddAssert("one of the two is left", () => editorLines().Count(l => l == "20736,-100,4,2,0,65,0,0"), () => Is.EqualTo(1));
        }

        /// <remarks>
        /// Two lines at the same time that say different things leave it unclear which one the mapper meant, so both
        /// stay for the mapper to sort out.
        /// </remarks>
        [Test]
        public void TestLinesAtTheSameTimeSayingDifferentThingsStay()
        {
            addLineJustBefore("20736,-100,4,2,0,30,0,0");
            cleanUp();

            AddAssert("both stay", () => editorLines(), () => Does.Contain("20736,-100,4,2,0,30,0,0").And.Contain("20736,-100,4,2,0,65,0,0"));
        }

        /// <remarks>
        /// Under a red line with no green line, sliders go at 1x.
        /// </remarks>
        [Test]
        public void TestALineSayingWhatTheRedLineAfterItAtTheSameTimeDoesGoes()
        {
            addLineJustBefore("119637,-100,4,2,0,80,0,0");
            cleanUp();

            AddAssert("it is gone", () => editorLines(), () => Has.None.StartsWith("119637,-100,"));
        }

        /// <remarks>
        /// The line before, at 119307, says 70 too, so this one says what the line before it does: it stays all the same,
        /// as the red line at the same time says something else.
        /// </remarks>
        [Test]
        public void TestALineSayingOtherThanTheRedLineAfterItAtTheSameTimeStays()
        {
            addLineJustBefore("119637,-100,4,2,0,70,0,0");
            cleanUp();

            AddAssert("it stays", () => editorLines(), () => Does.Contain("119637,-100,4,2,0,70,0,0"));
        }

        [Test]
        public void TestALineGivingAnotherVelocityThanTheRedLineAfterItAtTheSameTimeStays()
        {
            addLineJustBefore("161834,-80,4,2,0,65,0,0");
            cleanUp();

            AddAssert("it stays", () => editorLines(), () => Does.Contain("161834,-80,4,2,0,65,0,0"));
        }

        /// <remarks>
        /// Stable's own clean-up leaves these: a line nothing reads yet is read by the next object put under it, so
        /// taking it out would change what that object sounds like. Mappers leave them in on purpose, to set the index
        /// back between notes.
        /// </remarks>
        [Test]
        public void TestALineNothingReadsStays()
        {
            addLine("130000,-100,4,2,0,20,0,0");
            cleanUp();

            AddAssert("it stays", () => editorLines(), () => Does.Contain("130000,-100,4,2,0,20,0,0"));
        }

        [Test]
        public void TestALineTurningKiaiOffStays()
        {
            cleanUp();

            AddAssert("it stays", () => editorLines(), () => Does.Contain("74719,-100,4,2,0,70,0,0"));
        }

        [Test]
        public void TestRedLinesStay()
        {
            cleanUp();

            AddAssert("every red line is there", () => timingLines.Lines.Count(l => l.Uninherited), () => Is.EqualTo(4));
        }

        [Test]
        public void TestTheCountIsSaidAndItIsOneStepToUndo()
        {
            int before = 0;

            AddStep("count the lines", () => before = timingLines.Lines.Count);
            addLine("2400,-100,4,2,0,60,0,0");
            addLine("145000,-100,4,2,0,50,0,0");
            cleanUp();

            AddUntilStep("it says two went", () => Game.ChildrenOfType<SimpleNotification>().Any(n => n.Text.ToString() == "Removed 2 unneeded green lines."));

            AddStep("undo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));
            AddAssert("both are back", () => editorLines(), () => Does.Contain("2400,-100,4,2,0,60,0,0").And.Contain("145000,-100,4,2,0,50,0,0"));
            AddAssert("and nothing else changed", () => timingLines.Lines.Count, () => Is.EqualTo(before + 2));
        }

        private SavedTimingLines timingLines => Editor.ChildrenOfType<SavedTimingLines>().Single();

        private string[] editorLines() => timingLines.Lines.Select(l => l.Raw).ToArray();

        private void addLine(string raw) => AddStep($"add {raw}", () =>
        {
            var line = SavedTimingLines.Parse(raw)!.Value;

            EditorBeatmap.BeginChange();
            timingLines.Lines.Insert(timingLines.Lines.Count(l => l.Time <= line.Time), line);
            EditorBeatmap.EndChange();
        });

        /// <summary>
        /// Puts a line in ahead of the lines already at its time.
        /// </summary>
        private void addLineJustBefore(string raw) => AddStep($"add {raw} just before", () =>
        {
            var line = SavedTimingLines.Parse(raw)!.Value;

            EditorBeatmap.BeginChange();
            timingLines.Lines.Insert(timingLines.Lines.Count(l => l.Time < line.Time), line);
            EditorBeatmap.EndChange();
        });

        /// <summary>
        /// Asks for the clean-up from the editor, and confirms it.
        /// </summary>
        private void cleanUp()
        {
            AddStep("ask to remove the unneeded lines", () => Editor.ConfirmRemoveUnneededGreenLines());
            AddUntilStep("the dialog is up", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog is DangerousActionDialog);
            AddStep("confirm", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.PerformAction<PopupDialogDangerousButton>());
        }
    }
}
