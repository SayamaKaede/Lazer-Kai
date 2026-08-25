// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.Scoring.Legacy;
using osu.Game.Scoring;
using osu.Game.Scoring.Legacy;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// What the difficulty being edited is worth: its star rating, the performance an SS on it would be worth, and
    /// what it would total in stable's scoring. Refreshed on save.
    /// </summary>
    /// <remarks>
    /// None of it follows edits. Star rating is only recalculated as part of saving, and having the three read as of
    /// different moments would be worse than having them all read as of the last save.
    ///
    /// The mods chosen for gameplay test are always applied: what a mapper wants to read is what they are about to
    /// play, whether or not those mods are also being previewed in the composer.
    /// </remarks>
    public partial class EditorDifficultyTracker : Component
    {
        /// <summary>
        /// The current star rating, or null while it is being calculated.
        /// </summary>
        public IBindable<double?> StarRating => starRating;

        private readonly Bindable<double?> starRating = new Bindable<double?>();

        /// <summary>
        /// What an SS with no misses would be worth, or null while it is being calculated.
        /// </summary>
        public IBindable<double?> MaxPerformance => maxPerformance;

        private readonly Bindable<double?> maxPerformance = new Bindable<double?>();

        /// <summary>
        /// The highest total achievable on this build's own scoring, read the way the client is set to display a
        /// score, or null while it is being calculated.
        /// </summary>
        /// <remarks>
        /// Follows the score display setting because the standardised number is a million on every beatmap and says
        /// nothing about this one. Set to classic, it becomes a number that differs from beatmap to beatmap the way
        /// stable's did, which is the one a mapper is asking for.
        /// </remarks>
        public IBindable<long?> MaxScore => maxScore;

        private readonly Bindable<long?> maxScore = new Bindable<long?>();

        /// <summary>
        /// The highest total achievable under stable's scoring, or null while it is being calculated or when the
        /// ruleset has no stable scoring to speak of.
        /// </summary>
        /// <remarks>
        /// Stable's editor put this on its test screen, and it is the number a mapper checks a tournament map
        /// against. It is a different number from what this build would award for the same play.
        /// </remarks>
        public IBindable<long?> MaxLegacyScore => maxLegacyScore;

        private readonly Bindable<long?> maxLegacyScore = new Bindable<long?>();

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private Bindable<bool> applyModsToComposer = null!;

        /// <summary>
        /// Which of the two the client puts on screen, which is what the panel reads out.
        /// </summary>
        private Bindable<ScoringMode> scoreDisplayMode = null!;

        private CancellationTokenSource? calculationCancellation;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            applyModsToComposer = config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer);
            scoreDisplayMode = config.GetBindable<ScoringMode>(OsuSetting.ScoreDisplayMode);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (editor != null)
            {
                editor.Saved += recalculate;
                editor.TestPlayMods.SelectionChanged += onModsChanged;
            }

            applyModsToComposer.BindValueChanged(_ => recalculate());
            scoreDisplayMode.BindValueChanged(_ => recalculate());

            recalculate();
        }

        private ScheduledDelegate? recalculateDebounce;

        /// <summary>
        /// Held off briefly, because a mod's settings change with every frame of a slider drag and each change would
        /// otherwise start a difficulty calculation of its own.
        /// </summary>
        private void onModsChanged()
        {
            recalculateDebounce?.Cancel();
            recalculateDebounce = Scheduler.AddDelayed(recalculate, 200);
        }

        private void recalculate() => Schedule(() =>
        {
            calculationCancellation?.Cancel();
            calculationCancellation = new CancellationTokenSource();

            var mods = editor?.TestPlayMods.Mods.Value.Select(m => m.DeepClone()).ToArray() ?? Array.Empty<Mod>();
            var token = calculationCancellation.Token;

            // the beatmap on disk is what the difficulty cache reads, so this reflects the state as of the last save.
            difficultyCache.GetDifficultyAsync(editorBeatmap.BeatmapInfo, editorBeatmap.BeatmapInfo.Ruleset, mods, token)
                           .ContinueWith(task => Schedule(() =>
                           {
                               if (task.GetResultSafely() is StarDifficulty difficulty)
                               {
                                   starRating.Value = difficulty.Stars;

                                   // the cache works an SS out along the way, so the performance of one is already here.
                                   maxPerformance.Value = difficulty.PerformanceAttributes?.Total;
                               }
                           }), token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);

            recalculateScores(mods, token);
        });

        /// <remarks>
        /// Not part of the difficulty calculation and not cached with it, so it is worked out here. The simulator is
        /// the same one score conversion uses to decide what a beatmap was worth in stable, and the parts are put
        /// together the way conversion puts them together: the mod multiplier applies to the combo-driven portion
        /// alone, which is what stable's formula does.
        /// </remarks>
        private void recalculateScores(Mod[] mods, CancellationToken token)
        {
            var beatmapInfo = editorBeatmap.BeatmapInfo;
            var ruleset = beatmapInfo.Ruleset.CreateInstance();

            Task.Run(() =>
            {
                try
                {
                    var working = beatmaps.GetWorkingBeatmap(beatmapInfo);
                    var playable = working.GetPlayableBeatmap(beatmapInfo.Ruleset, mods, token);

                    long? total = MaximumScoreFor(ruleset, playable, mods, scoreDisplayMode.Value);
                    long? legacy = ruleset is ILegacyRuleset legacyRuleset
                        ? MaximumLegacyScoreFor(legacyRuleset, working, playable, mods)
                        : null;

                    Schedule(() =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        maxScore.Value = total;
                        maxLegacyScore.Value = legacy;
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not work out what this difficulty is worth.");

                    Schedule(() =>
                    {
                        maxScore.Value = null;
                        maxLegacyScore.Value = null;
                    });
                }
            }, token);
        }

        /// <summary>
        /// The highest total <paramref name="playable"/> can be worth on this build's own scoring, read out the way
        /// <paramref name="mode"/> would put it on screen, or <c>null</c> when it holds nothing to score.
        /// </summary>
        /// <remarks>
        /// The standardised maximum is a million on every beatmap and says nothing about this one, so what is worth
        /// reading is what the client would actually display. Classic mode scales that million by how much there is
        /// to hit, which is a number that differs from beatmap to beatmap the way stable's did.
        ///
        /// The score is carried on a <see cref="ScoreInfo"/> rather than read off the processor directly because the
        /// processor's own total is back to nothing once its walk through a perfect play is over.
        /// </remarks>
        public static long? MaximumScoreFor(Ruleset ruleset, IBeatmap playable, IReadOnlyList<Mod> mods, ScoringMode mode)
        {
            if (playable.HitObjects.Count == 0)
                return null;

            var processor = ruleset.CreateScoreProcessor();

            processor.Mods.Value = mods;
            processor.ApplyBeatmap(playable);

            var perfect = new ScoreInfo(playable.BeatmapInfo, ruleset.RulesetInfo)
            {
                TotalScore = processor.MaximumTotalScore,
                MaximumStatistics = new Dictionary<HitResult, int>(processor.MaximumStatistics),
            };

            return perfect.GetDisplayScore(mode);
        }

        /// <summary>
        /// The highest total <paramref name="playable"/> can be worth under stable's scoring, or <c>null</c> when it
        /// holds nothing to score.
        /// </summary>
        /// <remarks>
        /// Put together the way score conversion puts a maximum together: the mod multiplier applies to the
        /// combo-driven portion alone, because stable multiplies the combo bonus and leaves the hit values as they
        /// are. Bonus objects — spinner spins and the like — sit outside both.
        /// </remarks>
        public static long? MaximumLegacyScoreFor(ILegacyRuleset ruleset, IWorkingBeatmap working, IBeatmap playable, IReadOnlyList<Mod> mods)
        {
            if (playable.HitObjects.Count == 0)
                return null;

            var simulator = ruleset.CreateLegacyScoreSimulator();
            var attributes = simulator.Simulate(working, playable);
            double multiplier = simulator.GetLegacyScoreMultiplier(mods, LegacyBeatmapConversionDifficultyInfo.FromBeatmap(playable));

            return attributes.AccuracyScore
                   + (long)Math.Round(attributes.ComboScore * multiplier)
                   + attributes.BonusScore;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellation?.Cancel();
            recalculateDebounce?.Cancel();

            if (editor != null)
            {
                editor.Saved -= recalculate;
                editor.TestPlayMods.SelectionChanged -= onModsChanged;
            }
        }
    }
}
