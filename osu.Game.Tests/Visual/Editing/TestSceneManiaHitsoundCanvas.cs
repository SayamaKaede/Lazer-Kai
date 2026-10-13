// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
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
