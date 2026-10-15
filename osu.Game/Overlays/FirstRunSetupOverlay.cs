// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Overlays.FirstRunSetup;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens;
using osu.Game.Screens.Menu;

namespace osu.Game.Overlays
{
    [Cached]
    public partial class FirstRunSetupOverlay : WizardOverlay
    {
        [Resolved]
        private IPerformFromScreenRunner performer { get; set; } = null!;

        [Resolved]
        private INotificationOverlay notificationOverlay { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private GameHost host { get; set; } = null!;

        private readonly Bindable<bool> showFirstRunSetup = new Bindable<bool>();

        public FirstRunSetupOverlay()
            : base(OverlayColourScheme.Purple)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Lazer-Kai: the language alone (2026-10-14). The size of the interface, beatmaps to download and how the
            // game behaves are all in the settings, and were a run of screens to get through before reaching anything
            // about the fork; where to start with the fork comes right after instead (see PopOut).
            //
            // Importing from stable goes with them, and is nowhere else in a released build. That is wanted: this is a
            // client for mapping, and bringing every beatmap across from stable does it no good and only costs
            // (2026-10-15). A beatmap is brought in by dropping its .osz.
            AddStep<ScreenWelcome>();

            Header.Title = FirstRunSetupOverlayStrings.FirstRunSetupTitle;
            Header.Description = FirstRunSetupOverlayStrings.FirstRunSetupDescription;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            config.BindWith(OsuSetting.ShowFirstRunSetup, showFirstRunSetup);

            if (showFirstRunSetup.Value) Show();
        }

        public override void Show()
        {
            // if we are valid for display, only do so after reaching the main menu.
            performer.PerformFromScreen(screen =>
            {
                // Hides the toolbar for us.
                if (screen is MainMenu menu)
                    menu.ReturnToOsuLogo();

                base.Show();
            }, new[] { typeof(MainMenu) });
        }

        protected override void PopOut()
        {
            base.PopOut();

            // Lazer-Kai: closing the setup part-way on a first run is as good as finishing it. All it asks for is the
            // language, which is in the settings as well, so bringing it back on every start would only be in the
            // way. Either way round, where to start is shown next, and only on a first run: the setup run again
            // from the settings is somebody who has already been through this.
            if (showFirstRunSetup.Value)
            {
                showFirstRunSetup.Value = false;

                string? guide = LazerKaiManual.FindGuide(RuntimeInfo.StartupDirectory);
                dialogOverlay?.Push(new LazerKaiWelcomeDialog(guide == null ? null : () => host.OpenFileExternally(guide)));
                return;
            }

            if (CurrentStepIndex != null)
            {
                notificationOverlay.Post(new SimpleNotification
                {
                    Text = FirstRunSetupOverlayStrings.ClickToResumeFirstRunSetupAtAnyPoint,
                    Icon = FontAwesome.Solid.Redo,
                    Activated = () =>
                    {
                        Show();
                        return true;
                    },
                });
            }
        }

        protected override void ShowNextStep()
        {
            base.ShowNextStep();

            if (CurrentStepIndex == null)
                showFirstRunSetup.Value = false;
        }
    }
}
