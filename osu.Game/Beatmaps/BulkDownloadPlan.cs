// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Online.API.Requests.Responses;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// What of a listing still needs downloading, and how much of it was left out and why.
    /// </summary>
    public class BulkDownloadPlan
    {
        /// <summary>
        /// The sets to download, in the listing's order.
        /// </summary>
        public IReadOnlyList<APIBeatmapSet> ToDownload { get; }

        /// <summary>
        /// How many are downloaded already.
        /// </summary>
        public int AlreadyHave { get; }

        /// <summary>
        /// How many cannot be downloaded at all, having been taken down.
        /// </summary>
        public int Unavailable { get; }

        private BulkDownloadPlan(IReadOnlyList<APIBeatmapSet> toDownload, int alreadyHave, int unavailable)
        {
            ToDownload = toDownload;
            AlreadyHave = alreadyHave;
            Unavailable = unavailable;
        }

        /// <param name="listing">Every set in the listing.</param>
        /// <param name="installed">The online IDs of every set downloaded already.</param>
        /// <param name="downloadingAlready">Whether a set is on its way down already, started from somewhere else.</param>
        public static BulkDownloadPlan For(IEnumerable<APIBeatmapSet> listing, IReadOnlySet<int> installed, Func<APIBeatmapSet, bool> downloadingAlready)
        {
            var toDownload = new List<APIBeatmapSet>();
            var seen = new HashSet<int>();
            int alreadyHave = 0;
            int unavailable = 0;

            foreach (var set in listing)
            {
                // a listing paged while it is being written to can hand the same set back twice.
                if (set.OnlineID <= 0 || !seen.Add(set.OnlineID))
                    continue;

                if (installed.Contains(set.OnlineID))
                    alreadyHave++;
                else if (set.Availability.DownloadDisabled)
                    unavailable++;
                // on its way already, with a notification of its own; starting it again would only be refused.
                else if (!downloadingAlready(set))
                    toDownload.Add(set);
            }

            return new BulkDownloadPlan(toDownload, alreadyHave, unavailable);
        }
    }
}
