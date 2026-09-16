// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Overlays.Dialog;
using osu.Game.Screens.Edit;
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
    }
}
