// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Objects placed in the editor into a beatmap kept in stable's format.
    /// </summary>
    /// <remarks>
    /// Written after DOTNET-3 (2026-09-25), from the mapper's account of it. None of these made it happen, and neither
    /// did the mapper doing the same again; they stay as what was tried. The report now says more when it happens, and
    /// the next one, DOTNET-D, said enough: a reverse taken off a slider (<see cref="TestARepeatTakenBackOffASliderStaysStable"/>).
    /// </remarks>
    [TestFixture]
    public class StableFileNewObjectsTest
    {
        private const double beat_length = 337.07865168539325;

        private const string file_start = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                          + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                          + "[TimingPoints]\r\n978,337.07865168539325,4,1,0,100,1,0\r\n978,-100,4,1,0,100,0,0\r\n";

        private const string file_end = "\r\n[HitObjects]\r\n204,345,13955,2,0,P|209:298|223:248,1,90,0|0,1:0|1:0,1:0:0:0:\r\n";

        /// <summary>
        /// Reported through the error reports on 2026-09-25 (DOTNET-3): a slider with a repeat, placed on a half beat of
        /// a beatmap converted from lazer's format, "reads back with 2 nodes, not 3". The mapper's account: a green line
        /// saying 0.9x put on an empty stretch first, then the slider placed there, which takes the velocity of the
        /// slider before it, 1.0x, and not the line's.
        /// </summary>
        [TestCase(1.0, 90)]
        [TestCase(1.0, 126)]
        [TestCase(1.0, 180)]
        [TestCase(0.9, 126)]
        public void TestARepeatingSliderPlacedAfterAGreenLineStaysStable(double velocity, double distance)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file_start + "14629,-111.11111111111111,4,1,0,100,0,0\r\n" + file_end);
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            var slider = new Slider
            {
                StartTime = 978 + beat_length * 41.5,
                Position = new Vector2(250, 200),
                RepeatCount = 1,
                SliderVelocityMultiplier = velocity,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.PERFECT_CURVE),
                    new PathControlPoint(new Vector2(40, -40)),
                    new PathControlPoint(new Vector2(90, -20)),
                }, distance),
            };

            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(slider);

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
        }

        /// <summary>
        /// The same, with the green line put in from the green line panel and not yet saved: it reaches the save as the
        /// edited lines rather than from the file.
        /// </summary>
        [TestCase(1.0, 90)]
        [TestCase(1.0, 126)]
        [TestCase(1.0, 180)]
        public void TestARepeatingSliderPlacedAfterAnUnsavedGreenLineStaysStable(double velocity, double distance)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file_start + file_end);
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            string[] lines =
            {
                "978,337.07865168539325,4,1,0,100,1,0",
                "978,-100,4,1,0,100,0,0",
                "14629,-111.11111111111111,4,1,0,100,0,0",
            };

            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(new Slider
            {
                StartTime = 978 + beat_length * 41.5,
                Position = new Vector2(250, 200),
                RepeatCount = 1,
                SliderVelocityMultiplier = velocity,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.PERFECT_CURVE),
                    new PathControlPoint(new Vector2(40, -40)),
                    new PathControlPoint(new Vector2(90, -20)),
                }, distance),
            });

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, lines, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
        }

        /// <summary>
        /// The green line put in at the very millisecond the slider is then placed on, as when a line is put on an empty
        /// spot and the slider placed there after it.
        /// </summary>
        [TestCase(1, false)]
        [TestCase(1, true)]
        [TestCase(0, false)]
        [TestCase(0, true)]
        public void TestASliderPlacedOnAnUnsavedGreenLineStaysStable(int repeats, bool lineSaved)
        {
            double sliderTime = 978 + beat_length * 41.5;
            string line = $"{System.Math.Floor(sliderTime)},-111.11111111111111,4,1,0,100,0,0";

            byte[] bytes = Encoding.UTF8.GetBytes(file_start + (lineSaved ? line + "\r\n" : string.Empty) + file_end);
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            string[]? lines = lineSaved
                ? null
                : new[]
                {
                    "978,337.07865168539325,4,1,0,100,1,0",
                    "978,-100,4,1,0,100,0,0",
                    line,
                };

            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(new Slider
            {
                StartTime = sliderTime,
                Position = new Vector2(250, 200),
                RepeatCount = repeats,
                SliderVelocityMultiplier = 1,
                Path = new SliderPath(new[]
                {
                    new PathControlPoint(Vector2.Zero, PathType.PERFECT_CURVE),
                    new PathControlPoint(new Vector2(40, -40)),
                    new PathControlPoint(new Vector2(90, -20)),
                }, 126),
            });

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, lines, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
        }

        /// <summary>
        /// A slider already in the file, given a repeat.
        /// </summary>
        [Test]
        public void TestARepeatAddedToASliderStaysStable()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file_start + file_end + "250,200,14966,2,0,P|290:160|340:180,1,126,0|0,1:0|1:0,1:0:0:0:\r\n");
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            var slider = (Slider)beatmap.HitObjects[1];
            slider.RepeatCount = 1;

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
        }

        /// <summary>
        /// Reported through the error reports on 2026-10-04 (DOTNET-D), by another mapper: a slider with no repeats
        /// "reads back with 2 nodes, not 3", with its length, velocity and duration all as wanted. A slider keeps the
        /// samples of nodes it no longer has, so one that had a repeat taken off, or was given one and had it taken back
        /// as dragging its end on the timeline out and back does, holds three nodes' samples with two nodes. Another
        /// object is moved as well: with no other edit, the slider given a repeat and had it taken back writes the same
        /// as it was read, and the objects are not read back at all.
        /// </summary>
        [TestCase(0)]
        [TestCase(1)]
        public void TestARepeatTakenBackOffASliderStaysStable(int repeatsInFile)
        {
            int nodes = repeatsInFile + 2;
            byte[] bytes = Encoding.UTF8.GetBytes(file_start + file_end
                                                  + $"250,200,14966,2,0,P|290:160|340:180,{repeatsInFile + 1},126,{string.Join('|', Enumerable.Repeat("0", nodes))},"
                                                  + $"{string.Join('|', Enumerable.Repeat("1:0", nodes))},1:0:0:0:\r\n");
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            var slider = (Slider)beatmap.HitObjects[1];

            foreach (int repeats in new[] { 1, 0 })
            {
                slider.RepeatCount = repeats;
                slider.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
            }

            Assert.That(slider.NodeSamples, Has.Count.EqualTo(3), "the slider no longer keeps the samples of a node it lost");

            var other = (Slider)beatmap.HitObjects[0];
            other.Position += new Vector2(10, 0);
            other.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);
        }

        private static IBeatmap open(byte[] bytes)
        {
            Beatmap decoded;

            using (var reader = new LineBufferedReader(new MemoryStream(bytes)))
                decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            var playable = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            playable.ControlPointInfo = EditorBeatmap.ConvertControlPoints(playable.ControlPointInfo.DeepClone());
            return playable;
        }
    }
}
