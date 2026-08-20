// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Rulesets.UI;
using osu.Game.Screens;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Overlays.Mods;
using osu.Game.Screens.Edit.GameplayTest;
using osu.Game.Screens.Play;
using osu.Game.Storyboards;
using osu.Game.Tests.Beatmaps.IO;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Editing
{
    public partial class TestSceneEditorTestGameplay : EditorTestScene
    {
        protected override bool IsolateSavingFromDatabase => false;

        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuGameBase game { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; }

        [Resolved]
        private OsuConfigManager config { get; set; }

        private BeatmapSetInfo importedBeatmapSet;

        public override void SetUpSteps()
        {
            AddStep("import test beatmap", () => importedBeatmapSet = BeatmapImportHelper.LoadOszIntoOsu(game).GetResultSafely());
            // config outlives a single test, so a test that turns this on would otherwise change what every test after it does.
            AddStep("stop sharing the mod selection", () => config.SetValue(OsuSetting.EditorCarryOverSongSelectMods, false));
            base.SetUpSteps();
        }

        protected override WorkingBeatmap CreateWorkingBeatmap(IBeatmap beatmap, Storyboard storyboard = null)
            => beatmaps.GetWorkingBeatmap(importedBeatmapSet.Beatmaps.First(b => b.Ruleset.OnlineID == 0));

        protected override void LoadEditor()
        {
            SelectedMods.Value = new[] { new ModCinema() };
            base.LoadEditor();
        }

        [Test]
        public void TestBasicGameplayTest()
        {
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddUntilStep("background is correct", () => this.ChildrenOfType<BackgroundScreenStack>().Single().CurrentScreen is EditorBackgroundScreen);
            AddAssert("no mods selected", () => SelectedMods.Value.Count == 0);
        }

        [Test]
        public void TestSelectingTestPlayModThroughMenuBar()
        {
            // the selection is persisted to config, so it can carry over from a previously run test in this fixture.
            AddStep("clear test play mods", () => Editor.TestPlayMods.DeselectAll());

            openModSelect();

            AddStep("click Freeze Frame", () => panelForMod(typeof(OsuModFreezeFrame)).TriggerClick());

            AddAssert("freeze frame selected for test play", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModFreezeFrame>().Any());

            // the mod screen is the one song select uses, so it has to be writing to the editor's own selection and
            // not to the one the player left behind at song select.
            AddAssert("song select mods left alone", () => SelectedMods.Value, () => Is.Empty);
        }

        /// <summary>
        /// The reason for using the mod screen at all: a mod's own settings can be changed, and have to be kept.
        /// </summary>
        [Test]
        public void TestCustomisedModSurvivesTheEditorSession()
        {
            Editor firstEditor = null;

            AddStep("pick double time", () =>
            {
                firstEditor = Editor;
                Editor.TestPlayMods.Mods.Value = new[] { new OsuModDoubleTime() };
            });

            // edited in place, the way the mod screen's customisation panel does it: the selection itself does not
            // change, so nothing would be written out if only the list were being watched.
            AddStep("slow it to 1.25x", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModDoubleTime>().Single().SpeedChange.Value = 1.25);

            AddStep("exit editor", () => Editor.Exit());
            AddUntilStep("wait for editor exit", () => !firstEditor.IsCurrentScreen());

            AddStep("reload editor", LoadEditor);
            AddUntilStep("wait for new editor", () => Editor != firstEditor && Editor?.ReadyForUse == true);

            AddAssert("the speed came back with it", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModDoubleTime>().Single().SpeedChange.Value,
                () => Is.EqualTo(1.25));
        }

        private void openModSelect()
        {
            AddStep("click Test", () => this.ChildrenOfType<EditorMenuBar.DrawableEditorBarMenuItem>()
                                            .Single(item => hasText(item, "Test")).TriggerClick());

            hoverMenuItem("Mods");
            AddStep("click Mods", () => getMenuItem("Mods").TriggerClick());

            AddUntilStep("mod screen is up", () => modSelect().State.Value, () => Is.EqualTo(Visibility.Visible));
            AddUntilStep("its panels are loaded", () => modSelect().ChildrenOfType<ModPanel>().Any(), () => Is.True);
        }

        private EditorModSelectOverlay modSelect() => this.ChildrenOfType<EditorModSelectOverlay>().Single();

        private ModPanel panelForMod(Type modType) => modSelect().ChildrenOfType<ModPanel>().Single(panel => panel.Mod.GetType() == modType);

        private void hoverMenuItem(string text)
        {
            AddUntilStep($"wait for \"{text}\"", () => getMenuItem(text) != null);
            AddStep($"hover \"{text}\"", () => InputManager.MoveMouseTo(getMenuItem(text)));
        }

        private DrawableOsuMenuItem getMenuItem(string text) => this.ChildrenOfType<DrawableOsuMenuItem>().FirstOrDefault(item => hasText(item, text));

        private static bool hasText(Drawable item, string text) => item.ChildrenOfType<SpriteText>().Any(t => t.Text.ToString() == text);

        [Test]
        public void TestGameplayTestAppliesTestPlayMods()
        {
            AddStep("select freeze frame as a test play mod", () => Editor.TestPlayMods.Mods.Value = new[] { new OsuModFreezeFrame() });

            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddAssert("freeze frame applied to gameplay", () => editorPlayer.GameplayState.Mods.OfType<OsuModFreezeFrame>().Any());

            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddStep("exit editor", () => Editor.Exit());
            AddUntilStep("mod selection restored on leaving editor", () => SelectedMods.Value.Count == 1 && SelectedMods.Value.Single() is ModCinema);
        }

        [Test]
        public void TestTestPlayModsPersistAcrossEditorSessions()
        {
            Editor firstEditor = null;

            AddStep("select freeze frame as a test play mod", () =>
            {
                firstEditor = Editor;
                Editor.TestPlayMods.Mods.Value = new[] { new OsuModFreezeFrame() };
            });

            AddStep("exit editor", () => Editor.Exit());
            AddUntilStep("wait for editor exit", () => !firstEditor.IsCurrentScreen());

            AddStep("reload editor", LoadEditor);
            AddUntilStep("wait for new editor", () => Editor != firstEditor && Editor?.ReadyForUse == true);
            AddAssert("test play mods restored", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModFreezeFrame>().Any());
        }

        [Test]
        public void TestGameplayTestWhenTrackRunning()
        {
            AddStep("start track", () => EditorClock.Start());
            AddAssert("sample playback enabled", () => !Editor.SamplePlaybackDisabled.Value);

            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddAssert("editor track stopped", () => !EditorClock.IsRunning);
            AddAssert("sample playback disabled", () => Editor.SamplePlaybackDisabled.Value);

            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddUntilStep("background is correct", () => this.ChildrenOfType<BackgroundScreenStack>().Single().CurrentScreen is EditorBackgroundScreen);

            AddStep("start track", () => EditorClock.Start());
            AddAssert("sample playback re-enabled", () => !Editor.SamplePlaybackDisabled.Value);
        }

        [Test]
        public void TestGameplayTestResetsPlaybackSpeedAdjustment()
        {
            AddStep("start track", () => EditorClock.Start());
            AddStep("change playback speed", () =>
            {
                InputManager.MoveMouseTo(this.ChildrenOfType<PlaybackControl.PlaybackTabControl.PlaybackTabItem>().First());
                InputManager.Click(MouseButton.Left);
            });
            AddAssert("track playback rate is 0.25x", () => Beatmap.Value.Track.AggregateTempo.Value, () => Is.EqualTo(0.25));

            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddAssert("editor track stopped", () => !EditorClock.IsRunning);
            AddAssert("track playback rate is 1x", () => Beatmap.Value.Track.AggregateTempo.Value, () => Is.EqualTo(1));

            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddAssert("track playback rate is 0.25x", () => Beatmap.Value.Track.AggregateTempo.Value, () => Is.EqualTo(0.25));
        }

        [TestCase(2000)] // chosen to be after last object in the map
        [TestCase(22000)] // chosen to be in the middle of the last spinner
        public void TestGameplayTestAtEndOfBeatmap(int offsetFromEnd)
        {
            AddStep($"seek to end minus {offsetFromEnd}ms", () => EditorClock.Seek(importedBeatmapSet.MaxLength - offsetFromEnd));
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("player pushed", () => Stack.CurrentScreen is EditorPlayer);

            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
        }

        [Test]
        public void TestCancelGameplayTestWithUnsavedChanges()
        {
            AddStep("delete all but first object", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.Skip(1).ToList()));

            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("save prompt shown", () => DialogOverlay.CurrentDialog is SaveRequiredPopupDialog);

            AddStep("dismiss prompt", () =>
            {
                var button = DialogOverlay.CurrentDialog!.Buttons.Last();
                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            AddWaitStep("wait some", 3);
            AddAssert("stayed in editor", () => Stack.CurrentScreen is Editor);
        }

        [Test]
        public void TestSaveChangesBeforeGameplayTest()
        {
            AddStep("delete all but first object", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.Skip(1).ToList()));
            // bit of a hack to ensure this test can be ran multiple times without running into UNIQUE constraint failures
            AddStep("set unique difficulty name", () => EditorBeatmap.BeatmapInfo.DifficultyName = Guid.NewGuid().ToString());

            AddStep("start playing track", () => InputManager.Key(Key.Space));
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });
            AddUntilStep("save prompt shown", () => DialogOverlay.CurrentDialog is SaveRequiredPopupDialog);
            AddAssert("track stopped", () => !Beatmap.Value.Track.IsRunning);

            AddStep("save changes", () => DialogOverlay.CurrentDialog!.PerformOkAction());

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddUntilStep("track playing", () => Beatmap.Value.Track.IsRunning);
            AddAssert("beatmap has 1 object", () => editorPlayer.Beatmap.Value.Beatmap.HitObjects.Count == 1);

            AddUntilStep("wait for return to editor", () => Stack.CurrentScreen is Editor);
            AddAssert("track stopped", () => !Beatmap.Value.Track.IsRunning);
        }

        [Test]
        public void TestClockTimeTransferIsOneDirectional()
        {
            AddStep("seek to 00:01:00", () => EditorClock.Seek(60_000));
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);

            GameplayClockContainer gameplayClockContainer = null;
            AddStep("fetch gameplay clock", () => gameplayClockContainer = editorPlayer.ChildrenOfType<GameplayClockContainer>().First());
            AddUntilStep("gameplay clock running", () => gameplayClockContainer.IsRunning);
            // when the gameplay test is entered, the clock is expected to continue from where it was in the main editor...
            AddAssert("gameplay time past 00:01:00", () => gameplayClockContainer.CurrentTime >= 60_000);

            AddWaitStep("wait some", 5);

            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            // but when exiting from gameplay test back to editor, the expectation is that the editor time should revert to what it was at the point of initiating the gameplay test.
            AddAssert("time reverted to 00:01:00", () => EditorClock.CurrentTime, () => Is.EqualTo(60_000));
        }

        [Test]
        public void TestAutoplayToggle()
        {
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddUntilStep("no replay active", () => editorPlayer.ChildrenOfType<DrawableRuleset>().Single().ReplayScore, () => Is.Null);
            AddStep("press Tab", () => InputManager.Key(Key.Tab));
            AddUntilStep("replay active", () => editorPlayer.ChildrenOfType<DrawableRuleset>().Single().ReplayScore, () => Is.Not.Null);
            AddStep("press Tab", () => InputManager.Key(Key.Tab));
            AddUntilStep("no replay active", () => editorPlayer.ChildrenOfType<DrawableRuleset>().Single().ReplayScore, () => Is.Null);
            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
        }

        [Test]
        public void TestQuickPause()
        {
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);
            AddUntilStep("clock running", () => editorPlayer.ChildrenOfType<GameplayClockContainer>().Single().IsPaused.Value, () => Is.False);
            AddStep("press Ctrl-P", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.P);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("clock not running", () => editorPlayer.ChildrenOfType<GameplayClockContainer>().Single().IsPaused.Value, () => Is.True);
            AddStep("press Ctrl-P", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.P);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("clock running", () => editorPlayer.ChildrenOfType<GameplayClockContainer>().Single().IsPaused.Value, () => Is.False);
            AddStep("exit player", () => editorPlayer.Exit());
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
        }

        [Test]
        public void TestQuickExitAtInitialPosition()
        {
            AddStep("seek to 00:01:00", () => EditorClock.Seek(60_000));
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);

            GameplayClockContainer gameplayClockContainer = null;
            AddStep("fetch gameplay clock", () => gameplayClockContainer = editorPlayer.ChildrenOfType<GameplayClockContainer>().First());
            AddUntilStep("gameplay clock running", () => gameplayClockContainer.IsRunning);
            // when the gameplay test is entered, the clock is expected to continue from where it was in the main editor...
            AddAssert("gameplay time past 00:01:00", () => gameplayClockContainer.CurrentTime >= 60_000);

            AddWaitStep("wait some", 5);

            AddStep("exit player", () => InputManager.PressKey(Key.F1));
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddAssert("time reverted to 00:01:00", () => EditorClock.CurrentTime, () => Is.EqualTo(60_000));
        }

        [Test]
        public void TestQuickExitAtCurrentPosition()
        {
            AddStep("seek to 00:01:00", () => EditorClock.Seek(60_000));
            AddStep("click test gameplay button", () =>
            {
                var button = Editor.ChildrenOfType<TestGameplayButton>().Single();

                InputManager.MoveMouseTo(button);
                InputManager.Click(MouseButton.Left);
            });

            EditorPlayer editorPlayer = null;
            AddUntilStep("player pushed", () => (editorPlayer = Stack.CurrentScreen as EditorPlayer) != null);

            GameplayClockContainer gameplayClockContainer = null;
            AddStep("fetch gameplay clock", () => gameplayClockContainer = editorPlayer.ChildrenOfType<GameplayClockContainer>().First());
            AddUntilStep("gameplay clock running", () => gameplayClockContainer.IsRunning);
            // when the gameplay test is entered, the clock is expected to continue from where it was in the main editor...
            AddAssert("gameplay time past 00:01:00", () => gameplayClockContainer.CurrentTime >= 60_000);

            AddWaitStep("wait some", 5);

            AddStep("exit player", () => InputManager.PressKey(Key.F2));
            AddUntilStep("current screen is editor", () => Stack.CurrentScreen is Editor);
            AddAssert("time moved forward", () => EditorClock.CurrentTime, () => Is.GreaterThan(60_000));
        }

        public override void TearDownSteps()
        {
            base.TearDownSteps();
            AddStep("delete imported", () => Realm.Write(r =>
            {
                // delete from realm directly rather than via `BeatmapManager` to avoid cross-test pollution
                // (`BeatmapManager.Delete()` uses soft deletion, which can lead to beatmap reuse between test cases).
                r.RemoveAll<BeatmapMetadata>();
                r.RemoveAll<BeatmapInfo>();
                r.RemoveAll<BeatmapSetInfo>();
            }));
        }
    }
}
