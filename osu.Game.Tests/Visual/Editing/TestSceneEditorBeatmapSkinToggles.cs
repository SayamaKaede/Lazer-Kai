// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Turning the beatmap's own colours and hitsounds off while editing, which the editor otherwise forces on.
    /// </summary>
    public partial class TestSceneEditorBeatmapSkinToggles : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [TearDownSteps]
        public void RestoreSettings()
        {
            AddStep("put both back on", () =>
            {
                config.SetValue(OsuSetting.EditorBeatmapColours, true);
                config.SetValue(OsuSetting.EditorBeatmapHitsounds, true);
            });
        }

        /// <summary>
        /// Both on to begin with: somebody who has never opened these settings must see the beatmap they opened.
        /// </summary>
        [Test]
        public void TestBothAreOnByDefault()
        {
            AddAssert("colours are the beatmap's", () => provider().BeatmapColours.Value, () => Is.True);
            AddAssert("hitsounds are the beatmap's", () => provider().BeatmapHitsounds.Value, () => Is.True);
        }

        [Test]
        public void TestColoursFollowTheSetting()
        {
            AddStep("turn beatmap colours off", () => config.SetValue(OsuSetting.EditorBeatmapColours, false));
            AddUntilStep("the editor stops using them", () => provider().BeatmapColours.Value, () => Is.False);

            AddAssert("hitsounds are left alone", () => provider().BeatmapHitsounds.Value, () => Is.True);

            AddStep("turn them back on", () => config.SetValue(OsuSetting.EditorBeatmapColours, true));
            AddUntilStep("the editor uses them again", () => provider().BeatmapColours.Value, () => Is.True);
        }

        [Test]
        public void TestHitsoundsFollowTheSetting()
        {
            AddStep("turn beatmap hitsounds off", () => config.SetValue(OsuSetting.EditorBeatmapHitsounds, false));
            AddUntilStep("the editor stops using them", () => provider().BeatmapHitsounds.Value, () => Is.False);

            AddAssert("colours are left alone", () => provider().BeatmapColours.Value, () => Is.True);
        }

        /// <summary>
        /// The editor's own settings, not the ones used for playing: a mapper turning the beatmap's hitsounds
        /// off to hear what they are writing has said nothing about how the beatmap should play.
        /// </summary>
        [Test]
        public void TestTheGameplaySettingsAreNotTouched()
        {
            AddStep("turn both off for the editor", () =>
            {
                config.SetValue(OsuSetting.EditorBeatmapColours, false);
                config.SetValue(OsuSetting.EditorBeatmapHitsounds, false);
            });

            AddAssert("gameplay colours are untouched", () => config.Get<bool>(OsuSetting.BeatmapColours), () => Is.True);
            AddAssert("gameplay hitsounds are untouched", () => config.Get<bool>(OsuSetting.BeatmapHitsounds), () => Is.True);
        }

        private EditorSkinProvidingContainer provider() => Editor.ChildrenOfType<EditorSkinProvidingContainer>().First();
    }
}
