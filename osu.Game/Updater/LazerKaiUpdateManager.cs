// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.IO.Network;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Threading;
using osu.Game.Localisation;

namespace osu.Game.Updater
{
    /// <summary>
    /// Tells the user a newer build has been handed out, and sends them to where it is.
    /// </summary>
    /// <remarks>
    /// This fork is given out as a folder to copy over an existing one, so there is nothing here that installs
    /// anything: replacing a running program's own files is what the update frameworks exist to do carefully, and
    /// getting it wrong leaves everyone with a broken copy. Knowing a release happened is the part that was missing.
    /// </remarks>
    public partial class LazerKaiUpdateManager : UpdateManager
    {
        /// <summary>
        /// The release feed, a small file the maintainer edits by hand for each release.
        /// </summary>
        /// <remarks>
        /// Fetched by file id rather than by name, so the file can be renamed freely — but replacing it with a fresh
        /// upload gives it a new id and silently strands everyone already running a build. It is updated in place
        /// through Google Drive's "manage versions" instead.
        /// </remarks>
        private const string feed_url = @"https://drive.usercontent.google.com/download?id=1We9SHUgiJ1ayrCvDcLtEwf_YI7z00n_q&export=download";

        private const double check_interval = 60000 * 30;

        [Resolved]
        private GameHost host { get; set; } = null!;

        private string runningVersion = string.Empty;

        private ScheduledDelegate? scheduledCheck;

        [BackgroundDependencyLoader]
        private void load(OsuGameBase game)
        {
            runningVersion = game.Version;
        }

        protected override async Task<bool> PerformUpdateCheck(CancellationToken cancellationToken)
        {
            scheduledCheck?.Cancel();

            try
            {
                var request = new WebRequest(feed_url);
                await request.PerformAsync(cancellationToken).ConfigureAwait(false);

                var release = LazerKaiRelease.FromJson(request.GetResponseString() ?? string.Empty);

                // logged either way: a check that quietly does nothing is indistinguishable from one that never ran,
                // and this is the only trace of it there would otherwise be.
                Logger.Log($"Release feed says {release?.Version ?? "nothing"}, running {runningVersion}");

                if (release == null || !release.IsNewerThan(runningVersion))
                {
                    scheduleNextCheck();
                    return false;
                }

                Notifications.Post(new UpdateAvailableNotification(cancellationToken)
                {
                    Text = string.IsNullOrEmpty(release.Notes)
                        ? LazerKaiUpdateStrings.ReleaseAvailable(release.Version)
                        : LocalisableString.Interpolate($"{LazerKaiUpdateStrings.ReleaseAvailable(release.Version)}\n\n{release.Notes}"),
                    Activated = () =>
                    {
                        host.OpenUrlExternally(release.Url);
                        return true;
                    },
                });

                return true;
            }
            catch (System.Exception e)
            {
                Logger.Log($"Could not read the release feed ({e.Message})");

                scheduleNextCheck();
                return true;
            }
        }

        private void scheduleNextCheck()
        {
            scheduledCheck?.Cancel();
            scheduledCheck = Scheduler.AddDelayed(CheckForUpdate, check_interval);
        }
    }
}
