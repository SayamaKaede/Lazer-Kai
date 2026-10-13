// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// An object that comes to stand just before a red line, and sounds as another line than the red one says
    /// (DOTNET-H, 2026-10-13).
    /// </summary>
    /// <remarks>
    /// An object's line is looked up a few milliseconds after it, so a red line within those milliseconds is the one
    /// read for it. Reported with every object of a beatmap moved back after its red lines were copied from another
    /// difficulty, which left one a millisecond before a red line: the save in stable's format gave up.
    /// </remarks>
    [TestFixture]
    public class StableFileObjectBeforeRedLineTest
    {
        private const string file_start = "osu file format v14\r\n\r\n[General]\r\nAudioFilename: audio.mp3\r\nMode: 0\r\n\r\n[Metadata]\r\nTitle:t\r\nVersion:v\r\n\r\n"
                                          + "[Difficulty]\r\nHPDrainRate:5\r\nCircleSize:4\r\nOverallDifficulty:5\r\nApproachRate:5\r\nSliderMultiplier:1.4\r\nSliderTickRate:1\r\n\r\n"
                                          + "[TimingPoints]\r\n";

        /// <summary>
        /// A red line, a green line with another volume and custom index under which the first circle is, and a second
        /// red line saying what the first does, under which the second circle is.
        /// </summary>
        private const string file = file_start
                                    + "0,500,4,1,1,65,1,0\r\n1900,-100,4,1,2,55,0,0\r\n2000,500,4,1,1,65,1,0\r\n\r\n"
                                    + "[HitObjects]\r\n100,100,1900,1,0,0:0:0:0:\r\n200,100,2500,1,0,0:0:0:0:\r\n";

        [TestCase(1999)]
        [TestCase(1996)]
        public void TestAnObjectMovedToJustBeforeARedLineKeepsItsSounds(double time)
        {
            string saved = save(file, beatmap => beatmap.HitObjects[0].StartTime = time);

            var readBack = decode(saved);

            Assert.That(readBack.HitObjects[0].StartTime, Is.EqualTo(time));
            Assert.That(soundOf(readBack, 0), Is.EqualTo((55, @"2")), "the one moved");
            // the first custom set is the one with no number.
            Assert.That(soundOf(readBack, 1), Is.EqualTo((65, (string?)null)), "the one after the red line");

            Assert.That(readBack.ControlPointInfo.TimingPoints.Select(t => t.Time), Is.EqualTo(new[] { 0.0, 2000.0 }), "the red lines are where they were");
        }

        private static (int, string?) soundOf(IBeatmap beatmap, int index)
        {
            var sample = beatmap.HitObjects[index].Samples.First();

            return (sample.Volume, sample.Suffix);
        }

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
    }
}
