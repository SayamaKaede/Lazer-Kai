// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Game.Beatmaps;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Another difficulty from the same set, drawn faintly underneath the one being edited.
    /// </summary>
    /// <remarks>
    /// Building a spread means constantly asking what the difficulty above or below does at this point in the song,
    /// which otherwise costs leaving the editor and coming back. Which difficulty this is belongs to the beatmap being
    /// worked on rather than to the game, so it is chosen from the editor's own menu and not from the settings.
    /// </remarks>
    [Cached]
    public partial class EditorReferenceDifficulty : Component
    {
        /// <summary>
        /// The difficulty being referenced, by id. <see cref="Guid.Empty"/> for none.
        /// </summary>
        public readonly Bindable<Guid> Selected = new Bindable<Guid>();

        /// <summary>
        /// The referenced difficulty in playable form, or <c>null</c> while there is none or it is still loading.
        /// </summary>
        /// <remarks>
        /// Kept apart from <see cref="Selected"/> on purpose: what is drawn is decided by this, and loading is only
        /// what usually fills it in.
        /// </remarks>
        public readonly Bindable<IBeatmap?> Beatmap = new Bindable<IBeatmap?>();

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private CancellationTokenSource? loadCancellation;

        /// <summary>
        /// The difficulties that can be shown, easiest first.
        /// </summary>
        /// <remarks>
        /// A converted difficulty is not what that difficulty looks like, so only ones sharing this beatmap's ruleset
        /// are offered. Ordered by star rating because that is the order a spread is thought about.
        /// </remarks>
        public BeatmapInfo[] Available => editorBeatmap.BeatmapInfo.BeatmapSet?.Beatmaps
                                                       .Where(b => !b.Equals(editorBeatmap.BeatmapInfo) && b.Ruleset.Equals(editorBeatmap.BeatmapInfo.Ruleset))
                                                       .OrderBy(b => b.StarRating)
                                                       .ToArray()
                                          ?? Array.Empty<BeatmapInfo>();

        /// <summary>
        /// The difficulty last shown, so that turning the ghost off and on again brings back the same one.
        /// </summary>
        private Guid lastShown;

        /// <summary>
        /// Shows the ghost, or puts it away.
        /// </summary>
        public void Toggle()
        {
            if (Selected.Value != Guid.Empty)
            {
                lastShown = Selected.Value;
                Selected.Value = Guid.Empty;
                return;
            }

            var available = Available;

            if (available.Length == 0)
                return;

            Selected.Value = available.Any(b => b.ID == lastShown) ? lastShown : available[0].ID;
        }

        /// <summary>
        /// Moves to the next difficulty in <see cref="Available"/>, or the previous one for a negative
        /// <paramref name="direction"/>.
        /// </summary>
        /// <remarks>
        /// Nothing here selects <see cref="Guid.Empty"/>: putting the ghost away belongs to <see cref="Toggle"/>, so
        /// stepping never lands on an empty position on the way past. From nothing shown, stepping starts at whichever
        /// end was stepped towards.
        /// </remarks>
        public void Cycle(int direction)
        {
            var available = Available;

            if (available.Length == 0)
                return;

            int current = Array.FindIndex(available, b => b.ID == Selected.Value);

            int next = current < 0
                ? direction > 0 ? 0 : available.Length - 1
                : (current + direction + available.Length) % available.Length;

            Selected.Value = available[next].ID;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Selected.BindValueChanged(_ => load(), true);
        }

        private void load()
        {
            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
            loadCancellation = null;

            Beatmap.Value = null;

            var info = editorBeatmap.BeatmapInfo.BeatmapSet?.Beatmaps.FirstOrDefault(b => b.ID == Selected.Value);

            if (info == null)
                return;

            loadCancellation = new CancellationTokenSource();

            var token = loadCancellation.Token;
            var ruleset = editorBeatmap.BeatmapInfo.Ruleset;

            // converting a difficulty is slow enough to be felt, and the editor should not sit still for it.
            Task.Run(() =>
            {
                try
                {
                    var playable = beatmaps.GetWorkingBeatmap(info).GetPlayableBeatmap(ruleset);

                    Schedule(() =>
                    {
                        if (!token.IsCancellationRequested)
                            Beatmap.Value = playable;
                    });
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Could not load {info.DifficultyName} to show underneath this one.");
                }
            }, token);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            loadCancellation?.Cancel();
            loadCancellation?.Dispose();
        }
    }
}
