// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Screens.Edit.Setup;
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

        private SoundFileNamesScreen? screen => Game.ScreenStack.CurrentScreen as SoundFileNamesScreen;

        private SoundsOfTheSet sounds => screen!.Sounds;

        private PopupDialog? dialog => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                var importedSet = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely()!.ID)!;
                edited = importedSet.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(Game.BeatmapManager.QueryBeatmap(b => b.ID == edited.ID)!, true);
            };

            base.SetUpSteps();

            open();
        }

        private void open()
        {
            AddStep("open the screen", () => Editor.OpenSoundFileNames());
            AddUntilStep("the sounds of the set are listed", () => screen?.IsLoaded == true && sounds.Rows.Any());
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

        [Test]
        public void TestASoundInAFolderIsNamedOutOfIt()
        {
            string? tom = null;

            AddStep("a sound in a folder", () =>
            {
                using (var stream = new MemoryStream(resource(@"test-sample.ogg")()))
                    Game.BeatmapManager.AddFile(BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!, stream, @"drums/tom.ogg");

                tom = hashOf(@"drums/tom.ogg");
                sounds.Refresh();
            });

            choose(@"drums/tom.ogg");
            say(SoundsOfTheSet.NamedBank.Normal, SoundsOfTheSet.NamedSound.HitNormal, @"9");

            // beside the difficulties, where stable looks for a hitsound, and the kind of file it was.
            AddAssert("the name is out of the folder", () => sounds.NewName, () => Is.EqualTo(@"normal-hitnormal9.ogg"));
            AddStep("rename", () => sounds.RenameButton.TriggerClick());
            AddAssert("the file has the name", () => hashOf(@"normal-hitnormal9.ogg"), () => Is.EqualTo(tom).And.Not.Null);
            AddAssert("and not the one it had", () => hashOf(@"drums/tom.ogg"), () => Is.Null);
        }

        #region Files dropped on the screen

        [Test]
        public void TestASoundDroppedOnTheScreenIsASoundOfTheSet()
        {
            drop((@"dropped.wav", resource(@"hitsound-no-delay.wav")));

            AddUntilStep("it is listed", names, () => Does.Contain(@"dropped.wav"));
            AddAssert("it is a file of the set", () => hashOf(@"dropped.wav"), () => Is.Not.Null);
            AddAssert("and the one chosen, to be named next", () => sounds.Chosen, () => Is.EqualTo(@"dropped.wav"));
        }

        [Test]
        public void TestEveryKindOfFileAHitsoundIsCanBeDropped()
        {
            drop((@"a.wav", resource(@"hitsound-no-delay.wav")), (@"b.mp3", resource(@"test-sample-cut.mp3")), (@"c.ogg", resource(@"test-sample.ogg")));

            AddUntilStep("they are listed", names, () => Is.SupersetOf(new[] { @"a.wav", @"b.mp3", @"c.ogg" }));
        }

        [Test]
        public void TestAFileAsLongAsASongIsNotTaken()
        {
            // with one that is taken, for there to be something to see it was looked at by.
            drop((@"long.mp3", song), (@"short.wav", resource(@"hitsound-no-delay.wav")));

            AddUntilStep("the short one is listed", names, () => Does.Contain(@"short.wav"));
            AddAssert("the long one is no file of the set", () => hashOf(@"long.mp3"), () => Is.Null);
        }

        [Test]
        public void TestAFileThatIsNoSoundIsNotTaken()
        {
            drop((@"corrupt.wav", resource(@"corrupt.wav")), (@"fine.ogg", resource(@"test-sample.ogg")));

            AddUntilStep("the one that is a sound is listed", names, () => Does.Contain(@"fine.ogg"));
            AddAssert("the other is no file of the set", () => hashOf(@"corrupt.wav"), () => Is.Null);
        }

        [Test]
        public void TestASoundDroppedOverAnotherOfItsNameIsAskedAbout()
        {
            string? kick = null;

            AddStep("note the kick", () => kick = hashOf(@"Kick.wav"));

            // stable does not mind how a name is written.
            drop((@"kick.wav", resource(@"hitsound-no-delay.wav")));

            AddUntilStep("asked first", () => dialog is ConfirmDialog);
            AddAssert("the kick is the one it was", () => hashOf(@"Kick.wav"), () => Is.EqualTo(kick));

            AddStep("say so", () => dialog!.PerformOkAction());
            AddUntilStep("the kick is the one dropped", () => hashOf(@"Kick.wav"), () => Is.Not.EqualTo(kick).And.Not.Null);
            AddAssert("under the one name still", () => BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!.Files.Count(f => BeatmapSetSounds.IsTheSameSound(f.Filename, @"Kick.wav")),
                () => Is.EqualTo(1));
        }

        [Test]
        public void TestTheVeryFileDroppedAgainIsLeftAsItIs()
        {
            drop((@"Kick.wav", () => new BeatmapSetFiles(Game.BeatmapManager, BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!).Get(@"Kick.wav")));

            AddUntilStep("it is shown to be there", () => sounds.Chosen, () => Is.EqualTo(@"Kick.wav"));
            AddAssert("nothing was asked", () => dialog, () => Is.Null);
            AddAssert("and nothing changed", () => !sounds.Changed);
        }

        [Test]
        public void TestWhatIsDroppedHereIsNotMadeTheSongOfTheBeatmap()
        {
            Editor before = null!;

            AddStep("leave", () =>
            {
                before = Editor;
                screen!.Exit();
            });
            AddUntilStep("back in the editor", () => Game.ScreenStack.CurrentScreen == before);

            // where a file of sound dropped on the editor is made the song of the beatmap.
            AddStep("go to the setup screen", () => Editor.Mode.Value = EditorScreenMode.SongSetup);
            AddUntilStep("its file selectors are up", () => Editor.ChildrenOfType<FormBeatmapFileSelector>().Count(s => s.IsLoaded) >= 3);
            AddWaitStep("let them listen for files", 3);

            open();

            // one this screen does not take either, so that it is left with nothing changed, for the editor it is
            // left to: what the setup screen is given it does once it is shown again.
            drop((@"long.mp3", song));
            AddWaitStep("let it be looked at", 5);

            AddStep("leave", () => screen!.Exit());
            AddUntilStep("back in the editor", () => Game.ScreenStack.CurrentScreen == before);
            AddWaitStep("let the setup screen do what it was given", 10);

            AddAssert("the song is the one it was", () => EditorBeatmap.Metadata.AudioFile, () => Is.EqualTo(edited.Metadata.AudioFile));
            AddAssert("and no other file of sound is one of the set's", () => hashOf(@"long.mp3") == null && hashOf(@"audio.mp3") == null);
        }

        /// <summary>
        /// Drops files on the game, as they are dragged onto its window.
        /// </summary>
        private void drop(params (string name, Func<byte[]> contents)[] files) => AddStep($"drop {string.Join(@", ", files.Select(f => f.name))}", () =>
        {
            var folder = Game.Storage.GetStorageForDirectory(@"dropped");

            string[] paths = files.Select(f =>
            {
                using (var stream = folder.CreateFileSafely(f.name))
                    stream.Write(f.contents());

                return folder.GetFullPath(f.name);
            }).ToArray();

            Game.Import(paths).WaitSafely();
        });

        private static Func<byte[]> resource(string name) => () =>
        {
            using (var stream = TestResources.OpenResource($@"Samples/{name}"))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
        };

        /// <summary>
        /// The song of the beatmap set, which is minutes long.
        /// </summary>
        private byte[] song() => new BeatmapSetFiles(Game.BeatmapManager, BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!).Get(edited.Metadata.AudioFile);

        #endregion

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
