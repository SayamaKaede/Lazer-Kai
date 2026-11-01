// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// What the hitsound screen has that needs stable's format is always there, greyed out until the difficulty is kept
    /// in it, with what is missing said at the top and a button that does it (2026-10-15).
    /// </summary>
    public abstract partial class HitsoundStableFormatTestScene : StableFormatConversionTestScene
    {
        protected const string DIFFICULTY_FILE = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected BeatmapInfo Imported = null!;

        /// <summary>
        /// Whether the option is on when the editor opens.
        /// </summary>
        protected abstract bool CompatibilityAtFirst { get; }

        /// <summary>
        /// Whether the difficulty's file is saved once here first, which makes it lazer's.
        /// </summary>
        protected abstract bool SavedInLazerFormatFirst { get; }

        /// <summary>
        /// Whether the beatmap is one made here from nothing and never saved, rather than the imported difficulty.
        /// </summary>
        protected virtual bool MadeHere => false;

        public override void SetUpSteps()
        {
            if (!MadeHere)
            {
                CreateInitialBeatmap = () =>
                {
                    Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, CompatibilityAtFirst);

                    var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                    Imported = importedSet!.Value.Beatmaps.Single(b => b.Path == DIFFICULTY_FILE);

                    if (SavedInLazerFormatFirst)
                    {
                        var working = Game.BeatmapManager.GetWorkingBeatmap(Imported);
                        Game.BeatmapManager.Save(working.BeatmapInfo, working.GetPlayableBeatmap(working.BeatmapInfo.Ruleset), working.GetSkin());
                    }

                    return Game.BeatmapManager.GetWorkingBeatmap(Game.BeatmapManager.QueryBeatmap(b => b.ID == Imported.ID)!, true);
                };
            }

            base.SetUpSteps();

            // the game is only there once the editor is, so a beatmap made here has the option set after.
            if (MadeHere)
                AddStep("set stable compatibility", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, CompatibilityAtFirst));

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("the hitsound screen is loaded", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
        }

        protected HitsoundPanel Panel => Editor.ChildrenOfType<HitsoundPanel>().Single();

        protected HitsoundPanel.StableFormatNotice Notice => Panel.ChildrenOfType<HitsoundPanel.StableFormatNotice>().Single();

        protected void AssertAsked(StableFormatNeeds needs)
        {
            AddUntilStep($"asked for {needs}", () => Notice.Needs, () => Is.EqualTo(needs));
            AddAssert("the notice is shown", () => Notice.IsPresent);
        }

        protected void AssertNotAsked()
        {
            AddUntilStep("nothing asked for", () => Notice.Needs, () => Is.EqualTo(StableFormatNeeds.None));
            AddAssert("the notice is not shown", () => Notice.IsPresent, () => Is.False);
        }

        /// <summary>
        /// The green line's settings and the list of lines that may be unsnapped: there either way, available only once the
        /// difficulty is kept in stable's format.
        /// </summary>
        protected void AssertStableFormatPartsAre(bool available)
        {
            // where something sounds: with nothing there, an available line has nothing to show and is hidden, as before.
            AddStep("seek to an object", () =>
                ((EditorClock)Editor.Dependencies.Get(typeof(EditorClock))).Seek(EditorBeatmap.HitObjects.ElementAt(10).StartTime));
            AddUntilStep("the parts that need stable's format are all there", () => Panel.ChildrenOfType<HitsoundPanel.StableFormatPart>().ToArray(),
                () => Has.Length.EqualTo(2).And.All.Matches<HitsoundPanel.StableFormatPart>(p => p.IsPresent));
            AddUntilStep(available ? "available" : "greyed out", () => Panel.ChildrenOfType<HitsoundPanel.StableFormatPart>().All(p => p.Available == available));
        }

        /// <summary>
        /// Presses the notice's button, confirms, and waits for the editor to open the difficulty again.
        /// </summary>
        protected void PressAndConfirm()
        {
            Editor? before = null;

            AddStep("press the notice's button", () =>
            {
                before = Editor;
                Notice.ChildrenOfType<RoundedButton>().Single().TriggerClick();
            });
            AddUntilStep("the dialog is up", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog is DangerousActionDialog);
            AddStep("confirm", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.PerformAction<PopupDialogDangerousButton>());
            AddUntilStep("the editor opens the difficulty again", () => Editor != before && Editor?.ReadyForUse == true);
        }
    }

    public partial class TestSceneHitsoundsOnALazerFile : HitsoundStableFormatTestScene
    {
        protected override bool CompatibilityAtFirst => true;

        protected override bool SavedInLazerFormatFirst => true;

        [Test]
        public void TestConvertingIsAskedForAndDone()
        {
            AssertAsked(StableFormatNeeds.Conversion);
            AssertStableFormatPartsAre(false);

            PressAndConfirm();

            AddAssert("file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v14"));
            AddAssert("kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.EqualTo(14));
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AssertNotAsked();
            AssertStableFormatPartsAre(true);
        }

        [Test]
        public void TestWithTheOptionOffBothAreAskedForAndDone()
        {
            AddStep("turn stable compatibility off", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, false));
            AssertAsked(StableFormatNeeds.Compatibility | StableFormatNeeds.Conversion);

            PressAndConfirm();

            AddAssert("the option is on", () => Game.LocalConfig.Get<bool>(OsuSetting.StableCompatibility));
            AddAssert("file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v14"));
            AddAssert("kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.EqualTo(14));
        }
    }

    /// <summary>
    /// A beatmap made here from nothing is in lazer's format, and is asked to be converted the same as any other. Left as
    /// it is rather than saved in stable's format from the start, as for a difficulty added from nothing (2026-11-02).
    /// </summary>
    public partial class TestSceneHitsoundsOnANewBeatmap : HitsoundStableFormatTestScene
    {
        protected override bool CompatibilityAtFirst => true;

        protected override bool SavedInLazerFormatFirst => false;

        protected override bool MadeHere => true;

        [Test]
        public void TestConvertingIsAskedForAndDone()
        {
            AddAssert("not kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Null);
            AssertAsked(StableFormatNeeds.Conversion);

            PressAndConfirm();

            AddAssert("file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v14"));
            AddAssert("kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.EqualTo(14));
            AddAssert("a hitsound difficulty can be made from it", () => Editor.CanMakeHitsoundDifficulty.Value);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AssertNotAsked();
        }
    }

    public partial class TestSceneHitsoundsOnAStableFileWithTheOptionOff : HitsoundStableFormatTestScene
    {
        protected override bool CompatibilityAtFirst => false;

        protected override bool SavedInLazerFormatFirst => false;

        [Test]
        public void TestOnlyTheOptionIsAskedForAndTheFileIsLeftAlone()
        {
            byte[] original = null!;

            AddStep("read the file as it is", () => original = SavedFile());
            AssertAsked(StableFormatNeeds.Compatibility);
            AssertStableFormatPartsAre(false);

            PressAndConfirm();

            AddAssert("the option is on", () => Game.LocalConfig.Get<bool>(OsuSetting.StableCompatibility));
            AddAssert("the file is as it was", () => SavedFile(), () => Is.EqualTo(original));
            AddAssert("kept in stable's format", () => Editor.KeptStableVersion.Value, () => Is.Not.Null);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AssertNotAsked();
            AssertStableFormatPartsAre(true);
        }
    }

    public partial class TestSceneHitsoundsOnAStableFile : HitsoundStableFormatTestScene
    {
        protected override bool CompatibilityAtFirst => true;

        protected override bool SavedInLazerFormatFirst => false;

        [Test]
        public void TestNothingIsAskedFor()
        {
            AssertNotAsked();
            AssertStableFormatPartsAre(true);
        }
    }
}
