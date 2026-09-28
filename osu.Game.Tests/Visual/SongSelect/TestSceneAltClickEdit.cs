// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Select;
using osuTK.Input;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// Alt-clicking a difficulty opens it in the editor (2026-09-29 request), where clicking it only selects it.
    /// </summary>
    public partial class TestSceneAltClickEdit : SongSelectTestScene
    {
        private BeatmapInfo other => Beatmaps.GetAllUsableBeatmapSets().Single().Beatmaps.OrderBy(b => b.DifficultyName).Skip(1).First();

        [Test]
        public void TestAltClickOpensTheDifficultyInTheEditor()
        {
            loadWithOneSet();

            AddStep("alt-click a difficulty that is not selected", () => click(other, alt: true));

            // the loader hands over to the editor itself once that is ready.
            AddUntilStep("the editor is opened", () => Stack.CurrentScreen is EditorLoader || Stack.CurrentScreen is Editor);
            AddAssert("on that difficulty", () => Beatmap.Value.BeatmapInfo.Equals(other));
        }

        [Test]
        public void TestClickAloneOnlySelects()
        {
            loadWithOneSet();

            AddStep("click a difficulty that is not selected", () => click(other, alt: false));

            AddUntilStep("it is selected", () => Beatmap.Value.BeatmapInfo.Equals(other));
            AddAssert("the editor is not opened", () => Stack.CurrentScreen == SongSelect);
        }

        private void loadWithOneSet()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();
            AddUntilStep("its difficulties are listed", () => panel(other) != null);
        }

        private PanelBeatmap? panel(BeatmapInfo beatmap) =>
            Carousel.ChildrenOfType<PanelBeatmap>().FirstOrDefault(p => p.Item?.Model is GroupedBeatmap g && g.Beatmap.Equals(beatmap));

        private void click(BeatmapInfo beatmap, bool alt)
        {
            if (alt)
                InputManager.PressKey(Key.LAlt);

            InputManager.MoveMouseTo(panel(beatmap)!);
            InputManager.Click(MouseButton.Left);

            if (alt)
                InputManager.ReleaseKey(Key.LAlt);
        }
    }
}
