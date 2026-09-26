// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Configuration;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Osu.Tests
{
    /// <summary>
    /// A replay played with hidden and flashlight can be watched without what they hide, to see what was there to be
    /// hit, from the analysis settings.
    /// </summary>
    public partial class TestSceneReplayWithoutModEffects : RateAdjustedBeatmapTestScene
    {
        private TestReplayPlayer player = null!;

        [Resolved]
        private IRulesetConfigCache configCache { get; set; } = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("create player", () =>
            {
                Beatmap.Value = CreateWorkingBeatmap(new OsuRuleset().RulesetInfo);
                SelectedMods.Value = new Mod[] { new OsuModAutoplay(), new OsuModHidden(), new OsuModFlashlight() };
                player = new TestReplayPlayer(false);
            });

            AddStep("load player", () => LoadScreen(player));
            AddUntilStep("player loaded", () => player.IsLoaded);
        }

        [Test]
        public void TestHiddensEffectCanBeHidden()
        {
            AddUntilStep("approach circles are hidden", () => upcomingCircles().Any() && upcomingCircles().All(c => c.ApproachCircle.Alpha == 0));

            AddStep("hide hidden's effect", () => settings().HideHiddenEffect.Value = true);
            AddUntilStep("approach circles show", () => upcomingCircles().Any() && upcomingCircles().All(c => c.ApproachCircle.Alpha > 0));

            AddStep("show hidden's effect again", () => settings().HideHiddenEffect.Value = false);
            AddUntilStep("approach circles are hidden again", () => upcomingCircles().Any() && upcomingCircles().All(c => c.ApproachCircle.Alpha == 0));
        }

        [Test]
        public void TestFlashlightsEffectCanBeHidden()
        {
            AddUntilStep("the dark is there", () => flashlight().IsPresent);

            AddStep("hide flashlight's effect", () => settings().HideFlashlightEffect.Value = true);
            AddUntilStep("the dark is gone", () => !flashlight().IsPresent);

            AddStep("show flashlight's effect again", () => settings().HideFlashlightEffect.Value = false);
            AddUntilStep("the dark is back", () => flashlight().IsPresent);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            // from the configuration rather than the settings on screen, which a test that stopped early may not have.
            AddStep("settings back", () =>
            {
                var config = (OsuRulesetConfigManager)configCache.GetConfigFor(new OsuRuleset())!;
                config.SetValue(OsuRulesetSetting.ReplayHideHiddenEffect, false);
                config.SetValue(OsuRulesetSetting.ReplayHideFlashlightEffect, false);
            });
        }

        private ReplayAnalysisSettings settings() => player.ChildrenOfType<ReplayAnalysisSettings>().Single();

        private ModFlashlight<OsuHitObject>.Flashlight flashlight() => player.DrawableRuleset.Overlays.ChildrenOfType<ModFlashlight<OsuHitObject>.Flashlight>().Single();

        /// <summary>
        /// Circles on screen and not yet due, past the first object, which may be left showing for the player to find
        /// their way in.
        /// </summary>
        private DrawableHitCircle[] upcomingCircles()
        {
            double now = player.GameplayClockContainer.CurrentTime;
            var first = player.DrawableRuleset.Objects.First();

            return player.DrawableRuleset.Playfield.AllHitObjects
                         .OfType<DrawableHitCircle>()
                         .Where(c => c.HitObject != first && c.HitObject.StartTime - c.HitObject.TimePreempt < now && c.HitObject.StartTime > now + 100)
                         .ToArray();
        }
    }
}
