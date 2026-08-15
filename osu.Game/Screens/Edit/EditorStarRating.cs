// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Tracks the star rating of the difficulty being edited, refreshed on save.
    /// </summary>
    /// <remarks>
    /// Star rating is only recalculated as part of saving, so this deliberately does not follow edits.
    /// When gameplay test mods are being previewed in the composer, the rating is calculated with those mods applied,
    /// matching what the composer is showing.
    /// </remarks>
    public partial class EditorStarRating : Component
    {
        /// <summary>
        /// The current star rating, or null while it is being calculated.
        /// </summary>
        public IBindable<double?> Current => current;

        private readonly Bindable<double?> current = new Bindable<double?>();

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private Bindable<bool> applyModsToComposer = null!;

        private CancellationTokenSource? calculationCancellation;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            applyModsToComposer = config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (editor != null)
                editor.Saved += recalculate;

            applyModsToComposer.BindValueChanged(_ => recalculate());

            if (editor != null)
                editor.TestPlayMods.Mods.BindValueChanged(_ => recalculate());

            recalculate();
        }

        private void recalculate() => Schedule(() =>
        {
            calculationCancellation?.Cancel();
            calculationCancellation = new CancellationTokenSource();

            var mods = applyModsToComposer.Value && editor != null
                ? editor.TestPlayMods.Mods.Value.Select(m => m.DeepClone()).ToArray()
                : Array.Empty<Mod>();

            // the beatmap on disk is what the difficulty cache reads, so this reflects the state as of the last save.
            difficultyCache.GetDifficultyAsync(editorBeatmap.BeatmapInfo, editorBeatmap.BeatmapInfo.Ruleset, mods, calculationCancellation.Token)
                           .ContinueWith(task => Schedule(() =>
                           {
                               if (task.GetResultSafely() is StarDifficulty difficulty)
                                   current.Value = difficulty.Stars;
                           }), calculationCancellation.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);
        });

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellation?.Cancel();

            if (editor != null)
                editor.Saved -= recalculate;
        }
    }
}
