// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Timing;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The lines of a file once the red lines of another difficulty are put in the place of its own (2026-10-13).
    /// </summary>
    [TestFixture]
    public class RedLineCopyingTest
    {
        /// <summary>
        /// A red line of the soft set, custom index 1, at 80%.
        /// </summary>
        private const string red = @"1000,400,4,2,1,80,1,0";

        /// <summary>
        /// A green line of the drum set, custom index 3, at 40%, twice as fast and in kiai.
        /// </summary>
        private const string green = @"5000,-50,4,3,3,40,0,1";

        private static string[] with(string[] lines, double within, params RedLineCopying.RedLine[] redLines) =>
            RedLineCopying.LinesWith(lines.Select(l => SavedTimingLines.Parse(l)!.Value).ToArray(), redLines, within).Select(l => l.Raw).ToArray();

        private static RedLineCopying.RedLine at(double time, double beatLength = 400, int meter = 4, bool omitFirstBarLine = false) =>
            new RedLineCopying.RedLine(time, beatLength, meter, omitFirstBarLine);

        #region Where a time goes for its beat to be the same

        private static double moved(double time, RedLineCopying.RedLine[] from, RedLineCopying.RedLine[] to) => RedLineCopying.KeepingBeats(from, to)(time);

        [Test]
        public void TestRedLinesFurtherOnTakeEverythingAsFarOn()
        {
            var from = new[] { at(1000) };
            var to = new[] { at(1080) };

            Assert.That(moved(1000, from, to), Is.EqualTo(1080).Within(1e-6));
            Assert.That(moved(1400, from, to), Is.EqualTo(1480).Within(1e-6));

            // what is on no tick is as far from the ticks as it was.
            Assert.That(moved(1237, from, to), Is.EqualTo(1317).Within(1e-6));

            // before the first red line too, where the beats are counted back from it.
            Assert.That(moved(800, from, to), Is.EqualTo(880).Within(1e-6));
        }

        [Test]
        public void TestAnotherBeatLengthPutsEverythingAsManyBeatsOn()
        {
            var from = new[] { at(1000, 500) };
            var to = new[] { at(1000, 400) };

            Assert.That(moved(2000, from, to), Is.EqualTo(1800).Within(1e-6));
            Assert.That(moved(1250, from, to), Is.EqualTo(1200).Within(1e-6));
            Assert.That(moved(750, from, to), Is.EqualTo(800).Within(1e-6));
        }

        [Test]
        public void TestTheBeatsAreCountedThroughEveryRedLine()
        {
            // the second starts half a beat in, as a red line put in for a bar that is cut short does.
            var from = new[] { at(0, 500), at(10250, 400) };
            var to = new[] { at(80, 500), at(10330, 400) };

            Assert.That(moved(10000, from, to), Is.EqualTo(10080).Within(1e-6));
            Assert.That(moved(10250, from, to), Is.EqualTo(10330).Within(1e-6));
            Assert.That(moved(10650, from, to), Is.EqualTo(10730).Within(1e-6));
        }

        [Test]
        public void TestARedLinePutInWhereTheSongHasDriftedIsWhereItsBeatGoes()
        {
            // by the 120th beat the song is ten milliseconds behind the beat length, and a red line says where it is.
            var from = new[] { at(0, 500) };
            var to = new[] { at(0, 500), at(60010, 500) };

            Assert.That(moved(59500, from, to), Is.EqualTo(59500).Within(1e-6));
            Assert.That(moved(60000, from, to), Is.EqualTo(60010).Within(1e-6));
            Assert.That(moved(60500, from, to), Is.EqualTo(60510).Within(1e-6));

            // and the other way, onto red lines that do not have it.
            Assert.That(moved(60010, to, from), Is.EqualTo(60000).Within(1e-6));
            Assert.That(moved(60510, to, from), Is.EqualTo(60500).Within(1e-6));
        }

        [Test]
        public void TestWhatIsJustBeforeARedLineDoesNotGoPastWhatIsOnIt()
        {
            // the stretch before the red line is some milliseconds longer than its beats, and what is in them is no
            // later than the red line's own beat.
            var from = new[] { at(0, 500), at(60010, 500) };
            var to = new[] { at(0, 500) };

            Assert.That(moved(60009, from, to), Is.EqualTo(60000).Within(1e-6));
            Assert.That(moved(60009, from, to), Is.LessThanOrEqualTo(moved(60010, from, to)));
        }

        [Test]
        public void TestTheSameRedLinesMoveNothing()
        {
            var lines = new[] { at(123, 345.6), at(4567, 300) };

            foreach (double time in new[] { 0.0, 123, 1000.5, 4566, 4567, 9999 })
                Assert.That(moved(time, lines, lines), Is.EqualTo(time).Within(1e-6));
        }

        [Test]
        public void TestWithNoRedLinesToCountByNothingMoves()
        {
            Assert.That(moved(1000, System.Array.Empty<RedLineCopying.RedLine>(), new[] { at(80) }), Is.EqualTo(1000));
            Assert.That(moved(1000, new[] { at(80) }, System.Array.Empty<RedLineCopying.RedLine>()), Is.EqualTo(1000));
        }

        #endregion

        [Test]
        public void TestARedLineAFewMillisecondsOffIsTheSameLineMoved()
        {
            Assert.That(with(new[] { red, green }, 4, at(1002)), Is.EqualTo(new[] { @"1002,400,4,2,1,80,1,0", green }));
            Assert.That(with(new[] { red, green }, 4, at(997)), Is.EqualTo(new[] { @"997,400,4,2,1,80,1,0", green }));
        }

        [Test]
        public void TestItsBeatAndItsBarAreTheOtherDifficultys()
        {
            Assert.That(with(new[] { red }, 4, at(1000, 375.5, 3, true)), Is.EqualTo(new[] { @"1000,375.5,3,2,1,80,1,8" }));
        }

        [Test]
        public void TestItsKiaiIsKeptBesideABarLineLeftOut()
        {
            Assert.That(with(new[] { @"1000,400,4,2,1,80,1,1" }, 4, at(1000, omitFirstBarLine: true)), Is.EqualTo(new[] { @"1000,400,4,2,1,80,1,9" }));
        }

        [Test]
        public void TestARedLineTheOtherHasNotBecomesAGreenLine()
        {
            // what it says of the sounds is followed up to the next line, with or without a beat of its own.
            Assert.That(with(new[] { red, @"3000,300,4,1,0,50,1,0", green }, 4, at(1000)),
                Is.EqualTo(new[] { red, @"3000,-100,4,1,0,50,0,0", green }));
        }

        [Test]
        public void TestOneThatSaysWhatTheLineBeforeItDoesIsTakenOut()
        {
            Assert.That(with(new[] { red, @"3000,300,4,2,1,80,1,0", green }, 4, at(1000)), Is.EqualTo(new[] { red, green }));
        }

        [Test]
        public void TestOneAfterAFasterGreenLineStaysForTheVelocity()
        {
            // a red line is where the velocity is as it is written again: taken out, what follows would be as fast as
            // the green line before it says.
            string fast = @"2000,-50,4,2,1,80,0,0";

            Assert.That(with(new[] { red, fast, @"3000,300,4,2,1,80,1,0" }, 4, at(1000)),
                Is.EqualTo(new[] { red, fast, @"3000,-100,4,2,1,80,0,0" }));
        }

        [Test]
        public void TestARedLineThisHasNotSaysWhatIsFollowedThere()
        {
            Assert.That(with(new[] { red, green }, 4, at(1000), at(6000, 300)),
                Is.EqualTo(new[] { red, green, @"6000,300,4,3,3,40,1,1" }));
        }

        [Test]
        public void TestARedLineIsBeforeTheGreenLinesOfItsTime()
        {
            // which is where stable has it, so that the green line is still the one read. It says what the green line
            // does, being followed from the same time on.
            Assert.That(with(new[] { red, green }, 4, at(1000), at(5000, 300)),
                Is.EqualTo(new[] { red, @"5000,300,4,3,3,40,1,1", green }));
        }

        [Test]
        public void TestARedLineBeforeEveryLineSaysWhatTheFirstDoes()
        {
            Assert.That(with(new[] { red, green }, 4, at(200), at(1000)),
                Is.EqualTo(new[] { @"200,400,4,2,1,80,1,0", red, green }));
        }

        [Test]
        public void TestRedLinesFurtherOffThanToldAreOtherLines()
        {
            // the one that was there stays as a green line: the first of the file has no line before it to say the same.
            Assert.That(with(new[] { red, green }, 1, at(1002)), Is.EqualTo(new[] { @"1000,-100,4,2,1,80,0,0", @"1002,400,4,2,1,80,1,0", green }));
        }

        [Test]
        public void TestAFileOfNoLinesIsGivenTheRedLines()
        {
            Assert.That(with(System.Array.Empty<string>(), 4, at(500), at(900, 300, 3)), Is.EqualTo(new[] { @"500,400,4,0,0,100,1,0", @"900,300,3,0,0,100,1,0" }));
        }

        [Test]
        public void TestLinesNobodyTouchedAreWrittenAsTheyWere()
        {
            // spacing and all, as a line nobody edited is.
            string spaced = @" 5000, -50 ,4,3,3,40,0,1";

            Assert.That(with(new[] { red, spaced }, 4, at(1002)).Last(), Is.EqualTo(spaced));
        }
    }
}
