// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Skinning;

namespace osu.Game.Tests.Visual.Navigation
{
    /// <summary>
    /// A skin chosen for a ruleset goes on with that ruleset and comes off with it, and never takes the skin setting's place.
    /// </summary>
    public partial class TestSceneRulesetSkins : OsuGameTestScene
    {
        private SkinManager skins => Game.Dependencies.Get<SkinManager>();

        private RulesetStore rulesets => Game.Dependencies.Get<RulesetStore>();

        [SetUpSteps]
        public void SetUpRulesetSkins()
        {
            AddStep("osu! selected, argon in the skin setting, no ruleset skins", () =>
            {
                foreach (var setting in RulesetSkinSwitcher.SETTINGS.Values)
                    Game.LocalConfig.SetValue(setting, string.Empty);

                Game.Ruleset.Value = rulesets.GetRuleset(0)!;
                skins.SetSkinFromConfiguration(SkinInfo.ARGON_SKIN.ToString());
            });

            AddUntilStep("argon is on", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));
        }

        [Test]
        public void TestARulesetsSkinComesAndGoesWithIt()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddAssert("nothing changes on osu!", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));

            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));
            AddAssert("the skin setting still has argon", skinSetting, () => Is.EqualTo(SkinInfo.ARGON_SKIN.ToString()));

            AddStep("back to osu!", () => Game.Ruleset.Value = rulesets.GetRuleset(0)!);
            AddUntilStep("argon is back", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));
        }

        [Test]
        public void TestARulesetWithoutOneGetsTheSkinSettings()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("switch to mania, which has none", () => Game.Ruleset.Value = rulesets.GetRuleset(3)!);
            AddUntilStep("argon is on", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));
        }

        /// <summary>
        /// A skin deleted since it was chosen counts as none. Handed on regardless, the skin manager would put triangles
        /// on in its place — a skin nobody chose.
        /// </summary>
        [Test]
        public void TestASkinThatIsGoneCountsAsNone()
        {
            AddStep("taiko gets a skin that does not exist", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, Guid.NewGuid().ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);

            AddWaitStep("give it a moment", 3);
            AddAssert("argon is still on", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));
        }

        [Test]
        public void TestChoosingOneForTheSelectedRulesetPutsItOnAtOnce()
        {
            AddStep("osu! gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinOsu, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("osu! set back to none", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinOsu, string.Empty));
            AddUntilStep("argon is back", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));
        }

        /// <summary>
        /// A skin picked by hand while a ruleset's own is on becomes the skin setting's, and so the skin for every
        /// ruleset without one of its own.
        /// </summary>
        [Test]
        public void TestPickingOneByHandStillChangesTheSkinSetting()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("pick classic by hand", () => skins.SetSkinFromConfiguration(SkinInfo.CLASSIC_SKIN.ToString()));
            AddAssert("the skin setting has classic", skinSetting, () => Is.EqualTo(SkinInfo.CLASSIC_SKIN.ToString()));

            AddStep("switch to mania, which has none", () => Game.Ruleset.Value = rulesets.GetRuleset(3)!);
            AddUntilStep("classic is on", currentSkin, () => Is.EqualTo(SkinInfo.CLASSIC_SKIN));
        }

        private Guid currentSkin() => skins.CurrentSkinInfo.Value.ID;

        private string skinSetting() => Game.LocalConfig.Get<string>(OsuSetting.Skin);
    }
}
