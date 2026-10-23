// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
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
    /// With stable compatibility on, the editor keeps a copy of the difficulty's file exactly as it was when opened,
    /// among the backups it writes while editing: whatever a save then does to the file, what came in is there.
    /// </summary>
    public partial class TestSceneKeepingTheFileAsOpened : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private bool keepStableFiles = true;

        /// <summary>
        /// Files put in the folder before the editor opens, oldest first.
        /// </summary>
        private int olderCopies;

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            // every test is run by the same instance, and one that fails never reaches its tear-down.
            keepStableFiles = true;
            olderCopies = 0;

            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, keepStableFiles);
                Game.LocalConfig.SetValue(OsuSetting.EditorAutoBackupKeepCount, 5);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                foreach (string file in copies())
                    Game.Storage.Delete(file);

                for (int i = 0; i < olderCopies; i++)
                {
                    using (var stream = Game.Storage.CreateFileSafely(Path.Combine(directory(), $"2000-01-0{i + 1}_00-00-00.osu")))
                        stream.Write(new byte[] { (byte)i });
                }

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("settings back to default", () =>
            {
                Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault();
                Game.LocalConfig.GetBindable<int>(OsuSetting.EditorAutoBackupKeepCount).SetDefault();
            });
            base.TearDownSteps();
        }

        [Test]
        public void TestOpeningKeepsTheFileAsItWas()
        {
            AddUntilStep("one copy is kept", () => copies().Length, () => Is.EqualTo(1));
            AddAssert("it is the file, byte for byte", () => read(copies().Single()), () => Is.EqualTo(originalFile()));
        }

        [Test]
        public void TestOpeningTheSameFileAgainKeepsNoSecondCopy()
        {
            AddUntilStep("one copy is kept", () => copies().Length, () => Is.EqualTo(1));

            ReloadEditorToSameBeatmap();
            AddUntilStep("wait for the editor", () => Editor?.IsLoaded == true);
            AddWaitStep("give it time to write one", 20);

            AddAssert("still one", () => copies().Length, () => Is.EqualTo(1));
        }

        [Test]
        public void TestNothingIsKeptWithCompatibilityOff()
        {
            AddStep("compatibility off", () => keepStableFiles = false);
            base.SetUpSteps();

            AddWaitStep("give it time to write one", 20);
            AddAssert("nothing kept", () => copies(), () => Is.Empty);
        }

        /// <summary>
        /// As many are kept as the backups' own setting says, counted with the backups written while editing.
        /// </summary>
        [Test]
        public void TestAsManyAreKeptAsTheBackupsSettingSays()
        {
            AddStep("six older copies", () => olderCopies = 6);
            base.SetUpSteps();

            AddUntilStep("five are left", () => copies().Length, () => Is.EqualTo(5));
            AddAssert("the newest is the file", () => read(copies().OrderByDescending(f => f).First()), () => Is.EqualTo(originalFile()));
        }

        /// <summary>
        /// The names are the time to the second, so a backup asked for in the same second as the copy kept on opening
        /// was written over it (2026-10-23). Files are put in under the names of the next few seconds, which the backup
        /// would otherwise take one of.
        /// </summary>
        [Test]
        public void TestABackupInTheSameSecondWritesOverNothing()
        {
            string[] taken = null!;
            string opened = null!;

            AddUntilStep("one copy is kept", () => copies().Length, () => Is.EqualTo(1));
            AddStep("take the names of the next seconds", () =>
            {
                opened = copies().Single();
                Game.LocalConfig.SetValue(OsuSetting.EditorAutoBackupKeepCount, 10);

                var now = DateTimeOffset.Now;

                // the copy kept on opening may hold the first of them already.
                taken = Enumerable.Range(0, 4).Select(i => Path.Combine(directory(), $"{now.AddSeconds(i):yyyy-MM-dd_HH-mm-ss}.osu")).Where(f => f != opened).ToArray();

                foreach (string file in taken)
                {
                    using (var stream = Game.Storage.CreateFileSafely(file))
                        stream.Write(new byte[] { 1 });
                }
            });
            AddStep("back up", () => Editor.ChildrenOfType<EditorAutoBackup>().Single().Backup(true));

            AddUntilStep("written beside them", () => copies().Length, () => Is.EqualTo(1 + taken.Length + 1));
            AddAssert("they are as they were", () => taken.Select(read), () => Is.All.EqualTo(new byte[] { 1 }));
            AddAssert("the copy kept on opening is as it was", () => read(opened), () => Is.EqualTo(originalFile()));
            // the oldest are deleted by name, so the backup has to sort after the file of its second that came first.
            AddAssert("it sorts after the file of its second", () =>
            {
                string written = copies().Except(taken).Except(new[] { opened }).Single();
                string sameSecond = taken.Append(opened).Single(f => Path.GetFileName(written).StartsWith(Path.GetFileNameWithoutExtension(f), StringComparison.Ordinal));
                return string.CompareOrdinal(Path.GetFileName(written), Path.GetFileName(sameSecond));
            }, () => Is.Positive);
        }

        private string directory() => EditorAutoBackup.DirectoryFor(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!);

        private string[] copies() => Game.Storage.ExistsDirectory(directory()) ? Game.Storage.GetFiles(directory(), "*.osu").ToArray() : System.Array.Empty<string>();

        private byte[] read(string file)
        {
            using (var stream = Game.Storage.GetStream(file))
                return stream.ReadAllBytesToArray();
        }

        private static byte[] originalFile()
        {
            using (var archive = new ZipArchive(TestResources.GetTestBeatmapStream()))
            using (var stream = archive.GetEntry(difficulty_file)!.Open())
                return stream.ReadAllRemainingBytesToArray();
        }
    }
}
