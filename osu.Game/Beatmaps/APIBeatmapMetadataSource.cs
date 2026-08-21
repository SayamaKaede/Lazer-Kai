// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net;
using System.Diagnostics;
using osu.Game.Database;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Performs online metadata lookups using the osu-web API.
    /// </summary>
    public class APIBeatmapMetadataSource : IOnlineBeatmapMetadataSource
    {
        private readonly IAPIProvider api;

        public APIBeatmapMetadataSource(IAPIProvider api)
        {
            this.api = api;
        }

        public bool Available => api.State.Value == APIState.Online;

        public bool TryLookup(BeatmapInfo beatmapInfo, out OnlineBeatmapMetadata? onlineMetadata)
        {
            if (!Available)
            {
                onlineMetadata = null;
                return false;
            }

            Debug.Assert(beatmapInfo.BeatmapSet != null);

            var req = new BeatmapLookupRequest(md5Hash: beatmapInfo.MD5Hash, filename: beatmapInfo.Path);

            try
            {
                // intentionally blocking to limit web request concurrency
                api.Perform(req);

                if (req.CompletionState == APIRequestCompletionState.Failed)
                {
                    onlineMetadata = null;

                    // a request which failed for any other reason did not establish that this beatmap is unknown
                    // online — the server was simply unable to say. Reporting a lookup regardless has the caller
                    // throw the beatmap's online identity away over a passing outage, and it does not come back:
                    // nothing looks the beatmap up again, so it stays unranked and leaderboard-less until it is
                    // deleted and downloaded afresh.
                    if (!FailureMeansNotOnline(req.ResponseStatusCode))
                    {
                        logForModel(beatmapInfo.BeatmapSet, $@"Online retrieval could not be completed for {beatmapInfo} ({req.ResponseStatusCode?.ToString() ?? @"no response"})");
                        return false;
                    }

                    logForModel(beatmapInfo.BeatmapSet, $@"Online retrieval failed for {beatmapInfo}");
                    return true;
                }

                var res = req.Response;

                if (res != null)
                {
                    logForModel(beatmapInfo.BeatmapSet, $@"Online retrieval mapped {beatmapInfo} to {res.OnlineBeatmapSetID} / {res.OnlineID}.");

                    onlineMetadata = new OnlineBeatmapMetadata
                    {
                        BeatmapID = res.OnlineID,
                        BeatmapSetID = res.OnlineBeatmapSetID,
                        AuthorID = res.AuthorID,
                        BeatmapStatus = res.Status,
                        BeatmapSetStatus = res.BeatmapSet?.Status,
                        DateRanked = res.BeatmapSet?.Ranked,
                        DateSubmitted = res.BeatmapSet?.Submitted,
                        MD5Hash = res.MD5Hash,
                        LastUpdated = res.LastUpdated,
                        // Tags are not populated because the response does not contain tag data.
                        // TODO: consider web change to include the tag data? or a second web request for the set to retrieve tags?
                    };
                    return true;
                }
            }
            catch (Exception e)
            {
                logForModel(beatmapInfo.BeatmapSet, $@"Online retrieval failed for {beatmapInfo} ({e.Message})");
                onlineMetadata = null;
                return false;
            }

            onlineMetadata = null;
            return false;
        }

        /// <summary>
        /// Whether a failed lookup answered the question — that this beatmap is not one the server knows.
        /// </summary>
        internal static bool FailureMeansNotOnline(HttpStatusCode? status) => status == HttpStatusCode.NotFound;

        /// <summary>
        /// Exists only to read the status the server replied with, which the request keeps to itself.
        /// </summary>
        private class BeatmapLookupRequest : GetBeatmapRequest
        {
            public HttpStatusCode? ResponseStatusCode => WebRequest?.ResponseStatusCode;

            public BeatmapLookupRequest(string? md5Hash, string? filename)
                : base(md5Hash: md5Hash, filename: filename)
            {
            }
        }

        private void logForModel(BeatmapSetInfo set, string message) =>
            RealmArchiveModelImporter<BeatmapSetInfo>.LogForModel(set, $@"[{nameof(APIBeatmapMetadataSource)}] {message}");

        public void Dispose()
        {
        }
    }
}
