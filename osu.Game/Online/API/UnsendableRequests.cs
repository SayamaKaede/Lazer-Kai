// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Online.API.Requests;
using osu.Game.Online.Rooms;
using osu.Game.Online.Solo;

namespace osu.Game.Online.API
{
    /// <summary>
    /// The requests this build will not send, whether or not the user is signed in.
    /// </summary>
    /// <remarks>
    /// Signing in is allowed, so everything which only reads from osu! — rankings, profiles, beatmap listings and
    /// downloads, comments — works as it does in the official client. What is never sent is anything which writes a
    /// play or a beatmap under the user's name:
    /// <list type="bullet">
    /// <item>
    /// Scores. This is not an official build, and the server would reject its scores anyway: the token request carries
    /// the executable's hash, which is only recognised for builds osu! published itself. Refusing here means the game
    /// does not even try.
    /// </item>
    /// <item>
    /// Beatmap uploads. This build's beatmap encoder is not the official one — it deliberately writes control points
    /// the official encoder drops — so what it would upload is not what osu! would have written.
    /// </item>
    /// </list>
    /// Checked where every request passes through rather than at each screen, so a path added upstream later cannot
    /// quietly open a way around it.
    /// </remarks>
    public static class UnsendableRequests
    {
        public static bool IsUnsendable(APIRequest request) =>
            request is CreateSoloScoreRequest // the token a play must hold before a score may be submitted at all
                    or CreateRoomScoreRequest
                    or SubmitScoreRequest // the score itself, solo or in a room
                    or PutBeatmapSetRequest // creating or updating a beatmap set online
                    or ReplaceBeatmapPackageRequest
                    or PatchBeatmapPackageRequest;

        public const string REASON = @"This build does not submit scores or upload beatmaps.";
    }
}
