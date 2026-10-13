// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
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
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Difficulties and sounds of another beatmap set brought into the one being edited, without the folder of either
    /// being opened (2026-10-13).
    /// </summary>
    public partial class TestSceneBeatmapFilesImport : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private BeatmapInfo edited = null!;

        /// <summary>
        /// The set brought from.
        /// </summary>
        private BeatmapSetInfo other = null!;

        private BeatmapFilesScreen? screen => Game.ScreenStack.CurrentScreen as BeatmapFilesScreen;

        private ImportFromAnotherSet import => screen!.Import;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                var importedSet = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely()!.ID)!;
                edited = importedSet.Beatmaps.Single(b => b.Path == difficulty_file);

                var ours = new BeatmapSetFiles(Game.BeatmapManager, importedSet);

                other = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(archive(
                    (@"Them - Other Song (them) [Hitsounds].osu", Encoding.UTF8.GetBytes(difficulty(@"Other Song", @"Hitsounds"))),
                    (@"Them - Other Song (them) [Hard].osu", Encoding.UTF8.GetBytes(difficulty(@"Other Song", @"Hard"))),
                    (@"their song.mp3", new byte[] { 1, 2, 3 }),
                    // the very file the set being edited has.
                    (@"Snare.wav", ours.Get(@"Snare.wav")),
                    // another sound under a name the set being edited has.
                    (@"Kick.wav", ours.Get(@"D_Clap.wav")),
                    // and another kind of file of a name it has.
                    (@"D_Clap.ogg", new byte[] { 9, 9, 9 }),
                    (@"soft-hitclap2.wav", ours.Get(@"D_Snare.wav")),
                    (@"drums/tom.ogg", new byte[] { 4, 5, 6 })), @"other.osz")).GetResultSafely()!.ID)!;

                Game.BeatmapManager.Import(new ImportTask(archive(
                    (@"Them - Third Song (somebody) [Easy].osu", Encoding.UTF8.GetBytes(difficulty(@"Third Song", @"Easy").Replace(@"Creator:them", @"Creator:somebody"))),
                    (@"their song.mp3", new byte[] { 7, 8, 9 })), @"third.osz")).GetResultSafely();

                return Game.BeatmapManager.GetWorkingBeatmap(Game.BeatmapManager.QueryBeatmap(b => b.ID == edited.ID)!, true);
            };

            base.SetUpSteps();

            AddStep("open the files of the set", () => Editor.OpenBeatmapFiles());
            AddUntilStep("the sets of the game are listed", () => screen?.IsLoaded == true && import.SetsListed);
        }

        [Test]
        public void TestTheOtherSetsOfTheGameAreListedAndSearched()
        {
            AddAssert("every set but the one being edited", titles, () => Is.EquivalentTo(new[] { @"Them - Other Song", @"Them - Third Song" }));

            AddStep("search for a title", () => import.SearchBox.Current.Value = @"OTHER");
            AddUntilStep("the set of that title", titles, () => Is.EqualTo(new[] { @"Them - Other Song" }));

            AddStep("search for a title and a mapper", () => import.SearchBox.Current.Value = @"song somebody");
            AddUntilStep("the set that is both", titles, () => Is.EqualTo(new[] { @"Them - Third Song" }));

            AddStep("search for nothing there is", () => import.SearchBox.Current.Value = @"renatus");
            AddUntilStep("no set", titles, () => Is.Empty);
        }

        [Test]
        public void TestTheSoundsOfASetAreListedByWhatBringingThemInWouldDo()
        {
            choose();

            // the song is no sound of the set.
            AddAssert("its sounds", () => import.SoundRows.Select(r => (r.Sound.Name, r.Sound.Standing, r.Box.Current.Value)), () => Is.EqualTo(new[]
            {
                (@"drums/tom.ogg", ImportStanding.New, true),
                (@"D_Clap.ogg", ImportStanding.InPlaceOfAnother, false),
                (@"Kick.wav", ImportStanding.InPlaceOfAnother, false),
                (@"Snare.wav", ImportStanding.ThereAlready, false),
                (@"soft-hitclap2.wav", ImportStanding.New, true),
            }));

            AddAssert("one there already cannot be ticked", () => import.SoundRows.Single(r => r.Sound.Name == @"Snare.wav").Box.Current.Disabled);
            AddAssert("its difficulties, none ticked", () => import.DifficultyRows.Select(r => (r.Difficulty.DifficultyName, r.Box.Current.Value)),
                () => Is.EquivalentTo(new[] { (@"Hitsounds", false), (@"Hard", false) }));
            AddAssert("there is something to bring in", () => import.ImportButton.Enabled.Value);
        }

        [Test]
        public void TestSoundsAreBroughtInAsTicked()
        {
            string? kick = null;

            choose();
            AddStep("note the kick of the set being edited", () => kick = hashOf(@"Kick.wav"));

            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => import.Changed);

            AddAssert("the new sounds are files of the set", () => new[] { hashOf(@"drums/tom.ogg"), hashOf(@"soft-hitclap2.wav") },
                () => Is.EqualTo(new[] { theirHashOf(@"drums/tom.ogg"), theirHashOf(@"soft-hitclap2.wav") }).And.All.Not.Null);
            AddAssert("the kick is the one it was", () => hashOf(@"Kick.wav"), () => Is.EqualTo(kick));

            AddAssert("they are there already now", () => import.SoundRows.Where(r => r.Sound.Standing == ImportStanding.ThereAlready).Select(r => r.Sound.Name),
                () => Is.EqualTo(new[] { @"drums/tom.ogg", @"Snare.wav", @"soft-hitclap2.wav" }));
            AddAssert("nothing more to bring in", () => !import.ImportButton.Enabled.Value);
        }

        [Test]
        public void TestSoundsBroughtInAreSoundsOfTheSetToBeNamed()
        {
            var sounds = () => screen!.Sounds;

            choose();
            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => import.Changed);

            AddStep("go to the sounds of the set", () => screen!.Tab.Value = BeatmapFilesScreen.FilesTab.Sounds);
            AddAssert("the ones brought in are among them", () => sounds().Rows.Select(r => r.Filename), () => Is.SupersetOf(new[] { @"drums/tom.ogg", @"soft-hitclap2.wav" }));

            AddStep("choose the one in a folder", () => sounds().Rows.Single(r => r.Filename == @"drums/tom.ogg").TriggerClick());
            AddStep("say what it is to be", () =>
            {
                sounds().Bank.Current.Value = SoundsOfTheSet.NamedBank.Normal;
                sounds().Sound.Current.Value = SoundsOfTheSet.NamedSound.HitNormal;
                sounds().Number.Current.Value = @"9";
            });

            // beside the difficulties, where stable looks for a hitsound, and the kind of file it was.
            AddAssert("the name is out of the folder", () => sounds().NewName, () => Is.EqualTo(@"normal-hitnormal9.ogg"));
            AddStep("rename", () => sounds().RenameButton.TriggerClick());
            AddAssert("the file has the name", () => hashOf(@"normal-hitnormal9.ogg"), () => Is.EqualTo(theirHashOf(@"drums/tom.ogg")).And.Not.Null);
            AddAssert("and not the one it had", () => hashOf(@"drums/tom.ogg"), () => Is.Null);

            AddStep("back to the other set", () => screen!.Tab.Value = BeatmapFilesScreen.FilesTab.Import);
            AddAssert("its sound is one to bring in again", () => import.SoundRows.Single(r => r.Sound.Name == @"drums/tom.ogg").Sound.Standing, () => Is.EqualTo(ImportStanding.New));
        }

        [Test]
        public void TestASoundInPlaceOfAnotherIsBroughtInOnceItIsSaidTo()
        {
            string? kick = null;

            choose();
            AddStep("note the kick of the set being edited", () => kick = hashOf(@"Kick.wav"));

            AddStep("tick every sound", () => import.TickAllButton!.TriggerClick());
            AddAssert("the one in place of another is ticked", () => import.SoundRows.Single(r => r.Sound.Name == @"Kick.wav").Box.Current.Value);
            AddAssert("the one there already is not", () => !import.SoundRows.Single(r => r.Sound.Name == @"Snare.wav").Box.Current.Value);

            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("asked first", () => dialog is ConfirmDialog);
            AddAssert("nothing brought in yet", () => hashOf(@"Kick.wav") == kick && hashOf(@"drums/tom.ogg") == null);

            AddStep("say so", () => dialog!.PerformOkAction());
            AddUntilStep("brought in", () => import.Changed);
            AddAssert("the kick is the other set's", () => hashOf(@"Kick.wav"), () => Is.EqualTo(theirHashOf(@"Kick.wav")));
            AddAssert("under the one name still", () => editedSet().Files.Count(f => BeatmapSetSounds.IsTheSameSound(f.Filename, @"Kick.wav")), () => Is.EqualTo(1));
        }

        [Test]
        public void TestAnotherKindOfFileOfTheNameMakesWayForTheOneBroughtIn()
        {
            choose();

            AddStep("untick the sounds", () => import.TickNoneButton!.TriggerClick());
            AddStep("tick the one the set has as another kind of file", () => import.SoundRows.Single(r => r.Sound.Name == @"D_Clap.ogg").Box.Current.Value = true);

            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("asked first", () => dialog is ConfirmDialog);
            AddStep("say so", () => dialog!.PerformOkAction());
            AddUntilStep("brought in", () => import.Changed);

            // stable finds a sound by its name whatever kind of file it is: with both there, which is played is not
            // the mapper's to say.
            AddAssert("the set has the one brought in", () => hashOf(@"D_Clap.ogg"), () => Is.EqualTo(theirHashOf(@"D_Clap.ogg")).And.Not.Null);
            AddAssert("and not the one it had", () => hashOf(@"D_Clap.wav"), () => Is.Null);
        }

        [Test]
        public void TestADifficultyBroughtInIsOneOfThisSet()
        {
            string theirs = null!;
            string ours = null!;

            choose();
            AddStep("untick the sounds", () => import.TickNoneButton!.TriggerClick());
            AddAssert("nothing to bring in", () => !import.ImportButton.Enabled.Value);

            AddStep("tick the hitsound difficulty", () => import.DifficultyRows.Single(r => r.Difficulty.DifficultyName == @"Hitsounds").Box.Current.Value = true);
            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => import.Changed);

            AddStep("read the files", () =>
            {
                theirs = difficulty(@"Other Song", @"Hitsounds");
                ours = fileOf(editedSet().Beatmaps.Single(b => b.DifficultyName == @"Hitsounds"));
            });

            AddAssert("it is a difficulty of the set's song", () => editedSet().Beatmaps.Single(b => b.DifficultyName == @"Hitsounds").Metadata,
                () => Has.Property(nameof(BeatmapMetadata.Title)).EqualTo(edited.Metadata.Title)
                         .And.Property(nameof(BeatmapMetadata.AudioFile)).EqualTo(edited.Metadata.AudioFile));

            AddAssert("its file says the set's song", () => section(ours, @"[General]"), () => Does.Contain($@"AudioFilename: {edited.Metadata.AudioFile}"));
            AddAssert("and the set's names", () => section(ours, @"[Metadata]"), () => Does.Contain($@"Title:{edited.Metadata.Title}").And.Contain(@"Version:Hitsounds"));

            // as stable wrote them: lazer writing the file again would put the green lines where the objects are.
            AddAssert("its lines are as they were", () => section(ours, @"[TimingPoints]"), () => Is.EqualTo(section(theirs, @"[TimingPoints]")));
            AddAssert("its objects are as they were", () => section(ours, @"[HitObjects]"), () => Is.EqualTo(section(theirs, @"[HitObjects]")));

            AddAssert("no sound came with it", () => hashOf(@"drums/tom.ogg"), () => Is.Null);
        }

        [Test]
        public void TestADifficultyOfATakenNameIsGivenAnother()
        {
            choose();

            AddStep("tick the difficulty named as the one being edited", () => import.DifficultyRows.Single(r => r.Difficulty.DifficultyName == @"Hard").Box.Current.Value = true);
            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => import.Changed);

            AddAssert("both are there", () => editedSet().Beatmaps.Select(b => b.DifficultyName).Where(n => n.StartsWith(@"Hard", StringComparison.Ordinal)),
                () => Is.EquivalentTo(new[] { @"Hard", @"Hard (1)" }));
            AddAssert("the one brought in says its name", () => section(fileOf(editedSet().Beatmaps.Single(b => b.DifficultyName == @"Hard (1)")), @"[Metadata]"),
                () => Does.Contain(@"Version:Hard (1)"));
        }

        [Test]
        public void TestTheEditorOpensTheSetAgainForWhatWasBroughtIn()
        {
            Editor before = null!;

            AddStep("leave with nothing brought in", () =>
            {
                before = Editor;
                screen!.Exit();
            });
            AddUntilStep("back in the editor as it was", () => Game.ScreenStack.CurrentScreen == before);

            AddStep("open the files of the set", () => Editor.OpenBeatmapFiles());
            AddUntilStep("the sets of the game are listed", () => screen?.IsLoaded == true && import.SetsListed);

            choose();
            AddStep("tick the hitsound difficulty", () => import.DifficultyRows.Single(r => r.Difficulty.DifficultyName == @"Hitsounds").Box.Current.Value = true);
            AddStep("bring in", () => import.ImportButton.TriggerClick());
            AddUntilStep("brought in", () => import.Changed);

            AddStep("leave", () => screen!.Exit());
            AddUntilStep("the editor opens the set again", () => Editor != before && Editor?.ReadyForUse == true);

            AddAssert("on the difficulty it was on", () => EditorBeatmap.BeatmapInfo.ID, () => Is.EqualTo(edited.ID));
            AddAssert("with the one brought in among its difficulties", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.Select(b => b.DifficultyName), () => Does.Contain(@"Hitsounds"));
        }

        private void choose()
        {
            AddStep("choose the other set", () => import.SetRows.Single(r => r.Set.ID == other.ID).TriggerClick());
            AddUntilStep("what it has is shown", () => import.DifficultyRows.Any());
        }

        private string?[] titles() => import.SetRows.Select(r => r.Set.Title.Romanised).ToArray();

        private PopupDialog? dialog => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog;

        private BeatmapSetInfo editedSet() => BeatmapSetFiles.Read(Game.Realm, edited.BeatmapSet!.ID)!;

        private string? hashOf(string name) => editedSet().GetFile(name)?.File.Hash;

        private string? theirHashOf(string name) => other.GetFile(name)?.File.Hash;

        private string fileOf(BeatmapInfo difficulty) => Encoding.UTF8.GetString(new BeatmapSetFiles(Game.BeatmapManager, editedSet()).Get(difficulty.Path!));

        /// <summary>
        /// The lines under <paramref name="name"/> in <paramref name="file"/>, up to the next section.
        /// </summary>
        private static string[] section(string file, string name) =>
            file.Replace("\r\n", "\n").Split('\n').SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).ToArray();

        /// <summary>
        /// A beatmap set as it is downloaded, of the files given.
        /// </summary>
        private static MemoryStream archive(params (string name, byte[] contents)[] files)
        {
            var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var (name, contents) in files)
                {
                    // a file the set being edited was to have and has not: nothing is to be written for it unsaid.
                    ArgumentNullException.ThrowIfNull(contents, name);

                    using (var entry = zip.CreateEntry(name).Open())
                        entry.Write(contents);
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// A difficulty as stable writes one, with a green line that is on no object.
        /// </summary>
        private static string difficulty(string title, string name) => string.Join("\r\n", new[]
        {
            @"osu file format v14",
            @"",
            @"[General]",
            @"AudioFilename: their song.mp3",
            @"AudioLeadIn: 0",
            @"PreviewTime: 1234",
            @"Countdown: 0",
            @"SampleSet: Soft",
            @"StackLeniency: 0.7",
            @"Mode: 0",
            @"LetterboxInBreaks: 0",
            @"WidescreenStoryboard: 0",
            @"",
            @"[Editor]",
            @"DistanceSpacing: 1.2",
            @"BeatDivisor: 4",
            @"GridSize: 4",
            @"TimelineZoom: 2",
            @"",
            @"[Metadata]",
            $@"Title:{title}",
            $@"TitleUnicode:{title}",
            @"Artist:Them",
            @"ArtistUnicode:Them",
            @"Creator:them",
            $@"Version:{name}",
            @"Source:",
            @"Tags:their tags",
            @"BeatmapID:0",
            @"BeatmapSetID:-1",
            @"",
            @"[Difficulty]",
            @"HPDrainRate:5",
            @"CircleSize:4",
            @"OverallDifficulty:8",
            @"ApproachRate:9",
            @"SliderMultiplier:1.8",
            @"SliderTickRate:1",
            @"",
            @"[Events]",
            @"//Background and Video events",
            @"0,0,""their bg.jpg"",0,0",
            @"//Break Periods",
            @"//Storyboard Layer 0 (Background)",
            @"//Storyboard Layer 1 (Fail)",
            @"//Storyboard Layer 2 (Pass)",
            @"//Storyboard Layer 3 (Foreground)",
            @"//Storyboard Sound Samples",
            @"",
            @"[TimingPoints]",
            @"500,400,4,2,1,60,1,0",
            @"700,-100,4,2,2,40,0,0",
            @"900,-50,4,2,3,40,0,1",
            @"",
            @"",
            @"[HitObjects]",
            @"256,192,500,5,0,0:0:0:0:",
            @"256,192,900,1,8,0:0:0:0:",
            @"100,100,1300,2,0,L|200:100,1,90,2|0,0:0|0:0,0:0:0:0:",
            @"",
        });
    }
}
