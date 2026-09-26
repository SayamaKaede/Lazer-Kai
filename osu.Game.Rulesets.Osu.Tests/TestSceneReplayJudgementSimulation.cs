// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Scoring;
using osu.Game.Scoring;
using osu.Game.Screens.Play;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Osu.Tests
{
    /// <summary>
    /// Every judgement a replay earns, worked out before it is watched, so that its misses can be listed and gone to.
    /// </summary>
    public partial class TestSceneReplayJudgementSimulation : OsuTestScene
    {
        private IBeatmap beatmap = null!;
        private ReplayJudgementSimulation simulation = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("make a beatmap", () =>
                beatmap = CreateWorkingBeatmap(new OsuRuleset().RulesetInfo).GetPlayableBeatmap(new OsuRuleset().RulesetInfo, new Mod[] { new OsuModAutoplay() }));
        }

        [Test]
        public void TestAutoplayHitsEverything()
        {
            simulate(() => autoplay());

            AddAssert("every object was judged", () => topLevelResults().Length, () => Is.EqualTo(beatmap.HitObjects.Count));
            AddAssert("none was missed", () => simulation.Results.Where(r => r.Type.AffectsCombo() && !r.IsHit), () => Is.Empty);
            AddAssert("nothing is listed", () => ReplayMiss.From(beatmap, simulation.Results), () => Is.Empty);
        }

        /// <summary>
        /// A circle the replay does not click is a miss, and only that one.
        /// </summary>
        [Test]
        public void TestACircleNotClickedIsMissed()
        {
            HitCircle circle = null!;

            AddStep("pick a circle well into the beatmap", () => circle = beatmap.HitObjects.OfType<HitCircle>().Skip(5).First());

            simulate(() =>
            {
                var score = autoplay();

                foreach (var frame in score.Replay.Frames.Cast<OsuReplayFrame>().Where(f => f.Time >= circle.StartTime - 200 && f.Time <= circle.StartTime + 200))
                    frame.Actions.Clear();

                return score;
            });

            AddAssert("that circle was missed", () => simulation.Results.Single(r => r.HitObject == circle).Type, () => Is.EqualTo(HitResult.Miss));
            AddAssert("every other object was hit", () => topLevelResults().Where(r => r.HitObject != circle && !r.IsHit), () => Is.Empty);

            AddAssert("it is listed as a miss, and alone", () => ReplayMiss.From(beatmap, simulation.Results),
                () => Is.EqualTo(new[] { new ReplayMiss(circle, circle.StartTime, ReplayMissKind.Miss, null) }));
        }

        /// <summary>
        /// A circle clicked too late for a 300 is listed as a 100, with how late it was.
        /// </summary>
        [Test]
        public void TestACircleClickedLateIsListedWithHowLate()
        {
            HitCircle circle = null!;
            double late = 0;

            AddStep("pick a circle well into the beatmap", () =>
            {
                circle = beatmap.HitObjects.OfType<HitCircle>().Skip(5).First();
                late = circle.HitWindows.WindowFor(HitResult.Great) + 10;
            });

            simulate(() =>
            {
                var score = autoplay();
                var frames = score.Replay.Frames.Cast<OsuReplayFrame>().ToList();

                foreach (var frame in frames.Where(f => f.Time >= circle.StartTime - 200 && f.Time <= circle.StartTime + late))
                    frame.Actions.Clear();

                score.Replay.Frames.Insert(frames.FindIndex(f => f.Time > circle.StartTime + late),
                    new OsuReplayFrame(circle.StartTime + late, circle.StackedPosition, OsuAction.LeftButton));

                return score;
            });

            AddAssert("it is listed as a 100, late by as much", () => ReplayMiss.From(beatmap, simulation.Results),
                () => Is.EqualTo(new[] { new ReplayMiss(circle, circle.StartTime, ReplayMissKind.Ok, late) }));
        }

        /// <summary>
        /// A slider let go of at a tick breaks the combo there, and is listed at the tick rather than where it starts.
        /// </summary>
        [Test]
        public void TestASliderLetGoOfAtATickIsABreak()
        {
            Slider slider = null!;
            SliderTick tick = null!;

            AddStep("pick a slider with a tick", () =>
            {
                slider = beatmap.HitObjects.OfType<Slider>().First(s => s.NestedHitObjects.OfType<SliderTick>().Any());
                tick = slider.NestedHitObjects.OfType<SliderTick>().First();
            });

            simulate(() =>
            {
                var score = autoplay();

                foreach (var frame in score.Replay.Frames.Cast<OsuReplayFrame>().Where(f => f.Time >= tick.StartTime - 30 && f.Time <= tick.StartTime + 30))
                    frame.Actions.Clear();

                return score;
            });

            AddAssert("the tick was missed", () => simulation.Results.Single(r => r.HitObject == tick).Type, () => Is.EqualTo(HitResult.LargeTickMiss));
            AddAssert("it is listed as a break at the tick, and alone", () => ReplayMiss.From(beatmap, simulation.Results).Select(m => (m.HitObject, m.Time, m.Kind)),
                () => Is.EqualTo(new[] { ((HitObject)slider, tick.StartTime, ReplayMissKind.Break) }));
            AddAssert("with how early or late its head was hit", () => ReplayMiss.From(beatmap, simulation.Results).Single().Offset, () => Is.Not.Null);
        }

        private void simulate(System.Func<Score> score)
        {
            AddStep("simulate", () => Child = simulation = new ReplayJudgementSimulation(new OsuRuleset(), beatmap, System.Array.Empty<Mod>(), score()));
            AddUntilStep("finished", () => simulation.Finished);
            AddStep("say how long it took", () => System.Console.WriteLine($"simulated {beatmap.HitObjects.Last().GetEndTime():N0} ms of play in {simulation.ElapsedMilliseconds:N0} ms"));
        }

        private Score autoplay() => new OsuModAutoplay().CreateScoreFromReplayData(beatmap, new Mod[] { new OsuModAutoplay() });

        private Rulesets.Judgements.JudgementResult[] topLevelResults() => simulation.Results.Where(r => beatmap.HitObjects.Contains(r.HitObject)).ToArray();
    }
}
