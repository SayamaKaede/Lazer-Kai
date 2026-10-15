// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.FirstRunSetup;
using osu.Game.Overlays.Notifications;
using osu.Game.Overlays.Settings.Sections.LazerKai;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Navigation
{
    /// <summary>
    /// The first-run setup as the fork has it: the language alone, and then where to start.
    /// </summary>
    public partial class TestSceneLazerKaiWelcome : OsuGameTestScene
    {
        private bool firstRun;

        // each test starts its own game, as a first run or not.
        public override void SetUpSteps()
        {
        }

        [Test]
        public void TestTheSetupAsksForTheLanguageAlone()
        {
            startGame(true);

            AddUntilStep("at the language", () => Game.FirstRunOverlay.CurrentScreen?.IsLoaded == true && Game.FirstRunOverlay.CurrentScreen is ScreenWelcome);
            AddStep("go on", () => Game.FirstRunOverlay.NextButton!.TriggerClick());

            AddUntilStep("the setup is done", () => Game.FirstRunOverlay.State.Value, () => Is.EqualTo(Visibility.Hidden));
            AddAssert("and finished", () => Game.FirstRunOverlay.Completed);
            AddAssert("not to be shown again", () => Game.LocalConfig.Get<bool>(OsuSetting.ShowFirstRunSetup), () => Is.False);
            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());
        }

        [Test]
        public void TestClosingTheSetupCountsAsGoingThroughIt()
        {
            startGame(true);
            closeTheSetup();

            AddAssert("not to be shown again", () => Game.LocalConfig.Get<bool>(OsuSetting.ShowFirstRunSetup), () => Is.False);
            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());
            AddAssert("nothing asks to go back to the setup", () => Game.Notifications.AllNotifications.OfType<SimpleNotification>()
                                                                        .Any(n => n.Text.Equals(FirstRunSetupOverlayStrings.ClickToResumeFirstRunSetupAtAnyPoint)),
                () => Is.False);
        }

        [Test]
        public void TestTheRecommendedSettingsAreAskedAboutThenApplied()
        {
            startGame(true);
            closeTheSetup();

            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());
            AddStep("apply the recommended settings", () => buttonOf(LazerKaiWelcomeStrings.ApplyRecommendedSettings)!.TriggerClick());

            AddUntilStep("asked first", () => currentDialog, () => Is.InstanceOf<SettingsPresetSettings.ApplySettingsPresetDialog>());
            AddAssert("nothing applied yet", () => new LazerKaiSettingsPresetStore(Game.Storage).LoadBeforeApplying(), () => Is.Null);

            AddStep("yes", () => currentDialog!.PerformOkAction());
            AddUntilStep("applied, with the settings as they were put aside", () => new LazerKaiSettingsPresetStore(Game.Storage).LoadBeforeApplying(),
                () => Is.Not.Null);
        }

        [Test]
        public void TestSettingsAlreadyLoadedListWhatWasPutAside()
        {
            startGame(false);

            // the settings load their sections the first time they are shown, and read the presets then.
            AddStep("open the settings", () => Game.Settings.Show());
            AddUntilStep("the presets are loaded", () => presetChoices, () => Is.Not.Null);
            AddAssert("nothing to go back to", () => presetChoices, () => Does.Not.Contain(SettingsPresetSettings.BEFORE_APPLYING));
            AddStep("close the settings", () => Game.Settings.Hide());

            AddStep("show the welcome", () => Game.ChildrenOfType<DialogOverlay>().Single().Push(new LazerKaiWelcomeDialog(null)));
            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());
            AddStep("apply the recommended settings", () => buttonOf(LazerKaiWelcomeStrings.ApplyRecommendedSettings)!.TriggerClick());
            AddUntilStep("asked first", () => currentDialog, () => Is.InstanceOf<SettingsPresetSettings.ApplySettingsPresetDialog>());
            AddStep("yes", () => currentDialog!.PerformOkAction());

            AddStep("open the settings again", () => Game.Settings.Show());
            AddUntilStep("what was put aside can be gone back to", () => presetChoices, () => Does.Contain(SettingsPresetSettings.BEFORE_APPLYING));
        }

        [Test]
        public void TestNoGuideToOpenWhereThereIsNone()
        {
            // nothing copies the guide beside a build straight out of the repository, this one included.
            startGame(true);
            closeTheSetup();

            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());
            AddAssert("no button for the guide", () => buttonOf(LazerKaiSettingsStrings.OpenGuide), () => Is.Null);
            AddAssert("but one for the settings", () => buttonOf(LazerKaiWelcomeStrings.ApplyRecommendedSettings), () => Is.Not.Null);
        }

        [Test]
        public void TestOpeningTheGuideLeavesTheWelcomeOpen()
        {
            int opened = 0;

            startGame(false);

            // the game loads its overlays one after another, and here can reach the menu before this one is in.
            AddUntilStep("the dialogs are loaded", () => Game.ChildrenOfType<DialogOverlay>().Any());
            AddStep("show the welcome with a guide", () => Game.ChildrenOfType<DialogOverlay>().Single().Push(new LazerKaiWelcomeDialog(() => opened++)));
            AddUntilStep("the welcome is shown", () => currentDialog, () => Is.InstanceOf<LazerKaiWelcomeDialog>());

            AddStep("open the guide", () => buttonOf(LazerKaiSettingsStrings.OpenGuide)!.TriggerClick());
            AddAssert("it is opened", () => opened, () => Is.EqualTo(1));

            AddWaitStep("let the dialog settle", 5);
            AddAssert("the welcome is still shown", () => currentDialog?.State.Value, () => Is.EqualTo(Visibility.Visible));
        }

        [Test]
        public void TestNoWelcomeWhenTheSetupIsRunAgain()
        {
            startGame(false);

            AddStep("run the setup from the settings", () => Game.FirstRunOverlay.Show());
            AddUntilStep("the setup is shown", () => Game.FirstRunOverlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            closeTheSetup();

            AddWaitStep("let it settle", 5);
            AddAssert("no welcome", () => currentDialog, () => Is.Null);
        }

        private PopupDialog? currentDialog => Game.ChildrenOfType<DialogOverlay>().SingleOrDefault()?.CurrentDialog;

        private IEnumerable<string>? presetChoices => Game.Settings.ChildrenOfType<SettingsPresetSettings>().SingleOrDefault()?
                                                          .ChildrenOfType<FormDropdown<string>>().Single().Items;

        private PopupDialogButton? buttonOf(LocalisableString text) =>
            currentDialog?.ChildrenOfType<PopupDialogButton>().SingleOrDefault(b => b.Text.Equals(text));

        private void closeTheSetup()
        {
            AddUntilStep("the setup is shown", () => Game.FirstRunOverlay.State.Value, () => Is.EqualTo(Visibility.Visible));
            AddStep("close it", () => InputManager.Key(Key.Escape));
            AddUntilStep("the setup is closed", () => Game.FirstRunOverlay.State.Value, () => Is.EqualTo(Visibility.Hidden));
        }

        private void startGame(bool asFirstRun)
        {
            AddStep(asFirstRun ? "a first run" : "not a first run", () => firstRun = asFirstRun);

            CreateNewGame();
            ConfirmAtMainMenu();
        }

        protected override TestOsuGame CreateTestGame() => new WelcomeGame(LocalStorage, API, firstRun);

        private partial class WelcomeGame : TestOsuGame
        {
            private readonly bool firstRun;

            public WelcomeGame(Storage storage, IAPIProvider api, bool firstRun)
                : base(storage, api)
            {
                this.firstRun = firstRun;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                LocalConfig.SetValue(OsuSetting.ShowFirstRunSetup, firstRun);
            }
        }
    }
}
