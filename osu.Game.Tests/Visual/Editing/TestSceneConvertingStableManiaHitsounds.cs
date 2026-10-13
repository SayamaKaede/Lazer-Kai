// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// An osu!mania difficulty hitsounded in stable, where notes are given their sounds by copying ones that have them,
    /// turned into an osu! hitsound difficulty as it is: what Mapping Tools has been used for (2026-10-13).
    /// </summary>
    public partial class TestSceneConvertingStableManiaHitsounds : StableFormatConversionTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        private static readonly string[] timing_points =
        {
            "1000,500,4,2,0,100,1,0",
            "3000,-100,4,2,0,60,0,0",
        };

        /// <summary>
        /// Each note a file of its own, of the kind and the sample set it says. The beatmap has no file for the clap.
        /// </summary>
        private static readonly string[] hit_objects =
        {
            "64,192,1000,1,1,3:0:1:0:drum-hitnormal",
            "192,192,1000,1,2,0:2:1:0:soft-hitwhistle",
            "64,192,2000,1,8,0:3:1:0:drum-hitclap",
            "192,192,3000,1,2,0:2:1:0:soft-hitwhistle",
        };

        private BeatmapInfo mania = null!;
        private byte[] original = null!;

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                var imported = importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file);

                return Game.BeatmapManager.GetWorkingBeatmap(imported);
            };

            base.SetUpSteps();

            AddStep("give the beatmap an osu!mania difficulty hitsounded in stable, and two of its sounds", () =>
            {
                var set = EditorBeatmap.BeatmapInfo.BeatmapSet!;

                foreach (string name in new[] { @"drum-hitnormal.wav", @"soft-hitwhistle.wav" })
                {
                    using (var sound = TestResources.OpenResource(name.StartsWith(@"drum", StringComparison.Ordinal) ? @"Samples/hitsound-no-delay.wav" : @"Samples/hitsound-delay.wav"))
                        Game.BeatmapManager.AddFile(set, sound, name);
                }

                mania = Game.BeatmapManager.AddDifficultyFromFile(set, Game.Beatmap.Value, Encoding.UTF8.GetBytes(asMania(Encoding.UTF8.GetString(SavedFile())))).BeatmapInfo;
            });

            Editor? before = null;

            AddStep("open it", () => (before = Editor).SwitchToDifficulty(mania));
            AddUntilStep("opened", () => Editor != before && Editor?.ReadyForUse == true);
            AddStep("read its file as it is kept", () => original = SavedFile());
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("its buttons are up", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);
        }

        /// <summary>
        /// The osu! difficulty the test begins in, as an osu!mania one with the notes above.
        /// </summary>
        private static string asMania(string file)
        {
            var written = new List<string>();
            string? section = null;

            foreach (string line in file.Replace("\r\n", "\n").Split('\n'))
            {
                string trimmed = line.Trim();

                if (trimmed.StartsWith('[') && trimmed.EndsWith(']'))
                {
                    section = trimmed;
                    written.Add(line);

                    if (section == @"[TimingPoints]")
                        written.AddRange(timing_points);
                    else if (section == @"[HitObjects]")
                        written.AddRange(hit_objects);

                    continue;
                }

                if (section is @"[TimingPoints]" or @"[HitObjects]" && trimmed.Length > 0)
                    continue;

                // said anew below, whether or not the file says them.
                if (trimmed.StartsWith(@"Mode:", StringComparison.Ordinal) || trimmed.StartsWith(@"SampleSet:", StringComparison.Ordinal))
                    continue;

                written.Add(trimmed.StartsWith(@"Version:", StringComparison.Ordinal) ? @"Version:Hitsounds-mania" : line);
            }

            written.InsertRange(written.IndexOf(@"[General]") + 1, new[] { @"SampleSet: Soft", @"Mode: 3" });

            return string.Join("\r\n", written);
        }

        [Test]
        public void TestItIsTurnedIntoAHitsoundDifficultyAsItIs()
        {
            AddAssert("it has the hitsound screen of any difficulty, not lanes to set up", () => Editor.ChildrenOfType<LaneSoundPanel>(), () => Is.Empty);
            string[] sounds = null!;

            AddAssert("with a button to turn it into a hitsound difficulty", () => panel.ConvertManiaButton.Enabled.Value && panel.ConvertManiaButton.Parent!.Alpha > 0);
            AddStep("note the sound files the beatmap has", () => sounds = soundFiles());

            convert();

            AddAssert("there is an osu! difficulty for it", () => (converted?.DifficultyName, converted?.Ruleset.ShortName), () => Is.EqualTo((@"Hitsounds", @"osu")));

            AddAssert("a circle wherever notes sound, as each says", () => section(@"[HitObjects]"), () => Is.EqualTo(new[]
            {
                // the drum's hit sound and the soft whistle.
                @"256,192,1000,5,2,3:2:0:0:",
                // the clap is the drum's, and with no hit sound of its own the circle has the soft one.
                @"256,192,2000,5,8,2:3:0:0:",
                @"256,192,3000,5,2,2:2:0:0:",
            }));

            AddAssert("its lines say the custom index the sounds are under, and the volumes of this one", () => section(@"[TimingPoints]"), () => Is.EqualTo(new[]
            {
                @"1000,500,4,2,1,100,1,0",
                @"3000,-100,4,2,1,60,0,0",
            }));

            AddAssert("it is an osu! difficulty with nothing else changed", () => (line(@"Mode:"), line(@"CircleSize:"), line(@"Version:")),
                () => Is.EqualTo((@"Mode: 0", @"CircleSize:4", @"Version:Hitsounds")));

            AddAssert("the beatmap's own sounds are what the circles find, and none is written or taken away", soundFiles, () => Is.EqualTo(sounds));

            AddAssert("this difficulty is as it was", SavedFile, () => Is.EqualTo(original));
            AddAssert("and still the one open", () => EditorBeatmap.BeatmapInfo.DifficultyName, () => Is.EqualTo(@"Hitsounds-mania"));
        }

        /// <summary>
        /// Done again, the new one is put in place of the last. Without this one's volumes, the ones the last had are
        /// kept.
        /// </summary>
        [Test]
        public void TestDoneAgainItIsPutInPlaceOfTheLast()
        {
            Guid first = Guid.Empty;

            convert();
            AddStep("note which difficulty it is", () => first = converted!.ID);

            AddStep("leave the volumes out", () => panel.ConvertManiaVolumes.Current.Value = false);
            convert();

            AddAssert("it is the same difficulty", () => converted?.ID, () => Is.EqualTo(first));
            AddAssert("and the only one", () => EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.Count(b => b.DifficultyName.StartsWith(@"Hitsounds", StringComparison.Ordinal) && b.Ruleset.ShortName == @"osu"),
                () => Is.EqualTo(1));
            AddAssert("as loud as it was", () => section(@"[TimingPoints]").Last(), () => Is.EqualTo(@"3000,-100,4,2,1,60,0,0"));

            Editor? before = null;

            AddStep("open this one again", () => (before = Editor).SwitchToDifficulty(EditorBeatmap.BeatmapInfo));
            AddUntilStep("opened", () => Editor != before && Editor?.ReadyForUse == true);
            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("its buttons are up", () => Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.IsLoaded == true);

            AddAssert("it has not become one with lanes to set up", () => Editor.ChildrenOfType<LaneSoundPanel>(), () => Is.Empty);
            AddAssert("and that the volumes were left out is remembered", () => panel.ConvertManiaVolumes.Current.Value, () => Is.False);
        }

        private HitsoundPanel panel => Editor.ChildrenOfType<HitsoundPanel>().Single();

        /// <summary>
        /// The osu! difficulty named for hitsounds, which there is none of until one is made.
        /// </summary>
        private BeatmapInfo? converted =>
            EditorBeatmap.BeatmapInfo.BeatmapSet!.Beatmaps.FirstOrDefault(b => b.Ruleset.ShortName == @"osu" && b.DifficultyName == HitsoundDifficultyConverter.CONVERTED_NAME);

        private void convert()
        {
            AddStep("turn it into a hitsound difficulty", () => panel.ConvertManiaButton.TriggerClick());
            AddUntilStep("done", () => converted != null);
        }

        /// <summary>
        /// The sound files of the beatmap set, and what is in each.
        /// </summary>
        private string[] soundFiles() =>
            EditorBeatmap.BeatmapInfo.BeatmapSet!.Files.Where(f => f.Filename.EndsWith(@".wav", StringComparison.Ordinal) || f.Filename.EndsWith(@".ogg", StringComparison.Ordinal))
                         .Select(f => $@"{f.Filename} {f.File.Hash}")
                         .OrderBy(f => f, StringComparer.Ordinal)
                         .ToArray();

        private string[] hitsoundDifficulty()
        {
            var set = EditorBeatmap.BeatmapInfo.BeatmapSet!;

            using (var stream = Game.Beatmap.Value.GetStream(set.GetFile(converted!.Path!)!.File.GetStoragePath()))
            using (var reader = new StreamReader(stream))
                return reader.ReadToEnd().Replace("\r\n", "\n").Split('\n');
        }

        private string[] section(string name) => hitsoundDifficulty().SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();

        private string? line(string start) => hitsoundDifficulty().FirstOrDefault(l => l.StartsWith(start, StringComparison.Ordinal));
    }
}
