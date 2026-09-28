// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Select;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// Mods set on a difficulty from its menu are put on whenever it is selected (2026-09-29 request: a tournament pool
    /// fixes the mods each map is played with).
    /// </summary>
    public partial class TestSceneDifficultyMods : SongSelectTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        private BeatmapInfo first => Beatmaps.GetAllUsableBeatmapSets().Single().Beatmaps.OrderBy(b => b.DifficultyName).First();
        private BeatmapInfo second => Beatmaps.GetAllUsableBeatmapSets().Single().Beatmaps.OrderBy(b => b.DifficultyName).Skip(1).First();

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("forget the mods set on difficulties", () => storage.Delete(DifficultyModStore.FILENAME));
        }

        [Test]
        public void TestTheModsArePutOnWhenTheDifficultyIsSelected()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods(new OsuModHardRock());
            setModsOn(() => first);

            select(() => second);
            ChangeMods();

            select(() => first);
            AddAssert("hard rock is on", () => acronyms(), () => Is.EqualTo(new[] { "HR" }));
        }

        [Test]
        public void TestWhatWasSelectedBeforeComesBackOnADifficultyWithoutAny()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods(new OsuModHardRock());
            setModsOn(() => first);

            select(() => second);
            ChangeMods(new OsuModHidden());

            select(() => first);
            AddAssert("hard rock is on", () => acronyms(), () => Is.EqualTo(new[] { "HR" }));

            select(() => second);
            AddAssert("hidden is back", () => acronyms(), () => Is.EqualTo(new[] { "HD" }));
        }

        [Test]
        public void TestTheModsCanBeTakenOff()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods(new OsuModHardRock());
            setModsOn(() => first);

            AddStep("take them off", () => menuItem(first, LazerKaiSongSelectStrings.ClearDifficultyMods).Action.Value!.Invoke());
            AddAssert("none are kept", () => new DifficultyModStore(storage).Get(first, new OsuRuleset()), () => Is.Null);

            select(() => second);
            ChangeMods();

            select(() => first);
            AddAssert("nothing is on", () => acronyms(), () => Is.Empty);
        }

        [Test]
        public void TestNothingIsSetWithoutMods()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods();
            AddAssert("the item cannot be used", () => menuItem(first, LazerKaiSongSelectStrings.SetModsOnDifficulty).Action.Disabled);
        }

        [Test]
        public void TestTheModsAreShownOnTheDifficulty()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods(new OsuModHardRock(), new OsuModHidden());
            setModsOn(() => first);

            AddUntilStep("its panel shows both", () => iconsOn(first), () => Is.EquivalentTo(new[] { "HD", "HR" }));
            AddAssert("the other shows none", () => iconsOn(second), () => Is.Empty);
        }

        [Test]
        public void TestTheModsAreKept()
        {
            loadWithOneSet();

            select(() => first);
            ChangeMods(new OsuModDoubleTime { SpeedChange = { Value = 1.3 } });
            setModsOn(() => first);

            AddAssert("they are read back with their settings", () =>
            {
                var mods = new DifficultyModStore(storage).Get(first, new OsuRuleset());
                return mods?.OfType<OsuModDoubleTime>().SingleOrDefault()?.SpeedChange.Value;
            }, () => Is.EqualTo(1.3));
        }

        private void loadWithOneSet()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();
        }

        private void select(Func<BeatmapInfo> beatmap) =>
            AddStep("select a difficulty", () => Beatmap.Value = Beatmaps.GetWorkingBeatmap(beatmap()));

        private void setModsOn(Func<BeatmapInfo> beatmap) =>
            AddStep("set the current mods on it", () => menuItem(beatmap(), LazerKaiSongSelectStrings.SetModsOnDifficulty).Action.Value!.Invoke());

        private OsuMenuItem menuItem(BeatmapInfo beatmap, LocalisableString text) =>
            SongSelect.GetDifficultyModActions(beatmap).Single(i => i.Text.Value.Equals(text));

        private string[] acronyms() => SelectedMods.Value.Select(m => m.Acronym).ToArray();

        private string[] iconsOn(BeatmapInfo beatmap) =>
            Carousel.ChildrenOfType<PanelBeatmap>()
                    .Where(p => p.Item?.Model is GroupedBeatmap g && g.Beatmap.Equals(beatmap))
                    .SelectMany(p => p.ChildrenOfType<ModIcon>())
                    .Select(i => i.Mod.Acronym)
                    .ToArray();
    }
}
