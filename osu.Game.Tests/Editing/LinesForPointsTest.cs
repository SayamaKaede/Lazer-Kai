// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.GreenLines;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The lines put in for a change that is for the selected objects alone: those are given it, and every other sound
    /// of the beatmap goes on following what it did (2026-10-14).
    /// </summary>
    [TestFixture]
    public class LinesForPointsTest
    {
        private static readonly double[] every_half_second = { 1000, 1500, 2000, 2500, 3000 };

        [Test]
        public void TestARunIsGivenALineWhereItBeginsAndTheNextSoundOneThatPutsItBack()
        {
            var lines = change(new[] { red(0, 100) }, every_half_second, new double[] { 1500, 2000 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[] { "red 0 set1 index0 100%", "green 1500 set1 index0 50%", "green 2500 set1 index0 100%" }));
        }

        [Test]
        public void TestSoundsPickedHereAndThereEachHaveTheirOwn()
        {
            var lines = change(new[] { red(0, 100) }, every_half_second, new double[] { 1000, 2000 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[]
            {
                "red 0 set1 index0 100%", "green 1000 set1 index0 50%", "green 1500 set1 index0 100%", "green 2000 set1 index0 50%", "green 2500 set1 index0 100%"
            }));
        }

        [Test]
        public void TestNothingIsPutBackAfterTheLastSound()
        {
            var lines = change(new[] { red(0, 100) }, every_half_second, new double[] { 3000 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[] { "red 0 set1 index0 100%", "green 3000 set1 index0 50%" }));
        }

        /// <summary>
        /// A line inside the run is changed where it stands, and what it said is what the sound after the run goes back to.
        /// </summary>
        [Test]
        public void TestALineInsideTheRunIsChangedAndWhatItSaidIsPutBack()
        {
            var lines = change(new[] { red(0, 100), green(2000, 70) }, every_half_second, new double[] { 1500, 2000, 2500 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[]
            {
                "red 0 set1 index0 100%", "green 1500 set1 index0 50%", "green 2000 set1 index0 50%", "green 3000 set1 index0 70%"
            }));
        }

        /// <summary>
        /// Only what is changed is: a custom index given to a run leaves each of its sounds as loud as it was.
        /// </summary>
        [Test]
        public void TestTheRestOfWhatALineSaysIsKept()
        {
            var lines = change(new[] { red(0, 100), green(2000, 40) }, every_half_second, new double[] { 1500, 2000, 2500 }, l => l.WithSampleIndex(3));

            Assert.That(lines.Select(describe), Is.EqualTo(new[]
            {
                "red 0 set1 index0 100%", "green 1500 set1 index3 100%", "green 2000 set1 index3 40%", "green 3000 set1 index0 40%"
            }));
        }

        /// <summary>
        /// A red line is left saying what it did, since the beats are counted from it, and a green line goes in after it.
        /// </summary>
        [Test]
        public void TestARedLineIsLeftAndAGreenLinePutInAfterIt()
        {
            var lines = change(new[] { red(0, 100), red(1500, 100) }, every_half_second, new double[] { 1500 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[]
            {
                "red 0 set1 index0 100%", "red 1500 set1 index0 100%", "green 1500 set1 index0 50%", "green 2000 set1 index0 100%"
            }));
        }

        /// <summary>
        /// Stable reads a line up to a few milliseconds after a sound as that sound's, so a green line there is the one
        /// to change, and one put in before it would not be read.
        /// </summary>
        [Test]
        public void TestALineALittleAfterASoundIsTheOneChanged()
        {
            var lines = change(new[] { red(0, 100), green(1503, 70) }, every_half_second, new double[] { 1500 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[] { "red 0 set1 index0 100%", "green 1503 set1 index0 50%", "green 2000 set1 index0 70%" }));
        }

        [Test]
        public void TestChangingARunAgainChangesTheLinesItHas()
        {
            var once = change(new[] { red(0, 100) }, every_half_second, new double[] { 1500, 2000 }, l => l.WithVolume(50));
            var twice = change(once, every_half_second, new double[] { 1500, 2000 }, l => l.WithVolume(30));

            Assert.That(twice.Select(describe), Is.EqualTo(new[] { "red 0 set1 index0 100%", "green 1500 set1 index0 30%", "green 2500 set1 index0 100%" }));
        }

        /// <summary>
        /// Lines are on whole milliseconds, and the sounds of a slider's repeats are not.
        /// </summary>
        [Test]
        public void TestALineForASoundOffTheMillisecondIsOnTheOneItIsIn()
        {
            var lines = change(new[] { red(0, 100) }, new[] { 956, 1120.835164835165, 1285 }, new[] { 1120.835164835165 }, l => l.WithVolume(50));

            Assert.That(lines.Select(describe), Is.EqualTo(new[] { "red 0 set1 index0 100%", "green 1120 set1 index0 50%", "green 1285 set1 index0 100%" }));
        }

        [Test]
        public void TestNothingChangesWhereTheSoundsAlreadyFollowIt()
        {
            var lines = new[] { red(0, 100), green(2000, 70) };

            Assert.That(change(lines, every_half_second, new double[] { 2000, 2500 }, l => l.WithVolume(70)), Is.EqualTo(lines));
        }

        private static IReadOnlyList<SavedTimingLine> change(IReadOnlyList<SavedTimingLine> lines, double[] times, double[] changed,
                                                             System.Func<SavedTimingLine, SavedTimingLine> by) =>
            LinesForPoints.Change(lines, times.Select(t => (t, changed.Contains(t))), by);

        private static SavedTimingLine red(double time, int volume) => SavedTimingLines.Parse($"{time},500,4,1,0,{volume},1,0")!.Value;

        private static SavedTimingLine green(double time, int volume) => SavedTimingLines.Parse($"{time},-100,4,1,0,{volume},0,0")!.Value;

        private static string describe(SavedTimingLine line) =>
            $"{(line.Uninherited ? "red" : "green")} {line.Time} set{line.SampleSet} index{line.SampleIndex} {line.Volume}%";
    }
}
