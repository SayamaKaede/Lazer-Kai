// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Works through a batch of beatmap downloads a few at a time, keeping the counts one notification reports.
    /// </summary>
    /// <remarks>
    /// A few at a time rather than all at once: the downloader starts whatever it is handed straight away, and a
    /// listing can run to hundreds of sets — which the server answers by refusing downloads for a while.
    ///
    /// Everything here is expected to run on the one thread downloads report back on, so nothing is locked.
    /// </remarks>
    public class BulkBeatmapDownload
    {
        /// <summary>
        /// How many downloads run at once.
        /// </summary>
        public const int CONCURRENT_DOWNLOADS = 3;

        /// <summary>
        /// Starts downloading one set.
        /// </summary>
        /// <param name="set">The set to download.</param>
        /// <param name="finished">To be called once the download has ended, with how it ended.</param>
        /// <returns>A way to cancel the download, or <c>null</c> if it was not started.</returns>
        public delegate Action? StartDownload(APIBeatmapSet set, Action<BulkDownloadOutcome> finished);

        /// <summary>
        /// Raised each time a download ends.
        /// </summary>
        public event Action? Progressed;

        /// <summary>
        /// Raised once, when there is nothing left running and nothing left to start. Never raised once cancelled.
        /// </summary>
        public event Action? Completed;

        /// <summary>
        /// How many sets the batch was given.
        /// </summary>
        public int Total => sets.Count;

        /// <summary>
        /// How many have been dealt with, one way or another.
        /// </summary>
        public int Finished { get; private set; }

        /// <summary>
        /// How many downloaded.
        /// </summary>
        public int Downloaded { get; private set; }

        /// <summary>
        /// How many did not download — those never started because the server refused further downloads included.
        /// </summary>
        public int Failed { get; private set; }

        /// <summary>
        /// Whether the server refused further downloads, which stopped the rest from being started.
        /// </summary>
        public bool RateLimited { get; private set; }

        /// <summary>
        /// Whether the batch was cancelled.
        /// </summary>
        public bool Cancelled { get; private set; }

        private readonly IReadOnlyList<APIBeatmapSet> sets;
        private readonly StartDownload start;
        private readonly List<RunningDownload> running = new List<RunningDownload>();

        private int next;
        private bool completed;

        public BulkBeatmapDownload(IReadOnlyList<APIBeatmapSet> sets, StartDownload start)
        {
            this.sets = sets;
            this.start = start;
        }

        public void Begin() => fill();

        /// <summary>
        /// Starts nothing further and cancels whatever is running.
        /// </summary>
        public void Cancel()
        {
            if (Cancelled || completed)
                return;

            Cancelled = true;

            var cancelling = running.ToArray();
            running.Clear();

            foreach (var download in cancelling)
                download.Cancel?.Invoke();
        }

        private void fill()
        {
            while (!Cancelled && !RateLimited && running.Count < CONCURRENT_DOWNLOADS && next < sets.Count)
            {
                var download = new RunningDownload();
                running.Add(download);

                download.Cancel = start(sets[next++], outcome => finish(download, outcome));

                // not started — most likely because it has been started from somewhere else since the batch was planned,
                // which leaves it to report on itself. It is neither downloaded nor failed as far as the batch goes.
                if (download.Cancel == null && running.Remove(download))
                    Finished++;
            }

            if (!Cancelled && running.Count == 0 && (next == sets.Count || RateLimited))
                complete();
        }

        private void finish(RunningDownload download, BulkDownloadOutcome outcome)
        {
            // cancelled along with the batch, or reported twice.
            if (!running.Remove(download))
                return;

            Finished++;

            switch (outcome)
            {
                case BulkDownloadOutcome.Downloaded:
                    Downloaded++;
                    break;

                case BulkDownloadOutcome.Failed:
                    Failed++;
                    break;

                case BulkDownloadOutcome.RateLimited:
                    Failed++;
                    RateLimited = true;
                    break;

                // cancelled on its own, from its card: somebody decided against that one, which is not a failure.
                case BulkDownloadOutcome.Cancelled:
                    break;
            }

            Progressed?.Invoke();
            fill();
        }

        private void complete()
        {
            if (completed)
                return;

            completed = true;

            // what the refusal kept from starting did not download either.
            int neverStarted = sets.Count - next;
            Failed += neverStarted;
            Finished += neverStarted;
            next = sets.Count;

            Completed?.Invoke();
        }

        private class RunningDownload
        {
            public Action? Cancel;
        }
    }

    public enum BulkDownloadOutcome
    {
        Downloaded,
        Failed,

        /// <summary>
        /// Refused for downloading too much.
        /// </summary>
        RateLimited,

        Cancelled,
    }
}
