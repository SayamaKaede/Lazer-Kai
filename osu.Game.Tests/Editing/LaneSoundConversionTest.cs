// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps.Formats;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The circles and sound files a difficulty hitsounded in osu!mania is turned into.
    /// </summary>
    [TestFixture]
    public class LaneSoundConversionTest
    {
        /// <summary>
        /// The lanes of a difficulty the user hitsounded in stable and turned into a hitsound difficulty with Mapping
        /// Tools. Two of the drum's sounds had no file in the beatmap set, and were the skin's.
        /// </summary>
        private static readonly LaneSound[] lanes =
        {
            lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_WHISTLE, @"soft-hitwhistle.wav"),
            lane(HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_NORMAL, @"drum-hitnormal.wav"),
            lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"normal-hitnormal.wav"),
            lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP, @"soft-hitclap.wav"),
            lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_FINISH, @"soft-hitfinish.wav"),
            new LaneSound(),
            lane(HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP, string.Empty),
            lane(HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_FINISH, string.Empty),
        };

        private static readonly LaneSound fallback = lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_NORMAL, @"soft-hitnormal.wav");

        /// <summary>
        /// Every way the lanes of that difficulty sounded together, and the circle Mapping Tools wrote for each: its
        /// hitsound number, then the sample set of its hit sound and the one of what is added, as a file has them.
        /// </summary>
        private static readonly (int[] lanes, string circle)[] written =
        {
            (new[] { 1 }, @"0,3:3"),
            (new[] { 2, 3 }, @"8,1:2"),
            (new[] { 2 }, @"0,1:1"),
            (new[] { 0, 1 }, @"2,3:2"),
            (new[] { 0, 1, 4 }, @"6,3:2"),
            (new[] { 0, 2, 3 }, @"10,1:2"),
            (new[] { 1, 3 }, @"8,3:2"),
            (new[] { 0 }, @"2,2:2"),
            (new[] { 7 }, @"4,2:3"),
            (new[] { 6 }, @"8,2:3"),
            (new[] { 2, 4 }, @"4,1:2"),
            (new[] { 0, 2 }, @"2,1:2"),
            (new[] { 0, 2, 4 }, @"6,1:2"),
            (new[] { 1, 4 }, @"4,3:2"),
            (new[] { 0, 1, 3 }, @"10,3:2"),
            (new[] { 3 }, @"8,2:2"),
            (new[] { 0, 1, 6 }, @"10,3:3"),
            (new[] { 1, 6 }, @"8,3:3"),
            (new[] { 0, 1, 7 }, @"6,3:3"),
            (new[] { 4 }, @"4,2:2"),
            (new[] { 1, 7 }, @"4,3:3"),
            (new[] { 2, 6 }, @"8,1:3"),
            (new[] { 0, 3 }, @"10,2:2"),
            (new[] { 0, 7 }, @"6,2:3"),
            (new[] { 0, 6 }, @"10,2:3"),
        };

        [Test]
        public void TestWhatSoundsTogetherIsTheCircleMappingToolsWrote()
        {
            var notes = written.SelectMany((w, i) => w.lanes.Select(l => new LaneSoundConversion.Note(1000 * (i + 1), l)));

            var result = convert(notes, lanes);

            Assert.That(result.Circles.Select(c => c.Time), Is.EqualTo(written.Select((_, i) => 1000.0 * (i + 1))));
            Assert.That(result.Circles.Select(asWritten), Is.EqualTo(written.Select(w => w.circle)));
        }

        /// <summary>
        /// One custom index was enough for all of it, and the files were the lanes' own, each under its own name. The
        /// whistle is there twice: it is the soft one, and where it sounded along with a sound of the drum's that was
        /// added too, both had to be the drum's.
        /// </summary>
        [Test]
        public void TestTheFilesAreTheOnesMappingToolsWrote()
        {
            var notes = written.SelectMany((w, i) => w.lanes.Select(l => new LaneSoundConversion.Note(1000 * (i + 1), l)));

            var result = convert(notes, lanes);

            Assert.That(result.Circles.Select(c => c.CustomIndex), Is.All.EqualTo(1));
            Assert.That(result.Files.Select(f => $@"{f.Name} < {string.Join(@" + ", f.Sources)}"), Is.EquivalentTo(new[]
            {
                @"normal-hitnormal < normal-hitnormal.wav",
                @"soft-hitnormal < soft-hitnormal.wav",
                @"soft-hitwhistle < soft-hitwhistle.wav",
                @"soft-hitfinish < soft-hitfinish.wav",
                @"soft-hitclap < soft-hitclap.wav",
                @"drum-hitnormal < drum-hitnormal.wav",
                @"drum-hitwhistle < soft-hitwhistle.wav",
            }));
        }

        /// <summary>
        /// Two hit sounds at one time are one file with both in it, under the sample set of one of them. That set's
        /// hit sound is something else where one of the two sounds alone, so the two are told apart by custom index.
        /// </summary>
        [Test]
        public void TestTwoSoundsInOnePlaceAreMixedUnderACustomIndexOfTheirOwn()
        {
            var kickAndSnare = new[]
            {
                lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"kick.wav"),
                lane(HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_NORMAL, @"snare.wav"),
            };

            var result = convert(new[]
            {
                new LaneSoundConversion.Note(1000, 0),
                new LaneSoundConversion.Note(2000, 1),
                new LaneSoundConversion.Note(3000, 0),
                new LaneSoundConversion.Note(3000, 1),
            }, kickAndSnare);

            Assert.That(result.Circles.Select(c => $@"{asWritten(c)} under {c.CustomIndex}"), Is.EqualTo(new[]
            {
                @"0,1:1 under 1",
                @"0,3:3 under 1",
                @"0,3:3 under 2",
            }));

            Assert.That(result.Files.Select(f => $@"{f.Name} < {string.Join(@" + ", f.Sources)}"), Is.EquivalentTo(new[]
            {
                @"normal-hitnormal < kick.wav",
                @"drum-hitnormal < snare.wav",
                @"drum-hitnormal2 < kick.wav + snare.wav",
            }));
        }

        /// <summary>
        /// The sounds sliders make as they are followed are named for every sample set and custom index the lines say
        /// together, which is what stable looks up where the hitsounds are copied to: a silent file is written under
        /// each. A line of no custom index has the skin's sounds, and one of no sample set the beatmap's (2026-10-14).
        /// </summary>
        [Test]
        public void TestTheSoundsOfSlidersAreNamedForEverySampleSetAndCustomIndexTheLinesSay()
        {
            string file = string.Join("\r\n",
                "osu file format v14",
                "",
                "[General]",
                "SampleSet: Drum",
                "",
                "[TimingPoints]",
                "0,500,4,2,1,100,1,0",
                "1000,-100,4,2,1,60,0,0",
                "2000,-100,4,2,3,60,0,0",
                "3000,-100,4,1,3,60,0,0",
                "4000,-100,4,2,0,60,0,0",
                "5000,-100,4,0,12,60,0,0",
                "",
                "[HitObjects]",
                "256,192,0,5,0,2:2:0:0:");

            Assert.That(HitsoundDifficultyWriter.SliderSlidesOf(file), Is.EqualTo(new[]
            {
                @"drum-sliderslide12",
                @"normal-sliderslide3",
                @"soft-sliderslide",
                @"soft-sliderslide3",
            }));
        }

        /// <summary>
        /// Which lanes are heard together in a file is told for each such file, for the lanes to be shown as mixed: a
        /// kick and a snare that are both the hit sound, and with them a second kick lane of another sample set, which
        /// comes to the same hit sound as it is under the first's. A clap is a sound of its own beside them, and the
        /// same file in two lanes is the one sound (2026-10-14).
        /// </summary>
        [Test]
        public void TestTheLanesHeardTogetherInAFileAreTold()
        {
            var lanes = new[]
            {
                lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"kick.wav"),
                lane(HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_NORMAL, @"snare.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP, @"clap.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_NORMAL, @"tom.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP, @"clap.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_NORMAL, string.Empty),
            };

            var mixes = LaneSoundConversion.Mixes(new[]
            {
                // alone, and with a clap: nothing is mixed.
                new LaneSoundConversion.Note(1000, 0),
                new LaneSoundConversion.Note(2000, 0),
                new LaneSoundConversion.Note(2000, 2),
                // the same clap twice is the one clap, and a lane with no file is the skin's sound, of which there is
                // nothing to mix.
                new LaneSoundConversion.Note(3000, 2),
                new LaneSoundConversion.Note(3000, 4),
                new LaneSoundConversion.Note(3500, 0),
                new LaneSoundConversion.Note(3500, 5),
                // the tom and the kick, within what counts as one time.
                new LaneSoundConversion.Note(4000, 3),
                new LaneSoundConversion.Note(4010, 0),
                // the kick and the snare, twice: told once.
                new LaneSoundConversion.Note(5000, 0),
                new LaneSoundConversion.Note(5000, 1),
                new LaneSoundConversion.Note(6000, 1),
                new LaneSoundConversion.Note(6000, 0),
                new LaneSoundConversion.Note(6000, 2),
            }, lanes, fallback, _ => true);

            Assert.That(mixes.Select(m => string.Join('+', m)), Is.EqualTo(new[] { @"0+3", @"0+1" }));
        }

        [Test]
        public void TestALaneWhoseFileIsGoneIsNotMixed()
        {
            var lanes = new[]
            {
                lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"kick.wav"),
                lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"gone.wav"),
            };

            var notes = new[] { new LaneSoundConversion.Note(1000, 0), new LaneSoundConversion.Note(1000, 1) };

            Assert.That(LaneSoundConversion.Mixes(notes, lanes, fallback, f => f != @"gone.wav"), Is.Empty);
            Assert.That(LaneSoundConversion.Mixes(notes, lanes, fallback, _ => true), Has.Count.EqualTo(1));
        }

        /// <summary>
        /// Sounds a few milliseconds apart are one circle, at the time of the first of them gone through.
        /// </summary>
        [TestCase(15, 1)]
        [TestCase(16, 2)]
        public void TestSoundsCloseInTimeAreOneCircle(double apart, int circles)
        {
            var result = convert(new[]
            {
                new LaneSoundConversion.Note(1000, 2),
                new LaneSoundConversion.Note(1000 + apart, 3),
            }, lanes);

            Assert.That(result.Circles, Has.Count.EqualTo(circles));
            Assert.That(result.Circles[0].Time, Is.EqualTo(1000));
        }

        /// <summary>
        /// Where more than one custom index has a circle's sounds, the one that goes on having the sounds of the circles
        /// after it for longest is used: a change of custom index is a green line.
        /// </summary>
        [Test]
        public void TestTheCustomIndexChangesAsSeldomAsItCan()
        {
            var twoClaps = new[]
            {
                lane(HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL, @"kick.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP, @"clap.wav"),
                lane(HitSampleInfo.BANK_SOFT, HitSampleInfo.HIT_CLAP, @"snap.wav"),
            };

            var result = convert(new[]
            {
                new LaneSoundConversion.Note(1000, 0),
                new LaneSoundConversion.Note(1000, 1),
                new LaneSoundConversion.Note(2000, 0),
                new LaneSoundConversion.Note(2000, 2),
                // the kick alone is under both.
                new LaneSoundConversion.Note(3000, 0),
                new LaneSoundConversion.Note(4000, 0),
                new LaneSoundConversion.Note(4000, 2),
            }, twoClaps);

            Assert.That(result.Circles.Select(c => c.CustomIndex), Is.EqualTo(new[] { 1, 2, 2, 2 }));
        }

        /// <summary>
        /// A lane with no file is the skin's sound: the circle says which, and no file is written for it.
        /// </summary>
        [Test]
        public void TestALaneWithNoFileIsTheSoundOfTheSkin()
        {
            var result = convert(new[] { new LaneSoundConversion.Note(1000, 6) }, lanes);

            Assert.That(asWritten(result.Circles.Single()), Is.EqualTo(@"8,2:3"));
            Assert.That(result.Files.Select(f => f.Name), Is.EqualTo(new[] { @"soft-hitnormal" }));
        }

        /// <summary>
        /// A file that is not there is no different from none.
        /// </summary>
        [Test]
        public void TestAFileThatIsGoneIsTheSoundOfTheSkin()
        {
            var result = LaneSoundConversion.Convert(new[] { new LaneSoundConversion.Note(1000, 3) }, lanes, fallback, file => file != @"soft-hitclap.wav");

            Assert.That(asWritten(result.Circles.Single()), Is.EqualTo(@"8,2:2"));
            Assert.That(result.Files.Select(f => f.Name), Is.EqualTo(new[] { @"soft-hitnormal" }));
        }

        [Test]
        public void TestTheFirstCustomIndexCanBeAnother()
        {
            var result = LaneSoundConversion.Convert(new[] { new LaneSoundConversion.Note(1000, 3) }, lanes, fallback, _ => true, 4);

            Assert.That(result.Circles.Single().CustomIndex, Is.EqualTo(4));
            Assert.That(result.Files.Select(f => f.Name), Is.EquivalentTo(new[] { @"soft-hitnormal4", @"soft-hitclap4" }));
        }

        /// <summary>
        /// An osu! difficulty with nothing in it but two red lines.
        /// </summary>
        private static readonly string[] file =
        {
            "osu file format v14",
            "",
            "[General]",
            "AudioFilename: audio.mp3",
            "Mode: 0",
            "",
            "[Metadata]",
            "Title:Title",
            "Version:Hitsounds (mania)",
            "BeatmapID:0",
            "",
            "[Difficulty]",
            "CircleSize:4",
            "",
            "[TimingPoints]",
            "1000,500,4,2,0,100,1,0",
            "3000,400,4,2,0,100,1,0",
            "",
            "[HitObjects]",
            "",
        };

        /// <summary>
        /// Every circle is in the middle and starts a combo, as in the user's own, and says its two sample sets and
        /// nothing else.
        /// </summary>
        [Test]
        public void TestCirclesAreWrittenAsMappingToolsWritesThem()
        {
            string written = HitsoundDifficultyWriter.FromLanes(string.Join("\r\n", file), "Hitsounds", new[]
            {
                new LaneSoundConversion.Circle(1000, HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, true, false, true, 1),
                new LaneSoundConversion.Circle(1500.4, HitSampleInfo.BANK_DRUM, HitSampleInfo.BANK_DRUM, false, false, false, 1),
                new LaneSoundConversion.Circle(1999.9999999, HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM, false, true, false, 1),
            }, Array.Empty<(double, int)>(), HitSampleInfo.BANK_SOFT);

            Assert.That(section(written, "[HitObjects]"), Is.EqualTo(new[]
            {
                "256,192,1000,5,10,1:2:0:0:",
                "256,192,1500,5,0,3:3:0:0:",
                "256,192,2000,5,4,2:3:0:0:",
            }));

            Assert.That(written, Does.Contain("\r\nVersion:Hitsounds\r\n"));
            Assert.That(written, Does.Contain("\r\nStackLeniency: 0\r\n"));
        }

        /// <summary>
        /// A green line is written where the custom index of the circles changes, and where the volume does, and nowhere
        /// else. A red line says a custom index and a volume as a green one does, so it says what is wanted where it is
        /// and no green line is put on top of it.
        /// </summary>
        [Test]
        public void TestGreenLinesAreOnlyWhereTheCustomIndexOrTheVolumeChanges()
        {
            string written = HitsoundDifficultyWriter.FromLanes(string.Join("\r\n", file), "Hitsounds", new[]
            {
                circleUnder(1000, 1),
                circleUnder(1500, 1),
                circleUnder(2000, 2),
                circleUnder(2500, 2),
                circleUnder(3500, 2),
            }, new[] { (1000.0, 50), (1750.0, 70) }, HitSampleInfo.BANK_SOFT);

            Assert.That(section(written, "[TimingPoints]"), Is.EqualTo(new[]
            {
                "1000,500,4,2,1,50,1,0",
                "1750,-100,4,2,1,70,0,0",
                "2000,-100,4,2,2,70,0,0",
                "3000,400,4,2,2,70,1,0",
            }));
        }

        /// <summary>
        /// With no volume said, the lines say the one the red line does.
        /// </summary>
        [Test]
        public void TestTheVolumeIsTheRedLinesWhereNoneIsSaid()
        {
            string written = HitsoundDifficultyWriter.FromLanes(string.Join("\r\n", file), "Hitsounds", new[] { circleUnder(1500, 1), circleUnder(2000, 1) },
                Array.Empty<(double, int)>(), HitSampleInfo.BANK_SOFT);

            Assert.That(section(written, "[TimingPoints]"), Is.EqualTo(new[]
            {
                // nothing is wanted yet where the first red line is, and it is left saying what it did.
                "1000,500,4,2,0,100,1,0",
                "1500,-100,4,2,1,100,0,0",
                "3000,400,4,2,1,100,1,0",
            }));
        }

        /// <summary>
        /// Stable goes by the first red line for anything before it, so what is wanted before it is said by it.
        /// </summary>
        [Test]
        public void TestWhatIsWantedBeforeTheFirstRedLineIsSaidByIt()
        {
            string written = HitsoundDifficultyWriter.FromLanes(string.Join("\r\n", file), "Hitsounds", new[] { circleUnder(500, 1), circleUnder(2000, 1) },
                new[] { (200.0, 60) }, HitSampleInfo.BANK_SOFT);

            Assert.That(section(written, "[TimingPoints]"), Is.EqualTo(new[]
            {
                "1000,500,4,2,1,60,1,0",
                "3000,400,4,2,1,60,1,0",
            }));
        }

        /// <summary>
        /// Every red and green line says the sample set chosen for the sound of a circle with no hit sound of its own,
        /// whatever the lines of the difficulty hitsounded in said. A circle says its own sample sets, so the snare of a
        /// lane put under normal-hitnormal is still heard where the lines say soft (2026-11-01).
        /// </summary>
        [TestCase(HitSampleInfo.BANK_SOFT, "2")]
        [TestCase(HitSampleInfo.BANK_DRUM, "3")]
        [TestCase(HitSampleInfo.BANK_NORMAL, "1")]
        public void TestEveryLineSaysTheSampleSetChosenForTheFallback(string fallbackBank, string set)
        {
            string[] normalLines = file.Select(l => l.Replace(",4,2,", ",4,1,")).ToArray();

            string written = HitsoundDifficultyWriter.FromLanes(string.Join("\r\n", normalLines), "Hitsounds", new[]
            {
                new LaneSoundConversion.Circle(1000, HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, false, false, false, 1),
                new LaneSoundConversion.Circle(1500, HitSampleInfo.BANK_DRUM, HitSampleInfo.BANK_DRUM, false, false, false, 2),
                new LaneSoundConversion.Circle(3500, HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_SOFT, false, false, false, 1),
            }, new[] { (2000.0, 70) }, fallbackBank);

            string[] lines = section(written, "[TimingPoints]");

            Assert.That(lines, Has.Length.GreaterThan(2));
            Assert.That(lines.Select(l => l.Split(',')[3]), Is.All.EqualTo(set));
            Assert.That(section(written, "[HitObjects]").Select(l => l.Split(',')[5]), Is.EqualTo(new[] { "1:2:0:0:", "3:3:0:0:", "2:2:0:0:" }));
        }

        private static LaneSoundConversion.Circle circleUnder(double time, int customIndex) =>
            new LaneSoundConversion.Circle(time, HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_SOFT, false, false, false, customIndex);

        /// <summary>
        /// The lines of one section of a file.
        /// </summary>
        private static string[] section(string written, string name) =>
            written.Replace("\r\n", "\n").Split('\n').SkipWhile(l => l != name).Skip(1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();

        private static LaneSoundConversion.Result convert(IEnumerable<LaneSoundConversion.Note> notes, IReadOnlyList<LaneSound> lanes) =>
            LaneSoundConversion.Convert(notes, lanes, fallback, _ => true);

        private static LaneSound lane(string bank, string sound, string file) => new LaneSound { Bank = bank, Sound = sound, File = file };

        /// <summary>
        /// A circle's hitsound number and sample sets as a file has them.
        /// </summary>
        private static string asWritten(LaneSoundConversion.Circle circle)
        {
            int sound = (circle.Whistle ? 2 : 0) | (circle.Finish ? 4 : 0) | (circle.Clap ? 8 : 0);

            return $@"{sound},{number(circle.NormalBank)}:{number(circle.AdditionBank)}";

            static int number(string bank) => bank switch
            {
                HitSampleInfo.BANK_NORMAL => 1,
                HitSampleInfo.BANK_SOFT => 2,
                _ => 3,
            };
        }
    }
}
