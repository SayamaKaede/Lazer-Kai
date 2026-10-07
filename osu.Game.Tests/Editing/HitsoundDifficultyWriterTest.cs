// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The new difficulties made for hitsounding: a copy of a difficulty, and one where every point a sound is played
    /// becomes a circle — both written from the file as stable has it, so that its green lines come along as they are.
    /// </summary>
    [TestFixture]
    public class HitsoundDifficultyWriterTest
    {
        /// <summary>
        /// A circle; a slider with two reverses whose ends each sound their own way; a slider made twice as fast by
        /// the green line it starts on, with nothing said about its ends; and a spinner.
        /// </summary>
        private static readonly string[] file =
        {
            "osu file format v14",
            "",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "SampleSet: Soft",
            "",
            "[Metadata]",
            "Title:Title",
            "Artist:Artist",
            "Creator:Creator",
            "Version:Insane",
            "BeatmapID:123",
            "BeatmapSetID:45",
            "",
            "[Difficulty]",
            "HPDrainRate:5",
            "CircleSize:4",
            "OverallDifficulty:8",
            "ApproachRate:9",
            "SliderMultiplier:1",
            "SliderTickRate:1",
            "",
            "[Events]",
            "//Background and Video events",
            "",
            "[TimingPoints]",
            "1000,500,4,2,1,60,1,0",
            "2000,-100,4,3,1,80,0,0",
            "3000,-50,4,2,1,40,0,0",
            "",
            "[HitObjects]",
            "100,100,1000,1,2,1:2:0:0:",
            "200,100,1500,6,0,L|300:100,2,100,2|8|0,1:0|2:2|3:0,0:0:1:70:",
            "400,100,3000,2,0,L|500:100,1,100",
            "300,300,4000,12,4,5000,2:0:0:0:",
            "",
        };

        [Test]
        public void TestEveryPointASoundIsPlayedBecomesACircle()
        {
            Assert.That(hitObjectsOf(HitsoundDifficultyWriter.AsHitCircles(join(file, "\n"), "Hitsounds")!), Is.EqualTo(new[]
            {
                "256,192,1000,5,2,1:2:0:0:",
                // the slider's own index and volume go with every end of it.
                "256,192,1500,5,2,1:0:1:70:",
                "256,192,2000,5,8,2:2:1:70:",
                "256,192,2500,5,0,3:0:1:70:",
                // half a beat long at twice the speed; its ends sound as the slider does.
                "256,192,3000,5,0,0:0:0:0:",
                "256,192,3250,5,0,0:0:0:0:",
                // a spinner sounds as it ends.
                "256,192,5000,5,4,2:0:0:0:",
            }));
        }

        /// <summary>
        /// The circles of a hitsound difficulty are all in one place, and with no stack leniency are not drawn as a stack.
        /// </summary>
        [Test]
        public void TestAHitsoundDifficultyHasNoStackLeniency()
        {
            string[] written = lines(HitsoundDifficultyWriter.AsHitCircles(join(file, "\n"), "Hitsounds")!);
            Assert.That(written.TakeWhile(l => l != "[Metadata]").Where(l => l.StartsWith("StackLeniency", StringComparison.Ordinal)),
                Is.EqualTo(new[] { "StackLeniency: 0" }), "put in where there was none");

            string[] withLeniency = file.Select(l => l == "SampleSet: Soft" ? "SampleSet: Soft\nStackLeniency: 0.7" : l).SelectMany(l => l.Split('\n')).ToArray();

            Assert.That(lines(HitsoundDifficultyWriter.AsHitCircles(join(withLeniency, "\n"), "Hitsounds")!).Where(l => l.StartsWith("StackLeniency", StringComparison.Ordinal)),
                Is.EqualTo(new[] { "StackLeniency: 0" }), "in place of the one there was");
            Assert.That(lines(HitsoundDifficultyWriter.CopyAs(join(withLeniency, "\n"), "Insane (copy)")).Where(l => l.StartsWith("StackLeniency", StringComparison.Ordinal)),
                Is.EqualTo(new[] { "StackLeniency: 0.7" }), "a copy keeps its own");
        }

        [Test]
        public void TestEverythingElseIsKeptAsItWas()
        {
            string[] written = lines(HitsoundDifficultyWriter.AsHitCircles(join(file, "\n"), "Hitsounds")!).Where(l => !l.StartsWith("StackLeniency", StringComparison.Ordinal)).ToArray();

            Assert.That(beforeHitObjects(written).Where(l => !isNameOrId(l)), Is.EqualTo(beforeHitObjects(file).Where(l => !isNameOrId(l))));
            Assert.That(written.Where(l => l.Contains(",-")), Is.EqualTo(new[] { "2000,-100,4,3,1,80,0,0", "3000,-50,4,2,1,40,0,0" }), "the green lines");
        }

        [Test]
        public void TestTheNewDifficultyHasANameOfItsOwnAndNoOnlineId()
        {
            string[] written = lines(HitsoundDifficultyWriter.AsHitCircles(join(file, "\n"), "Hitsounds")!);

            Assert.That(written, Does.Contain("Version:Hitsounds"));
            Assert.That(written, Does.Contain("BeatmapID:0"));
            Assert.That(written, Does.Contain("BeatmapSetID:45"));
            Assert.That(written, Does.Not.Contain("Version:Insane"));
        }

        [Test]
        public void TestACopyKeepsTheObjectsAsTheyAre()
        {
            string[] written = lines(HitsoundDifficultyWriter.CopyAs(join(file, "\n"), "Insane (copy)"));

            Assert.That(hitObjectsOf(join(written, "\n")), Is.EqualTo(hitObjectsOf(join(file, "\n"))));
            Assert.That(written.Where(l => !isNameOrId(l)), Is.EqualTo(file.Where(l => !isNameOrId(l))));
            Assert.That(written, Does.Contain("Version:Insane (copy)"));
            Assert.That(written, Does.Contain("BeatmapID:0"));
        }

        [Test]
        public void TestTheFileKeepsItsLineEndings()
        {
            string written = HitsoundDifficultyWriter.AsHitCircles(join(file, "\r\n"), "Hitsounds")!;

            Assert.That(written.Replace("\r\n", string.Empty), Does.Not.Contain("\n"), "a bare line feed");
            Assert.That(written, Does.Contain("\r\n256,192,1000,5,2,1:2:0:0:\r\n"));
        }

        private static string join(string[] lines, string newLine) => string.Join(newLine, lines);

        private static string[] lines(string file) => file.Replace("\r\n", "\n").Split('\n');

        private static string[] hitObjectsOf(string file) =>
            lines(file).SkipWhile(l => l != "[HitObjects]").Skip(1).Where(l => l.Length > 0).ToArray();

        private static string[] beforeHitObjects(string[] file) => file.TakeWhile(l => l != "[HitObjects]").ToArray();

        private static bool isNameOrId(string line) => line.StartsWith("Version:", StringComparison.Ordinal) || line.StartsWith("BeatmapID:", StringComparison.Ordinal);
    }
}
