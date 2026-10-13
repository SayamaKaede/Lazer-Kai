// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Rulesets.Mania.Edit;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The osu!mania difficulty the hitsound screen makes to hitsound in, where each lane is a sound and a note sounds as
    /// the lane it is put in (2026-10-11).
    /// </summary>
    public partial class TestSceneManiaHitsoundCanvas : StableFormatConversionTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        private BeatmapInfo imported = null!;

        private (double time, double beatLength)[] redLines = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();

            AddStep("put two sounds in the sound folder", () =>
            {
                var library = Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY);

                put(library, @"kick.wav", @"Samples/hitsound-no-delay.wav");
                put(library, @"drums/snare.wav", @"Samples/hitsound-delay.wav");
            });

            AddStep("note the red lines", () => redLines = EditorBeatmap.ControlPointInfo.TimingPoints.Select(t => (t.Time, t.BeatLength)).ToArray());
        }

        private static void put(Storage library, string name, string resource)
        {
            using (var stream = library.CreateFileSafely(name))
            using (var sound = TestResources.OpenResource(resource))
                sound.CopyTo(stream);
        }

        [Test]
        public void TestAnEmptyManiaDifficultyIsMade()
        {
            AddAssert("offered", () => Editor.CanMakeManiaHitsoundDifficulty.Value);

            make();

            AddAssert("it is osu!mania's", () => EditorBeatmap.BeatmapInfo.Ruleset.ShortName, () => Is.EqualTo(@"mania"));
            AddAssert("named as one to hitsound in", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo(@"Hitsounds (mania)"));
            AddAssert("with eighteen lanes", () => EditorBeatmap.Difficulty.CircleSize, () => Is.EqualTo(18));
            AddAssert("in two stages of nine", () => Editor.ChildrenOfType<Stage>().Select(s => s.Columns.Length), () => Is.EqualTo(new[] { 9, 9 }));
            AddAssert("empty", () => EditorBeatmap.HitObjects, () => Is.Empty);
            AddAssert("with the red lines of the difficulty it was made from",
                () => EditorBeatmap.ControlPointInfo.TimingPoints.Select(t => (t.Time, t.BeatLength)).ToArray(), () => Is.EqualTo(redLines));

            AddAssert("still on the hitsound screen", () => Editor.Mode.Value, () => Is.EqualTo(EditorScreenMode.Hitsounds));
            AddAssert("which has a row for every lane", () => Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Count(), () => Is.EqualTo(Editor.HITSOUND_CANVAS_LANES));
            AddAssert("in place of the buttons", () => Editor.ChildrenOfType<HitsoundPanel>(), () => Is.Empty);
            AddAssert("each offering the sounds of the folder, in its folders too",
                () => Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().First().Sound.Items.Select(i => i.Path),
                () => Is.EqualTo(new[] { null, @"drums/snare.wav", @"kick.wav" }));

            AddAssert("not offered from it", () => !Editor.CanMakeManiaHitsoundDifficulty.Value);
        }

        [Test]
        public void TestANoteSoundsAsItsLane()
        {
            Note first = null!;
            Note second = null!;

            make();

            give(0, @"kick.wav");

            AddAssert("the sound is copied into the beatmap", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.GetFile(@"HS/kick.wav"), () => Is.Not.Null);
            AddAssert("and can be played from it", () => EditorBeatmap.BeatmapSkin!.GetSample(new ConvertHitObjectParser.FileHitSampleInfo(@"HS/kick.wav", 100)), () => Is.Not.Null);
            AddAssert("the lane says what it sounds as", () => Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == 0).SoundLabel!.Text.ToString(), () => Is.EqualTo(@"kick"));

            AddStep("put a note in that lane and one in the next", () =>
            {
                // as the compose screen puts one down: a plain hit.
                EditorBeatmap.Add(first = new Note { Column = 0, StartTime = redLines[0].time, Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });
                EditorBeatmap.Add(second = new Note
                {
                    Column = 1,
                    StartTime = redLines[0].time + redLines[0].beatLength,
                    Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
                });
            });

            AddAssert("the first sounds as its lane", () => fileOf(first), () => Is.EqualTo(@"HS/kick.wav"));
            AddAssert("the second as a plain hit", () => second.Samples.Select(s => s.Name), () => Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));

            AddStep("move the second into the first lane", () =>
            {
                second.Column = 0;
                EditorBeatmap.Update(second);
            });
            AddUntilStep("it sounds as that lane", () => fileOf(second), () => Is.EqualTo(@"HS/kick.wav"));

            give(0, @"drums/snare.wav");
            AddAssert("given another sound, the lane's notes have it", () => new[] { fileOf(first), fileOf(second) }, () => Is.All.EqualTo(@"HS/snare.wav"));

            give(0, null);
            AddAssert("with its sound taken away, they are plain hits",
                () => new[] { first, second }.SelectMany(n => n.Samples).Select(s => (s.Name, s is ConvertHitObjectParser.FileHitSampleInfo)),
                () => Is.All.EqualTo((HitSampleInfo.HIT_NORMAL, false)));
        }

        [Test]
        public void TestTheLanesAreKeptThroughASaveAndAReload()
        {
            make();

            give(2, @"kick.wav");

            AddStep("say it is to be a drum clap", () =>
            {
                var row = Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Single(r => r.Lane == 2);

                row.Bank.Current.Value = LaneSoundPanel.LaneBank.Drum;
                row.Kind.Current.Value = LaneSoundPanel.LaneKind.Clap;
            });

            AddStep("put a note in it", () => EditorBeatmap.Add(new Note { Column = 2, StartTime = redLines[0].time }));
            AddStep("save", () => Editor.Save());

            reopen();

            AddAssert("the lane has its sound", () => (lanes[2].File, lanes[2].Bank, lanes[2].Sound),
                () => Is.EqualTo((@"HS/kick.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP)));
            AddAssert("and its row says so", () =>
            {
                var row = Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Single(r => r.Lane == 2);
                return (row.Sound.Current.Value.Path, row.Bank.Current.Value, row.Kind.Current.Value);
            }, () => Is.EqualTo((@"kick.wav", LaneSoundPanel.LaneBank.Drum, LaneSoundPanel.LaneKind.Clap)));
            AddAssert("the note sounds as it", () => fileOf(EditorBeatmap.HitObjects.Single()), () => Is.EqualTo(@"HS/kick.wav"));
        }

        /// <summary>
        /// The two stages are as far apart as the setting says, rather than each in the middle of its own half of the
        /// playfield, which with most skins leaves a wide gap between them (2026-10-12).
        /// </summary>
        [Test]
        public void TestTheStagesAreAsFarApartAsAskedFor()
        {
            make();

            AddAssert("side by side as it comes", gapBetweenStages, () => Is.EqualTo(0).Within(0.5));

            AddStep("ask for forty", () => Game.LocalConfig.SetValue(OsuSetting.EditorManiaStageSpacing, 40f));
            AddUntilStep("forty", gapBetweenStages, () => Is.EqualTo(40).Within(0.5));

            AddStep("ask for ten", () => Game.LocalConfig.SetValue(OsuSetting.EditorManiaStageSpacing, 10f));
            AddUntilStep("ten", gapBetweenStages, () => Is.EqualTo(10).Within(0.5));

            AddStep("setting back", () => Game.LocalConfig.GetBindable<float>(OsuSetting.EditorManiaStageSpacing).SetDefault());
        }

        /// <summary>
        /// The room between the two stages, in pixels of the playfield.
        /// </summary>
        private float gapBetweenStages()
        {
            var playfield = Editor.ChildrenOfType<ManiaPlayfield>().Single();
            var stages = playfield.Stages;

            return playfield.ToLocalSpace(stages[1].ScreenSpaceDrawQuad.TopLeft).X - playfield.ToLocalSpace(stages[0].ScreenSpaceDrawQuad.TopRight).X;
        }

        /// <summary>
        /// A lane of the second stage has its sound as one of the first does.
        /// </summary>
        [Test]
        public void TestTwoStagesOfLanesEachHaveASound()
        {
            Note note = null!;

            make();

            AddAssert("a row for every lane", () => Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Count(), () => Is.EqualTo(18));
            AddAssert("and a lane for every row", () => Editor.ChildrenOfType<EditorColumn>().Select(c => c.Index).OrderBy(i => i), () => Is.EqualTo(Enumerable.Range(0, 18)));

            give(17, @"kick.wav");

            AddStep("put a note in the last lane", () => EditorBeatmap.Add(note = new Note
            {
                Column = 17,
                StartTime = redLines[0].time,
                Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
            }));

            AddAssert("it sounds as its lane", () => fileOf(note), () => Is.EqualTo(@"HS/kick.wav"));
            AddAssert("which says what it sounds as", () => Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == 17).SoundLabel!.Text.ToString(), () => Is.EqualTo(@"kick"));
        }

        /// <summary>
        /// A hold note's head is a note of its own, made from the hold note as it is put down: before it is made to
        /// sound as its lane.
        /// </summary>
        [Test]
        public void TestAHoldNotesHeadSoundsAsItsLane()
        {
            HoldNote hold = null!;

            make();

            give(0, @"kick.wav");

            AddStep("put a hold note in the lane", () => EditorBeatmap.Add(hold = new HoldNote
            {
                Column = 0,
                StartTime = redLines[0].time,
                Duration = redLines[0].beatLength,
                Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) },
            }));

            AddUntilStep("its head sounds as the lane", () => fileOf(hold.Head), () => Is.EqualTo(@"HS/kick.wav"));
        }

        /// <summary>
        /// The difficulty is turned into an osu! one of the same beatmap set, as stable has one: a circle wherever lanes
        /// sound, saying which sample sets and which added sounds, and the sounds themselves under the names stable
        /// looks those up by (2026-10-13).
        /// </summary>
        [Test]
        public void TestTheDifficultyIsTurnedIntoAHitsoundDifficulty()
        {
            make();

            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");
            AddStep("say the second lane is to be a soft clap", () => lanes.SetSlot(1, HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP));

            AddStep("put the kick on the first beat, both on the second and the clap alone on the third", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) });
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(1) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(1) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(2) });
            });

            convert();

            AddAssert("there is an osu! difficulty for it", () => (lanes.Converted?.DifficultyName, lanes.Converted?.Ruleset.ShortName), () => Is.EqualTo((@"Hitsounds", @"osu")));
            AddAssert("as stable has one", () => hitsoundDifficulty().First(), () => Is.EqualTo(@"osu file format v14"));

            AddAssert("a circle wherever lanes sound, in the middle", () => section(@"[HitObjects]"), () => Is.EqualTo(new[]
            {
                // the kick is the normal set's hit sound.
                $@"256,192,{whole(beat(0))},5,0,1:1:0:0:",
                $@"256,192,{whole(beat(1))},5,8,1:2:0:0:",
                // with no lane that is a hit sound, it is the soft one of the skin.
                $@"256,192,{whole(beat(2))},5,8,2:2:0:0:",
            }));

            AddAssert("the red lines say the custom index the sounds are under, with no green line for it",
                () => lines().Select(l => (l.red, l.index, l.volume)), () => Is.All.EqualTo((true, 1, 100)));

            AddAssert("the kick is the normal hit sound of the beatmap", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashOf(@"HS/kick.wav")));
            AddAssert("the snare its soft clap", () => hashOf(@"soft-hitclap.wav"), () => Is.EqualTo(hashOf(@"HS/snare.wav")));
            AddAssert("and nothing is written for the skin's sound", () => hashOf(@"soft-hitnormal.wav"), () => Is.Null);

            AddAssert("still in the difficulty hitsounded in", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo(@"Hitsounds (mania)"));

            AddStep("save", () => Editor.Save());
            reopen();

            AddAssert("the hitsound difficulty is still there once this one is saved", () => lanes.Converted?.DifficultyName, () => Is.EqualTo(@"Hitsounds"));
            AddAssert("in the database too", () => Game.BeatmapManager.QueryBeatmap(b => b.ID == lanes.Converted!.ID)?.Ruleset.ShortName, () => Is.EqualTo(@"osu"));
        }

        /// <summary>
        /// Turned into one again, it is put in place of the one made before, and so are the sound files written then.
        /// </summary>
        [Test]
        public void TestTurningItIntoOneAgainPutsItInPlaceOfTheLast()
        {
            Guid first = Guid.Empty;

            make();

            give(0, @"kick.wav");
            AddStep("put a note in the lane", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) }));

            convert();
            AddStep("note which difficulty it is", () => first = lanes.Converted!.ID);

            give(0, @"drums/snare.wav");
            AddStep("put another note in the lane", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(1) }));

            convert();

            AddAssert("it is the same difficulty", () => lanes.Converted?.ID, () => Is.EqualTo(first));
            AddAssert("and the only one made", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.Count(b => b.DifficultyName.StartsWith(@"Hitsounds", StringComparison.Ordinal) && b.Ruleset.ShortName == @"osu"),
                () => Is.EqualTo(1));
            AddAssert("with both circles", () => section(@"[HitObjects]"), () => Has.Length.EqualTo(2));
            AddAssert("and the sound the lane has now", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashOf(@"HS/snare.wav")));
        }

        /// <summary>
        /// A sound file of the mapper's own that one of the files is to be found in place of is not written over
        /// without asking.
        /// </summary>
        [Test]
        public void TestASoundFileOfTheMappersOwnIsAskedAboutBeforeItIsWrittenOver()
        {
            make();

            give(0, @"kick.wav");
            AddStep("put a note in the lane", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) }));

            AddStep("put a normal hit sound of another kind in the beatmap", () =>
            {
                using (var sound = TestResources.OpenResource(@"Samples/hitsound-delay.wav"))
                    Game.BeatmapManager.AddFile(EditorBeatmap.BeatmapInfo.BeatmapSet!, sound, @"normal-hitnormal.wav");
            });

            AddStep("turn it into a hitsound difficulty", () => lanes.ConvertToHitsoundDifficulty());

            AddUntilStep("asked first", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog, () => Is.InstanceOf<ConfirmDialog>());
            AddAssert("with nothing made yet", () => lanes.Converted, () => Is.Null);
            AddAssert("nor written", () => hashOf(@"normal-hitnormal.wav"), () => Is.Not.EqualTo(hashOf(@"HS/kick.wav")));

            AddStep("say yes", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.PerformOkAction());

            AddUntilStep("made", () => lanes.Converted, () => Is.Not.Null);
            AddAssert("and written", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashOf(@"HS/kick.wav")));
        }

        /// <summary>
        /// The volume is set from the playhead on, as the volume of the green line at the playhead is on the hitsound
        /// screen of an osu! difficulty, and goes into the hitsound difficulty as green lines.
        /// </summary>
        [Test]
        public void TestTheVolumeIsSetFromThePlayheadOn()
        {
            Note[] notes = null!;

            make();

            give(0, @"kick.wav");
            AddStep("put a note on each of three beats", () =>
            {
                notes = Enumerable.Range(0, 3).Select(i => new Note { Column = 0, StartTime = beat(i) }).ToArray();
                EditorBeatmap.AddRange(notes);
            });

            AddStep("go to the second", () => clock.Seek(beat(1)));
            AddStep("turn the volume down to sixty", () => panel.Volume.Current.Value = 60);

            AddAssert("notes are that loud from there on", () => notes.Select(n => n.Samples.Single().Volume), () => Is.EqualTo(new[] { 100, 60, 60 }));
            AddUntilStep("there is a button for it", () => panel.VolumePresets.Select(b => b.Value), () => Is.EqualTo(new[] { 60, 100 }));

            AddStep("put a note on the fourth beat", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(3) }));
            AddAssert("it is as loud", () => EditorBeatmap.HitObjects.Last().Samples.Single().Volume, () => Is.EqualTo(60));

            AddStep("go back to the first", () => clock.Seek(beat(0)));
            AddUntilStep("the volume shown is the one there", () => panel.Volume.Current.Value, () => Is.EqualTo(100));

            convert();
            AddAssert("the hitsound difficulty has a green line where it changes", () => greenLines(), () => Is.EqualTo(new[] { (whole(beat(1)), 1, 60) }));
            AddAssert("and the red lines after it say the same", () => lines().Where(l => l.red).Select(l => l.volume),
                () => Is.EqualTo(Enumerable.Repeat(60, redLines.Length - 1).Prepend(100)));

            AddStep("save", () => Editor.Save());
            reopen();

            AddAssert("the volume is kept through a save and a reload", () => lanes.VolumeAt(beat(2)), () => Is.EqualTo(60));
        }

        /// <summary>
        /// Without the volumes set here, the volumes of the hitsound difficulty made before are kept as its lines say
        /// them: the volume can be set there instead.
        /// </summary>
        [Test]
        public void TestWithoutTheVolumesTheOnesOfTheHitsoundDifficultyAreKept()
        {
            make();

            give(0, @"kick.wav");
            AddStep("put a note on each of three beats", () => EditorBeatmap.AddRange(Enumerable.Range(0, 3).Select(i => new Note { Column = 0, StartTime = beat(i) })));

            AddStep("sixty from the second beat on", () =>
            {
                clock.Seek(beat(1));
                lanes.SetVolume(60);
            });

            convert();

            AddStep("leave the volumes out", () => panel.WithVolumes.Current.Value = false);
            AddStep("thirty from the second beat on, here", () => lanes.SetVolume(30));

            convert();

            AddAssert("the hitsound difficulty is as loud as it was", () => greenLines(), () => Is.EqualTo(new[] { (whole(beat(1)), 1, 60) }));

            AddStep("put the volumes in again", () => panel.WithVolumes.Current.Value = true);

            convert();

            AddAssert("with them, it is as loud as is said here", () => greenLines(), () => Is.EqualTo(new[] { (whole(beat(1)), 1, 30) }));
        }

        private LaneSoundPanel panel => Editor.ChildrenOfType<LaneSoundPanel>().Single();

        private EditorClock clock => Editor.ChildrenOfType<EditorClock>().Single();

        private double beat(int number) => redLines[0].time + number * redLines[0].beatLength;

        /// <summary>
        /// A time as a file has it.
        /// </summary>
        private static int whole(double time) => (int)Math.Floor(time + 1e-6);

        private void convert()
        {
            AddStep("turn it into a hitsound difficulty", () => panel.ConvertButton.TriggerClick());
            AddUntilStep("done", () => lanes.Converted, () => Is.Not.Null);
        }

        /// <summary>
        /// The file of the hitsound difficulty the difficulty was turned into, by its lines.
        /// </summary>
        private string[] hitsoundDifficulty()
        {
            var set = EditorBeatmap.BeatmapInfo.BeatmapSet!;

            using (var stream = Game.Beatmap.Value.GetStream(set.GetFile(lanes.Converted!.Path!)!.File.GetStoragePath()))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd().Replace("\r\n", "\n").Split('\n');
        }

        private string[] section(string name) => hitsoundDifficulty().SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();

        /// <summary>
        /// The red and green lines of the hitsound difficulty: when, which, and the custom index and volume each says.
        /// </summary>
        private (int time, bool red, int index, int volume)[] lines() =>
            section(@"[TimingPoints]").Select(l => l.Split(','))
                                      .Select(f => ((int)double.Parse(f[0], CultureInfo.InvariantCulture), f[6] == @"1", int.Parse(f[4]), int.Parse(f[5])))
                                      .ToArray();

        private (int time, int index, int volume)[] greenLines() => lines().Where(l => !l.red).Select(l => (l.time, l.index, l.volume)).ToArray();

        private string? hashOf(string file) => EditorBeatmap.BeatmapInfo.BeatmapSet!.GetFile(file)?.File.Hash;

        private void reopen()
        {
            Editor? before = null;

            AddStep("open the difficulty again", () => (before = Editor).SwitchToDifficulty(EditorBeatmap.BeatmapInfo));
            AddUntilStep("opened", () => Editor != before && Editor?.ReadyForUse == true);
            AddUntilStep("its lanes are up", () => Editor.ChildrenOfType<LaneSoundPanel>().SingleOrDefault()?.IsLoaded == true);
        }

        private LaneSounds lanes => Editor.ChildrenOfType<LaneSounds>().Single();

        private void give(int lane, string? sound) => AddStep($"give lane {lane + 1} {sound ?? "no sound"}", () => lanes.Give(lane, sound));

        private static string? fileOf(HitObject hitObject) => (hitObject.Samples.SingleOrDefault() as ConvertHitObjectParser.FileHitSampleInfo)?.Filename;

        private void make()
        {
            Editor? before = null;

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddStep("make an osu!mania difficulty to hitsound in", () => (before = Editor).MakeManiaHitsoundDifficulty());
            AddUntilStep("the editor opens it", () => Editor != before && Editor?.ReadyForUse == true);
            AddUntilStep("its lanes are up", () => Editor.ChildrenOfType<LaneSoundPanel>().SingleOrDefault()?.IsLoaded == true);
        }
    }
}
