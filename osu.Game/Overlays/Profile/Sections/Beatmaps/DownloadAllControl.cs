// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays.Notifications;

namespace osu.Game.Overlays.Profile.Sections.Beatmaps
{
    /// <summary>
    /// A listing's "download all": reads the whole listing, leaves out what is downloaded already, and downloads the
    /// rest a few at a time under one notification.
    /// </summary>
    /// <remarks>
    /// A run carries on after the profile is closed. Nothing of it waits on this drawable's own scheduler — which stops
    /// while the profile is hidden — but on the API's, which every download reports back through anyway.
    /// </remarks>
    public partial class DownloadAllControl : CompositeDrawable
    {
        private readonly Func<CancellationToken, Task<IReadOnlyList<APIBeatmapSet>>> readListing;

        [Resolved]
        private IAPIProvider api { get; set; } = null!;

        [Resolved]
        private BeatmapModelDownloader downloader { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();
        private readonly Bindable<bool> preferNoVideo = new Bindable<bool>();

        private DownloadAllButton button = null!;

        /// <summary>
        /// Whether a run started here is still going. One at a time, so a second hold cannot start the same downloads
        /// over again.
        /// </summary>
        private bool running;

        /// <param name="readListing">Reads every set in the listing, or fails if the whole of it cannot be read.</param>
        public DownloadAllControl(Func<CancellationToken, Task<IReadOnlyList<APIBeatmapSet>>> readListing)
        {
            this.readListing = readListing;

            AutoSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.PreferNoVideo, preferNoVideo);
            apiState.BindTo(api.State);

            InternalChild = button = new DownloadAllButton { Action = begin };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // the API's state changes on a thread of its own.
            apiState.BindValueChanged(_ => Schedule(updateButton), true);
        }

        private void updateButton()
        {
            bool loggedIn = apiState.Value == APIState.Online;

            button.Enabled.Value = loggedIn && !running;
            button.TooltipText = loggedIn ? LazerKaiProfileStrings.DownloadAllTooltip : LazerKaiProfileStrings.DownloadAllNeedsLogin;
        }

        private void begin()
        {
            if (running)
                return;

            running = true;
            updateButton();

            var cancellation = new CancellationTokenSource();
            BulkBeatmapDownload? batch = null;

            var notification = new ProgressNotification
            {
                Text = LazerKaiProfileStrings.DownloadAllReading,
                State = ProgressNotificationState.Active,
            };

            notification.CancelRequested = () =>
            {
                cancellation.Cancel();
                batch?.Cancel();
                finishRun();
                return true;
            };

            notifications?.Post(notification);

            readListing(cancellation.Token).ContinueWith(read => api.Schedule(() =>
            {
                if (cancellation.IsCancellationRequested)
                    return;

                if (!read.IsCompletedSuccessfully)
                {
                    // reading the exception is what marks it as seen; left unread, it would be reported as an error again
                    // once collected.
                    Logger.Log($@"A profile listing could not be read in full to download: {read.Exception?.GetBaseException().Message ?? @"cancelled"}");

                    notification.Text = LazerKaiProfileStrings.DownloadAllCouldNotRead;
                    notification.State = ProgressNotificationState.Cancelled;
                    finishRun();
                    return;
                }

                var plan = BulkDownloadPlan.For(read.GetResultSafely(), installedSets(), set => downloader.GetExistingDownload(set) != null);

                if (plan.ToDownload.Count == 0)
                {
                    // "every one downloaded already" would be untrue of a listing with sets nobody can download.
                    notification.CompletionText = plan.Unavailable == 0
                        ? LazerKaiProfileStrings.DownloadAllNothingLeft
                        : summary(LazerKaiProfileStrings.DownloadAllDone(0), plan, 0);
                    notification.State = ProgressNotificationState.Completed;
                    finishRun();
                    return;
                }

                batch = new BulkBeatmapDownload(plan.ToDownload, start);

                batch.Progressed += () => showProgress(notification, batch, plan);
                batch.Completed += () =>
                {
                    // the downloader says this itself for a download of its own; for a batch's, it leaves it to the batch.
                    if (batch.RateLimited)
                        notifications?.Post(new TooManyDownloadsNotification());

                    notification.CompletionText = summary(LazerKaiProfileStrings.DownloadAllDone(batch.Downloaded), plan, batch.Failed);
                    notification.State = ProgressNotificationState.Completed;
                    finishRun();
                };

                showProgress(notification, batch, plan);
                batch.Begin();
            }), TaskScheduler.Default);
        }

        private Action? start(APIBeatmapSet set, Action<BulkDownloadOutcome> finished)
        {
            ArchiveDownloadRequest<IBeatmapSetInfo>? request = null;

            bool started = downloader.DownloadWithoutNotification(set, preferNoVideo.Value, r =>
            {
                request = r;
                r.Success += _ => finished(BulkDownloadOutcome.Downloaded);
                r.Failure += error => finished(outcomeOf(error));
            });

            if (!started)
                return null;

            return () => request!.Cancel();
        }

        private static BulkDownloadOutcome outcomeOf(Exception error)
        {
            switch (error)
            {
                case OperationCanceledException:
                    return BulkDownloadOutcome.Cancelled;

                // the same test the downloader makes for its own downloads.
                case WebException { Message: @"TooManyRequests" }:
                    return BulkDownloadOutcome.RateLimited;

                default:
                    return BulkDownloadOutcome.Failed;
            }
        }

        /// <summary>
        /// The online IDs of every set downloaded already, read in one go rather than once per set.
        /// </summary>
        private HashSet<int> installedSets() => realm.Run(r => r.All<BeatmapSetInfo>()
                                                                .Where(s => !s.DeletePending && s.OnlineID > 0)
                                                                .AsEnumerable()
                                                                .Select(s => s.OnlineID)
                                                                .ToHashSet());

        private static void showProgress(ProgressNotification notification, BulkBeatmapDownload batch, BulkDownloadPlan plan)
        {
            LocalisableString text = LazerKaiProfileStrings.DownloadAllProgress(batch.Finished, batch.Total);

            if (plan.AlreadyHave > 0)
                text = LocalisableString.Interpolate($"{text}\n{LazerKaiProfileStrings.DownloadAllAlreadyHave(plan.AlreadyHave)}");

            notification.Text = text;
            notification.Progress = (float)batch.Finished / batch.Total;
        }

        private static LocalisableString summary(LocalisableString headline, BulkDownloadPlan plan, int failed)
        {
            var text = headline;

            if (plan.AlreadyHave > 0)
                text = LocalisableString.Interpolate($"{text}\n{LazerKaiProfileStrings.DownloadAllAlreadyHave(plan.AlreadyHave)}");

            if (plan.Unavailable > 0)
                text = LocalisableString.Interpolate($"{text}\n{LazerKaiProfileStrings.DownloadAllUnavailable(plan.Unavailable)}");

            if (failed > 0)
                text = LocalisableString.Interpolate($"{text}\n{LazerKaiProfileStrings.DownloadAllFailed(failed)}");

            return text;
        }

        /// <summary>
        /// Lets the button be held again. Through this drawable's own scheduler, so a profile closed since simply picks it
        /// up when it is next shown.
        /// </summary>
        private void finishRun() => Schedule(() =>
        {
            running = false;
            updateButton();
        });
    }
}
