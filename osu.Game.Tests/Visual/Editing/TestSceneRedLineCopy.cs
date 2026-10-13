// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Screens.Edit.Timing;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The red lines of one difficulty put in the place of another's, with what was on a tick put back on one
    /// (2026-10-13).
    /// </summary>
    /// <remarks>
    /// Two difficulties of one set: one whose timing was mended, by two milliseconds and with a red line the other has
    /// not, and one that is as both were before.
    /// </remarks>
    public partial class TestSceneRedLineCopy : EditorSavingTestScene
    {
        private const string mended = @"Timing";
        private const string as_it_was = @"Target";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        /// <summary>
        /// Which of the two difficulties the editor opens.
        /// </summary>
        private string opened = as_it_was;

        /// <summary>
        /// Whether the difficulties are kept as stable has them.
        /// </summary>
        private bool keptAsStables = true;

        private BeatmapSetInfo set = null!;

        private RedLineCopyOverlay window => Editor.ChildrenOfType<RedLineCopyOverlay>().Single();

        private SavedTimingLines lines => (SavedTimingLines)Editor.Dependencies.Get(typeof(SavedTimingLines));

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, keptAsStables);

                set = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(archive(
                    (@"Them - Song (them) [Timing].osu", difficulty(mended, new[]
                    {
                        @"502,400,4,1,0,100,1,0",
                        @"4502,300,3,1,0,100,1,0",
                    }, new[]
                    {
                        @"256,192,502,5,0,0:0:0:0:",
                    })),
                    // timed to a cut of the song that starts eighty milliseconds later.
                    (@"Them - Song (them) [Shifted].osu", difficulty(@"Shifted", new[]
                    {
                        @"580,400,4,1,0,100,1,0",
                    }, new[]
                    {
                        @"256,192,580,5,0,0:0:0:0:",
                    })),
                    (@"Them - Song (them) [Target].osu", difficulty(as_it_was, new[]
                    {
                        @"500,400,4,2,1,80,1,0",
                        @"900,-50,4,3,2,40,0,1",
                    }, new[]
                    {
                        @"256,192,500,5,0,0:0:0:0:",
                        @"256,192,900,1,8,0:0:0:0:",
                        @"100,100,1300,2,0,L|200:100,1,90,2|0,0:0|0:0,0:0:0:0:",
                        // on no tick of either timing.
                        @"256,192,2144,1,0,0:0:0:0:",
                        @"256,192,4500,5,0,0:0:0:0:",
                    })),
                    (@"song.mp3", new byte[] { 1, 2, 3 })), @"timed.osz")).GetResultSafely()!.ID)!;

                return Game.BeatmapManager.GetWorkingBeatmap(set.Beatmaps.Single(b => b.DifficultyName == opened), true);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());

            base.TearDownSteps();
        }

        [TestCase(true, true)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void TestRedLinesOfAnotherDifficultyArePutOnThisOne(bool stable, bool keepBeats)
        {
            // what is on no tick goes with its beat, or is where it was when only what is near a tick is moved.
            double offEveryTick = keepBeats ? 2146 : 2144;

            keptAsStables = stable;
            opened = as_it_was;

            copyFromTheOther(keepBeats: keepBeats);

            AddAssert("the red lines are the other's", timing, () => Is.EqualTo(new[] { (502.0, 400.0, 4), (4502.0, 300.0, 3) }));

            // what was on a tick is on one again.
            AddAssert("the objects are on the ticks", starts, () => Is.EqualTo(new[] { 502.0, 902.0, 1302.0, offEveryTick, 4502.0 }).Within(1e-6));
            AddAssert("the kiai starts on its tick", () => EditorBeatmap.ControlPointInfo.EffectPoints.Select(e => (e.Time, e.KiaiMode)), () => Is.EqualTo(new[] { (902.0, true) }));
            AddAssert("the bookmarks too", () => EditorBeatmap.Bookmarks.ToArray(), () => Is.EqualTo(new[] { 902, (int)offEveryTick }));

            if (stable)
            {
                // each red line says of the sounds what was followed where it is, and the green line is on its tick.
                AddAssert("the lines of the file follow", () => lines.Lines.Select(l => l.Raw), () => Is.EqualTo(new[]
                {
                    @"502,400,4,2,1,80,1,0",
                    @"902,-50,4,3,2,40,0,1",
                    @"4502,300,3,3,2,40,1,1",
                }));
            }

            AddStep("undo", () => InputManager.Keys(PlatformAction.Undo));

            AddAssert("the red lines are its own again", timing, () => Is.EqualTo(new[] { (500.0, 400.0, 4) }));
            AddAssert("the objects are where they were", starts, () => Is.EqualTo(new[] { 500.0, 900.0, 1300.0, 2144.0, 4500.0 }));
            AddAssert("the bookmarks too", () => EditorBeatmap.Bookmarks.ToArray(), () => Is.EqualTo(new[] { 900, 2144 }));

            if (stable)
            {
                AddAssert("the lines of the file too", () => lines.Lines.Select(l => l.Raw), () => Is.EqualTo(new[]
                {
                    @"500,400,4,2,1,80,1,0",
                    @"900,-50,4,3,2,40,0,1",
                }));
            }
        }

        [Test]
        public void TestADifficultyKeptAsStablesIsSavedAsOneWithTheRedLines()
        {
            keptAsStables = true;
            opened = as_it_was;

            copyFromTheOther();
            SaveEditor();

            AddUntilStep("saved", () => section(fileOf(as_it_was), @"[TimingPoints]").Any(l => l.StartsWith(@"502,", System.StringComparison.Ordinal)));

            AddAssert("in stable's format still", () => fileOf(as_it_was), () => Does.StartWith(@"osu file format v14"));
            AddAssert("with the lines", () => section(fileOf(as_it_was), @"[TimingPoints]").Where(l => l.Length > 0), () => Is.EqualTo(new[]
            {
                @"502,400,4,2,1,80,1,0",
                @"902,-50,4,3,2,40,0,1",
                @"4502,300,3,3,2,40,1,1",
            }));
            AddAssert("and the objects", () => startsIn(fileOf(as_it_was)), () => Is.EqualTo(new[] { @"502", @"902", @"1302", @"2146", @"4502" }));
        }

        [Test]
        public void TestNothingIsPutOnATickWhenToldNone()
        {
            keptAsStables = true;
            opened = as_it_was;

            copyFromTheOther(within: @"0", keepBeats: false);

            AddAssert("the red lines are the other's", timing, () => Is.EqualTo(new[] { (502.0, 400.0, 4), (4502.0, 300.0, 3) }));
            AddAssert("the objects are where they were", starts, () => Is.EqualTo(new[] { 500.0, 900.0, 1300.0, 2144.0, 4500.0 }));
        }

        /// <summary>
        /// The red line is two milliseconds further on, which is how far off the objects are.
        /// </summary>
        [TestCase(@"2", true)]
        [TestCase(@"1", false)]
        public void TestWhatIsAsFarOffAsToldIsPutOnATick(string within, bool put)
        {
            keptAsStables = true;
            opened = as_it_was;

            copyFromTheOther(within, keepBeats: false);

            AddAssert(put ? "the objects are on the ticks" : "the objects are where they were", starts,
                () => Is.EqualTo(put ? new[] { 502.0, 902.0, 1302.0, 2144.0, 4502.0 } : new[] { 500.0, 900.0, 1300.0, 2144.0, 4500.0 }));
        }

        /// <summary>
        /// Red lines eighty milliseconds on, as those of a difficulty timed to another cut of the song are: nothing is
        /// near a tick of them, and everything goes with its beat or not at all.
        /// </summary>
        [TestCase(true)]
        [TestCase(false)]
        public void TestRedLinesSomeWayOnTakeEverythingWithThem(bool keepBeats)
        {
            keptAsStables = true;
            opened = as_it_was;

            copyFromTheOther(from: @"Shifted (osu!)", keepBeats: keepBeats);

            AddAssert("the red line is the other's", timing, () => Is.EqualTo(new[] { (580.0, 400.0, 4) }));

            if (keepBeats)
            {
                AddAssert("the objects are as far on", starts, () => Is.EqualTo(new[] { 580.0, 980.0, 1380.0, 2224.0, 4580.0 }).Within(1e-6));
                AddAssert("the bookmarks too", () => EditorBeatmap.Bookmarks.ToArray(), () => Is.EqualTo(new[] { 980, 2224 }));
                AddAssert("and the lines of the file", () => lines.Lines.Select(l => l.Raw), () => Is.EqualTo(new[]
                {
                    @"580,400,4,2,1,80,1,0",
                    @"980,-50,4,3,2,40,0,1",
                }));

                SaveEditor();
                AddUntilStep("saved", () => section(fileOf(as_it_was), @"[TimingPoints]").Any(l => l.StartsWith(@"580,", System.StringComparison.Ordinal)));
                AddAssert("in stable's format still", () => fileOf(as_it_was), () => Does.StartWith(@"osu file format v14"));
                AddAssert("with the objects", () => startsIn(fileOf(as_it_was)), () => Is.EqualTo(new[] { @"580", @"980", @"1380", @"2224", @"4580" }));
            }
            else
                AddAssert("the objects on a tick before are where they were", () => starts().Take(3), () => Is.EqualTo(new[] { 500.0, 900.0, 1300.0 }));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void TestRedLinesOfThisDifficultyArePutOnOthers(bool stable)
        {
            Editor before = null!;

            keptAsStables = stable;
            opened = mended;

            AddStep("open the window", () => Editor.CopyRedLines());
            AddUntilStep("it is up", () => window.State.Value == Visibility.Visible);
            AddStep("to the others", () => window.DirectionChoice.Current.Value = RedLineCopyOverlay.Direction.ToOthers);
            AddAssert("the other difficulties are ticked", () => window.Targets.Select(t => t.Current.Value), () => Is.EqualTo(new[] { true, true }));

            AddStep("copy", () =>
            {
                before = Editor;
                window.ApplyButton.TriggerClick();
            });
            AddUntilStep("the editor opens the set again", () => Editor != before && Editor?.ReadyForUse == true);

            AddAssert("the other difficulty has the red lines", () => timingOf(as_it_was), () => Is.EqualTo(new[] { (502.0, 400.0, 4), (4502.0, 300.0, 3) }));
            AddAssert("and its objects are on the ticks", () => startsOf(as_it_was), () => Is.EqualTo(new[] { 502.0, 902.0, 1302.0, 2146.0, 4502.0 }));

            if (stable)
            {
                AddAssert("it is in stable's format still", () => fileOf(as_it_was), () => Does.StartWith(@"osu file format v14"));
                AddAssert("with the lines", () => section(fileOf(as_it_was), @"[TimingPoints]").Where(l => l.Length > 0), () => Is.EqualTo(new[]
                {
                    @"502,400,4,2,1,80,1,0",
                    @"902,-50,4,3,2,40,0,1",
                    @"4502,300,3,3,2,40,1,1",
                }));
            }

            AddAssert("the editor is on the difficulty it was on", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo(mended));
        }

        private void copyFromTheOther(string? within = null, string from = @"Timing (osu!)", bool keepBeats = true)
        {
            AddStep("open the window", () => Editor.CopyRedLines());
            AddUntilStep("it is up", () => window.State.Value == Visibility.Visible);
            AddAssert("everything goes with its beat unless said otherwise", () => window.KeepBeats.Current.Value);

            AddStep($"from {from}", () => window.Source.Current.Value = from);
            AddStep(keepBeats ? "with its beat" : "only what is near a tick", () => window.KeepBeats.Current.Value = keepBeats);

            if (within != null)
                AddStep($"within {within}", () => window.Within.Current.Value = within);

            AddStep("copy", () => window.ApplyButton.TriggerClick());
            AddUntilStep("it is gone", () => window.State.Value == Visibility.Hidden);
        }

        private (double, double, int)[] timing() => EditorBeatmap.ControlPointInfo.TimingPoints.Select(t => (t.Time, t.BeatLength, t.TimeSignature.Numerator)).ToArray();

        private double[] starts() => EditorBeatmap.HitObjects.Select(h => h.StartTime).ToArray();

        private BeatmapInfo saved(string name)
        {
            var id = set.Beatmaps.Single(b => b.DifficultyName == name).ID;

            return Game.BeatmapManager.QueryBeatmap(b => b.ID == id)!;
        }

        private (double, double, int)[] timingOf(string name) =>
            Game.BeatmapManager.GetWorkingBeatmap(saved(name), true).Beatmap.ControlPointInfo.TimingPoints.Select(t => (t.Time, t.BeatLength, t.TimeSignature.Numerator)).ToArray();

        private double[] startsOf(string name) => Game.BeatmapManager.GetWorkingBeatmap(saved(name), true).Beatmap.HitObjects.Select(h => h.StartTime).ToArray();

        private string fileOf(string name)
        {
            var now = BeatmapSetFiles.Read(Game.Realm, set.ID)!;

            return Encoding.UTF8.GetString(new BeatmapSetFiles(Game.BeatmapManager, now).Get(now.Beatmaps.Single(b => b.DifficultyName == name).Path!));
        }

        /// <summary>
        /// The lines under <paramref name="name"/> in <paramref name="file"/>, up to the next section.
        /// </summary>
        private static string[] section(string file, string name) =>
            file.Replace("\r\n", "\n").Split('\n').SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).ToArray();

        private static string[] startsIn(string file) => section(file, @"[HitObjects]").Where(l => l.Length > 0).Select(l => l.Split(',')[2]).ToArray();

        private static MemoryStream archive(params (string name, object contents)[] files)
        {
            var stream = new MemoryStream();

            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, true))
            {
                foreach (var (name, contents) in files)
                {
                    using (var entry = zip.CreateEntry(name).Open())
                        entry.Write(contents as byte[] ?? Encoding.UTF8.GetBytes((string)contents));
                }
            }

            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        /// <summary>
        /// A difficulty as stable writes one.
        /// </summary>
        private static string difficulty(string name, string[] timingPoints, string[] hitObjects) => string.Join("\r\n", new[]
        {
            @"osu file format v14",
            @"",
            @"[General]",
            @"AudioFilename: song.mp3",
            @"AudioLeadIn: 0",
            @"PreviewTime: -1",
            @"Countdown: 0",
            @"SampleSet: Soft",
            @"StackLeniency: 0.7",
            @"Mode: 0",
            @"LetterboxInBreaks: 0",
            @"WidescreenStoryboard: 0",
            @"",
            @"[Editor]",
            @"Bookmarks: 900,2144",
            @"DistanceSpacing: 1.2",
            @"BeatDivisor: 4",
            @"GridSize: 4",
            @"TimelineZoom: 2",
            @"",
            @"[Metadata]",
            @"Title:Song",
            @"TitleUnicode:Song",
            @"Artist:Them",
            @"ArtistUnicode:Them",
            @"Creator:them",
            $@"Version:{name}",
            @"Source:",
            @"Tags:",
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
            @"//Break Periods",
            @"//Storyboard Layer 0 (Background)",
            @"//Storyboard Layer 1 (Fail)",
            @"//Storyboard Layer 2 (Pass)",
            @"//Storyboard Layer 3 (Foreground)",
            @"//Storyboard Sound Samples",
            @"",
            @"[TimingPoints]",
        }.Concat(timingPoints).Concat(new[]
        {
            @"",
            @"",
            @"[HitObjects]",
        }).Concat(hitObjects).Append(@""));
    }
}
