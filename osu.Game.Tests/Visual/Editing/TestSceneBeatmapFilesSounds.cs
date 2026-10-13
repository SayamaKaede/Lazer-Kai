// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The sounds of the beatmap set being edited given the names they are played as hitsounds by, without the folder
    /// of the set being opened (2026-10-13).
    /// </summary>
    public partial class TestSceneBeatmapFilesSounds : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private BeatmapInfo edited = null!;

        private BeatmapFilesScreen? screen => Game.ScreenStack.CurrentScreen as BeatmapFilesScreen;

        private SoundsOfTheSet sounds => screen!.Sounds;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                var importedSet = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely()!.ID)!;
                edited = importedSet.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(Game.BeatmapManager.QueryBeatmap(b => b.ID == edited.ID)!, true);
            };

            base.SetUpSteps();

            AddStep("open the files of the set", () => Editor.OpenBeatmapFiles());
            AddUntilStep("the screen is up", () => screen?.IsLoaded == true);
            AddStep("go to its sounds", () => screen!.Tab.Value = BeatmapFilesScreen.FilesTab.Sounds);
            AddUntilStep("they are listed", () => sounds.Rows.Any());
        }

        [Test]
        public void TestTheSoundsOfTheSetAreListed()
        {
            AddAssert("its sounds", names, () => Is.SupersetOf(new[] { @"D_Clap.wav", @"D_Snare.wav", @"Kick.wav", @"Snare.wav" }));
            AddAssert("and not its song", names, () => Does.Not.Contain(edited.Metadata.AudioFile));
            AddAssert("none is chosen, so none can be renamed", () => sounds.Chosen == null && !sounds.RenameButton.Enabled.Value);
        }

        [Test]
        public void TestASoundIsGivenTheNameOfWhatItIsToBe()
        {
            string? kick = null;

            AddStep("note the kick", () => kick = hashOf(@"Kick.wav"));

            choose(@"Kick.wav");
            say(SoundsOfTheSet.NamedBank.Soft, SoundsOfTheSet.NamedSound.HitClap, @"2");

            AddAssert("the name follows", () => sounds.NewName, () => Is.EqualTo(@"soft-hitclap2.wav"));
            AddStep("rename", () => sounds.RenameButton.TriggerClick());

            AddAssert("the file has the name", () => hashOf(@"soft-hitclap2.wav"), () => Is.EqualTo(kick).And.Not.Null);
            AddAssert("and not the one it had", () => hashOf(@"Kick.wav"), () => Is.Null);

            AddAssert("it is listed by the name", names, () => Does.Contain(@"soft-hitclap2.wav").And.Not.Contain(@"Kick.wav"));
            AddAssert("and is the one chosen still", () => sounds.Chosen, () => Is.EqualTo(@"soft-hitclap2.wav"));
            AddAssert("with no other name to give it", () => !sounds.RenameButton.Enabled.Value);
        }

        [Test]
        public void TestTheFirstCustomSetIsTheNameWithNoNumber()
        {
            choose(@"Snare.wav");

            say(SoundsOfTheSet.NamedBank.Drum, SoundsOfTheSet.NamedSound.HitFinish, @"1");
            AddAssert("no number", () => sounds.NewName, () => Is.EqualTo(@"drum-hitfinish.wav"));

            AddStep("say no number", () => sounds.Number.Current.Value = string.Empty);
            AddAssert("no number", () => sounds.NewName, () => Is.EqualTo(@"drum-hitfinish.wav"));

            AddStep("say a number there is not", () => sounds.Number.Current.Value = @"0");
            AddAssert("no name", () => sounds.NewName, () => Is.Null);
            AddAssert("and nothing to rename to", () => !sounds.RenameButton.Enabled.Value);

            AddStep("say a number", () => sounds.Number.Current.Value = @"12");
            AddAssert("the number", () => sounds.NewName, () => Is.EqualTo(@"drum-hitfinish12.wav"));
        }

        [Test]
        public void TestTheArrowsTakeTheNumberOneUpOrDown()
        {
            choose(@"Snare.wav");
            say(SoundsOfTheSet.NamedBank.Drum, SoundsOfTheSet.NamedSound.HitFinish, @"1");

            AddStep("up", () => sounds.NumberUp.TriggerClick());
            AddAssert("the next custom set", () => (sounds.Number.Current.Value, sounds.NewName), () => Is.EqualTo((@"2", @"drum-hitfinish2.wav")));

            AddStep("up", () => sounds.NumberUp.TriggerClick());
            AddStep("down", () => sounds.NumberDown.TriggerClick());
            AddAssert("back on it", () => sounds.Number.Current.Value, () => Is.EqualTo(@"2"));

            AddStep("down twice", () =>
            {
                sounds.NumberDown.TriggerClick();
                sounds.NumberDown.TriggerClick();
            });
            AddAssert("there is none before the first", () => (sounds.Number.Current.Value, sounds.NewName), () => Is.EqualTo((@"1", @"drum-hitfinish.wav")));

            AddStep("say no number", () => sounds.Number.Current.Value = string.Empty);
            AddStep("up", () => sounds.NumberUp.TriggerClick());
            AddAssert("up from the first", () => sounds.Number.Current.Value, () => Is.EqualTo(@"2"));

            // as the custom index of the hitsound screen is: a number's box, not a name's.
            AddAssert("the box is as wide as a number needs", () => sounds.Number.DrawWidth, () => Is.EqualTo(165).And.LessThan(sounds.Bank.DrawWidth));
        }

        [Test]
        public void TestANameAnotherSoundHasCannotBeGiven()
        {
            string? snare = null;

            AddStep("note the snare", () => snare = hashOf(@"Snare.wav"));

            choose(@"Kick.wav");
            say(SoundsOfTheSet.NamedBank.Soft, SoundsOfTheSet.NamedSound.HitClap, @"2");
            AddStep("rename", () => sounds.RenameButton.TriggerClick());

            // what was chosen for the kick is left for the next sound, which is no hitsound to be shown as its own.
            choose(@"Snare.wav");
            AddAssert("the same name", () => sounds.NewName, () => Is.EqualTo(@"soft-hitclap2.wav"));
            AddAssert("which cannot be given", () => !sounds.RenameButton.Enabled.Value);

            AddStep("try all the same", () => sounds.RenameButton.TriggerClick());
            AddAssert("the snare is where it was", () => hashOf(@"Snare.wav"), () => Is.EqualTo(snare));

            AddStep("say the next number", () => sounds.Number.Current.Value = @"3");
            AddAssert("which can", () => sounds.RenameButton.Enabled.Value);
        }

        [Test]
        public void TestAHitsoundIsShownAsWhatItIs()
        {
            choose(@"Kick.wav");
            say(SoundsOfTheSet.NamedBank.Drum, SoundsOfTheSet.NamedSound.SliderSlide, @"5");
            AddStep("rename", () => sounds.RenameButton.TriggerClick());

            choose(@"Snare.wav");
            say(SoundsOfTheSet.NamedBank.Normal, SoundsOfTheSet.NamedSound.HitNormal, @"1");

            choose(@"drum-sliderslide5.wav");
            AddAssert("what it is", () => (sounds.Bank.Current.Value, sounds.Sound.Current.Value, sounds.Number.Current.Value),
                () => Is.EqualTo((SoundsOfTheSet.NamedBank.Drum, SoundsOfTheSet.NamedSound.SliderSlide, @"5")));
        }

        [Test]
        public void TestTheEditorOpensTheSetAgainForARenamedSound()
        {
            Editor before = null!;

            choose(@"Kick.wav");
            say(SoundsOfTheSet.NamedBank.Soft, SoundsOfTheSet.NamedSound.HitClap, @"2");
            AddStep("rename", () => sounds.RenameButton.TriggerClick());

            AddStep("leave", () =>
            {
                before = Editor;
                screen!.Exit();
            });
            AddUntilStep("the editor opens the set again", () => Editor != before && Editor?.ReadyForUse == true);
            AddAssert("on the difficulty it was on", () => EditorBeatmap.BeatmapInfo.ID, () => Is.EqualTo(edited.ID));
        }

        private void choose(string name)
        {
            AddStep($"choose {name}", () => sounds.Rows.Single(r => r.Filename == name).TriggerClick());
            AddAssert("it is chosen", () => sounds.Chosen, () => Is.EqualTo(name));
        }

        private void say(SoundsOfTheSet.NamedBank bank, SoundsOfTheSet.NamedSound sound, string number) => AddStep($"say {bank} {sound} {number}", () =>
        {
            sounds.Bank.Current.Value = bank;
            sounds.Sound.Current.Value = sound;
            sounds.Number.Current.Value = number;
        });

        private string[] names() => sounds.Rows.Select(r => r.Filename).ToArray();

        private string? hashOf(string name) => BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!.GetFile(name)?.File.Hash;
    }
}
