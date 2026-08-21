// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Database;

namespace osu.Game.Beatmaps
{
    /// <summary>
    /// Finds out who actually authored a difficulty, rather than who owns the set it sits in.
    /// </summary>
    /// <remarks>
    /// A guest difficulty is credited to its own author online, and importing a beatmap records that author's id on the
    /// difficulty — but leaves the name alone, because the name comes from the beatmap file, which carries the set
    /// owner's. So the two disagree: the name reads as the set owner while the profile it links to is the guest's.
    /// <para>
    /// Only a name is missing, and only a lookup can supply it. Nothing here decides whether a difficulty is a guest
    /// one: the recorded id is the author either way, so a difficulty the set owner made resolves to their own name and
    /// nothing appears to change.
    /// </para>
    /// </remarks>
    public partial class DifficultyAuthorLookup : Component
    {
        /// <summary>
        /// Ids at or below this are not a real user: <see cref="Models.RealmUser"/> starts at 1 and stays there until
        /// online metadata says otherwise.
        /// </summary>
        private const int first_real_user_id = 2;

        [Resolved]
        private UserLookupCache users { get; set; } = null!;

        /// <summary>
        /// Calls back with the name the difficulty's author goes by, if it turns out to differ from the one already
        /// shown. Never calls back otherwise, so a caller which has already drawn the beatmap file's name can leave it
        /// where it is.
        /// </summary>
        public void GetAuthorName(BeatmapInfo beatmap, Action<string> onDiffering, CancellationToken cancellationToken)
        {
            int id = beatmap.Metadata.Author.OnlineID;
            string known = beatmap.Metadata.Author.Username;

            if (id < first_real_user_id)
                return;

            users.GetUserAsync(id, cancellationToken).ContinueWith(task =>
            {
                string? found = task.GetResultSafely()?.Username;

                if (string.IsNullOrEmpty(found) || found == known)
                    return;

                Schedule(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                        onDiffering(found);
                });
            }, cancellationToken, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        }
    }
}
