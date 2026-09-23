// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Objects that carry their own volume or custom index in the last field of their line, as Mapping Tools' hitsound
    /// copier writes them. A change to their sounds goes into that field, since no green line can change what it says.
    /// </summary>
    [TestFixture]
    public class StableFileObjectSoundsTest
    {
        private const string file_start = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                          + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                          + "[TimingPoints]\r\n0,500,4,2,0,60,1,0\r\n\r\n[HitObjects]\r\n";

        [Test]
        public void TestAnObjectsOwnVolumeIsChangedInItsLine()
        {
            string file = file_start + "100,100,1000,1,0,0:0:0:80:\r\n200,100,2000,1,0,0:0:0:0:\r\n";

            string saved = save(file, beatmap =>
            {
                var circle = beatmap.HitObjects[0];
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 40)).ToList();
            });

            Assert.That(hitObjects(saved), Is.EqualTo(new[] { "100,100,1000,1,0,0:0:0:40:", "200,100,2000,1,0,0:0:0:0:" }));
            Assert.That(timingPoints(saved), Is.EqualTo(new[] { "0,500,4,2,0,60,1,0" }));
        }

        [Test]
        public void TestAnObjectsOwnIndexIsChangedInItsLine()
        {
            string file = file_start + "100,100,1000,1,0,0:0:3:80:\r\n200,100,2000,1,0,0:0:0:0:\r\n";

            string saved = save(file, beatmap =>
            {
                var circle = beatmap.HitObjects[0];
                circle.Samples = circle.Samples.Select(s => s.With(newSuffix: "5")).ToList();
            });

            Assert.That(hitObjects(saved), Is.EqualTo(new[] { "100,100,1000,1,0,0:0:5:80:", "200,100,2000,1,0,0:0:0:0:" }));
        }

        /// <summary>
        /// A slider's last field is read for its banks alone: its volume and index there are not read at all, so its
        /// sounds follow the green lines whatever it says, and a change to them goes on lines.
        /// </summary>
        [Test]
        public void TestASlidersOwnVolumeIsNotReadSoItsHeadGoesOnALine()
        {
            string file = file_start + "100,100,1000,2,0,L|300:100,1,140,0|0,0:0|0:0,0:0:0:80:\r\n200,100,3000,1,0,0:0:0:0:\r\n";

            string saved = save(file, beatmap =>
            {
                var slider = (IHasRepeats)beatmap.HitObjects[0];
                slider.NodeSamples[0] = slider.NodeSamples[0].Select(s => s.With(newVolume: 40)).ToList();
            });

            Assert.That(hitObjects(saved)[0], Is.EqualTo("100,100,1000,2,0,L|300:100,1,140,0|0,0:0|0:0,0:0:0:80:"), "its line is as it was");

            var readBack = decode(saved);
            var slider = (IHasRepeats)readBack.HitObjects[0];

            Assert.That(slider.NodeSamples[0].First().Volume, Is.EqualTo(40), "head");
            Assert.That(readBack.HitObjects[0].Samples.First().Volume, Is.EqualTo(60), "body");
            Assert.That(slider.NodeSamples[1].First().Volume, Is.EqualTo(60), "tail");
            Assert.That(readBack.HitObjects[1].Samples.First().Volume, Is.EqualTo(60), "the next circle");
        }

        [Test]
        public void TestAnObjectCarryingItsOwnIsLeftAsItWasWhenAnotherChanges()
        {
            string file = file_start + "100,100,1000,1,0,0:0:0:80:\r\n200,100,2000,1,0,0:0:0:0:\r\n";

            string saved = save(file, beatmap =>
            {
                var circle = beatmap.HitObjects[1];
                circle.Samples = circle.Samples.Select(s => s.With(newVolume: 30)).ToList();
            });

            Assert.That(hitObjects(saved)[0], Is.EqualTo("100,100,1000,1,0,0:0:0:80:"));
        }

        private static string save(string file, System.Action<IBeatmap> edit)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file);

            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            edit(beatmap);

            // as the editor does once an object is changed: its nested objects, a slider's head among them, take the
            // change from it, and the encoder writes both.
            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
            return Encoding.UTF8.GetString(written!);
        }

        private static IBeatmap open(byte[] file)
        {
            var decoded = decode(Encoding.UTF8.GetString(file));
            var playable = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            playable.ControlPointInfo = EditorBeatmap.ConvertControlPoints(playable.ControlPointInfo.DeepClone());
            return playable;
        }

        private static Beatmap decode(string file)
        {
            using var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(file)));
            return osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        private static string[] section(string text, string name) =>
            text.Split("\r\n").SkipWhile(l => l != $"[{name}]").Skip(1).TakeWhile(l => !l.StartsWith('[')).Where(l => l.Length > 0).ToArray();

        private static string[] hitObjects(string text) => section(text, "HitObjects");

        private static string[] timingPoints(string text) => section(text, "TimingPoints");
    }
}
