// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Framework.Testing;
using osu.Game.Database;
using osu.Game.Graphics.Sprites;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A beatmap from stable, opened and saved with stable compatibility on: what was not edited stays exactly as it
    /// was read.
    /// </summary>
    public partial class TestSceneKeepingStableFiles : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private bool keepStableFiles = true;

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, keepStableFiles);

                // the grid opens on the mapper's own spacing, and a mapper's differs from what a stable beatmap
                // saved: the defaults match the beatmap here and hid that opening it wrote the spacing in.
                Game.LocalConfig.SetValue(OsuSetting.EditorGridDefaultSpacing, 256f);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("settings back to default", () =>
            {
                Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault();
                Game.LocalConfig.GetBindable<float>(OsuSetting.EditorGridDefaultSpacing).SetDefault();
            });
            base.TearDownSteps();
        }

        [Test]
        public void TestSavingWithoutEditingKeepsTheFileAsItWas()
        {
            AddAssert("file is stable's", () => firstLine(originalFile()), () => Is.EqualTo("osu file format v14"));

            SaveEditor();

            AddUntilStep("saved file is the original, byte for byte", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));
        }

        [Test]
        public void TestSavingWithoutEditingLeavesTheBeatmapAsItWasOnline()
        {
            BeatmapOnlineStatus statusBefore = default;

            AddStep("note the status", () => statusBefore = EditorBeatmap.BeatmapInfo.Status);
            AddAssert("not local to begin with", () => statusBefore, () => Is.Not.EqualTo(BeatmapOnlineStatus.LocallyModified));

            SaveEditor();
            AddUntilStep("saved", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));

            AddAssert("status as it was", () => EditorBeatmap.BeatmapInfo.Status, () => Is.EqualTo(statusBefore));
            AddAssert("set not marked local either", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Status, () => Is.Not.EqualTo(BeatmapOnlineStatus.LocallyModified));
        }

        [Test]
        public void TestExportingForStableWithoutEditingGivesTheFileAsItWas()
        {
            AddAssert("exported file is the original, byte for byte", () => exportedFile(), () => Is.EqualTo(originalFile()));
        }

        private byte[] exportedFile()
        {
            var set = imported.BeatmapSet!;
            var file = set.Files.Single(f => f.Filename == difficulty_file);

            var reader = new ExportReader(Game.Storage);
            reader.KeepStableFiles.BindTo(Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility));

            using (var stream = reader.Read(set, file))
                return stream.ReadAllBytesToArray();
        }

        private static byte[] originalFile()
        {
            using (var archive = new ZipArchive(TestResources.GetTestBeatmapStream()))
            using (var stream = archive.GetEntry(difficulty_file)!.Open())
                return stream.ReadAllRemainingBytesToArray();
        }

        private static string firstLine(byte[] file)
        {
            using (var reader = new StreamReader(new MemoryStream(file)))
                return reader.ReadLine()!.Trim();
        }

        private class ExportReader : LegacyBeatmapExporter
        {
            public ExportReader(Storage storage)
                : base(storage)
            {
            }

            public Stream Read(BeatmapSetInfo set, RealmNamedFileUsage file) => GetFileContents(set, file)!;
        }

        [Test]
        public void TestTheStatusSaysWhichFormatTheSaveWrites()
        {
            AddUntilStep("status says stable's", () => shownSaveFormat(), () => Is.EqualTo("Stable v14"));

            makeAnEditThatCannotStayStable();
            SaveEditor();

            // since 2026-09-24 the save asks first; this test is about the status once it has gone ahead.
            AddUntilStep("asked first", () => cannotStayStableDialog(), () => Is.Not.Null);
            AddStep("save in lazer's format", () => cannotStayStableDialog()!.PerformAction<PopupDialogDangerousButton>());

            AddUntilStep("status says lazer's", () => shownSaveFormat(), () => Is.EqualTo("Lazer"));
        }

        [Test]
        public void TestASaveThatCannotStayStableAsksFirst()
        {
            makeAnEditThatCannotStayStable();
            SaveEditor();

            AddUntilStep("asked first", () => cannotStayStableDialog(), () => Is.Not.Null);
            AddAssert("nothing saved yet", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));
            AddAssert("status still says stable's", () => shownSaveFormat(), () => Is.EqualTo("Stable v14"));
        }

        [Test]
        public void TestNotSavingLeavesTheFileAsItWas()
        {
            makeAnEditThatCannotStayStable();
            SaveEditor();

            AddUntilStep("asked first", () => cannotStayStableDialog(), () => Is.Not.Null);
            AddStep("do not save", () => cannotStayStableDialog()!.PerformAction<PopupDialogCancelButton>());

            AddAssert("the file is as it was", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.EqualTo(new MemoryStream(originalFile()).ComputeMD5Hash()));
            AddAssert("still stable's", () => shownSaveFormat(), () => Is.EqualTo("Stable v14"));
            AddAssert("the edit is still there", () => EditorBeatmap.Storyboard.GetLayer(@"Background").Elements.Any(e => e.Path == @"sprite.png"), () => Is.True);
        }

        [Test]
        public void TestSavingInLazersFormatKeepsTheStableFileFirst()
        {
            makeAnEditThatCannotStayStable();
            SaveEditor();

            AddUntilStep("asked first", () => cannotStayStableDialog(), () => Is.Not.Null);
            AddStep("save in lazer's format", () => cannotStayStableDialog()!.PerformAction<PopupDialogDangerousButton>());

            AddUntilStep("saved in lazer's format", () => shownSaveFormat(), () => Is.EqualTo("Lazer"));
            AddUntilStep("the stable file is kept", () => keptFiles().Select(readStored), () => Has.Some.EqualTo(originalFile()));
        }

        /// <summary>
        /// Another difficulty of the set is written to as well, by syncing the bookmarks, the metadata or a background to
        /// it. Where that cannot stay in stable's format it is left as it was: saved in lazer's format without a word, its
        /// green lines went where its objects are (2026-10-23).
        /// </summary>
        [Test]
        public void TestAnotherDifficultyThatCannotStayStableIsLeftAsItWas()
        {
            Guid other = Guid.Empty;
            string? hashBefore = null;
            bool saved = true;

            AddStep("pick another difficulty", () =>
            {
                other = Game.Realm.Run(r => r.Find<BeatmapInfo>(imported.ID)!.BeatmapSet!.Beatmaps.First(b => b.ID != imported.ID).ID);
                hashBefore = Game.Realm.Run(r => r.Find<BeatmapInfo>(other)!.MD5Hash);
            });

            AddStep("write to it what cannot stay stable", () =>
            {
                var working = Game.BeatmapManager.GetWorkingBeatmap(Game.Realm.Run(r => r.Find<BeatmapInfo>(other)!.Detach()));

                saved = StableFileSaving.Save(Game.BeatmapManager, working, true, _ =>
                    working.Storyboard.GetLayer(@"Background").Add(new osu.Game.Storyboards.StoryboardSprite(osu.Game.Storyboards.StoryboardElementSource.Beatmap,
                        @"sprite.png", osu.Framework.Graphics.Anchor.Centre, osuTK.Vector2.Zero)));
            });

            AddAssert("said it was not saved", () => saved, () => Is.False);
            AddAssert("its file is as it was", () => Game.Realm.Run(r => r.Find<BeatmapInfo>(other)!.MD5Hash), () => Is.EqualTo(hashBefore));
        }

        /// <summary>
        /// A storyboard is not written into a stable file, so a save goes to lazer's format. Nothing in the editor changes
        /// one; it stands in for whatever edit cannot be written. (This was a red line, then a background, until those
        /// were written, on 2026-09-23.)
        /// </summary>
        private void makeAnEditThatCannotStayStable() => AddStep("add to the storyboard", () =>
            EditorBeatmap.Storyboard.GetLayer(@"Background").Add(new osu.Game.Storyboards.StoryboardSprite(osu.Game.Storyboards.StoryboardElementSource.Beatmap, @"sprite.png", osu.Framework.Graphics.Anchor.Centre, osuTK.Vector2.Zero)));

        private CannotStayStableDialog? cannotStayStableDialog() => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog as CannotStayStableDialog;

        private string[] keptFiles()
        {
            string directory = EditorAutoBackup.DirectoryFor(EditorBeatmap.BeatmapInfo);
            return Game.Storage.ExistsDirectory(directory) ? Game.Storage.GetFiles(directory, "*.osu").ToArray() : System.Array.Empty<string>();
        }

        private byte[] readStored(string file)
        {
            using (var stream = Game.Storage.GetStream(file))
                return stream.ReadAllBytesToArray();
        }

        private string? shownSaveFormat()
        {
            var status = Editor.ChildrenOfType<ComposeStatusDisplay>().FirstOrDefault();
            var row = status?.ChildrenOfType<OsuSpriteText>().FirstOrDefault(t => t.Text.ToString() == "Format")?.Parent;

            if (row == null || !row.IsPresent)
                return null;

            return row.ChildrenOfType<OsuSpriteText>().Last().Text.ToString();
        }
    }
}
