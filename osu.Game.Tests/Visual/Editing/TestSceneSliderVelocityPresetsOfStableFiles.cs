// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.IO.Compression;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The slider velocity presets of a difficulty kept in stable's format are kept beside its file rather than in it.
    /// </summary>
    public partial class TestSceneSliderVelocityPresetsOfStableFiles : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private bool keepStableFiles = true;

        /// <summary>
        /// Presets kept before the editor opens, if any.
        /// </summary>
        private double[]? keptBefore;

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            // every test is run by the same instance, and one that fails never reaches its tear-down.
            keepStableFiles = true;
            keptBefore = null;

            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, keepStableFiles);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                string path = SliderVelocityPresetStore.PathFor(imported);

                if (Game.Storage.Exists(path))
                    Game.Storage.Delete(path);

                if (keptBefore != null)
                    new SliderVelocityPresetStore(Game.Storage).Save(imported, keptBefore);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("settings back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestAPresetAddedLeavesTheFileAsItWas()
        {
            AddStep("add a preset", () => EditorBeatmap.SliderVelocityPresets.Add(2));
            SaveEditor();

            AddUntilStep("the file is as it was", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));
        }

        [Test]
        public void TestAPresetAddedIsKeptBesideTheFile()
        {
            AddStep("add a preset", () => EditorBeatmap.SliderVelocityPresets.Add(2));
            AddUntilStep("kept", () => kept(), () => Is.EqualTo(new[] { 0.75, 1, 1.5, 2 }));

            SaveEditor();
            ReloadEditorToSameBeatmap();
            AddUntilStep("wait for the editor", () => Editor?.IsLoaded == true);

            AddAssert("there when opened again", () => EditorBeatmap.SliderVelocityPresets, () => Is.EquivalentTo(new[] { 0.75, 1, 1.5, 2 }));
        }

        /// <summary>
        /// Reported on 2026-09-25: a preset added was gone the moment the beatmap was saved.
        /// </summary>
        [Test]
        public void TestAPresetAddedIsStillThereOnceSaved()
        {
            AddStep("add a preset", () => EditorBeatmap.SliderVelocityPresets.Add(2));
            SaveEditor();
            AddWaitStep("let the save settle", 5);

            AddAssert("still there", () => EditorBeatmap.SliderVelocityPresets, () => Does.Contain(2.0));
            AddAssert("and on the button row", () => Editor.ChildrenOfType<osu.Game.Screens.Edit.Timing.SliderVelocityAdjustmentControl>()
                                                          .SelectMany(c => c.ChildrenOfType<osu.Game.Screens.Edit.Components.TernaryButtons.DrawableTernaryButton>())
                                                          .Count(b => b.GetType().Name == @"SliderVelocityPresetTernaryButton"), () => Is.EqualTo(4));
        }

        /// <summary>
        /// Nothing is written for presets that have never left the ones every beatmap starts with; once they have, going
        /// back is written too.
        /// </summary>
        [Test]
        public void TestNothingIsKeptUntilThePresetsChange()
        {
            AddAssert("nothing kept to begin with", () => kept(), () => Is.Null);

            AddStep("remove one", () => EditorBeatmap.SliderVelocityPresets.Remove(1.5));
            AddUntilStep("kept", () => kept(), () => Is.EqualTo(new[] { 0.75, 1 }));

            AddStep("put it back", () => EditorBeatmap.SliderVelocityPresets.Add(1.5));
            AddUntilStep("kept as they started", () => kept(), () => Is.EqualTo(new[] { 0.75, 1, 1.5 }));
        }

        [Test]
        public void TestKeptPresetsAreThereWhenOpened()
        {
            AddStep("presets kept", () => keptBefore = new[] { 0.5, 1, 2.5 });
            base.SetUpSteps();

            AddAssert("they are the editor's", () => EditorBeatmap.SliderVelocityPresets, () => Is.EquivalentTo(new[] { 0.5, 1, 2.5 }));

            // having them is not an edit: saving writes the file as it was.
            SaveEditor();
            AddUntilStep("the file is as it was", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));
        }

        /// <summary>
        /// A beatmap saved in lazer's format has its presets in the file, as it always did, and beside it as well.
        /// </summary>
        [Test]
        public void TestLazersFormatHasThemInBoth()
        {
            AddStep("compatibility off", () => keepStableFiles = false);
            base.SetUpSteps();

            AddStep("add a preset", () => EditorBeatmap.SliderVelocityPresets.Add(2));
            AddUntilStep("kept beside it", () => kept(), () => Is.EqualTo(new[] { 0.75, 1, 1.5, 2 }));

            SaveEditor();
            AddAssert("and in the file", () => savedFileText(), () => Does.Contain(@"VelocityPresets: 0.75,1,1.5,2"));
        }

        /// <summary>
        /// Kept beside a file in lazer's format after it was last saved: they are the newer, and used.
        /// </summary>
        [Test]
        public void TestLazersFormatUsesThoseKeptSinceTheSave()
        {
            AddStep("compatibility off, presets kept", () =>
            {
                keepStableFiles = false;
                keptBefore = new[] { 0.5, 1 };
            });
            base.SetUpSteps();

            AddAssert("they are the editor's", () => EditorBeatmap.SliderVelocityPresets, () => Is.EquivalentTo(new[] { 0.5, 1 }));
        }

        /// <summary>
        /// Saved in lazer's format after they were kept: the file's are the newer, and used.
        /// </summary>
        [Test]
        public void TestLazersFormatUsesTheFilesWhenSavedSince()
        {
            AddStep("compatibility off", () => keepStableFiles = false);
            base.SetUpSteps();

            AddStep("add a preset", () => EditorBeatmap.SliderVelocityPresets.Add(2));
            AddStep("something else kept beside it", () => new SliderVelocityPresetStore(Game.Storage).Save(imported, new[] { 9.0 }));
            SaveEditor();

            ReloadEditorToSameBeatmap();
            AddUntilStep("wait for the editor", () => Editor?.IsLoaded == true);
            AddAssert("the file's", () => EditorBeatmap.SliderVelocityPresets, () => Is.EquivalentTo(new[] { 0.75, 1, 1.5, 2 }));
        }

        private string savedFileText()
        {
            var info = Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!;
            var working = Game.BeatmapManager.GetWorkingBeatmap(info, true);

            using (var stream = working.GetStream(working.BeatmapSetInfo.GetPathForFile(info.Path!)))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd();
        }

        private double[]? kept()
        {
            string path = SliderVelocityPresetStore.PathFor(imported);

            if (!Game.Storage.Exists(path))
                return null;

            using (var stream = Game.Storage.GetStream(path))
            using (var reader = new StreamReader(stream))
                return JsonConvert.DeserializeObject<double[]>(reader.ReadToEnd());
        }

        private static byte[] originalFile()
        {
            using (var archive = new ZipArchive(TestResources.GetTestBeatmapStream()))
            using (var stream = archive.GetEntry(difficulty_file)!.Open())
                return stream.ReadAllRemainingBytesToArray();
        }
    }
}
