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
    /// A green line the editor keeps aside from a stable file, standing between sliders at a velocity of their own.
    /// </summary>
    [TestFixture]
    public class PreservedLinesVelocityTest
    {
        private const string file = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                    + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                    + "[TimingPoints]\r\n0,500,4,2,0,60,1,0\r\n3000,-111.11111111111111,4,2,0,60,0,0\r\n\r\n"
                                    + "[HitObjects]\r\n100,100,1000,1,0,0:0:0:0:\r\n";

        /// <summary>
        /// Reported on 2026-09-25 with a recording: sliders placed at 1.5x across a 0.9x line put in on an empty stretch
        /// went to 0.9x on an undo, which reads the beatmap back from what the encoder wrote.
        /// </summary>
        [Test]
        public void TestSlidersAtOneVelocityAcrossAKeptLineReadBackAsTheyAre()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file);
            Beatmap decoded;

            using (var reader = new LineBufferedReader(new MemoryStream(bytes)))
                decoded = osu.Game.Beatmaps.Formats.Decoder.GetDecoder<Beatmap>(reader).Decode(reader);

            var beatmap = new FlatWorkingBeatmap(decoded).GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
            beatmap.ControlPointInfo = EditorBeatmap.ConvertControlPoints(beatmap.ControlPointInfo.DeepClone());

            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(slider(2000));
            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(slider(4000));
            ((Beatmap<OsuHitObject>)beatmap).HitObjects.Add(slider(5000));

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            var builder = new StringBuilder();

            using (var writer = new StringWriter(builder))
                new LegacyBeatmapEncoder(beatmap, null, null).Encode(writer);

            Beatmap readBack;

            using (var reader = new LineBufferedReader(new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()))))
                readBack = new LegacyBeatmapDecoder().Decode(reader);

            Assert.That(readBack.HitObjects.OfType<IHasSliderVelocity>().Select(s => s.SliderVelocityMultiplier), Is.EqualTo(new[] { 1.5, 1.5, 1.5 }));
        }

        private static Slider slider(double time) => new Slider
        {
            StartTime = time,
            Position = new Vector2(100, 200),
            SliderVelocityMultiplier = 1.5,
            Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(100, 0) }),
        };
    }
}
