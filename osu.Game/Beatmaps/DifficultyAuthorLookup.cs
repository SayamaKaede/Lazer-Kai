// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Database;
using osu.Game.Online;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Users;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Finds out who actually authored a difficulty, rather than who owns the set it sits in.
    /// </summary>
    /// <remarks>
    /// A guest difficulty is credited to its own authors online, and a collaboration has several of them. None of that
    /// survives importing: a beatmap file carries one creator name, the set owner's, and the import records only the
    /// difficulty's author id alongside it — which is why the name reads as the set owner while the profile it links to
    /// is the guest's.
    /// <para>
    /// The authors, names included, come back with the beatmap itself, so one lookup answers the whole question.
    /// Nothing here decides whether a difficulty is a guest one: the set owner's own difficulties come back credited to
    /// them, and the caller sees the name it already had.
    /// </para>
    /// </remarks>
    public partial class DifficultyAuthorLookup : Component
    {
        /// <summary>
        /// The user an owner refers to, in the shape opening a profile expects.
        /// </summary>
        /// <remarks>
        /// <see cref="LinkAction.OpenUserProfile"/> casts its argument to <see cref="IUser"/>, and an id on its own
        /// throws there rather than failing to compile.
        /// </remarks>
        public static APIUser AsUser(APIBeatmap.BeatmapOwner owner) => new APIUser { Id = owner.Id, Username = owner.Username };

        [Resolved]
        private BeatmapLookupCache beatmaps { get; set; } = null!;

        /// <summary>
        /// Calls back with everyone credited with this difficulty, once they are known. Never calls back for a beatmap
        /// which has no online counterpart, or when the lookup cannot be made, so a caller which has already drawn the
        /// beatmap file's name can leave it where it is.
        /// </summary>
        public void GetAuthors(BeatmapInfo beatmap, Action<APIBeatmap.BeatmapOwner[]> onFound, CancellationToken cancellationToken)
        {
            int id = beatmap.OnlineID;

            if (id <= 0)
                return;

            beatmaps.GetBeatmapAsync(id, cancellationToken).ContinueWith(task =>
            {
                var owners = task.GetResultSafely()?.BeatmapOwners;

                if (owners == null || owners.Length == 0)
                    return;

                Schedule(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                        onFound(owners);
                });
            }, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }
}
