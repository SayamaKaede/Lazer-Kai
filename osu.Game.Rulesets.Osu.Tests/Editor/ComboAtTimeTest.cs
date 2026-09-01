// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The combo the status panel shows for wherever the playhead is.
    /// </summary>
    /// <remarks>
    /// A number in the editor that disagrees with the number in gameplay is worse than no number, so what counts
    /// here is taken from the same place the game's own maximum combo is. These check that the two agree.
    /// </remarks>
    [TestFixture]
    public class ComboAtTimeTest
    {
        [Test]
        public void TestNothingHasBeenHitBeforeTheFirstObject()
        {
            var times = ComposeStatusDisplay.ComboTimesFor(beatmap());

            Assert.That(ComposeStatusDisplay.ComboAt(times, 0), Is.Zero);
            Assert.That(ComposeStatusDisplay.ComboAt(times, 999), Is.Zero);
        }

        [Test]
        public void TestACircleCountsAsItIsReached()
        {
            var times = ComposeStatusDisplay.ComboTimesFor(beatmap());

            Assert.That(ComposeStatusDisplay.ComboAt(times, 1000), Is.EqualTo(1), @"the first circle is at 1000");
            Assert.That(ComposeStatusDisplay.ComboAt(times, 1499), Is.EqualTo(1));
            Assert.That(ComposeStatusDisplay.ComboAt(times, 1500), Is.EqualTo(2));
        }

        /// <summary>
        /// The end of the beatmap has to read the same as what the game would call the maximum combo, or the panel
        /// is telling a mapper something the results screen will contradict.
        /// </summary>
        [Test]
        public void TestTheEndAgreesWithTheGamesOwnMaximum()
        {
            var map = beatmap();
            var times = ComposeStatusDisplay.ComboTimesFor(map);

            Assert.That(ComposeStatusDisplay.ComboAt(times, double.MaxValue), Is.EqualTo(map.GetMaxCombo()));
        }

        /// <summary>
        /// A slider is not one step but several — its head, every tick, every reverse and its tail — which is what
        /// a player watching the number climb through one actually sees.
        /// </summary>
        [Test]
        public void TestASliderCountsInPiecesRatherThanAllAtOnce()
        {
            var map = beatmap();
            var times = ComposeStatusDisplay.ComboTimesFor(map);

            var slider = map.HitObjects.OfType<Slider>().Single();

            int atHead = ComposeStatusDisplay.ComboAt(times, slider.StartTime);
            int atTail = ComposeStatusDisplay.ComboAt(times, slider.EndTime);

            Assert.That(atTail, Is.GreaterThan(atHead), @"the combo has to climb across a slider, not jump at one end");
        }

        /// <summary>
        /// It only ever goes up, whichever moment it is asked about.
        /// </summary>
        [Test]
        public void TestItNeverGoesBackwards()
        {
            var times = ComposeStatusDisplay.ComboTimesFor(beatmap());

            int previous = 0;

            for (double t = 0; t < 8000; t += 50)
            {
                int now = ComposeStatusDisplay.ComboAt(times, t);

                Assert.That(now, Is.GreaterThanOrEqualTo(previous), $@"went backwards at {t}");
                previous = now;
            }
        }

        [Test]
        public void TestAnEmptyBeatmapIsZeroThroughout()
        {
            var empty = new OsuBeatmap
            {
                BeatmapInfo = new BeatmapInfo { Ruleset = new OsuRuleset().RulesetInfo, Difficulty = new BeatmapDifficulty() },
            };

            var times = ComposeStatusDisplay.ComboTimesFor(empty);

            Assert.That(times, Is.Empty);
            Assert.That(ComposeStatusDisplay.ComboAt(times, 5000), Is.Zero);
        }

        private static OsuBeatmap beatmap()
        {
            var beatmap = new OsuBeatmap
            {
                BeatmapInfo = new BeatmapInfo
                {
                    Ruleset = new OsuRuleset().RulesetInfo,
                    Difficulty = new BeatmapDifficulty(),
                },
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 500 });

            beatmap.HitObjects.AddRange(new OsuHitObject[]
            {
                new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) },
                new HitCircle { StartTime = 1500, Position = new Vector2(200, 100) },
                new Slider
                {
                    StartTime = 2000,
                    Position = new Vector2(300, 100),
                    Path = new SliderPath(PathType.LINEAR, new[] { Vector2.Zero, new Vector2(200, 0) }),
                    RepeatCount = 1,
                },
                new Spinner { StartTime = 5000, Duration = 1000, Position = new Vector2(256, 192) },
                new HitCircle { StartTime = 7000, Position = new Vector2(400, 200) },
            });

            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }
    }
}
