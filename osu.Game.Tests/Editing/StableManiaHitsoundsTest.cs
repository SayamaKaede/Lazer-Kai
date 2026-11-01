// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.Formats;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// What an osu!mania difficulty hitsounded in stable is read to sound as, from its file.
    /// </summary>
    [TestFixture]
    public class StableManiaHitsoundsTest
    {
        private static readonly string[] file =
        {
            "osu file format v14",
            "",
            "[General]",
            "AudioFilename: audio.mp3",
            "SampleSet: Soft",
            "Mode: 3",
            "",
            "[Metadata]",
            "Title:Title",
            "Version:Hitsounds-mania",
            "BeatmapID:0",
            "",
            "[Difficulty]",
            "CircleSize:18",
            "",
            "[TimingPoints]",
            "0,500,4,2,0,100,1,0",
            "2000,-100,4,2,0,70,0,0",
            "4000,-100,4,3,2,70,0,0",
            "",
            "[HitObjects]",
            // each a file of its own, as notes copied in stable are.
            "14,192,1000,1,2,0:2:1:0:soft-hitwhistle",
            "42,192,1000,1,1,3:0:1:0:drum-hitnormal",
            // a file the beatmap does not have.
            "184,192,1500,1,8,0:3:1:0:drum-hitclap",
            // saying nothing, so as its line says.
            "71,192,2000,1,0,0:0:0:0:",
            "99,192,2500,1,2,0:0:0:0:",
            // a hold note, which says when it ends first.
            "128,192,3000,128,4,3500:1:3:2:0:",
            "128,192,4500,1,3,0:0:0:0:",
            "",
        };

        private static readonly string[] in_the_set =
        {
            @"soft-hitwhistle.wav",
            @"drum-hitnormal.wav",
            @"soft-hitnormal.wav",
            @"drum-hitnormal2.wav",
            @"drum-hitwhistle2.ogg",
            @"drum-hitfinish2.wav",
        };

        private static string? fileOf(string name) => in_the_set.FirstOrDefault(f => f.StartsWith($@"{name}.", System.StringComparison.Ordinal));

        [Test]
        public void TestEachNoteIsReadAsWhatItSounds()
        {
            var read = StableManiaHitsounds.From(string.Join("\r\n", file), fileOf);

            Assert.That(read.Notes.Select(n => $@"{n.Time}: {read.Sounds[n.Lane].Bank}-{read.Sounds[n.Lane].Sound} < {read.Sounds[n.Lane].File}"), Is.EqualTo(new[]
            {
                // a file is the note's one sound, of the kind and the sample set the note says.
                @"1000: soft-hitwhistle < soft-hitwhistle.wav",
                @"1000: drum-hitnormal < drum-hitnormal.wav",
                // one the beatmap has no file for is the skin's.
                @"1500: drum-hitclap < ",
                // under no custom index, the skin's, of the sample set of the line.
                @"2000: soft-hitnormal < ",
                // in osu!mania a sound that is added is heard alone.
                @"2500: soft-hitwhistle < ",
                @"3000: drum-hitfinish < drum-hitfinish2.wav",
                // unless the note says it is the hit sound too. Both are as the line says: the drum's, under the second.
                @"4500: drum-hitnormal < drum-hitnormal2.wav",
                @"4500: drum-hitwhistle < drum-hitwhistle2.ogg",
            }));
        }

        /// <summary>
        /// A circle with no hit sound has the one the file's own sample set gives it, and the lines say the volumes.
        /// </summary>
        [Test]
        public void TestTheRestIsAsTheFileSays()
        {
            var read = StableManiaHitsounds.From(string.Join("\r\n", file), fileOf);

            Assert.That((read.Fallback.Bank, read.Fallback.Sound, read.Fallback.File), Is.EqualTo((@"soft", @"hitnormal", @"soft-hitnormal.wav")));
            Assert.That(read.Volumes, Is.EqualTo(new[] { (0.0, 100), (2000.0, 70), (4000.0, 70) }));
        }

        /// <summary>
        /// Written from itself it is an osu! difficulty, as Mapping Tools writes it.
        /// </summary>
        [Test]
        public void TestItIsWrittenAsAnOsuDifficulty()
        {
            string text = string.Join("\r\n", file);
            var read = StableManiaHitsounds.From(text, fileOf);
            var result = LaneSoundConversion.Convert(read.Notes, read.Sounds, read.Fallback, f => f.Length > 0);

            string[] written = HitsoundDifficultyWriter.FromLanes(text, "Hitsounds", result.Circles, read.Volumes, read.Fallback.Bank).Replace("\r\n", "\n").Split('\n');

            Assert.That(written, Does.Contain("Mode: 0"));
            Assert.That(written, Does.Contain("CircleSize:4"));
            Assert.That(written, Does.Contain("Version:Hitsounds"));
            Assert.That(written, Does.Contain("StackLeniency: 0"));
            Assert.That(written.Count(l => l.StartsWith("256,192,", System.StringComparison.Ordinal)), Is.EqualTo(6));
        }
    }
}
