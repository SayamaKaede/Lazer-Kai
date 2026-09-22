// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Reading the red and green lines out of a saved beatmap file.
    /// </summary>
    [TestFixture]
    public class SavedTimingLinesTest
    {
        private const string file = "osu file format v14\r\n"
                                    + "\r\n"
                                    + "[General]\r\n"
                                    + "SampleSet: Soft\r\n"
                                    + "\r\n"
                                    + "[TimingPoints]\r\n"
                                    + "27577,327.868852459016,4,2,1,60,1,0\r\n"
                                    + "27577,-117.647058823529,4,1,1,60,0,0\r\n"
                                    + "37904,-400,4,3,2,5,0,1\r\n"
                                    + "\r\n"
                                    + "\r\n"
                                    + "[HitObjects]\r\n"
                                    + "256,192,27577,1,0,0:0:0:0:\r\n";

        [Test]
        public void TestEveryLineIsReadInTheOrderWritten()
        {
            var lines = SavedTimingLines.Read(Encoding.UTF8.GetBytes(file));

            Assert.That(lines.Select(l => (l.Time, l.Uninherited)), Is.EqualTo(new[] { (27577d, true), (27577d, false), (37904d, false) }));
        }

        [Test]
        public void TestARedLineCarriesItsTempo()
        {
            var red = SavedTimingLines.Read(Encoding.UTF8.GetBytes(file))[0];

            Assert.That(red.BeatsPerMinute, Is.EqualTo(183).Within(0.01));
            Assert.That(red.Meter, Is.EqualTo(4));
            Assert.That((red.SampleSet, red.SampleIndex, red.Volume, red.Kiai), Is.EqualTo((2, 1, 60, false)));
        }

        [Test]
        public void TestAGreenLineCarriesItsVelocityAndSounds()
        {
            var green = SavedTimingLines.Read(Encoding.UTF8.GetBytes(file))[2];

            Assert.That(green.SliderVelocity, Is.EqualTo(0.25).Within(0.0001));
            Assert.That((green.SampleSet, green.SampleIndex, green.Volume, green.Kiai), Is.EqualTo((3, 2, 5, true)));
        }

        [Test]
        public void TestALineWithoutItsLaterFieldsTakesStablesDefaults()
        {
            var lines = SavedTimingLines.Read(Encoding.UTF8.GetBytes("osu file format v3\r\n[TimingPoints]\r\n1000,500\r\n2000,-50\r\n"));

            Assert.That(lines.Select(l => (l.Meter, l.Volume, l.Uninherited)), Is.EqualTo(new[] { (4, 100, true), (4, 100, false) }));
        }

        [Test]
        public void TestAFileWithABomIsRead()
        {
            byte[] withBom = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(file)).ToArray();

            Assert.That(SavedTimingLines.Read(withBom), Has.Count.EqualTo(3));
        }
    }
}
