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
    /// Removing the green lines that change nothing: ones saying what the line before them does, and ones another line
    /// at the same time replaces.
    /// </summary>
    /// <remarks>
    /// Renatus [Hard]: the red line at 956 says 60 on the soft set, and nothing changes that until 20736. No object is
    /// between 122274 and 140735, around the break. The line at 140735 says 50 on the soft set up to 151285. The line at
    /// 74719 turns kiai off with no object before the next line.
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
        public void TestALineReplacedAtTheSameTimeGoes()
        {
            AddStep("put a line in just before the one at 20736", () =>
            {
                var lines = timingLines.Lines;
                lines.Insert(lines.IndexOf(lines.Single(l => l.Time == 20736)), SavedTimingLines.Parse("20736,-100,4,2,0,30,0,0")!.Value);
            });
            cleanUp();

            AddAssert("it is gone", () => editorLines(), () => Has.None.EqualTo("20736,-100,4,2,0,30,0,0"));
            AddAssert("the one after it stays", () => editorLines(), () => Does.Contain("20736,-100,4,2,0,65,0,0"));
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
