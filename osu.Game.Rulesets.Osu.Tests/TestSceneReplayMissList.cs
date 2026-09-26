// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Play.PlayerSettings;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests
{
    /// <summary>
    /// What a replay missed is listed beside it as it is watched, and each can be gone back to.
    /// </summary>
    public partial class TestSceneReplayMissList : RateAdjustedBeatmapTestScene
    {
        private TestReplayPlayer player = null!;
        private double missedAt;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create player with a circle not clicked", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new OsuRuleset().RulesetInfo);
                SelectedMods.Value = System.Array.Empty<Mod>();

                var beatmap = Beatmap.Value.GetPlayableBeatmap(new OsuRuleset().RulesetInfo);
                var score = new OsuModAutoplay().CreateScoreFromReplayData(beatmap, new Mod[] { new OsuModAutoplay() });
                var circle = beatmap.HitObjects.OfType<HitCircle>().Skip(5).First();

                foreach (var frame in score.Replay.Frames.Cast<OsuReplayFrame>().Where(f => f.Time >= circle.StartTime - 200 && f.Time <= circle.StartTime + 200))
                    frame.Actions.Clear();

                missedAt = circle.StartTime;
                player = new TestReplayPlayer(score, showResults: false);
                player.Configuration.JudgeReplayAhead = true;
            });

            AddStep("load player", () => LoadScreen(player));
            AddUntilStep("player loaded", () => player.IsLoaded);
        }

        [Test]
        public void TestTheMissIsListed()
        {
            AddUntilStep("the replay has been judged", () => simulation()?.Misses != null);
            AddAssert("the circle is the only miss", () => simulation()!.Misses!.Select(m => (m.Time, m.Kind)), () => Is.EqualTo(new[] { (missedAt, ReplayMissKind.Miss) }));

            // the list is only made once it is seen.
            bringOutTheSettings();
            AddUntilStep("with a row for it", () => rows().Length, () => Is.EqualTo(1));
        }

        /// <summary>
        /// The settings are left out of the way until they are wanted, and the replay is judged all the same.
        /// </summary>
        [Test]
        public void TestTheMissIsListedWhenTheSettingsComeOutLater()
        {
            AddUntilStep("the replay has been judged", () => simulation()?.Misses != null);
            AddAssert("no rows made yet", () => rows().Length, () => Is.Zero);

            bringOutTheSettings();
            AddUntilStep("a row for the miss", () => rows().Select(r => r.Miss.Time), () => Is.EqualTo(new[] { missedAt }));
        }

        [Test]
        public void TestClickingAMissPlaysFromALittleBefore()
        {
            bringOutTheSettings();
            AddUntilStep("the miss is listed", () => rows().Length, () => Is.EqualTo(1));

            AddStep("pause", () => player.GameplayClockContainer.Stop());
            double timeGoneTo = 0;

            // read at once: the clock carries on from there straight away.
            AddStep("click the miss", () =>
            {
                rows().Single().TriggerClick();
                timeGoneTo = player.GameplayClockContainer.CurrentTime;
            });

            AddAssert("gone to a second before the miss", () => timeGoneTo, () => Is.EqualTo(missedAt - ReplayPlayer.MISS_LEAD_IN).Within(1));
            AddAssert("playing", () => player.GameplayClockContainer.IsPaused.Value, () => Is.False);
        }

        /// <summary>
        /// The timeline along the bottom comes and goes with its key, and has the miss marked where it is.
        /// </summary>
        [Test]
        public void TestTheTimelineComesAndGoesWithItsKey()
        {
            AddAssert("no timeline to start with", () => timeline().Alpha, () => Is.Zero);

            AddStep("press T", () => InputManager.Key(Key.T));
            AddUntilStep("the timeline is shown", () => timeline().Alpha, () => Is.EqualTo(1));
            AddUntilStep("with a mark for the miss", () => marks().Select(m => m.Miss.Time), () => Is.EqualTo(new[] { missedAt }));

            // well away from the miss, for going to it to be seen.
            AddStep("pause far past the miss", () =>
            {
                player.GameplayClockContainer.Stop();
                player.Seek(missedAt + 30000);
            });

            AddStep("click the mark", () =>
            {
                InputManager.MoveMouseTo(marks().Single());
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("playing from a second before the miss", () => player.GameplayClockContainer.CurrentTime,
                () => Is.InRange(missedAt - ReplayPlayer.MISS_LEAD_IN, missedAt - ReplayPlayer.MISS_LEAD_IN + 500));
            AddAssert("playing", () => player.GameplayClockContainer.IsPaused.Value, () => Is.False);

            AddStep("press T", () => InputManager.Key(Key.T));
            AddUntilStep("the timeline is gone", () => timeline().Alpha, () => Is.Zero);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("timeline back to hidden", () => config.SetValue(OsuSetting.ReplayShowMissTimeline, false));
        }

        private ReplayMissTimeline timeline() => player.ChildrenOfType<ReplayMissTimeline>().Single();

        private ReplayMissTimeline.Marker[] marks() => timeline().ChildrenOfType<ReplayMissTimeline.Marker>().ToArray();

        /// <summary>
        /// The settings come out with the mouse at the right edge, and go back once it leaves.
        /// </summary>
        private void bringOutTheSettings()
        {
            // from elsewhere first, for it to be moved to the edge even where it was left there.
            AddStep("move the mouse to the middle", () => InputManager.MoveMouseTo(player.ScreenSpaceDrawQuad.Centre));
            AddStep("move the mouse to the right edge", () => InputManager.MoveMouseTo(player.ScreenSpaceDrawQuad.TopRight + new Vector2(-5, 300)));
            AddUntilStep("the settings are out", () => player.ReplayOverlay.Settings.Expanded.Value);
        }

        private ReplayJudgementSimulation? simulation() => player.ChildrenOfType<ReplayJudgementSimulation>().SingleOrDefault();

        private ReplayMissList.Row[] rows() => player.ChildrenOfType<ReplayMissList.Row>().ToArray();
    }
}
