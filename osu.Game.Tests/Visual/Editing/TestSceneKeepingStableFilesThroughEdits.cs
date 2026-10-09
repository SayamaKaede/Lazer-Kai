// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Formats;
using osu.Game.Beatmaps.Timing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Framework.Testing;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Resources;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// A beatmap from stable, edited and saved with stable compatibility on: what was edited is written the way stable
    /// writes it, and nothing else in the file moves.
    /// </summary>
    /// <remarks>
    /// The beatmap's first line is a red line at 956 with volume 60, sample set soft and custom index 0, and nothing
    /// changes the volume until a green line at 20736. From 48428 to 53373 the volume is 50.
    /// </remarks>
    public partial class TestSceneKeepingStableFilesThroughEdits : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        private BeatmapInfo imported = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestDeletingAnObjectTakesOutOnlyItsLine()
        {
            AddStep("delete the circle at 1285", () => EditorBeatmap.Remove(circleAt(1285)));
            saveAndWait();

            AddAssert("only its line is gone", () => savedText(), () => Is.EqualTo(originalText().Replace("248,192,1285,1,8,0:0:0:0:\r\n", string.Empty)));
        }

        [Test]
        public void TestMovingAnObjectRewritesOnlyItsLine()
        {
            AddStep("move the circle at 1285 right", () =>
            {
                var circle = circleAt(1285);
                circle.Position += new Vector2(10, 0);
                EditorBeatmap.Update(circle);
            });
            saveAndWait();

            // its sampleset stays 0, the line's: until 2026-10-10 it came out as 2, the line's bank written as its own,
            // since a sound following its line could not be written as one.
            AddAssert("one line differs", () => differingLines(), () => Is.EqualTo(new[] { ("248,192,1285,1,8,0:0:0:0:", "258,192,1285,1,8,0:0:0:0:") }));
        }

        [Test]
        public void TestChangingAnObjectsVolumePutsGreenLinesAroundIt()
        {
            AddStep("turn the circle at 1285 down to 30", () =>
            {
                var circle = circleAt(1285);
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 30)).ToList();
                EditorBeatmap.Update(circle);
            });
            saveAndWait();

            AddAssert("objects untouched", () => section(savedText(), "HitObjects"), () => Is.EqualTo(section(originalText(), "HitObjects")));
            // back at the next object, where a mapper would put it, rather than a few milliseconds after this one.
            AddAssert("a line at the circle, and one at the next object putting 60 back", () => addedTimingLines(), () => Is.EqualTo(new[] { "1285,-100,4,2,0,30,0,0", "1450,-100,4,2,0,60,0,0" }));
            AddAssert("the circle reads back at 30", () => volumeOfSavedObjectAt(1285), () => Is.EqualTo(30));
            AddAssert("the next circle still at 60", () => volumeOfSavedObjectAt(2109), () => Is.EqualTo(60));
        }

        [Test]
        public void TestChangingASlidersVelocityPutsGreenLinesAtItsHead()
        {
            AddStep("double the velocity of the slider at 1450", () =>
            {
                var slider = (Slider)EditorBeatmap.HitObjects.Single(h => h.StartTime == 1450);
                slider.SliderVelocityMultiplier = 2;
                EditorBeatmap.Update(slider);
            });
            saveAndWait();

            AddAssert("objects untouched", () => section(savedText(), "HitObjects"), () => Is.EqualTo(section(originalText(), "HitObjects")));
            AddAssert("a line at the head, and one at the next slider putting it back", () => addedTimingLines(), () => Is.EqualTo(new[] { "1450,-50,4,2,0,60,0,0", "1780,-100,4,2,0,60,0,0" }));
            AddAssert("the slider reads back at twice the velocity", () => ((IHasSliderVelocity)savedObjectAt(1450)).SliderVelocityMultiplier, () => Is.EqualTo(2).Within(0.001));
        }

        /// <remarks>
        /// The slider at 20736 has a green line of its own at the same time, saying 65. Changing the slider changes
        /// that line rather than putting a second one next to it.
        /// </remarks>
        [Test]
        public void TestChangingAnObjectWithALineOfItsOwnChangesThatLine()
        {
            AddStep("turn the slider at 20736 down to 30", () =>
            {
                var slider = (Slider)EditorBeatmap.HitObjects.Single(h => h.StartTime == 20736);
                slider.Samples = slider.Samples.Select(s => s.With(newVolume: 30)).ToList();
                slider.NodeSamples = slider.NodeSamples.Select(node => (IList<HitSampleInfo>)node.Select(s => s.With(newVolume: 30)).ToList()).ToList();
                EditorBeatmap.Update(slider);
            });
            saveAndWait();

            AddAssert("its line now says 30", () => removedAndAddedTimingLines(), () => Is.EqualTo((
                new[] { "20736,-100,4,2,0,65,0,0" },
                new[] { "20736,-100,4,2,0,30,0,0", "21065,-100,4,2,0,65,0,0" })));
            AddAssert("the next slider reads back at 65", () => volumeOfSavedObjectAt(21065), () => Is.EqualTo(65));
        }

        /// <remarks>
        /// The sound follows the object: it keeps the 60 it had, where the green lines say 50.
        /// </remarks>
        [Test]
        public void TestMovingAnObjectInTimeTakesItsSoundAlong()
        {
            AddStep("move the circle at 1285 to 52500", () =>
            {
                var circle = circleAt(1285);
                circle.StartTime = 52500;
                EditorBeatmap.Update(circle);
            });
            saveAndWait();

            AddAssert("a line at the circle, and one at the next object putting 50 back", () => addedTimingLines(), () => Is.EqualTo(new[] { "52500,-100,4,2,0,60,0,0", "52714,-100,4,2,0,50,0,0" }));
            AddAssert("the circle reads back at 60", () => volumeOfSavedObjectAt(52500), () => Is.EqualTo(60));
            AddAssert("its old line is gone", () => savedText().Contains("248,192,1285,1,8,0:0:0:0:"), () => Is.False);
        }

        /// <remarks>
        /// At the velocity the lines give it, the slider's tail would land past the green line at 20736, where the
        /// volume is 65. At its own velocity it ends well before. Its tail is only looked at once its velocity is
        /// right: looked at before, the tail was where the old velocity put it, and a line was left behind for a
        /// moment that is not there. A beatmap edited in the game went to lazer's format over exactly this.
        /// </remarks>
        [Test]
        public void TestAFasterSliderIsLookedAtWhereItEnds()
        {
            AddStep("add a slider at 20580 at twice the velocity", () =>
            {
                var sound = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 60) };

                EditorBeatmap.Add(new Slider
                {
                    StartTime = 20580,
                    Position = new Vector2(100, 100),
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(70, 0) }),
                    SliderVelocityMultiplier = 2,
                    Samples = sound,
                    NodeSamples = new List<IList<HitSampleInfo>> { sound, sound },
                });
            });
            saveAndWait();

            AddAssert("only the line at its head", () => addedTimingLines(), () => Is.EqualTo(new[] { "20580,-50,4,2,0,60,0,0" }));
        }

        /// <remarks>
        /// The decoder reads a slider's head from the line in effect 5ms after it, and the slider's own sounds from the
        /// one 6ms after. A head louder than the rest therefore takes a line at the head and another straight after
        /// it — the same pair stable itself needs for this.
        /// </remarks>
        [Test]
        public void TestASlidersHeadAloneCanBeLouder()
        {
            AddStep("turn the head of the slider at 1450 up to 100", () =>
            {
                var slider = (Slider)EditorBeatmap.HitObjects.Single(h => h.StartTime == 1450);
                var nodes = slider.NodeSamples.ToList();
                nodes[0] = nodes[0].Select(s => s.With(newVolume: 100)).ToList();
                slider.NodeSamples = nodes;
                EditorBeatmap.Update(slider);
            });
            saveAndWait();

            AddAssert("a line at the head, and one straight after for the rest", () => addedTimingLines(), () => Is.EqualTo(new[] { "1450,-100,4,2,0,100,0,0", "1456,-100,4,2,0,60,0,0" }));
        }

        [Test]
        public void TestAddingACircleNeedsNoLineWhereTheSoundAgrees()
        {
            AddStep("add a circle at 1367", () => EditorBeatmap.Add(new HitCircle
            {
                StartTime = 1367,
                Position = new Vector2(300, 200),
                Samples = new List<HitSampleInfo> { new HitSampleInfo(HitSampleInfo.HIT_NORMAL, HitSampleInfo.BANK_SOFT, volume: 60) },
            }));
            saveAndWait();

            AddAssert("no lines added", () => addedTimingLines(), () => Is.Empty);
            AddAssert("its line is there, in order", () => section(savedText(), "HitObjects").SkipWhile(l => !l.Contains(",1285,")).Take(2).Last(), () => Does.StartWith("300,200,1367,1,"));
        }

        /// <remarks>
        /// Until 2026-09-23 this was saved in lazer's format, which is what this test used to check. The new red line
        /// takes its sounds from the line before it, which is what stable does with one put in.
        /// </remarks>
        [Test]
        public void TestAddingARedLineWritesIt()
        {
            AddStep("add a red line", () => EditorBeatmap.ControlPointInfo.Add(15000, new TimingControlPoint { BeatLength = 400 }));
            saveAndWait();

            AddAssert("file is still stable's", () => savedText().Split("\r\n")[0].TrimStart('﻿'), () => Is.EqualTo("osu file format v14"));
            AddAssert("the red line is added and nothing else", () => removedAndAddedTimingLines(), () => Is.EqualTo((System.Array.Empty<string>(), new[] { "15000,400,4,2,0,60,1,0" })));
        }

        [Test]
        public void TestChangingARedLineRewritesOnlyThatLine()
        {
            AddStep("make the red line at 956 three beats to a bar", () => EditorBeatmap.ControlPointInfo.TimingPointAt(956).TimeSignature = new TimeSignature(3));
            saveAndWait();

            AddAssert("file is still stable's", () => savedText().Split("\r\n")[0].TrimStart('﻿'), () => Is.EqualTo("osu file format v14"));
            AddAssert("only that line changed", () => removedAndAddedTimingLines(),
                () => Is.EqualTo((new[] { "956,329.67032967033,4,2,0,60,1,0" }, new[] { "956,329.67032967033,3,2,0,60,1,0" })));
        }

        /// <remarks>
        /// Stable says kiai on each line, so where the timing screen turns it on or off and no line stands, a green line
        /// goes in there, saying what the line before it did.
        /// </remarks>
        [Test]
        public void TestKiaiFromTheTimingScreenGoesOnLines()
        {
            AddStep("kiai from 30000 to 35000", () =>
            {
                EditorBeatmap.ControlPointInfo.Add(30000, new EffectControlPoint { KiaiMode = true });
                EditorBeatmap.ControlPointInfo.Add(35000, new EffectControlPoint { KiaiMode = false });
            });
            saveAndWait();

            AddAssert("file is still stable's", () => savedText().Split("\r\n")[0].TrimStart('﻿'), () => Is.EqualTo("osu file format v14"));
            AddAssert("a line turning it on and one turning it off", () => removedAndAddedTimingLines(),
                () => Is.EqualTo((System.Array.Empty<string>(), new[] { "30000,-100,4,2,0,70,0,1", "35000,-100,4,2,0,70,0,0" })));
        }

        /// <remarks>
        /// The line says what the red line at 956 already does, so no object sounds any different and the save has no
        /// reason to touch it.
        /// </remarks>
        [Test]
        public void TestALineAddedInTheEditorIsWritten()
        {
            addLine("2400,-100,4,2,0,60,0,0");
            saveAndWait();

            AddAssert("it is in the file", () => addedTimingLines(), () => Is.EqualTo(new[] { "2400,-100,4,2,0,60,0,0" }));
            AddAssert("in time order", () => section(savedText(), "TimingPoints").SkipWhile(l => !l.StartsWith("956,", System.StringComparison.Ordinal)).Skip(1).First(),
                () => Is.EqualTo("2400,-100,4,2,0,60,0,0"));
        }

        [Test]
        public void TestUndoTakesAnAddedLineOutAgain()
        {
            addLine("2400,-100,4,2,0,60,0,0");
            AddStep("undo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(-1));

            AddAssert("the lines are the file's again", () => editorLines(), () => Is.EqualTo(section(originalText(), "TimingPoints")));

            AddStep("redo", () => ((EditorChangeHandler)Editor.Dependencies.Get(typeof(IEditorChangeHandler))).RestoreState(1));

            AddAssert("and back", () => editorLines(), () => Does.Contain("2400,-100,4,2,0,60,0,0"));
        }

        /// <summary>
        /// The lines a save puts in become the editor's, so that what is drawn after a save is what was written.
        /// </summary>
        [Test]
        public void TestTheLinesASavePutsInAreTheEditorsAfterwards()
        {
            AddStep("turn the circle at 1285 down to 30", () =>
            {
                var circle = circleAt(1285);
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 30)).ToList();
                EditorBeatmap.Update(circle);
            });
            saveAndWait();

            AddAssert("the editor has them", () => editorLines(), () => Is.EqualTo(section(savedText(), "TimingPoints")));
            AddAssert("which are the new ones", () => editorLines(), () => Does.Contain("1285,-100,4,2,0,30,0,0").And.Contain("1450,-100,4,2,0,60,0,0"));
        }

        [Test]
        public void TestChangingTheBackgroundRewritesItsLine()
        {
            AddStep("change the background", () => EditorBeatmap.BeatmapInfo.Metadata.BackgroundFile = @"another.jpg");
            saveAndWait();

            AddAssert("only its line changed", () => differingLines(), () => Is.EqualTo(new[] { ("0,0,\"machinetop_background.jpg\",0,0", "0,0,\"another.jpg\",0,0") }));
        }

        /// <remarks>
        /// What copying bookmarks to every difficulty, or giving every difficulty a background, does to the others.
        /// </remarks>
        [TestCase(@"Soleily - Renatus (Gamu) [Insane].osu")]
        [TestCase(@"Soleily - Renatus (ExPew) [Normal].osu")]
        public void TestAnotherDifficultyKeepsItsFormatWhenChangedFromHere(string otherFile)
        {
            AddStep("copy bookmarks to it", () =>
            {
                var other = Game.BeatmapManager.GetWorkingBeatmap(imported.BeatmapSet!.Beatmaps.Single(b => b.Path == otherFile));
                StableFileSaving.Save(Game.BeatmapManager, other, true, playable => playable.Bookmarks = new[] { 1000, 2000 });
            });

            AddAssert("only its bookmarks changed", () =>
            {
                string[] original = originalText(otherFile).Split("\r\n");
                string[] saved = savedText(otherFile).Split("\r\n");
                return original.Length == saved.Length ? original.Zip(saved).Where(p => p.First != p.Second).ToArray() : new[] { ("line counts", $"{original.Length} and {saved.Length}") };
            }, () => Has.Length.EqualTo(1).And.All.Matches<(string, string)>(p => p.Item2 == "Bookmarks: 1000,2000"));
        }

        private SavedTimingLines timingLines => Editor.ChildrenOfType<SavedTimingLines>().Single();

        private string[] editorLines() => timingLines.Lines.Select(l => l.Raw).ToArray();

        private void addLine(string raw) => AddStep($"add {raw}", () =>
        {
            var line = SavedTimingLines.Parse(raw)!.Value;

            EditorBeatmap.BeginChange();
            timingLines.Lines.Insert(timingLines.Lines.Count(l => l.Time <= line.Time), line);
            EditorBeatmap.EndChange();
        });

        private void saveAndWait()
        {
            string before = string.Empty;

            AddStep("note the hash", () => before = EditorBeatmap.BeatmapInfo.MD5Hash);
            SaveEditor();
            AddUntilStep("saved", () => EditorBeatmap.BeatmapInfo.MD5Hash, () => Is.Not.EqualTo(before));
        }

        private HitCircle circleAt(double time) => EditorBeatmap.HitObjects.OfType<HitCircle>().Single(h => h.StartTime == time);

        private (string, string)[] differingLines()
        {
            string[] original = originalText().Split("\r\n");
            string[] saved = savedText().Split("\r\n");

            if (original.Length != saved.Length)
                return new[] { ("different line counts", $"{original.Length} and {saved.Length}") };

            return original.Zip(saved).Where(p => p.First != p.Second).ToArray();
        }

        private string[] addedTimingLines()
        {
            var original = section(originalText(), "TimingPoints");
            var saved = section(savedText(), "TimingPoints");

            if (original.Any(l => !saved.Contains(l)))
                return new[] { "an original line was lost" };

            return saved.Where(l => !original.Contains(l)).ToArray();
        }

        private (string[] Removed, string[] Added) removedAndAddedTimingLines()
        {
            var original = section(originalText(), "TimingPoints");
            var saved = section(savedText(), "TimingPoints");

            return (original.Where(l => !saved.Contains(l)).ToArray(), saved.Where(l => !original.Contains(l)).ToArray());
        }

        private int volumeOfSavedObjectAt(double time) => savedObjectAt(time).Samples.First().Volume;

        private HitObject savedObjectAt(double time)
        {
            using var reader = new LineBufferedReader(new MemoryStream(savedFile()));
            return new LegacyBeatmapDecoder().Decode(reader).HitObjects.Single(h => h.StartTime == time);
        }

        private static string[] section(string text, string name)
        {
            var lines = text.Split("\r\n");
            int start = System.Array.IndexOf(lines, $"[{name}]");
            return lines.Skip(start + 1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();
        }

        private string savedText(string? difficulty = null) => Encoding.UTF8.GetString(savedFile(difficulty));

        private byte[] savedFile(string? difficulty = null) => Game.Realm.Run(r =>
        {
            var set = r.Find<BeatmapInfo>(imported.ID)!.BeatmapSet!;
            var beatmap = set.Beatmaps.Single(b => b.Path == (difficulty ?? difficulty_file));
            var file = set.Files.Single(f => f.File.Hash == beatmap.Hash);

            using (var stream = Game.Storage.GetStorageForDirectory(@"files").GetStream(file.File.GetStoragePath()))
                return stream.ReadAllRemainingBytesToArray();
        });

        private static string originalText(string? difficulty = null)
        {
            using (var archive = new ZipArchive(TestResources.GetTestBeatmapStream()))
            using (var stream = archive.GetEntry(difficulty ?? difficulty_file)!.Open())
                return Encoding.UTF8.GetString(stream.ReadAllRemainingBytesToArray());
        }
    }
}
