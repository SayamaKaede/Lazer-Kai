// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Overlays.Dialog;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Select;
using osu.Game.Tests.Resources;
using osu.Framework.Extensions;
using osuTK.Input;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// Outside the editor the game only deletes whole sets, so song select deletes one difficulty.
    /// </summary>
    public partial class TestSceneDeleteDifficultyFromSongSelect : SongSelectTestScene
    {
        private BeatmapSetInfo beatmapSet => Beatmaps.GetAllUsableBeatmapSets().Last();

        [Test]
        public void TestDeleteOneDifficulty()
        {
            Guid deleted = Guid.Empty;
            int countBefore = 0;

            LoadSongSelect();
            // a real archive rather than a made-up set: deleting a difficulty deletes its file, which a made-up one has not got.
            AddStep("import a beatmap", () => Beatmaps.Import(TestResources.GetQuickTestBeatmapForImport()).WaitSafely());
            WaitForFiltering();

            AddStep("delete the first difficulty", () =>
            {
                var difficulties = beatmapSet.Beatmaps.ToArray();

                countBefore = difficulties.Length;
                deleted = difficulties.First().ID;
                SongSelect.DeleteDifficulty(difficulties.First());
            });

            AddUntilStep("confirmation is asked for", () => DialogOverlay.CurrentDialog is DeleteDifficultyConfirmationDialog);
            AddStep("hold to confirm", () =>
            {
                var confirm = DialogOverlay.CurrentDialog!.ChildrenOfType<PopupDialogDangerousButton>().First();

                InputManager.MoveMouseTo(confirm);
                InputManager.PressButton(MouseButton.Left);
            });

            AddUntilStep("the difficulty is gone", () => beatmapSet.Beatmaps.All(b => b.ID != deleted));
            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));
            AddAssert("the rest of the set is still there", () => beatmapSet.Beatmaps.Count, () => Is.EqualTo(countBefore - 1));
        }

        [Test]
        public void TestTheLastDifficultyIsKept()
        {
            AddStep("close any dialog", () => DialogOverlay.Hide());

            LoadSongSelect();
            ImportBeatmapForRuleset(_ => { }, 1, 0);
            WaitForFiltering();

            AddStep("try to delete the only difficulty", () => SongSelect.DeleteDifficulty(beatmapSet.Beatmaps.Single()));
            AddAssert("nothing is asked", () => DialogOverlay.CurrentDialog, () => Is.Null);
            AddAssert("it is still there", () => beatmapSet.Beatmaps.Count, () => Is.EqualTo(1));
        }

        /// <summary>
        /// The selected difficulty stayed selected once deleted: what song select had of it said nothing of its being
        /// gone, and it is not in the database to be asked.
        /// </summary>
        [Test]
        public void TestTheSelectionGoesToAnotherDifficultyWhenTheSelectedOneIsDeleted()
        {
            loadWithADifficultySelected();
            deleteTheSelectedDifficulty();

            AddUntilStep("another difficulty is selected", () => !Beatmap.IsDefault && Beatmap.Value.BeatmapInfo.ID != deletedSelection);
            assertADifficultyOfTheSetIsSelected();
        }

        /// <summary>
        /// Going back from a set shown alone selects what was selected before it was. A difficulty deleted since is not
        /// in the database, which the options button reads what is selected from (2026-10-12, DOTNET-E and DOTNET-F).
        /// </summary>
        /// <param name="deletedBefore">Whether the difficulty is deleted before the set is shown alone, or while it is.</param>
        [TestCase(true)]
        [TestCase(false)]
        public void TestGoingBackFromASetShownAloneDoesNotSelectADeletedDifficulty(bool deletedBefore)
        {
            loadWithADifficultySelected();

            if (deletedBefore)
                deleteTheSelectedDifficulty();

            AddStep("show the set alone", () => SongSelect.ScopeToBeatmapSet(archiveSet));
            WaitForFiltering();

            if (!deletedBefore)
                deleteTheSelectedDifficulty();

            AddStep("go back", () => SongSelect.UnscopeBeatmapSet());
            WaitForFiltering();
            AddWaitStep("wait for debounce", 5);

            assertADifficultyOfTheSetIsSelected();
        }

        private Guid deletedSelection;

        private Guid archive;

        private BeatmapSetInfo archiveSet => Beatmaps.GetAllUsableBeatmapSets().Single(s => s.ID == archive);

        private void loadWithADifficultySelected()
        {
            AddStep("close any dialog", () => DialogOverlay.Hide());

            LoadSongSelect();
            // another set beside it: of the only set it has, the carousel selects a difficulty by itself whenever what is
            // selected is not among what it has, and so it does not come to this.
            ImportBeatmapForRuleset(0);
            AddStep("import a beatmap", () => archive = Beatmaps.Import(new ImportTask(TestResources.GetQuickTestBeatmapForImport())).GetResultSafely()!.ID);
            AddUntilStep("it is in the carousel", () => !Carousel.IsFiltering
                                                        && Carousel.GetCarouselItems()!.Select(i => i.Model).OfType<GroupedBeatmap>().Any(b => b.Beatmap.BeatmapSet?.ID == archive));

            AddStep("select a difficulty of it", () => Beatmap.Value = Beatmaps.GetWorkingBeatmap(archiveSet.Beatmaps.First(b => b.Ruleset.OnlineID == 0)));
            AddUntilStep("the carousel has it selected", () => Carousel.CurrentBeatmap?.ID, () => Is.EqualTo(Beatmap.Value.BeatmapInfo.ID));
        }

        private void deleteTheSelectedDifficulty()
        {
            AddStep("delete the selected difficulty", () =>
            {
                deletedSelection = Beatmap.Value.BeatmapInfo.ID;
                SongSelect.DeleteDifficulty(Beatmap.Value.BeatmapInfo);
            });

            AddUntilStep("confirmation is asked for", () => DialogOverlay.CurrentDialog is DeleteDifficultyConfirmationDialog);
            AddStep("hold to confirm", () =>
            {
                var confirm = DialogOverlay.CurrentDialog!.ChildrenOfType<PopupDialogDangerousButton>().First();

                InputManager.MoveMouseTo(confirm);
                InputManager.PressButton(MouseButton.Left);
            });

            AddUntilStep("the difficulty is gone", () => archiveSet.Beatmaps.All(b => b.ID != deletedSelection));
            AddStep("release", () => InputManager.ReleaseButton(MouseButton.Left));

            AddUntilStep("the carousel has it no more", () => !Carousel.IsFiltering
                                                              && Carousel.GetCarouselItems()!.Select(i => i.Model).OfType<GroupedBeatmap>().All(b => b.Beatmap.ID != deletedSelection));
        }

        private void assertADifficultyOfTheSetIsSelected()
        {
            AddAssert("a difficulty the set has is selected", () => archiveSet.Beatmaps.Select(b => b.ID), () => Does.Contain(Beatmap.Value.BeatmapInfo.ID));
            AddUntilStep("which the carousel has selected", () => Carousel.CurrentBeatmap?.ID, () => Is.EqualTo(Beatmap.Value.BeatmapInfo.ID));
        }
    }
}
