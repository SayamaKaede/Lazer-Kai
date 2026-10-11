// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// A file kept in stable's format whose difficulty stops being the one it was online.
    /// </summary>
    /// <remarks>
    /// A `.osu` dropped into a set under a name the set does not have becomes a new, local difficulty of it
    /// (2026-10-11). This game leaves the line with a number out for a difficulty that has none, which the file could
    /// not be made to say, and so it was saved in lazer's format. Stable writes 0 there, and -1 for a set with none.
    /// </remarks>
    [TestFixture]
    public class StableFileOnlineNumbersTest
    {
        private const string file = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n"
                                    + "[Metadata]\r\nTitle:t\r\nVersion:v\r\nBeatmapID:557821\r\nBeatmapSetID:241526\r\n\r\n"
                                    + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                    + "[TimingPoints]\r\n978,337.07865168539325,4,1,0,100,1,0\r\n\r\n"
                                    + "[HitObjects]\r\n256,192,978,1,0,0:0:0:0:\r\n";

        [Test]
        public void TestADifficultyNoLongerOnlineIsWrittenAsStableWritesOne()
        {
            byte[] bytes = Encoding.UTF8.GetBytes(file);
            var beatmap = open(bytes);
            var keeper = new StableFileKeeper(bytes, beatmap, null, null);

            beatmap.BeatmapInfo.ResetOnlineInfo();
            beatmap.BeatmapInfo.BeatmapSet = new BeatmapSetInfo();

            byte[]? written = keeper.Write(beatmap, null, null, null, out _);

            Assert.That(keeper.LastFailure, Is.Null);
            Assert.That(written, Is.Not.Null);
            Assert.That(Encoding.UTF8.GetString(written!), Is.EqualTo(file.Replace("BeatmapID:557821", "BeatmapID:0").Replace("BeatmapSetID:241526", "BeatmapSetID:-1")));
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
