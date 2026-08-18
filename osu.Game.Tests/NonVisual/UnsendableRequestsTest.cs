// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;
using osu.Game.Scoring;
using osu.Game.Screens.Edit.Submission;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class UnsendableRequestsTest
    {
        private static readonly object[] refused =
        {
            new CreateSoloScoreRequest(new BeatmapInfo(), 0, "hash"),
            new CreateRoomScoreRequest(1, 1, new BeatmapInfo(), 0, "hash"),
            new SubmitSoloScoreRequest(new ScoreInfo(), 1, 1),
            PutBeatmapSetRequest.CreateNew(1, new BeatmapSubmissionSettings()),
            new ReplaceBeatmapPackageRequest(1, Array.Empty<byte>()),
            new PatchBeatmapPackageRequest(1),
        };

        private static readonly object[] allowed =
        {
            new GetUserRequest(),
            new GetBeatmapSetRequest(1),
            new GetBeatmapRequest(1),
        };

        [TestCaseSource(nameof(refused))]
        public void TestSubmissionsRefused(APIRequest request)
            => Assert.That(UnsendableRequests.IsUnsendable(request), Is.True, $"{request.GetType().Name} would submit a score or upload a beatmap");

        [TestCaseSource(nameof(allowed))]
        public void TestReadsAllowed(APIRequest request)
            => Assert.That(UnsendableRequests.IsUnsendable(request), Is.False, $"{request.GetType().Name} only reads, and signing in should still work normally");
    }
}
