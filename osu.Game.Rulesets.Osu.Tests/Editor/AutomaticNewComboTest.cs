// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Which objects placed by hand get a combo of their own.
    /// </summary>
    /// <remarks>
    /// The beatmap in these is 120 BPM in four four: a beat is 500ms and a bar is 2000ms, starting at zero.
    /// </remarks>
    [TestFixture]
    public class AutomaticNewComboTest
    {
        /// <summary>
        /// The plain case: the first object of a bar opens it.
        /// </summary>
        [Test]
        public void TestTheFirstObjectOfABarStartsACombo()
        {
            Assert.That(shouldStart(circle(2000)), Is.True);
        }

        /// <summary>
        /// A bar whose downbeat is empty still gets a combo, on whatever does open it.
        /// </summary>
        [Test]
        public void TestTheFirstObjectAfterAnEmptyDownbeatStartsACombo()
        {
            Assert.That(shouldStart(circle(2500)), Is.True);
        }

        /// <summary>
        /// Only the one. Everything else in the bar carries on the combo the bar opened with.
        /// </summary>
        [Test]
        public void TestTheRestOfTheBarDoesNot()
        {
            var opener = circle(2000, newCombo: true);

            Assert.That(shouldStart(circle(2500), opener), Is.False);
            Assert.That(shouldStart(circle(3000), opener), Is.False);
        }

        /// <summary>
        /// A phrase that starts early. What says a note before the bar line opens the next bar rather than closing
        /// this one is that it carries over: the body runs past the line.
        /// </summary>
        [Test]
        public void TestASliderCarryingOverTheBarLineStartsTheNextBarsCombo()
        {
            // half a beat before the bar at 4000, running a beat: it passes the line.
            Assert.That(shouldStart(slider(3750, 500)), Is.True);
        }

        /// <summary>
        /// The same note that stops short of the line does not: nothing about where it sits says it belongs to what
        /// comes after, and it is not what opened the bar it is in.
        /// </summary>
        [Test]
        public void TestOneThatStopsShortOfTheLineDoesNot()
        {
            var opener = circle(2000, newCombo: true);

            Assert.That(shouldStart(circle(3750), opener), Is.False);
            Assert.That(shouldStart(slider(3750, 250), opener), Is.False);
        }

        /// <summary>
        /// A phrase that started early has already taken the bar's combo, so the downbeat behind it does not take it
        /// again -- which would read as two combos a beat apart.
        /// </summary>
        [Test]
        public void TestADownbeatBehindAnEarlyStartDoesNot()
        {
            var early = slider(3750, 500);
            early.NewCombo = true;

            Assert.That(shouldStart(circle(4000), early), Is.False);
        }

        /// <summary>
        /// An object that begins on a line opens that line's bar, however far it runs. Otherwise a slider longer
        /// than a bar would hand its combo to the bar it happens to end in.
        /// </summary>
        [Test]
        public void TestALongObjectBeginningOnALineOpensThatBar()
        {
            Assert.That(shouldStart(slider(2000, 2500)), Is.True);
        }

        /// <summary>
        /// A note added inside a bar that already has objects is not what opens it, whether or not the opener was
        /// given a combo. Putting one here would leave a combo starting in the middle of a bar.
        /// </summary>
        [Test]
        public void TestAnObjectAddedInsideABarDoesNot()
        {
            var opener = circle(2000, newCombo: false);

            Assert.That(shouldStart(circle(3000), opener), Is.False);
        }

        /// <summary>
        /// Every other bar, for a beatmap that reads in two bar phrases.
        /// </summary>
        [Test]
        public void TestTwoBarsApartSkipsTheBarBetween()
        {
            var opener = circle(0, newCombo: true);

            Assert.That(shouldStart(circle(2000), bars: 2, existing: opener), Is.False);
            Assert.That(shouldStart(circle(4000), bars: 2, existing: opener), Is.True);
        }

        /// <summary>
        /// Half a bar, for a beatmap that reads in half bar phrases.
        /// </summary>
        [Test]
        public void TestHalfABarStartsOneInTheMiddleOfTheBar()
        {
            var opener = circle(0, newCombo: true);

            Assert.That(shouldStart(circle(1000), bars: 0.5, existing: opener), Is.True);
        }

        private static bool shouldStart(HitObject placed, params HitObject[] existing)
            => AutomaticNewCombo.ShouldStartCombo(placed, controlPoints(), existing, 1);

        private static bool shouldStart(HitObject placed, double bars, params HitObject[] existing)
            => AutomaticNewCombo.ShouldStartCombo(placed, controlPoints(), existing, bars);

        private static HitObject circle(double time, bool newCombo = false) => new HitCircle { StartTime = time, NewCombo = newCombo };

        /// <remarks>
        /// Built with the duration asked for rather than from a path and a velocity: what is being checked is
        /// whether the object carries over the line, and how long it runs for is the whole of that.
        /// </remarks>
        private static Slider slider(double time, double duration)
        {
            var s = new Slider { StartTime = time };

            s.Path.ControlPoints.Add(new PathControlPoint());
            s.Path.ControlPoints.Add(new PathControlPoint(new osuTK.Vector2(100, 0)));

            s.ApplyDefaults(controlPoints(), new osu.Game.Beatmaps.BeatmapDifficulty());
            s.Path.ExpectedDistance.Value = duration * s.Velocity;
            s.ApplyDefaults(controlPoints(), new osu.Game.Beatmaps.BeatmapDifficulty());

            return s;
        }

        private static ControlPointInfo controlPoints()
        {
            var info = new ControlPointInfo();
            info.Add(0, new TimingControlPoint { BeatLength = 500 });
            return info;
        }
    }
}
