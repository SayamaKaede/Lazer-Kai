// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Graphics.UserInterface;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Localisation;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The hitsound and music switches in the editor's menu bar.
    /// </summary>
    public partial class TestSceneEditorMuteToggles : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Test]
        public void TestHitsoundSwitchSilencesSamples()
        {
            AddStep("play", () => EditorClock.Start());
            AddUntilStep("samples playing", () => samplesDisabled(), () => Is.False);

            AddStep("turn hitsounds off", () => toggle(EditorStrings.Hitsounds));
            AddUntilStep("samples silenced", () => samplesDisabled(), () => Is.True);

            AddStep("turn hitsounds back on", () => toggle(EditorStrings.Hitsounds));
            AddUntilStep("samples playing again", () => samplesDisabled(), () => Is.False);
        }

        [Test]
        public void TestMusicSwitchSilencesTheTrackWithoutTouchingItsVolume()
        {
            AddAssert("track at its own volume", () => trackVolume(), () => Is.EqualTo(1).Within(0.001));

            AddStep("turn music off", () => toggle(EditorStrings.Music));
            AddUntilStep("track silenced", () => trackVolume(), () => Is.Zero);

            AddStep("turn music back on", () => toggle(EditorStrings.Music));
            AddUntilStep("volume back, untouched", () => trackVolume(), () => Is.EqualTo(1).Within(0.001));
        }

        /// <summary>
        /// The switches are not persisted, so every editor session starts audible. Leaving also has to take the mute
        /// off the track, which outlives the editor and would otherwise stay silent everywhere else.
        /// </summary>
        [Test]
        public void TestSwitchesResetWithTheEditor()
        {
            // this editor was just opened by the fixture's set-up steps.
            AddAssert("hitsounds start on", () => menuItemChecked(EditorStrings.Hitsounds), () => Is.True);
            AddAssert("music starts on", () => menuItemChecked(EditorStrings.Music), () => Is.True);

            AddStep("turn both off", () =>
            {
                toggle(EditorStrings.Hitsounds);
                toggle(EditorStrings.Music);
            });
            AddUntilStep("both silenced", () => samplesDisabled() && trackVolume() == 0, () => Is.True);

            AddStep("leave the editor", () => Editor.Exit());
            AddUntilStep("track audible again", () => trackVolume(), () => Is.EqualTo(1).Within(0.001));
        }

        private void toggle(LocalisableString label) => menuItem(label).Action.Value?.Invoke();

        private bool menuItemChecked(LocalisableString label) => menuItem(label).State.Value;

        private ToggleMenuItem menuItem(LocalisableString label)
            => Editor.ChildrenOfType<EditorMenuBar>().Single().Items.OfType<ToggleMenuItem>().Single(i => i.Text.Value.Equals(label));

        private bool samplesDisabled() => ((Game.Audio.ISamplePlaybackDisabler)Editor).SamplePlaybackDisabled.Value;

        private double trackVolume() => Beatmap.Value.Track.AggregateVolume.Value;
    }
}
