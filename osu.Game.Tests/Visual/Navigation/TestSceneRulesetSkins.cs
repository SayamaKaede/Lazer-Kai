// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Overlays.OSD;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Skinning;
using osuTK.Input;

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
        /// A skin picked while a ruleset's own is on — in the skin section, with the skin shortcuts, anywhere — replaces
        /// that ruleset's skin. Written into the skin setting instead, it went on for the moment and then stayed there,
        /// with the ruleset's own skin still set and doing nothing, until that was cleared and set again (reported
        /// 2026-09-11).
        /// </summary>
        [Test]
        public void TestPickingOneWhileARulesetsOwnIsOnChangesThatOne()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("pick classic", () => skins.SetSkinFromConfiguration(SkinInfo.CLASSIC_SKIN.ToString()));
            AddAssert("taiko's skin is classic now", () => Game.LocalConfig.Get<string>(OsuSetting.RulesetSkinTaiko), () => Is.EqualTo(SkinInfo.CLASSIC_SKIN.ToString()));
            AddAssert("the skin setting still has argon", skinSetting, () => Is.EqualTo(SkinInfo.ARGON_SKIN.ToString()));

            AddStep("switch to mania, which has none", () => Game.Ruleset.Value = rulesets.GetRuleset(3)!);
            AddUntilStep("argon is on", currentSkin, () => Is.EqualTo(SkinInfo.ARGON_SKIN));

            AddStep("back to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("classic is on", currentSkin, () => Is.EqualTo(SkinInfo.CLASSIC_SKIN));
        }

        /// <summary>
        /// The shortcut the report was about, pressed the way somebody presses it.
        /// </summary>
        [Test]
        public void TestTheNextSkinShortcutChangesARulesetsOwnSkin()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("press the next skin shortcut", pressNextSkin);
            AddUntilStep("the skin moved on", currentSkin, () => Is.Not.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddAssert("taiko's skin went with it", () => Game.LocalConfig.Get<string>(OsuSetting.RulesetSkinTaiko), () => Is.EqualTo(currentSkin().ToString()));
            AddAssert("the skin setting still has argon", skinSetting, () => Is.EqualTo(SkinInfo.ARGON_SKIN.ToString()));
        }

        /// <summary>
        /// A ruleset with no skin of its own takes the shortcut into the skin setting, as it always did.
        /// </summary>
        [Test]
        public void TestTheNextSkinShortcutOnARulesetWithoutOneChangesTheSkinSetting()
        {
            AddStep("press the next skin shortcut", pressNextSkin);
            AddUntilStep("the skin moved on", currentSkin, () => Is.Not.EqualTo(SkinInfo.ARGON_SKIN));

            AddAssert("the skin setting went with it", skinSetting, () => Is.EqualTo(currentSkin().ToString()));
            AddAssert("osu! still has none of its own", () => Game.LocalConfig.Get<string>(OsuSetting.RulesetSkinOsu), () => Is.Empty);
        }

        /// <summary>
        /// The skin shortcuts are pressed blind, and the toast is the only way to tell which skin they landed on. A skin
        /// that went into a ruleset's own setting rather than the skin setting was left without one (reported 2026-09-11).
        /// </summary>
        [Test]
        public void TestTheShortcutSaysWhichSkinItLandedOn()
        {
            AddStep("taiko gets triangles", () => Game.LocalConfig.SetValue(OsuSetting.RulesetSkinTaiko, SkinInfo.TRIANGLES_SKIN.ToString()));
            AddStep("switch to taiko", () => Game.Ruleset.Value = rulesets.GetRuleset(1)!);
            AddUntilStep("triangles is on", currentSkin, () => Is.EqualTo(SkinInfo.TRIANGLES_SKIN));

            AddStep("press the next skin shortcut", pressNextSkin);
            AddUntilStep("the skin moved on", currentSkin, () => Is.Not.EqualTo(SkinInfo.TRIANGLES_SKIN));

            // the mode in the heading keeps a toast from the skin setting's own changes, made while setting up, from
            // passing for this one. A toast's heading is drawn in capitals, so it is matched without regard to case.
            AddUntilStep("a toast names it, as taiko's", () => Game.ChildrenOfType<TrackedSettingToast>().Any(toast =>
            {
                string[] texts = toast.ChildrenOfType<SpriteText>().Select(t => t.Text.ToString()).ToArray();
                return texts.Contains(nameOf(currentSkin())) && texts.Any(t => t.Contains(@"osu!taiko", StringComparison.OrdinalIgnoreCase));
            }));
        }

        /// <summary>
        /// The name a toast gives a skin, looked up the way the game looks it up for the skin setting's own toast.
        /// </summary>
        private string nameOf(Guid id) => skins.Query(s => s.ID == id)?.ToString() ?? @"Unknown";

        private void pressNextSkin()
        {
            InputManager.PressKey(Key.ControlLeft);
            InputManager.PressKey(Key.ShiftLeft);
            InputManager.Key(Key.T);
            InputManager.ReleaseKey(Key.ShiftLeft);
            InputManager.ReleaseKey(Key.ControlLeft);
        }

        private Guid currentSkin() => skins.CurrentSkinInfo.Value.ID;

        private string skinSetting() => Game.LocalConfig.Get<string>(OsuSetting.Skin);
    }
}
