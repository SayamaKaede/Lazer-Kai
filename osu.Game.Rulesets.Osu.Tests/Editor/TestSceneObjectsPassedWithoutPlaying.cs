// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Objects the playhead has passed without playing over them -- the editor opening past them, or the playhead jumping
    /// past them -- are drawn the way objects it played over are.
    /// </summary>
    /// <remarks>
    /// The timeline keeps the objects around the playhead on the playfield so it can show them, further back than an
    /// osu! object's own lifetime, which ends when it can no longer be missed. An object between the two was kept but
    /// never updated, so never judged: straight after opening, scrolling back over it showed it as if it were still
    /// to come -- fully drawn with no fade, and its approach circle missing. Measured in the client on 2026-09-21:
    /// nothing before the playhead judged for the first seconds after opening, and the first judgements only as those
    /// objects dropped off the timeline's range.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneObjectsPassedWithoutPlaying : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private OsuPlayfield playfield = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);

            // two circles more than a miss window (400 ms) before where the editor opens, which is where an object's
            // own lifetime ends and only the timeline's range keeps it.
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) });
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1500, Position = new Vector2(200, 100) });

            // one closer, too far back to be hit any more but not so far that its lifetime is over...
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1750, Position = new Vector2(250, 200) });

            // ...and one just before it, near enough that it could still be hit.
            beatmap.HitObjects.Add(new HitCircle { StartTime = 1900, Position = new Vector2(300, 100) });

            beatmap.BeatmapInfo.EditorTimestamp = 2000;

            return beatmap;
        }

        protected override void LoadEditor()
        {
            // with hit markers on, a passed object is faded by its selection blueprint whether it was judged or not,
            // which hides all of this. Off, it is judged objects alone that fade -- and that is how it was reported.
            config.SetValue(OsuSetting.EditorShowHitMarkers, false);
            base.LoadEditor();
        }

        public override void SetUpSteps()
        {
            base.SetUpSteps();
            AddStep("get playfield", () => playfield = Editor.ChildrenOfType<OsuPlayfield>().First());
        }

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("back to the stock answer", () => config.SetValue(OsuSetting.EditorShowHitMarkers, true));
        }

        [Test]
        public void TestScrollingBackStraightAfterOpening()
        {
            AddAssert("the editor opened where it was left", () => EditorClock.CurrentTime, () => Is.EqualTo(2000));
            AddWaitStep("let it settle", 5);

            AddAssert("the circle's own lifetime ended before the playhead", () =>
            {
                var circle = circleAt(1500)!.HitObject;
                return circle.GetEndTime() + circle.HitWindows.WindowFor(HitResult.Miss);
            }, () => Is.LessThan(2000));

            scrollBackOverThePassedCircle();
        }

        /// <summary>
        /// Straight after opening, a circle just before the playhead is part way through being hit, as it is when the
        /// playhead plays up to where the editor opens -- not drawn whole, as if it were still to be hit.
        /// </summary>
        /// <remarks>
        /// It is still within its hit window, so nothing misses it either: left alone, it waits to be hit by a key that
        /// was pressed before the editor was opened.
        /// </remarks>
        [Test]
        public void TestTheCircleJustBeforeWhereItOpens()
        {
            AddAssert("the editor opened where it was left", () => EditorClock.CurrentTime, () => Is.EqualTo(2000));
            AddWaitStep("let it settle", 5);

            AddAssert("the circle could still be hit", () => circleAt(1900)!.HitObject.HitWindows.CanBeHit(2000 - 1900));
            AddAssert("it has been hit", () => circleAt(1900)?.IsHit, () => Is.True);
        }

        /// <summary>
        /// Straight after opening, a circle too far back to be hit any more has been hit, and has finished being hit,
        /// as it has when the playhead plays up to where the editor opens -- not missed, and drawn whole.
        /// </summary>
        /// <remarks>
        /// Missed, it would fade from the moment it was missed, which is the moment the editor opened; with the clock
        /// standing there, that fade never got going.
        /// </remarks>
        [Test]
        public void TestTheCircleTooFarBackToBeHitWhereItOpens()
        {
            AddAssert("the editor opened where it was left", () => EditorClock.CurrentTime, () => Is.EqualTo(2000));
            AddWaitStep("let it settle", 5);

            AddAssert("the circle could no longer be hit", () => circleAt(1750)!.HitObject.HitWindows.CanBeHit(2000 - 1750), () => Is.False);
            AddAssert("it has been hit", () => circleAt(1750)?.IsHit, () => Is.True);
        }

        /// <summary>
        /// The same, reached by playing over the circles rather than opening past them: what it should look like.
        /// </summary>
        [Test]
        public void TestScrollingBackAfterPlayingThrough()
        {
            AddStep("go back before the circles", () => EditorClock.Seek(500));
            AddWaitStep("let it settle", 5);

            // a frame at a time, the way playback moves, so autoplay hits the circles on its way through.
            for (double time = 516; time <= 2000; time += 16)
            {
                double target = time;
                AddStep($"play to {target}", () => EditorClock.Seek(target));
            }

            AddStep("play to 2000", () => EditorClock.Seek(2000));
            AddWaitStep("let it settle", 5);

            AddAssert("the circle just before the playhead has been hit", () => circleAt(1900)?.IsHit, () => Is.True);
            AddAssert("so has the one before it", () => circleAt(1750)?.IsHit, () => Is.True);

            scrollBackOverThePassedCircle();
        }

        private void scrollBackOverThePassedCircle()
        {
            // slowly, a step a frame, the way the wheel does it.
            foreach (double time in new[] { 1950.0, 1900, 1850, 1800, 1750, 1700, 1650, 1600, 1550 })
            {
                double target = time;
                AddStep($"scroll back to {target}", () => EditorClock.Seek(target));
                AddWaitStep("a frame", 1);
            }

            AddAssert("just after the circle, it has been hit", () => circleAt(1500)?.IsHit, () => Is.True);

            foreach (double time in new[] { 1500.0, 1450 })
            {
                double target = time;
                AddStep($"scroll back to {target}", () => EditorClock.Seek(target));
                AddWaitStep("a frame", 1);
            }

            AddAssert("just before it, it is still to come", () => circleAt(1500)?.Judged, () => Is.False);
            AddAssert("with its approach circle", () => circleAt(1500)?.ApproachCircle.Alpha, () => Is.GreaterThan(0));
        }

        private DrawableHitCircle? circleAt(double time) =>
            playfield.ChildrenOfType<DrawableHitCircle>().SingleOrDefault(d => d.HitObject.StartTime == time);
    }
}
