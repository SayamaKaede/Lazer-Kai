// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Notifications;
using osu.Game.Rulesets.Mania.Edit;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Skinning;
using osu.Game.Tests.Resources;
using osuTK.Input;

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

            // the lanes sound as the files of the folder themselves. They were copied into the beatmap until 2026-10-13,
            // under a folder of their own, which a mapper who had only given a lane a sound then found in the beatmap.
            AddAssert("nothing is put in the beatmap for it", beatmapFiles, () => Is.EqualTo(filesAtFirst));
            AddAssert("the lane is the sound of the folder", () => lanes[0].File, () => Is.EqualTo(@"kick.wav"));
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

            AddAssert("the first sounds as its lane", () => fileOf(first), () => Is.EqualTo(@"kick.wav"));
            AddAssert("the second as a plain hit", () => second.Samples.Select(s => s.Name), () => Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));

            AddStep("move the second into the first lane", () =>
            {
                second.Column = 0;
                EditorBeatmap.Update(second);
            });
            AddUntilStep("it sounds as that lane", () => fileOf(second), () => Is.EqualTo(@"kick.wav"));

            give(0, @"drums/snare.wav");
            AddAssert("given another sound, the lane's notes have it", () => new[] { fileOf(first), fileOf(second) }, () => Is.All.EqualTo(@"drums/snare.wav"));

            // looked for under a name nothing in the beatmap has: the beatmap these tests are run on has a kick of its
            // own, which would be found under the name of the folder's.
            AddStep("seek to the first", () => clock.Seek(first.StartTime));
            AddUntilStep("what it has found to play is the sound of the folder", () => soundsFoundFor(first), () => Is.EqualTo(new[] { @"drums/snare.wav" }));
            AddAssert("with nothing put in the beatmap for it", beatmapFiles, () => Is.EqualTo(filesAtFirst));

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
                () => Is.EqualTo((@"kick.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP)));
            AddAssert("and its row says so", () =>
            {
                var row = Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Single(r => r.Lane == 2);
                return (row.Sound.Current.Value.Path, row.Bank.Current.Value, row.Kind.Current.Value);
            }, () => Is.EqualTo((@"kick.wav", LaneSoundPanel.LaneBank.Drum, LaneSoundPanel.LaneKind.Clap)));
            AddAssert("the note sounds as it", () => fileOf(EditorBeatmap.HitObjects.Single()), () => Is.EqualTo(@"kick.wav"));
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

            AddAssert("it sounds as its lane", () => fileOf(note), () => Is.EqualTo(@"kick.wav"));
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

            AddUntilStep("its head sounds as the lane", () => fileOf(hold.Head), () => Is.EqualTo(@"kick.wav"));
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

            AddAssert("the kick is the normal hit sound of the beatmap", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashInFolder(@"kick.wav")));
            AddAssert("the snare its soft clap", () => hashOf(@"soft-hitclap.wav"), () => Is.EqualTo(hashInFolder(@"drums/snare.wav")));
            AddAssert("and nothing is written for the skin's sound", () => hashOf(@"soft-hitnormal.wav"), () => Is.Null);

            AddAssert("still in the difficulty hitsounded in", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo(@"Hitsounds (mania)"));

            AddStep("save", () => Editor.Save());
            reopen();

            AddAssert("the hitsound difficulty is still there once this one is saved", () => lanes.Converted?.DifficultyName, () => Is.EqualTo(@"Hitsounds"));
            AddAssert("in the database too", () => Game.BeatmapManager.QueryBeatmap(b => b.ID == lanes.Converted!.ID)?.Ruleset.ShortName, () => Is.EqualTo(@"osu"));
        }

        /// <summary>
        /// Two lanes that are the same sound of a circle, sounding at one time, are one file with both in it: a circle
        /// has the one hit sound. It is written under a custom index of its own, each of the two being something else
        /// alone (2026-10-14).
        /// </summary>
        [Test]
        public void TestLanesThatComeToOneSoundOfACircleAreMixedIntoOneFile()
        {
            make();

            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");

            AddStep("put the kick on the first beat, the snare on the second and both on the third", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(1) });
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(2) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(2) });
            });

            convert();

            AddAssert("three circles, each the normal hit sound", () => section(@"[HitObjects]"), () => Is.EqualTo(new[]
            {
                $@"256,192,{whole(beat(0))},5,0,1:1:0:0:",
                $@"256,192,{whole(beat(1))},5,0,1:1:0:0:",
                $@"256,192,{whole(beat(2))},5,0,1:1:0:0:",
            }));

            AddAssert("under a custom index each", () => new[] { indexAt(beat(0)), indexAt(beat(1)), indexAt(beat(2)) }, () => Is.Unique);

            AddAssert("the kick alone is the kick", () => hashOf(hitNormalUnder(indexAt(beat(0)))), () => Is.EqualTo(hashInFolder(@"kick.wav")));
            AddAssert("the snare alone the snare", () => hashOf(hitNormalUnder(indexAt(beat(1)))), () => Is.EqualTo(hashInFolder(@"drums/snare.wav")));

            AddAssert("the two at once are a file of their own", () => hashOf(hitNormalUnder(indexAt(beat(2)))),
                () => Is.Not.Null.And.Not.EqualTo(hashInFolder(@"kick.wav")).And.Not.EqualTo(hashInFolder(@"drums/snare.wav")));
            AddAssert("which is a wave file", () => Encoding.ASCII.GetString(bytesOf(hitNormalUnder(indexAt(beat(2)))), 0, 4), () => Is.EqualTo(@"RIFF"));

            string? mixed = null;

            AddStep("note the file", () => mixed = hashOf(hitNormalUnder(indexAt(beat(2)))));
            convert();
            AddAssert("turned into one again, the file is the same one", () => hashOf(hitNormalUnder(indexAt(beat(2)))), () => Is.EqualTo(mixed));
        }

        /// <summary>
        /// Asked to, a file with no sound in it is written for sliders to make as they are followed, for every sample
        /// set and custom index the lines of the hitsound difficulty say: where its hitsounds are copied to, that is
        /// what stable plays in place of the skin's. Taken away again when no longer asked for, and never put in place
        /// of one the mapper put there (2026-10-14).
        /// </summary>
        [Test]
        public void TestSlidersAreMadeSilentForWhatTheLinesSay()
        {
            string[] slides = null!;

            make();

            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");

            // both the normal hit sound, so each is under a custom index of its own.
            AddStep("put the kick on the first beat and the snare on the second", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(1) });
            });

            AddAssert("not asked for unless it is", () => panel.MuteSliderBodies.Current.Value, () => Is.False);
            convert();
            AddAssert("nothing is written for sliders", () => slideFiles(), () => Is.Empty);

            AddStep("ask for it", () => panel.MuteSliderBodies.Current.Value = true);
            convert();

            AddStep("note what the lines say", () => slides = HitsoundDifficultyWriter.SliderSlidesOf(string.Join('\n', hitsoundDifficulty())).Select(s => s + @".wav").ToArray());
            AddAssert("two custom indices, of the one sample set", () => slides.Select(s => s.Split('-')[1]), () => Is.EqualTo(new[] { @"sliderslide.wav", @"sliderslide2.wav" }));
            AddAssert("a file for each", () => slideFiles(), () => Is.EquivalentTo(slides));
            AddAssert("with no sound in it", () => slides.Select(s => System.Convert.ToHexString(bytesOf(s))), () => Is.All.EqualTo(
                "52494646" + "24000000" + "57415645" + "666D7420" + "10000000" + "0100" + "0200" + "44AC0000" + "10B10200" + "0400" + "1000" + "64617461" + "00000000"));

            AddStep("no longer ask for it", () => panel.MuteSliderBodies.Current.Value = false);
            convert();
            AddAssert("they are taken away", () => slideFiles(), () => Is.Empty);

            AddStep("put a sound of the mapper's own under the first of the names, as another kind of file", () =>
            {
                using (var sound = TestResources.OpenResource(@"Samples/hitsound-delay.wav"))
                    Game.BeatmapManager.AddFile(EditorBeatmap.BeatmapInfo.BeatmapSet!, sound, Path.ChangeExtension(slides[0], @".ogg"));
            });

            AddStep("ask for it again", () => panel.MuteSliderBodies.Current.Value = true);
            convert();
            AddAssert("the mapper's is left, and the other written", () => slideFiles(), () => Is.EquivalentTo(new[] { Path.ChangeExtension(slides[0], @".ogg"), slides[1] }));
            AddAssert("as it was", () => hashOf(Path.ChangeExtension(slides[0], @".ogg")), () => Is.EqualTo(hashInFolder(@"drums/snare.wav")));

            AddStep("save", () => Editor.Save());
            reopen();
            AddAssert("asked for still, opened again", () => panel.MuteSliderBodies.Current.Value);
        }

        private string[] slideFiles() => EditorBeatmap.BeatmapInfo.BeatmapSet!.Files.Select(f => f.Filename).Where(f => f.Contains(@"sliderslide")).ToArray();

        /// <summary>
        /// Lanes that will be heard together in one file once turned into a hitsound difficulty are marked in the colour
        /// of that file: on their rows, and over the lanes themselves where the notes are put (2026-10-14).
        /// </summary>
        [Test]
        public void TestLanesThatWillBeMixedAreMarkedInOneColour()
        {
            Note snare = null!;

            make();

            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");
            give(2, @"kick.wav");
            AddStep("say the third lane is to be a soft clap", () => lanes.SetSlot(2, HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP));

            AddAssert("nothing is mixed with no notes", () => lanes.Mixes, () => Is.Empty);

            AddStep("put the kick, the snare and the clap on the first beat", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) });
                EditorBeatmap.Add(snare = new Note { Column = 1, StartTime = beat(0) });
                EditorBeatmap.Add(new Note { Column = 2, StartTime = beat(0) });
            });

            AddUntilStep("the kick and the snare are to be mixed", () => lanes.Mixes.Select(m => string.Join('+', m)), () => Is.EqualTo(new[] { @"0+1" }));

            AddUntilStep("their rows are marked in one colour", () => (rowMarks(0).Length, rowMarks(1).Length), () => Is.EqualTo((1, 1)));
            AddAssert("the same one", () => rowMarks(0), () => Is.EqualTo(rowMarks(1)));
            AddAssert("the clap's is not marked", () => rowMarks(2), () => Is.Empty);

            AddStep("switch to the compose screen", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("the lanes are up", () => Editor.ChildrenOfType<EditorColumn>().Count(), () => Is.GreaterThanOrEqualTo(3));
            AddUntilStep("the two lanes are marked there in that colour", () => (columnMarks(0), columnMarks(1), columnMarks(2).Length),
                () => Is.EqualTo((new[] { LaneSounds.ColourOfMix(0) }, new[] { LaneSounds.ColourOfMix(0) }, 0)));

            AddStep("take the snare away", () => EditorBeatmap.Remove(snare));
            AddUntilStep("nothing is mixed", () => lanes.Mixes, () => Is.Empty);
            AddUntilStep("and no lane is marked", () => columnMarks(0).Length + columnMarks(1).Length, () => Is.Zero);
        }

        private Colour4[] rowMarks(int lane) =>
            Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Single(r => r.Lane == lane).ChildrenOfType<LaneMixMarks>().Single().Colours.ToArray();

        private Colour4[] columnMarks(int lane) =>
            Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == lane).ChildrenOfType<LaneMixMarks>().Single().Colours.ToArray();

        /// <summary>
        /// A click on what a lane sounds as, at its top on the compose screen, selects the notes of that lane and no
        /// others, whichever tool is in hand, and puts no note down (2026-10-14).
        /// </summary>
        [Test]
        public void TestAClickOnWhatALaneSoundsAsSelectsTheNotesOfTheLane()
        {
            make();

            // a lane with no sound has nothing written at its top, and so nothing to click.
            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");
            give(2, @"kick.wav");
            give(3, @"drums/snare.wav");

            AddStep("notes in three lanes, one of them held", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0) });
                EditorBeatmap.Add(new HoldNote { Column = 0, StartTime = beat(1), Duration = beat(1) - beat(0) });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(0) });
                EditorBeatmap.Add(new Note { Column = 2, StartTime = beat(1) });
            });

            AddStep("switch to the compose screen", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("the lanes are up", () => Editor.ChildrenOfType<EditorColumn>().Count(c => c.SoundLabelArea?.IsLoaded == true), () => Is.GreaterThanOrEqualTo(4));

            AddStep("put the mouse on what the second lane sounds as", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == 1).SoundLabelArea!));
            AddUntilStep("it says what a click does", () => (Editor.ChildrenOfType<ManiaBlueprintContainer>().Single() as IHasTooltip)?.TooltipText,
                () => Is.EqualTo(LazerKaiHitsoundStrings.SelectLaneNotes));
            AddStep("put it on the lane under that", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == 1).ScreenSpaceDrawQuad.Centre));
            AddUntilStep("which says nothing", () => (Editor.ChildrenOfType<ManiaBlueprintContainer>().Single() as IHasTooltip)?.TooltipText, () => Is.EqualTo(default(LocalisableString)));

            clickLane(0);
            AddAssert("its two are selected, and no others", () => selectedLanes(), () => Is.EqualTo(new[] { 0, 0 }));

            clickLane(1);
            AddAssert("then the one of the second alone", () => selectedLanes(), () => Is.EqualTo(new[] { 1 }));

            AddStep("take the note tool", () => InputManager.Key(Key.Number2));
            clickLane(2);
            AddAssert("with the note tool as well", () => selectedLanes(), () => Is.EqualTo(new[] { 2 }));

            AddStep("take the hold note tool", () => InputManager.Key(Key.Number3));
            clickLane(0);
            AddAssert("and the hold note tool", () => selectedLanes(), () => Is.EqualTo(new[] { 0, 0 }));

            clickLane(3);
            AddAssert("a lane with none in it leaves nothing selected", () => selectedLanes(), () => Is.Empty);

            AddAssert("no note was put down by any of it", () => EditorBeatmap.HitObjects.Count, () => Is.EqualTo(4));
        }

        private void clickLane(int lane) => AddStep($"click what lane {lane + 1} sounds as", () =>
        {
            InputManager.MoveMouseTo(Editor.ChildrenOfType<EditorColumn>().Single(c => c.Index == lane).SoundLabelArea!);
            InputManager.Click(MouseButton.Left);
        });

        private int[] selectedLanes() => EditorBeatmap.SelectedHitObjects.OfType<ManiaHitObject>().Select(n => n.Column).OrderBy(c => c).ToArray();

        /// <summary>
        /// The custom index the lines of the hitsound difficulty say at a time.
        /// </summary>
        private int indexAt(double time) => lines().Last(l => l.time <= whole(time)).index;

        private static string hitNormalUnder(int index) => index == 1 ? @"normal-hitnormal.wav" : $@"normal-hitnormal{index}.wav";

        private byte[] bytesOf(string file)
        {
            using (var stream = Game.Beatmap.Value.GetStream(EditorBeatmap.BeatmapInfo.BeatmapSet!.GetFile(file)!.File.GetStoragePath()))
            using (var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                return memory.ToArray();
            }
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
            AddAssert("and the sound the lane has now", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashInFolder(@"drums/snare.wav")));
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
            AddAssert("nor written", () => hashOf(@"normal-hitnormal.wav"), () => Is.Not.EqualTo(hashInFolder(@"kick.wav")));

            AddStep("say yes", () => Game.ChildrenOfType<DialogOverlay>().Single().CurrentDialog!.PerformOkAction());

            AddUntilStep("made", () => lanes.Converted, () => Is.Not.Null);
            AddAssert("and written", () => hashOf(@"normal-hitnormal.wav"), () => Is.EqualTo(hashInFolder(@"kick.wav")));
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

        #region The sounds of the folder

        /// <summary>
        /// Until 2026-10-13 a sound a lane was given was copied into the beatmap under a folder of its own, and the
        /// lane was kept as the copy. Lanes kept so are the sounds of the folder the copies were made of. The copies
        /// are left where they are: nothing plays them now, and they are the mapper's to take out.
        /// </summary>
        [Test]
        public void TestLanesKeptAsCopiesInTheBeatmapAreTheSoundsOfTheFolder()
        {
            make();

            AddStep("put a note in the first lane", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } }));
            AddStep("save", () => Editor.Save());

            AddStep("a copy of the kick in the beatmap, as one was made then", () =>
            {
                using (var sound = TestResources.OpenResource(@"Samples/hitsound-no-delay.wav"))
                    Game.BeatmapManager.AddFile(EditorBeatmap.BeatmapInfo.BeatmapSet!, sound, @"HS/kick.wav");
            });
            AddStep("its lanes kept as they were then", () =>
            {
                string path = Path.Combine(LaneSoundProjectStore.DIRECTORY, LaneSoundProjectStore.FolderNameFor(EditorBeatmap.BeatmapInfo), @"lanes.json");

                using (var stream = Game.Storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                {
                    writer.Write(@"{ ""lanes"": [ { ""file"": ""HS/kick.wav"", ""source"": ""kick.wav"", ""bank"": ""drum"", ""sound"": ""hitclap"" } ],"
                                 + @" ""fallback"": { ""file"": ""HS/snare.wav"", ""source"": ""drums/snare.wav"", ""bank"": ""soft"", ""sound"": ""hitnormal"" } }");
                }
            });

            reopen();

            AddAssert("the first lane is the sound of the folder, and what it was to be", () => (lanes[0].File, lanes[0].Bank, lanes[0].Sound),
                () => Is.EqualTo((@"kick.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP)));
            AddAssert("so is the hit sound of a circle with none", () => (lanes.Fallback.File, lanes.Fallback.Bank), () => Is.EqualTo((@"drums/snare.wav", HitSampleInfo.BANK_SOFT)));
            AddAssert("its row says so", () => Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().Single(r => r.Lane == 0).Sound.Current.Value.Path, () => Is.EqualTo(@"kick.wav"));
            AddAssert("its note sounds as it", () => fileOf(EditorBeatmap.HitObjects.Single()), () => Is.EqualTo(@"kick.wav"));
            AddAssert("the copy is left in the beatmap", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.GetFile(@"HS/kick.wav"), () => Is.Not.Null);
        }

        /// <summary>
        /// A lane is a sound of the folder, so a sound taken out of the folder is a lane that no longer sounds. The
        /// mapper is told which as the difficulty is opened, and it is not turned into a hitsound difficulty with the
        /// skin's sound where that one was to be.
        /// </summary>
        [Test]
        public void TestASoundTakenOutOfTheFolderIsSaidToBeMissing()
        {
            const string said = @"These sounds are no longer in the hitsound folder: kick.wav";

            make();

            give(0, @"kick.wav");
            give(1, @"drums/snare.wav");
            AddStep("put a note in each", () =>
            {
                EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });
                EditorBeatmap.Add(new Note { Column = 1, StartTime = beat(1), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });
            });
            AddStep("save", () => Editor.Save());

            AddStep("take the kick out of the folder", () => Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY).Delete(@"kick.wav"));

            reopen();

            AddUntilStep("it is said to be missing", () => timesSaid(said), () => Is.EqualTo(1));
            AddAssert("the lane is still said to be it", () => lanes[0].File, () => Is.EqualTo(@"kick.wav"));

            AddStep("turn it into a hitsound difficulty", () => lanes.ConvertToHitsoundDifficulty());

            AddUntilStep("it is said again", () => timesSaid(said), () => Is.EqualTo(2));
            AddAssert("and nothing is made", () => lanes.Converted, () => Is.Null);

            AddAssert("another lane cannot be given it", () => lanes.Give(2, @"kick.wav"), () => Is.False);
            AddAssert("and is left with none", () => lanes[2].HasFile, () => Is.False);
        }

        private int timesSaid(string text) => Game.ChildrenOfType<SimpleErrorNotification>().Count(n => n.Text.ToString() == text);

        /// <summary>
        /// Reading the folder again is hearing it again: a sound put back in it, or put in it under the name of
        /// another, is what the notes play from then on, rather than what was read under that name before.
        /// </summary>
        [Test]
        public void TestTheFolderReadAgainIsHeardAgain()
        {
            HitObject note = null!;

            // a sound of a name nothing in the beatmap has, to be found in the folder or not at all.
            AddStep("put a tom in the sound folder", () => put(Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY), @"tom.wav", @"Samples/hitsound-no-delay.wav"));

            make();

            give(0, @"tom.wav");
            AddStep("put a note in the lane", () => EditorBeatmap.Add(new Note { Column = 0, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } }));
            AddStep("save", () => Editor.Save());

            AddStep("take the tom out of the folder", () => Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY).Delete(@"tom.wav"));

            reopen();

            AddStep("seek to the note", () => clock.Seek((note = EditorBeatmap.HitObjects.Single()).StartTime));
            AddUntilStep("it is on screen", () => Editor.ChildrenOfType<DrawableHitObject>().Any(d => d.HitObject == note && d.IsLoaded));
            AddAssert("with no tom to play", () => soundsFoundFor(note), () => Does.Not.Contain(@"tom.wav"));

            AddStep("put the tom back", () => put(Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY), @"tom.wav", @"Samples/hitsound-no-delay.wav"));
            AddStep("read the folder again", () => panel.ChildrenOfType<RoundedButton>().Single(b => b.Text.ToString() == @"Reload the hitsound folder").TriggerClick());

            AddUntilStep("the note has the tom to play", () => soundsFoundFor(note), () => Is.EqualTo(new[] { @"tom.wav" }));
        }

        #endregion

        #region Presets

        /// <summary>
        /// The lanes are kept under a name and put back as a whole: on the lanes in place of what they have become, on
        /// the rows that show them, and on the notes, which sound as their lanes (2026-10-13).
        /// </summary>
        [Test]
        public void TestLanesKeptAsAPresetComeBackInPlaceOfTheOthers()
        {
            Note inTheFirst = null!;
            Note inTheSecond = null!;

            make();

            give(0, @"kick.wav");
            give(3, @"drums/snare.wav");
            AddStep("the fourth is to be a drum clap", () => lanes.SetSlot(3, HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP));
            AddStep("a circle with none is a kick of the drum set", () =>
            {
                lanes.GiveFallback(@"kick.wav");
                lanes.SetFallbackBank(HitSampleInfo.BANK_DRUM);
            });

            openPresets();
            AddAssert("none are kept yet", rowNames, () => Is.Empty);

            saveAs(@"kit", @"Save");

            AddAssert("the list shows it", rowNames, () => Is.EqualTo(new[] { @"kit" }));
            AddAssert("it is kept beside the game, by the sounds of the folder", () => soundsOf(@"kit"), () => Is.EqualTo(new[]
            {
                (0, @"kick.wav", HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                (3, @"drums/snare.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP),
            }));
            AddAssert("with a lane for every lane", () => presets.Load().Find(@"kit")!.Lanes.Count, () => Is.EqualTo(Editor.HITSOUND_CANVAS_LANES));
            AddAssert("and the hit sound of a circle with none", () =>
            {
                var fallback = presets.Load().Find(@"kit")!.Fallback;
                return (fallback.Source, fallback.Bank);
            }, () => Is.EqualTo((@"kick.wav", HitSampleInfo.BANK_DRUM)));

            AddStep("put a note in the first lane and one in the second", () =>
            {
                EditorBeatmap.Add(inTheFirst = new Note { Column = 0, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });
                EditorBeatmap.Add(inTheSecond = new Note { Column = 1, StartTime = beat(1), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } });
            });

            give(0, null);
            give(1, @"drums/snare.wav");
            give(3, null);
            AddStep("a circle with none is the skin's again", () =>
            {
                lanes.GiveFallback(null);
                lanes.SetFallbackBank(HitSampleInfo.BANK_SOFT);
            });
            AddAssert("the second note sounds as its lane", () => fileOf(inTheSecond), () => Is.EqualTo(@"drums/snare.wav"));

            AddStep("click the preset", () => row(@"kit").TriggerClick());

            AddAssert("the lanes are the preset's", () => Enumerable.Range(0, 4).Select(l => (lanes[l].File, lanes[l].Bank, lanes[l].Sound)).ToArray(), () => Is.EqualTo(new[]
            {
                (@"kick.wav", HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                (string.Empty, HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                (string.Empty, HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                (@"drums/snare.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP),
            }));
            AddAssert("so is the hit sound of a circle with none", () => (lanes.Fallback.File, lanes.Fallback.Bank), () => Is.EqualTo((@"kick.wav", HitSampleInfo.BANK_DRUM)));
            AddUntilStep("the rows say so", () => new[] { 0, 1, 3 }.Select(lane =>
            {
                var laneRow = Editor.ChildrenOfType<LaneSoundPanel.LaneRow>().SingleOrDefault(r => r.Lane == lane && r.IsLoaded);
                return (laneRow?.Sound.Current.Value.Path, laneRow?.Bank.Current.Value, laneRow?.Kind.Current.Value);
            }).ToArray(), () => Is.EqualTo(new (string?, LaneSoundPanel.LaneBank?, LaneSoundPanel.LaneKind?)[]
            {
                (@"kick.wav", LaneSoundPanel.LaneBank.Normal, LaneSoundPanel.LaneKind.Normal),
                (null, LaneSoundPanel.LaneBank.Normal, LaneSoundPanel.LaneKind.Normal),
                (@"drums/snare.wav", LaneSoundPanel.LaneBank.Drum, LaneSoundPanel.LaneKind.Clap),
            }));
            AddUntilStep("the first note sounds as its lane again", () => fileOf(inTheFirst), () => Is.EqualTo(@"kick.wav"));
            AddAssert("the second is a plain hit", () => inTheSecond.Samples.Select(s => (s.Name, s is ConvertHitObjectParser.FileHitSampleInfo)),
                () => Is.EqualTo(new[] { (HitSampleInfo.HIT_NORMAL, false) }));
            AddUntilStep("the presets are closed", () => popover?.State.Value != Visibility.Visible);

            AddStep("save", () => Editor.Save());
            reopen();

            AddAssert("the lanes are the preset's through a save and a reload", () => (lanes[0].File, lanes[1].File, lanes[3].File),
                () => Is.EqualTo((@"kick.wav", string.Empty, @"drums/snare.wav")));
        }

        /// <summary>
        /// A preset is of the sounds of the folder, not of a beatmap: one kept from any beatmap is offered in every
        /// other, and puts nothing in the beatmap it is put on.
        /// </summary>
        [Test]
        public void TestAPresetKeptElsewhereIsPutOnTheLanes()
        {
            Note note = null!;

            make();

            AddStep("a preset kept from another beatmap", () => keep(new LaneSoundPreset
            {
                Name = @"elsewhere",
                Lanes =
                {
                    new LaneSoundPresetSound(),
                    new LaneSoundPresetSound { Source = @"drums/snare.wav", Bank = HitSampleInfo.BANK_SOFT, Sound = HitSampleInfo.HIT_WHISTLE },
                },
            }));

            openPresets();
            AddAssert("it is offered", rowNames, () => Is.EqualTo(new[] { @"elsewhere" }));

            AddStep("click it", () => row(@"elsewhere").TriggerClick());

            AddAssert("nothing is put in the beatmap for it", beatmapFiles, () => Is.EqualTo(filesAtFirst));
            AddAssert("the second lane is it", () => (lanes[1].File, lanes[1].Bank, lanes[1].Sound),
                () => Is.EqualTo((@"drums/snare.wav", HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_WHISTLE)));

            AddStep("put a note in that lane", () => EditorBeatmap.Add(note = new Note { Column = 1, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } }));
            AddAssert("it sounds as it", () => fileOf(note), () => Is.EqualTo(@"drums/snare.wav"));
        }

        /// <summary>
        /// A sound taken out of the folder since the preset was kept is not put on its lane, which is left with none
        /// rather than with what it had: the lanes are the preset's and nothing else's. The mapper is told which.
        /// </summary>
        [Test]
        public void TestASoundNoLongerInTheFolderLeavesItsLaneWithNone()
        {
            Note note = null!;

            make();

            give(0, @"kick.wav");
            AddStep("put a note in the first lane", () => EditorBeatmap.Add(note = new Note { Column = 0, StartTime = beat(0), Samples = { new HitSampleInfo(HitSampleInfo.HIT_NORMAL) } }));
            AddAssert("it sounds as it", () => fileOf(note), () => Is.EqualTo(@"kick.wav"));

            AddStep("put on a preset of a sound that is gone and one that is not", () => lanes.ApplyPreset(new LaneSoundPreset
            {
                Name = @"old",
                Lanes =
                {
                    new LaneSoundPresetSound { Source = @"gone.wav", Bank = HitSampleInfo.BANK_DRUM, Sound = HitSampleInfo.HIT_FINISH },
                    new LaneSoundPresetSound { Source = @"kick.wav" },
                },
            }));

            AddAssert("the first lane has no sound, and is otherwise as the preset says", () => (lanes[0].File, lanes[0].Bank, lanes[0].Sound),
                () => Is.EqualTo((string.Empty, HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_FINISH)));
            AddUntilStep("its note is a plain hit", () => note.Samples.Select(s => (s.Name, s is ConvertHitObjectParser.FileHitSampleInfo)),
                () => Is.EqualTo(new[] { (HitSampleInfo.HIT_NORMAL, false) }));
            AddAssert("the second lane has its sound", () => lanes[1].File, () => Is.EqualTo(@"kick.wav"));
            AddUntilStep("which sound was left out is said", () => Game.ChildrenOfType<SimpleErrorNotification>()
                                                                     .Any(n => n.Text.ToString() == @"These sounds are no longer in the hitsound folder, and were left out: gone.wav"));
        }

        /// <summary>
        /// A preset kept from more lanes than the difficulty has is put on the lanes there are, and nothing is kept of
        /// the others. No lane is left as it was either, one that is not there now included.
        /// </summary>
        [Test]
        public void TestLanesTheDifficultyDoesNotHaveAreLeftOut()
        {
            make();

            give(17, @"kick.wav");

            AddStep("say the difficulty has four lanes", () => EditorBeatmap.Difficulty.CircleSize = 4);
            AddStep("put on a preset with a sound for the twelfth", () => lanes.ApplyPreset(new LaneSoundPreset
            {
                Name = @"wide",
                Lanes = Enumerable.Range(0, 12).Select(lane => new LaneSoundPresetSound { Source = lane == 11 ? @"drums/snare.wav" : lane == 1 ? @"kick.wav" : string.Empty }).ToList(),
            }));

            AddAssert("the second lane has its sound", () => lanes[1].File, () => Is.EqualTo(@"kick.wav"));

            AddStep("say it has eighteen again", () => EditorBeatmap.Difficulty.CircleSize = 18);
            AddAssert("the twelfth has no sound", () => lanes[11].HasFile, () => Is.False);
            AddAssert("nor has the last, which had one before the preset", () => lanes[17].HasFile, () => Is.False);
        }

        [Test]
        public void TestKeepingUnderATakenNameOverwrites()
        {
            make();

            give(0, @"kick.wav");

            openPresets();
            saveAs(@"mine", @"Save");

            give(0, @"drums/snare.wav");

            saveAs(@"mine", @"Overwrite");

            AddAssert("the list has it once", rowNames, () => Is.EqualTo(new[] { @"mine" }));
            AddAssert("it holds the lanes as they were the second time", () => soundsOf(@"mine"),
                () => Is.EqualTo(new[] { (0, @"drums/snare.wav", HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL) }));
        }

        /// <summary>
        /// Lanes none of which has a sound say nothing worth keeping, and are not kept.
        /// </summary>
        [Test]
        public void TestLanesWithNoSoundAtAllAreNotKept()
        {
            make();

            openPresets();
            clickTab(@"Save as a preset");
            typeName(@"nothing");

            AddAssert("not to be saved", () => saveButton(@"Save").Enabled.Value, () => Is.False);

            AddStep("try all the same", () => saveButton(@"Save").TriggerClick());
            AddAssert("nothing is kept", () => presets.Load().Presets, () => Is.Empty);
        }

        /// <summary>
        /// As the combo colour presets are deleted: the middle button offers it on the row, which is the right button
        /// for whoever has the two swapped.
        /// </summary>
        [Test]
        public void TestAPresetIsDeletedWithTheMiddleButton()
        {
            make();

            give(0, @"kick.wav");

            AddStep("two presets kept before", () => keep(
                new LaneSoundPreset { Name = @"gone", Lanes = { new LaneSoundPresetSound { Source = @"drums/snare.wav" } } },
                new LaneSoundPreset { Name = @"kept", Lanes = { new LaneSoundPresetSound { Source = @"kick.wav" } } }));

            openPresets();

            AddStep("press the right button on the first", () =>
            {
                InputManager.MoveMouseTo(row(@"gone"));
                InputManager.Click(MouseButton.Right);
            });
            AddWaitStep("give it time", 5);
            AddAssert("deleting is not offered", () => row(@"gone").DeleteButton.Alpha, () => Is.EqualTo(0));

            AddStep("press the middle button on it", () => InputManager.Click(MouseButton.Middle));
            AddUntilStep("deleting is offered", () => row(@"gone").DeleteButton.Alpha, () => Is.EqualTo(1));
            AddAssert("nothing is deleted yet", rowNames, () => Is.EqualTo(new[] { @"gone", @"kept" }));

            AddStep("click delete", () => row(@"gone").DeleteButton.TriggerClick());

            AddAssert("only the other is left", rowNames, () => Is.EqualTo(new[] { @"kept" }));
            AddAssert("only the other is on disk", () => presets.Load().Presets.Select(p => p.Name), () => Is.EqualTo(new[] { @"kept" }));
            AddAssert("the presets are still open", () => popover?.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddAssert("the lanes are left alone", () => lanes[0].File, () => Is.EqualTo(@"kick.wav"));
        }

        private LaneSoundPresetPopover? popover => Editor.ChildrenOfType<LaneSoundPresetPopover>().SingleOrDefault();

        private LaneSoundPresetStore presets => new LaneSoundPresetStore(Game.Storage);

        /// <summary>
        /// Keeps presets as if they had been kept before, from any beatmap.
        /// </summary>
        private void keep(params LaneSoundPreset[] kept)
        {
            var list = presets.Load();

            foreach (var preset in kept)
                list.Save(preset);

            presets.Save(list);
        }

        /// <summary>
        /// The lanes of a preset on disk that have a sound: which, the sound, and what it is to be.
        /// </summary>
        private (int lane, string source, string bank, string sound)[] soundsOf(string name) =>
            presets.Load().Find(name)!.Lanes.Select((l, lane) => (lane, l.Source, l.Bank, l.Sound)).Where(l => l.Source.Length > 0).ToArray();

        private void openPresets()
        {
            AddStep("open the presets", () => panel.PresetButton.TriggerClick());
            AddUntilStep("they are open", () => popover?.State.Value == Visibility.Visible && popover.IsLoaded);
        }

        // the tab control keeps a dropdown of the same items for when they do not fit, so the tab is found by its kind.
        private void clickTab(string text) => AddStep($"click \"{text}\"", () =>
        {
            InputManager.MoveMouseTo(popover!.ChildrenOfType<TabItem>().Single(i => i.ChildrenOfType<SpriteText>().Any(t => t.Text.ToString() == text)));
            InputManager.Click(MouseButton.Left);
        });

        private void typeName(string name) => AddStep($"type \"{name}\"", () =>
            // the tab control's dropdown has a search box of its own.
            popover!.ChildrenOfType<OsuTextBox>().Single(t => t.PlaceholderText.ToString() == @"Preset name").Current.Value = name);

        private RoundedButton saveButton(string text) => popover!.ChildrenOfType<RoundedButton>().Single(b => b.Text.ToString() == text);

        private void saveAs(string name, string button)
        {
            clickTab(@"Save as a preset");
            typeName(name);
            AddStep($"click {button}", () => saveButton(button).TriggerClick());
        }

        private string[] rowNames() => popover!.ChildrenOfType<LaneSoundPresetPopover.PresetRow>().Select(r => r.Preset.Name).ToArray();

        private LaneSoundPresetPopover.PresetRow row(string name) => popover!.ChildrenOfType<LaneSoundPresetPopover.PresetRow>().Single(r => r.Preset.Name == name);

        #endregion

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

        /// <summary>
        /// What tells a sound of the folder from another, as <see cref="hashOf"/> does a file of the beatmap.
        /// </summary>
        private string hashInFolder(string sound)
        {
            using (var stream = Game.Storage.GetStorageForDirectory(HitsoundLibrary.DIRECTORY).GetStream(sound))
                return stream.ComputeSHA2Hash();
        }

        /// <summary>
        /// The files of the beatmap as the difficulty to hitsound in was made, before any lane was given a sound.
        /// </summary>
        private string[] filesAtFirst = null!;

        private string[] beatmapFiles() => EditorBeatmap.BeatmapInfo.BeatmapSet!.Files.Select(f => f.Filename).OrderBy(f => f, StringComparer.Ordinal).ToArray();

        /// <summary>
        /// The names of the sounds a note on screen has found to play, as what it found them in names them.
        /// </summary>
        private string[] soundsFoundFor(HitObject hitObject) =>
            Editor.ChildrenOfType<DrawableHitObject>().Where(d => d.HitObject == hitObject)
                  .SelectMany(d => d.ChildrenOfType<PoolableSkinnableSample>())
                  .Where(s => s.Sample != null)
                  .Select(s => s.Sample.Name)
                  .ToArray();

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
            AddStep("note the files of the beatmap", () => filesAtFirst = beatmapFiles());
        }
    }
}
