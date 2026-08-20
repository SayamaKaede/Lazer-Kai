// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Previewing a mod that changes the speed, which the composer preview could not show on its own: those mods move
    /// nothing on the timeline, so the only thing there is to see is the song running at a different pace.
    /// </summary>
    public partial class TestSceneEditorModRatePreview : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestDoubleTimeOnlyRunsTheSongFasterWhilePreviewing()
        {
            setPreview(false);
            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });

            AddAssert("song plays as written", rate, () => Is.EqualTo(1).Within(0.001));

            setPreview(true);
            AddUntilStep("song sped up", rate, () => Is.EqualTo(1.5).Within(0.001));

            setPreview(false);
            AddUntilStep("back to as written", rate, () => Is.EqualTo(1).Within(0.001));
        }

        /// <summary>
        /// The speed is the mod's to decide while it is previewed, so the control that would otherwise fight it is
        /// taken away, exactly as timing mode does.
        /// </summary>
        [Test]
        public void TestPlaybackSpeedIsTakenAwayWhileARateModIsPreviewed()
        {
            setPreview(true);
            AddStep("no mods", () => Editor.TestPlayMods.Mods.Value = []);

            AddStep("halve the playback speed", () => speedControl().Current.Value = 0.5);
            AddUntilStep("the song is halved", rate, () => Is.EqualTo(0.5).Within(0.001));

            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });

            AddUntilStep("the control is taken away", () => speedControl().Current.Disabled, () => Is.True);
            AddUntilStep("and it let go of the half speed", rate, () => Is.EqualTo(1.5).Within(0.001));

            AddStep("clear the mods", () => Editor.TestPlayMods.Mods.Value = []);
            AddUntilStep("the control is back", () => speedControl().Current.Disabled, () => Is.False);
        }

        /// <summary>
        /// Timing by ear against a resampled track is inaccurate, which is why the editor refuses to change speed in
        /// timing mode at all. A previewed mod must not get around that.
        /// </summary>
        [Test]
        public void TestTimingModeIsLeftAtATrueRate()
        {
            setPreview(true);
            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });
            AddUntilStep("song sped up", rate, () => Is.EqualTo(1.5).Within(0.001));

            AddStep("go to timing", () => Editor.Mode.Value = EditorScreenMode.Timing);
            AddUntilStep("song back to a true rate", rate, () => Is.EqualTo(1).Within(0.001));

            AddStep("back to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("song sped up again", rate, () => Is.EqualTo(1.5).Within(0.001));
        }

        /// <summary>
        /// The mods survive into the next editor session, so the editor can be opened with one already on. Nothing
        /// changes at that point, so anything waiting for a change would sit out the whole session.
        /// </summary>
        [Test]
        public void TestSpeedIsAlreadyTakenAwayOnEnteringWithARateModOn()
        {
            TestEditor firstEditor = null!;

            setPreview(true);
            AddStep("pick double time", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModDoubleTime() });

            AddStep("exit editor", () =>
            {
                firstEditor = Editor;
                Editor.Exit();
            });
            AddUntilStep("wait for editor exit", () => !firstEditor.IsCurrentScreen());

            AddStep("reload editor", LoadEditor);
            AddUntilStep("wait for new editor", () => Editor != firstEditor && Editor?.ReadyForUse == true);

            AddAssert("the control was already taken away", () => speedControl().Current.Disabled, () => Is.True);
            AddUntilStep("and the song is already sped up", rate, () => Is.EqualTo(1.5).Within(0.001));
        }

        private void setPreview(bool enabled)
            => AddStep(enabled ? "preview mods in the composer" : "do not preview mods in the composer",
                () => config.SetValue(OsuSetting.EditorApplyTestPlayModsToComposer, enabled));

        private double rate() => EditorClock.AudioAdjustments.AggregateTempo.Value * EditorClock.AudioAdjustments.AggregateFrequency.Value;

        private PlaybackControl.PlaybackTabControl speedControl()
            => Editor.ChildrenOfType<PlaybackControl.PlaybackTabControl>().Single();
    }
}
