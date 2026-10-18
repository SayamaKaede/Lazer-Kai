// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The sample set [General] says of a beatmap kept in stable's format, which stable writes as that of the first
    /// line: an edit is not to leave it saying None (2026-10-18).
    /// </summary>
    [TestFixture]
    public class StableFileSampleSetTest
    {
        private const string file_start = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.ogg\r\nAudioLeadIn: 0\r\nPreviewTime: 64398\r\nCountdown: 0\r\n"
                                          + "SampleSet: Soft\r\nStackLeniency: 0.7\r\nMode: 0\r\nLetterboxInBreaks: 0\r\nWidescreenStoryboard: 0\r\n\r\n"
                                          + "[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                          + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                          + "[TimingPoints]\r\n566,333.333333333333,4,2,1,75,1,0\r\n566,-83.3333333333333,4,2,1,75,0,0\r\n1065,-83.3333333333333,4,2,1,65,0,0\r\n\r\n"
                                          + "[HitObjects]\r\n100,100,566,1,0,0:0:0:0:\r\n200,100,1065,1,0,0:0:0:0:\r\n300,100,1565,1,0,0:0:0:0:\r\n";

        [Test]
        public void TestNothingEdited()
        {
            Assert.That(general(save(file_start, _ => { })), Does.Contain("SampleSet: Soft"));
        }

        [Test]
        public void TestAVolumeChanged()
        {
            string saved = save(file_start, beatmap =>
            {
                var circle = beatmap.HitObjects[2];
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 40)).ToList();
            });

            Assert.That(general(saved), Does.Contain("SampleSet: Soft"), saved);
        }

        [Test]
        public void TestAnAdditionChanged()
        {
            string saved = save(file_start, beatmap =>
            {
                var circle = beatmap.HitObjects[0];
                circle.Samples = circle.Samples.Append(circle.Samples.First().With(HitSampleInfo.HIT_WHISTLE)).ToList();
            });

            Assert.That(general(saved), Does.Contain("SampleSet: Soft"), saved);
        }

        [Test]
        public void TestTheFirstObjectsBankChanged()
        {
            string saved = save(file_start, beatmap =>
            {
                var circle = beatmap.HitObjects[0];
                circle.Samples = circle.Samples.Select(s => s.With(newBank: HitSampleInfo.BANK_DRUM)).ToList();
            });

            Assert.That(general(saved), Does.Contain("SampleSet: Soft"), saved);
        }

        /// <summary>
        /// One that says None, as Attractor [Memory] came to (2026-10-18): stable writes the first line's on a save, and
        /// so does a save here, edited or not.
        /// </summary>
        [Test]
        public void TestNoneIsSavedAsTheFirstLinesWithNothingEdited()
        {
            string file = file_start.Replace("SampleSet: Soft", "SampleSet: None");

            string saved = save(file, _ => { });

            Assert.That(saved, Is.EqualTo(file.Replace("SampleSet: None", "SampleSet: Soft")), "nothing but that line changes");
        }

        [Test]
        public void TestNoneIsSavedAsTheFirstLinesWithAVolumeChanged()
        {
            string file = file_start.Replace("SampleSet: Soft", "SampleSet: None");

            string saved = save(file, beatmap =>
            {
                var circle = beatmap.HitObjects[2];
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 40)).ToList();
            });

            Assert.That(general(saved), Does.Contain("SampleSet: Soft"), saved);
        }

        [Test]
        public void TestAnotherSampleSetIsSavedAsTheFirstLines()
        {
            string file = file_start.Replace("SampleSet: Soft", "SampleSet: Normal").Replace("566,333.333333333333,4,2,1,75,1,0", "566,333.333333333333,4,3,1,75,1,0");

            Assert.That(general(save(file, _ => { })), Does.Contain("SampleSet: Drum"));
        }

        /// <summary>
        /// A first line that says no sample set of its own is of the one [General] says, so there is nothing to take
        /// from it.
        /// </summary>
        [Test]
        public void TestAFirstLineOfNoSampleSetLeavesItAsItIs()
        {
            string file = file_start.Replace("SampleSet: Soft", "SampleSet: None").Replace("566,333.333333333333,4,2,1,75,1,0", "566,333.333333333333,4,0,1,75,1,0");

            Assert.That(save(file, _ => { }), Is.EqualTo(file));
        }

        [Test]
        public void TestByteOrderMarkAndLineEndsAreKept()
        {
            string file = "﻿" + file_start.Replace("SampleSet: Soft", "SampleSet: None");

            Assert.That(save(file, _ => { }), Is.EqualTo(file.Replace("SampleSet: None", "SampleSet: Soft")));
        }

        private static string general(string file) =>
            string.Join("\n", file.Split("\r\n").SkipWhile(l => l != "[General]").TakeWhile(l => l.Length > 0));

        private static string save(string file, System.Action<IBeatmap> edit)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file);

            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            edit(beatmap);

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
            return Encoding.UTF8.GetString(written!);
        }

        private static IBeatmap open(byte[] file)
        {
            Beatmap decoded;

            using (var reader = new LineBufferedReader(new MemoryStream(file)))
                decoded = new LegacyBeatmapDecoder().Decode(reader);

            var playable = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            playable.ControlPointInfo = EditorBeatmap.ConvertControlPoints(playable.ControlPointInfo.DeepClone());
            return playable;
        }
    }
}
