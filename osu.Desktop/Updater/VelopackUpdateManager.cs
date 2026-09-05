// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Overlays.Notifications;
using osu.Game.Screens.Play;
using Velopack;
using Velopack.Locators;
using Velopack.Sources;
using UpdateManager = osu.Game.Updater.UpdateManager;

namespace osu.Desktop.Updater
{
    public partial class VelopackUpdateManager : UpdateManager
    {
        [Resolved]
        private INotificationOverlay notificationOverlay { get; set; } = null!;

        [Resolved]
        private OsuGameBase game { get; set; } = null!;

        [Resolved]
        private ILocalUserPlayInfo? localUserInfo { get; set; }

        private bool isInGameplay => localUserInfo?.PlayingState.Value != LocalUserPlayingState.NotPlaying;

        private ScheduledDelegate? scheduledBackgroundCheck;

        /// <summary>
        /// Where releases of this fork are published.
        /// </summary>
        /// <remarks>
        /// A repository of its own, holding nothing but the releases: the source sits elsewhere, and reading this one
        /// has to work without a token, which anyone holding a copy of the game would be able to read back out of it.
        /// The release stream setting has no part here — there is one stream.
        /// </remarks>
        private static IUpdateSource CreateSource() => new GithubSource(@"https://github.com/SayamaKaede/Lazer-Kai-Releases", null, false);

        public override osu.Game.Configuration.ReleaseStream? FixedReleaseStream => osu.Game.Configuration.ReleaseStream.LazerKai;

        // there is nowhere else to get a build of this from, so there is nothing to point at.
        public override LocalisableString? FixedReleaseStreamNote => null;

        /// <summary>
        /// The folder a packaged build lives in, which is the one above the versioned folder the executable sits in.
        /// Null when this build was not packed by velopack.
        /// </summary>
        /// <remarks>
        /// Velopack replaces the versioned folder wholesale on every update, so nothing that has to outlive one can be
        /// kept in there. This is the folder that does outlive them.
        /// </remarks>
        public static string? RootDirectory
        {
            get
            {
                var locator = VelopackLocator.Current;

                // null for a build that was never packed, which is what tells the two kinds apart everywhere below.
                return locator.CurrentlyInstalledVersion == null ? null : locator.RootAppDir;
            }
        }

        /// <summary>
        /// Whether this build was packed by velopack, and can therefore replace itself.
        /// </summary>
        /// <remarks>
        /// A build handed out as a folder to copy over an existing one has nothing to update from, and velopack's
        /// check fails on every run. Those are told a release happened instead, by osu.Game's LazerKaiUpdateManager.
        /// </remarks>
        public static bool CanReplaceItself => RootDirectory != null;

        private void scheduleNextUpdateCheck()
        {
            scheduledBackgroundCheck?.Cancel();
            scheduledBackgroundCheck = Scheduler.AddDelayed(() =>
            {
                log("Running scheduled background update check...");
                CheckForUpdate();
            }, 60000 * 30);
        }

        protected override async Task<bool> PerformUpdateCheck(CancellationToken cancellationToken)
        {
            scheduledBackgroundCheck?.Cancel();

            if (isInGameplay)
            {
                log("Update check cancelled - user is in gameplay");
                scheduleNextUpdateCheck();
                return false;
            }

            try
            {
                Velopack.UpdateManager updateManager = new Velopack.UpdateManager(CreateSource(), new UpdateOptions
                {
                    AllowVersionDowngrade = true
                });

                UpdateInfo? update = await updateManager.CheckForUpdatesAsync().ConfigureAwait(false);

                if (cancellationToken.IsCancellationRequested)
                {
                    log("Update check cancelled");
                    scheduleNextUpdateCheck();
                    return true;
                }

                if (update == null)
                {
                    // No update is available.
                    log("No update found");
                    scheduleNextUpdateCheck();
                    return false;
                }

                // Download update in the background while notifying awaiters of the update being available.
                log($"New update available: {update.TargetFullRelease.Version}");
                downloadUpdate(updateManager, update, cancellationToken);
                return true;
            }
            catch (Exception e)
            {
                log($"Update check failed with error ({e.Message})");

                // we shouldn't crash on a web failure. or any failure for the matter.
                scheduleNextUpdateCheck();
                return true;
            }
        }

        private void downloadUpdate(Velopack.UpdateManager updateManager, UpdateInfo update, CancellationToken cancellationToken) => Task.Run(async () =>
        {
            log($"Beginning download of update {update.TargetFullRelease.Version}...");

            UpdateDownloadProgressNotification progressNotification = new UpdateDownloadProgressNotification(cancellationToken)
            {
                CompletionClickAction = () =>
                {
                    restartToApplyUpdate(updateManager, update);
                    return true;
                }
            };

            try
            {
                using (var cts = CancellationTokenSource.CreateLinkedTokenSource(progressNotification.CancellationToken, cancellationToken))
                {
                    progressNotification.StartDownload();
                    runOutsideOfGameplay(() => notificationOverlay.Post(progressNotification), cts.Token);

                    await updateManager.DownloadUpdatesAsync(update, p => progressNotification.Progress = p / 100f, cts.Token).ConfigureAwait(false);
                    runOutsideOfGameplay(() => progressNotification.State = ProgressNotificationState.Completed, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                progressNotification.FailDownload();
                log(@"Update cancelled");
            }
            catch (Exception e)
            {
                // In the case of an error, a separate notification will be displayed.
                progressNotification.FailDownload();
                Logger.Error(e, @"Update failed!");
            }

            return true;
        }, cancellationToken);

        private void runOutsideOfGameplay(Action action, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            if (isInGameplay)
            {
                Scheduler.AddDelayed(() => runOutsideOfGameplay(action, cancellationToken), 1000);
                return;
            }

            action();
        }

        private void restartToApplyUpdate(Velopack.UpdateManager updateManager, UpdateInfo update)
        {
            game.RestartOnExitAction = () => updateManager.WaitExitThenApplyUpdates(update.TargetFullRelease);
            game.AttemptExit();
        }

        private static void log(string text) => Logger.Log($"VelopackUpdateManager: {text}");
    }
}
