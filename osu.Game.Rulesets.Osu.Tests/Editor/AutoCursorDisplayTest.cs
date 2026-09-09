// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    [TestFixture]
    public class AutoCursorDisplayTest
    {
        private static OsuReplayFrame frame(double time, float x, params OsuAction[] actions) =>
            new OsuReplayFrame(time, new Vector2(x, 0), actions);

        private static readonly OsuReplayFrame[] path =
        {
            frame(0, 0),
            frame(1000, 100, OsuAction.LeftButton),
            frame(1050, 100),
            frame(2000, 300),
        };

        [Test]
        public void TestTheCursorTravelsBetweenFramesRatherThanJumpingToTheNearest()
        {
            // frames are only laid down where the movement changes, so most times fall between two of them.
            // 500 is halfway from the frame at 0 to the one at 1000, so halfway from 0 to 100.
            Assert.That(AutoCursorDisplay.PositionAt(path, 500).X, Is.EqualTo(50).Within(0.01f));

            // 1500 is between the frames at 1050 and 2000 -- not the one at 1000, which the button release ends.
            Assert.That(AutoCursorDisplay.PositionAt(path, 1500).X, Is.EqualTo(100 + 200 * (450f / 950f)).Within(0.01f));
        }

        [Test]
        public void TestLandingExactlyOnAFrameGivesThatFrame()
        {
            Assert.That(AutoCursorDisplay.PositionAt(path, 1000).X, Is.EqualTo(100).Within(0.01f));
        }

        [Test]
        public void TestBeforeAndAfterTheBeatmapTheCursorWaitsAtTheEnds()
        {
            Assert.That(AutoCursorDisplay.PositionAt(path, -5000).X, Is.EqualTo(0));
            Assert.That(AutoCursorDisplay.PositionAt(path, 99999).X, Is.EqualTo(300));
        }

        [Test]
        public void TestAButtonReadsAsHeldForAsLongAsTheFrameHoldingItLasts()
        {
            Assert.That(AutoCursorDisplay.HeldAt(path, 999), Is.False);
            Assert.That(AutoCursorDisplay.HeldAt(path, 1020), Is.True);
            Assert.That(AutoCursorDisplay.HeldAt(path, 1060), Is.False);
        }

        [Test]
        public void TestABeatmapWithNothingInItHasNoPathRatherThanThrowing()
        {
            Assert.That(AutoCursorDisplay.GenerateFor(new OsuBeatmap()), Is.Empty);
        }

        [Test]
        public void TestABeatmapOfAnotherRulesetHasNoPathRatherThanThrowing()
        {
            // the generator reads its beatmap as an OsuBeatmap, so anything else has to be turned away before it
            // gets there rather than after.
            var beatmap = new Beatmap { HitObjects = new List<HitObject> { new HitCircle { StartTime = 1000 } } };

            Assert.That(AutoCursorDisplay.GenerateFor(beatmap), Is.Empty);
        }

        [Test]
        public void TestThePathFollowsTheObjectsItWasBuiltFrom()
        {
            var beatmap = osuBeatmapWith(
                new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) },
                new HitCircle { StartTime = 2000, Position = new Vector2(400, 200) });

            var generated = AutoCursorDisplay.GenerateFor(beatmap);

            Assert.That(generated, Is.Not.Empty);

            // the cursor is on each object as it is hit, which is the whole claim the display makes.
            Assert.That(AutoCursorDisplay.PositionAt(generated, 1000), Is.EqualTo(new Vector2(100, 100)));
            Assert.That(AutoCursorDisplay.PositionAt(generated, 2000), Is.EqualTo(new Vector2(400, 200)));
        }

        [Test]
        public void TestFramesComeBackInTimeOrder()
        {
            var beatmap = osuBeatmapWith(Enumerable.Range(0, 50)
                                                   .Select(i => new HitCircle { StartTime = 1000 + i * 200, Position = new Vector2(50 + i * 5, 100) })
                                                   .ToArray());

            var generated = AutoCursorDisplay.GenerateFor(beatmap);

            // the search for the surrounding frames is a binary one, so out-of-order frames would not fail loudly:
            // the cursor would quietly sit in the wrong place.
            Assert.That(generated.Select(f => f.Time), Is.Ordered);
        }

        private static OsuBeatmap osuBeatmapWith(params OsuHitObject[] objects)
        {
            var beatmap = new OsuBeatmap { HitObjects = objects.ToList() };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }
    }
}
