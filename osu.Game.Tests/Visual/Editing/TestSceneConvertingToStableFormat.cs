// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.Sprites;
using osu.Game.Models;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Resources;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A beatmap in lazer's format, converted to stable's from the file menu: saved the way exporting for stable writes
    /// it, and kept in stable's format from then on.
    /// </summary>
    public partial class TestSceneConvertingToStableFormat : StableFormatConversionTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                // saved here once, so that its file is lazer's, as the file of a beatmap made in this game is.
                var working = Game.BeatmapManager.GetWorkingBeatmap(imported);
                Game.BeatmapManager.Save(working.BeatmapInfo, working.GetPlayableBeatmap(working.BeatmapInfo.Ruleset), working.GetSkin());

                return Game.BeatmapManager.GetWorkingBeatmap(Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!, true);
            };

            base.SetUpSteps();
        }

        [Test]
        public void TestConvertingSavesInStableFormatAndKeepsIt()
        {
            AddAssert("file is lazer's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v128"));
            AddUntilStep("status says lazer's", () => ShownSaveFormat(), () => Is.EqualTo("Lazer"));
            AddAssert("converting is offered", () => Editor.ConvertToStableFormatMenuItem.Action.Disabled, () => Is.False);

            Convert();

            AddAssert("file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v14"));
            AddUntilStep("status says stable's", () => ShownSaveFormat(), () => Is.EqualTo("Stable v14"));
            AddAssert("converting is not offered any more", () => Editor.ConvertToStableFormatMenuItem.Action.Disabled, () => Is.True);
        }

        [Test]
        public void TestTheConvertedFileIsWhatExportingForStableWrites()
        {
            byte[] exported = null!;

            AddStep("export for stable", () => exported = exportedFile());
            Convert();

            AddAssert("saved file is the exported one", () => SavedFile(), () => Is.EqualTo(exported));
        }

        [Test]
        public void TestAnUnsavedEditIsConvertedWithTheRest()
        {
            AddStep("move the first circle off whole coordinates", () =>
            {
                var circle = EditorBeatmap.HitObjects.OfType<HitCircle>().First();

                EditorBeatmap.BeginChange();
                circle.Position = new Vector2(100.4f, 50.6f);
                EditorBeatmap.Update(circle);
                EditorBeatmap.EndChange();
            });

            Convert();

            AddAssert("it is where it was moved, on whole coordinates", () => EditorBeatmap.HitObjects.OfType<HitCircle>().First().Position,
                () => Is.EqualTo(new Vector2(100, 51)));
        }

        [Test]
        public void TestNotOfferedWithStableCompatibilityOff()
        {
            AddStep("turn stable compatibility off", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, false));
            AddAssert("converting is not offered", () => Editor.ConvertToStableFormatMenuItem.Action.Disabled, () => Is.True);
        }

        private byte[] exportedFile()
        {
            var set = Game.BeatmapManager.QueryBeatmap(b => b.ID == imported.ID)!.BeatmapSet!;
            var file = set.Files.Single(f => f.Filename == difficulty_file);

            var reader = new ExportReader(Game.Storage);
            reader.KeepStableFiles.BindTo(Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility));

            using (var stream = reader.Read(set, file))
                return stream.ReadAllBytesToArray();
        }

        private class ExportReader : LegacyBeatmapExporter
        {
            public ExportReader(Storage storage)
                : base(storage)
            {
            }

            public Stream Read(BeatmapSetInfo set, RealmNamedFileUsage file) => GetFileContents(set, file)!;
        }
    }

    /// <summary>
    /// A beatmap made here and never saved: converting it is its first save.
    /// </summary>
    public partial class TestSceneConvertingANewBeatmapToStableFormat : StableFormatConversionTestScene
    {
        [Test]
        public void TestConvertingIsTheFirstSave()
        {
            AddStep("turn stable compatibility on", () => Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true));
            AddAssert("converting is offered", () => Editor.ConvertToStableFormatMenuItem.Action.Disabled, () => Is.False);

            Convert();

            AddAssert("file is stable's", () => FirstLine(SavedFile()), () => Is.EqualTo("osu file format v14"));
            AddUntilStep("status says stable's", () => ShownSaveFormat(), () => Is.EqualTo("Stable v14"));
        }
    }

    public abstract partial class StableFormatConversionTestScene : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        /// <summary>
        /// Asks for the conversion from the editor, confirms it, and waits for the editor to open the converted file.
        /// </summary>
        protected void Convert()
        {
            Editor? before = null;

            AddStep("ask to convert", () => (before = Editor).ConfirmConvertToStableFormat());
            AddUntilStep("the dialog is up", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog is DangerousActionDialog);
            AddStep("confirm", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.PerformAction<PopupDialogDangerousButton>());
            // ready for use, not just loaded: an editor left with its compose screen still loading fails the next test.
            AddUntilStep("the editor opens the file again", () => Editor != before && Editor?.ReadyForUse == true);
        }

        /// <summary>
        /// The file of the difficulty being edited, as it is stored.
        /// </summary>
        protected byte[] SavedFile()
        {
            var info = Game.BeatmapManager.QueryBeatmap(b => b.ID == EditorBeatmap.BeatmapInfo.ID)!;

            using (var stream = Game.BeatmapManager.GetWorkingBeatmap(info, true).GetStream(info.BeatmapSet!.GetPathForFile(info.Path!)))
                return stream.ReadAllRemainingBytesToArray();
        }

        protected static string FirstLine(byte[] file)
        {
            using (var reader = new StreamReader(new MemoryStream(file)))
                return reader.ReadLine()!.Trim();
        }

        protected string? ShownSaveFormat()
        {
            var status = Editor.ChildrenOfType<ComposeStatusDisplay>().FirstOrDefault();
            var row = status?.ChildrenOfType<OsuSpriteText>().FirstOrDefault(t => t.Text.ToString() == "Format")?.Parent;

            if (row == null || !row.IsPresent)
                return null;

            return row.ChildrenOfType<OsuSpriteText>().Last().Text.ToString();
        }
    }
}
