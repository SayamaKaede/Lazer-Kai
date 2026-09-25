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
using osuTK;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The lines a save puts in for the sliders' velocities, worked out as the editor shows them, before saving.
    /// </summary>
    [TestFixture]
    public class StableFileVelocityLinesTest
    {
        private const string file_start = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                          + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                          + "[TimingPoints]\r\n0,500,4,2,0,60,1,0\r\n0,-111.11111111111111,4,2,0,60,0,0\r\n\r\n"
                                          + "[HitObjects]\r\n100,100,1000,1,0,0:0:0:0:\r\n";

        [Test]
        public void TestASliderSayingWhatTheLineSaysNeedsNone()
        {
            var beatmap = open(out _);
            add(beatmap, 2000, 0.9);

            Assert.That(StableFileMerger.WithVelocityLines(lines(), beatmap), Is.EqualTo(lines()));
        }

        [Test]
        public void TestASliderSayingOtherwiseGetsALineAtItsHead()
        {
            var beatmap = open(out _);
            add(beatmap, 2000.5, 1.2);

            var shown = StableFileMerger.WithVelocityLines(lines(), beatmap);

            Assert.That(shown, Has.Count.EqualTo(3));
            Assert.That(SavedTimingLines.Parse(shown[2])!.Value.Time, Is.EqualTo(2000));
            Assert.That(SavedTimingLines.Parse(shown[2])!.Value.SliderVelocity, Is.EqualTo(1.2).Within(0.0005));
        }

        /// <summary>
        /// Sliders placed one after another at the same velocity take one line between them: the line at the first
        /// carries on to the rest, as a green line does in stable.
        /// </summary>
        [Test]
        public void TestSlidersAfterEachOtherAtOneVelocityTakeOneLine()
        {
            var beatmap = open(out _);
            add(beatmap, 2000, 1.2);
            add(beatmap, 3000, 1.2);
            add(beatmap, 4000, 1.2);

            Assert.That(StableFileMerger.WithVelocityLines(lines(), beatmap), Has.Count.EqualTo(3));
        }

        /// <summary>
        /// What is shown before saving is what the save writes.
        /// </summary>
        [Test]
        public void TestWhatIsShownIsWhatIsSaved()
        {
            var beatmap = open(out byte[] file);
            var keeper = new StableFileKeeper(file, beatmap, null, null);

            add(beatmap, 2000.5, 1.2);
            add(beatmap, 3000, 1.2);
            add(beatmap, 4000, 0.9);
            add(beatmap, 5000.25, 0.75);

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);
            Assert.That(written, Is.Not.Null, "could not be kept in stable's format: " + keeper.LastFailure);

            var saved = SavedTimingLines.Read(written!).Select(l => (l.Time, System.Math.Round(l.SliderVelocity, 4)));
            var shown = StableFileMerger.WithVelocityLines(lines(), beatmap).Select(l => SavedTimingLines.Parse(l)!.Value).Select(l => (l.Time, System.Math.Round(l.SliderVelocity, 4)));

            Assert.That(shown, Is.EqualTo(saved));
        }

        private static string[] lines() => new[] { "0,500,4,2,0,60,1,0", "0,-111.11111111111111,4,2,0,60,0,0" };

        private static void add(IBeatmap beatmap, double time, double velocity)
        {
            var slider = new Slider
            {
                StartTime = time,
                Position = new Vector2(100, 200),
                SliderVelocityMultiplier = velocity,
                Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
            };

            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(slider);

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);
        }

        private static IBeatmap open(out byte[] bytes)
        {
            bytes = Encoding.UTF8.GetBytes(file_start);
            Beatmap decoded;

            using (var reader = new LineBufferedReader(new MemoryStream(bytes)))
                decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            var playable = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            playable.ControlPointInfo = EditorBeatmap.ConvertControlPoints(playable.ControlPointInfo.DeepClone());
            return playable;
        }
    }
}
