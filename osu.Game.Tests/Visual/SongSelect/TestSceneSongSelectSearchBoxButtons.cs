// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Screens.Select;
using osuTK.Input;

namespace osu.Game.Tests.Visual.SongSelect
{
    /// <summary>
    /// The two buttons at the right end of song select's search box: the saved searches and the user tags.
    /// </summary>
    public partial class TestSceneSongSelectSearchBoxButtons : SongSelectTestScene
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private SessionStatics statics { get; set; } = null!;

        private FilterControl filterControl => SongSelect.ChildrenOfType<FilterControl>().Single();

        private FilterControl.UserTagPopover? tagPopover => this.ChildrenOfType<FilterControl.UserTagPopover>().SingleOrDefault();

        private FilterControl.SearchPresetPopover? presetPopover => this.ChildrenOfType<FilterControl.SearchPresetPopover>().SingleOrDefault();

        private FilterControl.UserTagPopover.TagButton? tagButton(string tag) =>
            tagPopover?.ChildrenOfType<FilterControl.UserTagPopover.TagButton>().SingleOrDefault(b => b.Tag == tag);

        private string searchText => filterControl.SearchText.Value;

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("forget the presets", () => storage.Delete(SearchPresetStore.FILENAME));
            AddStep("forget the descriptions", () => statics.SetValue<APITag[]?>(Static.AllBeatmapTags, null));
        }

        [Test]
        public void TestUserTagsAreCountedOverWhatIsShown()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(set =>
            {
                set.Beatmaps[0].Metadata.UserTags.Add("style/clean");
                set.Beatmaps[0].Metadata.UserTags.Add("aim/aim control");
                set.Beatmaps[1].Metadata.UserTags.Add("style/clean");
            }, 3, 0);
            WaitForFiltering();

            openTheTags();

            AddUntilStep("clean is on two", () => tagButton("style/clean")?.Difficulties, () => Is.EqualTo(2));
            AddAssert("aim control is on one", () => tagButton("aim/aim control")?.Difficulties, () => Is.EqualTo(1));
            AddAssert("nothing is searched", () => tagButton("style/clean")!.Searched, () => Is.False);

            AddStep("click aim control", () => click(tagButton("aim/aim control")!));
            AddAssert("it is searched for", () => searchText, () => Is.EqualTo(@"tag=""aim/aim control""!"));
            WaitForFiltering();

            AddUntilStep("clean is counted over the one shown", () => tagButton("style/clean")?.Difficulties, () => Is.EqualTo(1));
            AddAssert("aim control shows it is searched", () => tagButton("aim/aim control")!.Searched, () => Is.True);
            AddAssert("the list is still open", () => tagPopover, () => Is.Not.Null);

            AddStep("click aim control again", () => click(tagButton("aim/aim control")!));
            AddAssert("it is taken out of the search", () => searchText, () => Is.Empty);
            WaitForFiltering();
            AddUntilStep("clean is on two again", () => tagButton("style/clean")?.Difficulties, () => Is.EqualTo(2));
        }

        [Test]
        public void TestATagIsTakenOutFromAmongOtherTerms()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(set => set.Beatmaps[0].Metadata.UserTags.Add("style/clean"), 3, 0);
            WaitForFiltering();

            AddStep("search with a tag between other terms", () => filterControl.Search(@"stars>1 tag=""style/clean""! length<600"));
            WaitForFiltering();

            openTheTags();

            AddUntilStep("clean shows it is searched", () => tagButton("style/clean")?.Searched, () => Is.True);
            AddStep("click clean", () => click(tagButton("style/clean")!));
            AddAssert("only the tag is taken out", () => searchText, () => Is.EqualTo("stars>1 length<600"));
        }

        /// <summary>
        /// The official list has a tag of one name for each ruleset it is had in (DOTNET-4 to DOTNET-7).
        /// </summary>
        [Test]
        public void TestTagsOfOneNameForEachRuleset()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(set => set.Beatmaps[0].Metadata.UserTags.Add("expression/improvisation"), 3, 0);
            WaitForFiltering();

            AddStep("describe it once for each ruleset", () => statics.SetValue<APITag[]?>(Static.AllBeatmapTags, new[]
            {
                new APITag { Id = 1, Name = "expression/improvisation", Description = "for taiko", RulesetId = 1 },
                new APITag { Id = 2, Name = "expression/improvisation", Description = "for osu!", RulesetId = 0 },
            }));

            openTheTags();

            AddUntilStep("it is listed", () => tagButton("expression/improvisation")?.Difficulties, () => Is.EqualTo(1));
            AddAssert("it is described for the ruleset shown", () => tagButton("expression/improvisation")!.TooltipText.ToString(), () => Is.EqualTo("for osu!"));
        }

        [Test]
        public void TestNoTags()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();

            openTheTags();

            AddAssert("no tag is listed", () => tagPopover!.ChildrenOfType<FilterControl.UserTagPopover.TagButton>(), () => Is.Empty);
        }

        [Test]
        public void TestSaveApplyAndDeleteAPreset()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();

            AddStep("search", () => filterControl.Search("stars>5 status=r"));
            openThePresets();

            AddStep("name it", () => presetPopover!.ChildrenOfType<Graphics.UserInterface.OsuTextBox>().Single().Text = "hard ranked");
            AddStep("save", () => click(presetPopover!.ChildrenOfType<Graphics.UserInterfaceV2.RoundedButton>().Single()));

            AddAssert("it is kept", () => new SearchPresetStore(storage).Load(), () => Is.EqualTo(new[] { new SearchPreset("hard ranked", "stars>5 status=r") }));
            AddAssert("it is listed as what is searched now", () => presetPopover!.ChildrenOfType<FilterControl.SearchPresetPopover.PresetRow>().Single().Current, () => Is.True);

            AddStep("clear the search", () => filterControl.Search(string.Empty));
            AddAssert("it is no longer what is searched", () => presetPopover!.ChildrenOfType<FilterControl.SearchPresetPopover.PresetRow>().Single().Current, () => Is.False);

            AddStep("click it", () => click(presetPopover!.ChildrenOfType<FilterControl.SearchPresetPopover.PresetRow>().Single()));
            AddAssert("its search is put back", () => searchText, () => Is.EqualTo("stars>5 status=r"));
            AddUntilStep("the presets close", () => presetPopover, () => Is.Null);

            openThePresets();
            AddStep("delete it", () => click(presetPopover!.ChildrenOfType<FilterControl.SearchPresetPopover.PresetRow>().Single()
                                                           .ChildrenOfType<Graphics.UserInterface.IconButton>().Single()));
            AddUntilStep("it is not listed", () => presetPopover!.ChildrenOfType<FilterControl.SearchPresetPopover.PresetRow>(), () => Is.Empty);
            AddAssert("it is not kept", () => new SearchPresetStore(storage).Load(), () => Is.Empty);
            AddAssert("the search is left alone", () => searchText, () => Is.EqualTo("stars>5 status=r"));
        }

        [Test]
        public void TestPresetUnderTheSameNameIsReplaced()
        {
            AddStep("keep two", () =>
            {
                var store = new SearchPresetStore(storage);
                store.Add("a", "stars>5");
                store.Add("b", "status=l");
            });
            AddStep("keep a again", () => new SearchPresetStore(storage).Add("a", "stars>6"));
            AddAssert("a is replaced where it stood", () => new SearchPresetStore(storage).Load(),
                () => Is.EqualTo(new[] { new SearchPreset("a", "stars>6"), new SearchPreset("b", "status=l") }));

            AddStep("keep one with no name", () => new SearchPresetStore(storage).Add(" ", " length<90 "));
            AddAssert("it is named after its search", () => new SearchPresetStore(storage).Load().Last(), () => Is.EqualTo(new SearchPreset("length<90", "length<90")));
        }

        [Test]
        public void TestNothingToSave()
        {
            LoadSongSelect();
            ImportBeatmapForRuleset(0);
            WaitForFiltering();

            openThePresets();
            AddAssert("save is disabled", () => presetPopover!.ChildrenOfType<Graphics.UserInterfaceV2.RoundedButton>().Single().Enabled.Value, () => Is.False);

            AddStep("search", () => filterControl.Search("stars>5"));
            AddAssert("save is enabled", () => presetPopover!.ChildrenOfType<Graphics.UserInterfaceV2.RoundedButton>().Single().Enabled.Value, () => Is.True);
        }

        private void openTheTags()
        {
            AddStep("click the tags button", () => click(filterControl.ChildrenOfType<FilterControl.SearchBoxButton>().Last()));
            AddUntilStep("the tags are open", () => tagPopover?.IsPresent == true);
        }

        private void openThePresets()
        {
            AddStep("click the presets button", () => click(filterControl.ChildrenOfType<FilterControl.SearchBoxButton>().First()));
            AddUntilStep("the presets are open", () => presetPopover?.IsPresent == true);
        }

        private void click(Drawable drawable)
        {
            InputManager.MoveMouseTo(drawable);
            InputManager.Click(MouseButton.Left);
        }
    }
}
