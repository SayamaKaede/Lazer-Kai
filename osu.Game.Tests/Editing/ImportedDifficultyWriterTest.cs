// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.Formats;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// A difficulty of another beatmap set written as one of the set being edited (2026-10-13).
    /// </summary>
    [TestFixture]
    public class ImportedDifficultyWriterTest
    {
        /// <summary>
        /// The difficulty brought in: one of stable's, of another song's set.
        /// </summary>
        private static readonly string[] theirs =
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
            @"",
            @"[Editor]",
            @"Bookmarks: 100,200",
            @"DistanceSpacing: 1.2",
            @"BeatDivisor: 4",
            @"GridSize: 4",
            @"TimelineZoom: 2",
            @"",
            @"[Metadata]",
            @"Title:Their Title",
            @"TitleUnicode:あちらの曲",
            @"Artist:Their Artist",
            @"ArtistUnicode:あちらの人",
            @"Creator:them",
            @"Version:Hitsounds",
            @"Source:their source",
            @"Tags:their tags",
            @"BeatmapID:1234",
            @"BeatmapSetID:5678",
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
            @"Video,-200,""their video.mp4""",
            @"0,0,""their bg.jpg"",0,0",
            @"//Break Periods",
            @"2,3000,4000",
            @"//Storyboard Layer 0 (Background)",
            @"Sprite,Background,Centre,""sb\their sprite.png"",320,240",
            @" F,0,0,1000,0,1",
            @"//Storyboard Sound Samples",
            @"",
            @"[TimingPoints]",
            @"500,400,4,2,1,60,1,0",
            @"900,-50,4,2,3,40,0,1",
            @"",
            @"[Colours]",
            @"Combo1 : 255,0,0",
            @"",
            @"[HitObjects]",
            @"256,192,500,5,0,0:0:0:0:",
            @"256,192,900,1,8,0:0:0:0:",
            @"",
        };

        /// <summary>
        /// The difficulty being edited, as lazer keeps one.
        /// </summary>
        private static readonly string[] ours =
        {
            @"osu file format v128",
            @"",
            @"[General]",
            @"AudioFilename: our song.ogg",
            @"AudioLeadIn: 500",
            @"PreviewTime: 42",
            @"Mode: 3",
            @"",
            @"[Metadata]",
            @"Title:Our Title",
            @"TitleUnicode:こちらの曲",
            @"Artist:Our Artist",
            @"ArtistUnicode:こちらの人",
            @"Creator:us",
            @"Version:Insane",
            @"Source:",
            @"Tags:our tags",
            @"BeatmapID:99",
            @"BeatmapSetID:77",
            @"",
            @"[Events]",
            @"//Background and Video events",
            @"0,0,""our bg.png"",0,0",
            @"",
            @"[HitObjects]",
            @"64,192,100,1,0,0:0:0:0:",
            @"",
        };

        private static string stable(IEnumerable<string> lines) => string.Join("\r\n", lines);

        private static string lazer(IEnumerable<string> lines) => string.Join("\n", lines);

        private static string[] linesOf(string file) => file.Replace("\r\n", "\n").Split('\n');

        /// <summary>
        /// The lines under <paramref name="name"/> in <paramref name="file"/>, up to the next section.
        /// </summary>
        private static string[] section(string file, string name) =>
            linesOf(file).SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).ToArray();

        private static string written(IEnumerable<string>? file = null, IEnumerable<string>? like = null, string name = @"Hitsounds") =>
            ImportedDifficultyWriter.AsOneOf(stable(file ?? theirs), lazer(like ?? ours), name);

        [Test]
        public void TestTheSongIsThatOfThisSet()
        {
            string[] general = section(written(), @"[General]");

            Assert.That(general, Does.Contain(@"AudioFilename: our song.ogg"));
            Assert.That(general, Does.Contain(@"PreviewTime: 42"));
            Assert.That(general.Count(l => l.StartsWith(@"AudioFilename", StringComparison.Ordinal)), Is.EqualTo(1));

            // the rest of what it says of itself is its own: it is no osu!mania difficulty for being brought into a
            // set by way of one.
            Assert.That(general, Does.Contain(@"AudioLeadIn: 0"));
            Assert.That(general, Does.Contain(@"SampleSet: Soft"));
            Assert.That(general, Does.Contain(@"Mode: 0"));
        }

        [Test]
        public void TestTheNamesAreThoseOfThisSet()
        {
            Assert.That(section(written(), @"[Metadata]"), Is.EqualTo(new[]
            {
                @"Title:Our Title",
                @"TitleUnicode:こちらの曲",
                @"Artist:Our Artist",
                @"ArtistUnicode:こちらの人",
                @"Creator:us",
                @"Version:Hitsounds",
                @"Source:",
                @"Tags:our tags",
                @"BeatmapID:0",
                @"BeatmapSetID:77",
                @"",
            }));
        }

        [Test]
        public void TestItIsNamedAsTold()
        {
            Assert.That(section(written(name: @"Hitsounds (1)"), @"[Metadata]"), Does.Contain(@"Version:Hitsounds (1)"));
        }

        [Test]
        public void TestTheBackgroundIsThatOfThisSet()
        {
            Assert.That(section(written(), @"[Events]"), Is.EqualTo(new[]
            {
                @"//Background and Video events",
                // this set has no video, so the difficulty has none: the other set's is not a file of this one.
                @"0,0,""our bg.png"",0,0",
                @"//Break Periods",
                @"2,3000,4000",
                @"//Storyboard Layer 0 (Background)",
                @"Sprite,Background,Centre,""sb\their sprite.png"",320,240",
                @" F,0,0,1000,0,1",
                @"//Storyboard Sound Samples",
                @"",
            }));
        }

        [Test]
        public void TestAVideoOfThisSetIsPutInWithItsBackground()
        {
            string[] withVideo = ours.Select(l => l == @"0,0,""our bg.png"",0,0" ? "Video,0,\"our video.mp4\"\n0,0,\"our bg.png\",0,0" : l).ToArray();

            Assert.That(section(written(like: withVideo), @"[Events]").Take(3), Is.EqualTo(new[]
            {
                @"//Background and Video events",
                @"Video,0,""our video.mp4""",
                @"0,0,""our bg.png"",0,0",
            }));
        }

        [Test]
        public void TestABackgroundIsPutInWhereTheFileHasNone()
        {
            string[] bare = theirs.Where(l => !l.Contains(@"their video") && !l.Contains(@"their bg")).ToArray();

            Assert.That(section(written(bare), @"[Events]").Take(3), Is.EqualTo(new[]
            {
                @"//Background and Video events",
                @"0,0,""our bg.png"",0,0",
                @"//Break Periods",
            }));
        }

        [TestCase(@"[Editor]")]
        [TestCase(@"[Difficulty]")]
        [TestCase(@"[TimingPoints]")]
        [TestCase(@"[Colours]")]
        [TestCase(@"[HitObjects]")]
        public void TestTheRestIsAsItWas(string name)
        {
            Assert.That(section(written(), name), Is.EqualTo(section(stable(theirs), name)));
        }

        [Test]
        public void TestTheLinesEndAsTheFilesDid()
        {
            string file = written();

            Assert.That(file, Does.StartWith("osu file format v14\r\n"));
            Assert.That(file.Replace("\r\n", string.Empty), Does.Not.Contain("\n"));

            Assert.That(ImportedDifficultyWriter.AsOneOf(lazer(theirs), stable(ours), @"Hitsounds"), Does.Not.Contain("\r"));
        }

        [Test]
        public void TestWhatTheFileDoesNotSayAtAllIsSaidAsThisSetDoes()
        {
            // as a file of an old version is, which has no names in the script they are written in.
            string[] old = theirs.Where(l => !l.StartsWith(@"TitleUnicode", StringComparison.Ordinal) && !l.StartsWith(@"ArtistUnicode", StringComparison.Ordinal)).ToArray();

            Assert.That(section(written(old), @"[Metadata]"), Is.EqualTo(new[]
            {
                @"Title:Our Title",
                @"Artist:Our Artist",
                @"Creator:us",
                @"Version:Hitsounds",
                @"Source:",
                @"Tags:our tags",
                @"BeatmapID:0",
                @"BeatmapSetID:77",
                @"TitleUnicode:こちらの曲",
                @"ArtistUnicode:こちらの人",
                @"",
            }));
        }

        [Test]
        public void TestWhatThisSetDoesNotSayIsNotSaid()
        {
            string[] withoutSource = ours.Where(l => l != @"Source:").ToArray();

            Assert.That(section(written(like: withoutSource), @"[Metadata]"), Has.None.StartsWith(@"Source"));
        }
    }
}
