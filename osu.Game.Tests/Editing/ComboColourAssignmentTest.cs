// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Screens.Edit.ComboColours;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class ComboColourAssignmentTest
    {
        private const int colours = 4;

        /// <summary>
        /// Walks the skips back into the colour each combo actually ends up with, which is what a mapper sees.
        /// </summary>
        private static int[] coloursFrom(int[] skips)
        {
            int colour = 0;

            return skips.Select(skip => colour = (colour + skip + 1) % colours).ToArray();
        }

        private static ComboColourAssignment.Combo[] combosOfLength(int count, int length = 4)
            => Enumerable.Range(0, count).Select(i => new ComboColourAssignment.Combo(1000 * i, length)).ToArray();

        [Test]
        public void TestNoPointsLeavesEverythingAlone()
        {
            int[] skips = ComboColourAssignment.Calculate(combosOfLength(5), new ComboColourProject(), colours);

            Assert.That(skips, Is.All.Zero);
        }

        [Test]
        public void TestNormalPointWalksItsSequence()
        {
            var project = new ComboColourProject();
            project.Points.Add(point(0, ComboColourPointMode.Normal, 2, 3));

            int[] skips = ComboColourAssignment.Calculate(combosOfLength(6), project, colours);

            // the sequence is walked one colour per combo and repeats.
            Assert.That(coloursFrom(skips), Is.EqualTo(new[] { 2, 3, 2, 3, 2, 3 }));
        }

        [Test]
        public void TestSecondPointTakesOverFromItsTime()
        {
            var project = new ComboColourProject();
            project.Points.Add(point(0, ComboColourPointMode.Normal, 0, 1));
            project.Points.Add(point(3500, ComboColourPointMode.Normal, 2, 3));

            int[] skips = ComboColourAssignment.Calculate(combosOfLength(6), project, colours);

            Assert.That(coloursFrom(skips), Is.EqualTo(new[] { 0, 1, 0, 1, 2, 3 }));
        }

        /// <summary>
        /// A burst takes the one combo sitting on it and is then spent, so the following combos go back to the
        /// ordinary point behind it.
        /// </summary>
        [Test]
        public void TestBurstTakesOneComboThenGivesItBack()
        {
            var project = new ComboColourProject { MaxBurstLength = 1 };
            project.Points.Add(point(0, ComboColourPointMode.Normal, 0, 1));
            project.Points.Add(point(2000, ComboColourPointMode.Burst, 3));

            var combos = combosOfLength(5, length: 1);

            int[] colourPerCombo = coloursFrom(ComboColourAssignment.Calculate(combos, project, colours));

            Assert.That(colourPerCombo[2], Is.EqualTo(3), "the combo the burst sits on takes the burst colour");
            Assert.That(colourPerCombo.Where((_, i) => i != 2), Is.All.Not.EqualTo(3), "no other combo does");
        }

        /// <summary>
        /// The point of the length limit: a burst is for a quick flash, so it passes over a long combo.
        /// </summary>
        [Test]
        public void TestBurstSkipsAComboLongerThanTheLimit()
        {
            var project = new ComboColourProject { MaxBurstLength = 1 };
            project.Points.Add(point(0, ComboColourPointMode.Normal, 0, 1));
            project.Points.Add(point(2000, ComboColourPointMode.Burst, 3));

            var combos = combosOfLength(5, length: 8);

            Assert.That(coloursFrom(ComboColourAssignment.Calculate(combos, project, colours)), Is.All.Not.EqualTo(3));
        }

        [Test]
        public void TestSkipsAreNeverNegative()
        {
            var project = new ComboColourProject();
            project.Points.Add(point(0, ComboColourPointMode.Normal, 3, 0));

            int[] skips = ComboColourAssignment.Calculate(combosOfLength(6), project, colours);

            Assert.That(skips, Is.All.GreaterThanOrEqualTo(0));
            Assert.That(skips, Is.All.LessThan(colours));
        }

        [Test]
        public void TestColourOutsideThePaletteIsRefused()
        {
            var project = new ComboColourProject();
            project.Points.Add(point(0, ComboColourPointMode.Normal, 9));

            Assert.That(() => ComboColourAssignment.Calculate(combosOfLength(2), project, colours),
                Throws.InvalidOperationException);
        }

        private static ComboColourPoint point(double time, ComboColourPointMode mode, params int[] sequence)
        {
            var p = new ComboColourPoint { Time = time, Mode = mode };
            p.Colours.AddRange(sequence);
            return p;
        }
    }
}
