// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Logging;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.UI;
using osu.Game.Scoring;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// Works out every judgement a replay earns, from its start to its end, ahead of it being watched: a replay keeps
    /// only what was pressed where, and what that earned is otherwise only known as it plays.
    /// </summary>
    /// <remarks>
    /// The replay is played through a second playfield of its own, the way it will be watched, so what comes out is what
    /// the game itself judges rather than a copy of its rules. That playfield is never drawn and makes no sound, and it is
    /// run as far as its frame stability allows on every update — a few seconds of play a frame — until it is done.
    /// </remarks>
    [Cached]
    public partial class ReplayJudgementSimulation : CompositeDrawable, ISamplePlaybackDisabler
    {
        /// <summary>
        /// The judgements so far, in the order they were made.
        /// </summary>
        public IReadOnlyList<JudgementResult> Results => results;

        private readonly List<JudgementResult> results = new List<JudgementResult>();

        /// <summary>
        /// Whether the whole replay has been judged.
        /// </summary>
        public bool Finished { get; private set; }

        /// <summary>
        /// What the replay missed, broke the combo on, or hit for less than the most, once it has all been judged.
        /// </summary>
        public IReadOnlyList<ReplayMiss>? Misses { get; private set; }

        /// <summary>
        /// How long working it out took, from being loaded to being finished.
        /// </summary>
        public long ElapsedMilliseconds => stopwatch.ElapsedMilliseconds;

        private readonly Ruleset ruleset;
        private readonly IBeatmap beatmap;
        private readonly IReadOnlyList<Mod> mods;
        private readonly Score score;

        private readonly Stopwatch stopwatch = new Stopwatch();
        private readonly SimulationClock clock = new SimulationClock();

        private DrawableRuleset drawableRuleset = null!;
        private double endTime;

        public ReplayJudgementSimulation(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods, Score score)
        {
            this.ruleset = ruleset;
            this.beatmap = beatmap;
            this.mods = mods;
            this.score = score;

            RelativeSizeAxes = Axes.Both;

            // updated, but never seen: it is only here to be judged. Not masked away instead, which would have been
            // cheaper to draw — whatever is masked away stops having its children updated, and nothing would be judged.
            Alpha = 0;
            AlwaysPresent = true;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            var dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            dependencies.CacheAs<IGameplayClock>(clock);
            dependencies.CacheAs<ISamplePlaybackDisabler>(this);
            return dependencies;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // a copy of each mod, so that nothing done to the ones being watched reaches these, or the other way round.
            drawableRuleset = ruleset.CreateDrawableRulesetWith(beatmap, mods.Select(m => m.DeepClone()).ToArray());
            drawableRuleset.NewResult += results.Add;

            InternalChild = drawableRuleset;

            endTime = beatmap.HitObjects.Count == 0 ? 0 : beatmap.HitObjects.Max(h => h.GetEndTime());
            clock.Time = drawableRuleset.GameplayStartTime;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            drawableRuleset.SetReplayScore(score);
            stopwatch.Start();
        }

        private bool started;

        protected override void Update()
        {
            base.Update();

            if (Finished)
                return;

            // the first update puts the playfield at the start; only after that is it asked to reach the end, which it
            // then works its way towards a frame at a time rather than jumping straight there.
            if (!started)
            {
                started = true;
                return;
            }

            clock.Time = endTime + 1000;

            if (drawableRuleset.FrameStableClock.CurrentTime >= endTime + 1000)
            {
                Misses = ReplayMiss.From(beatmap, results);
                Finished = true;
                stopwatch.Stop();

                Logger.Log($"Judged {endTime:N0} ms of a replay ahead of watching it, in {stopwatch.ElapsedMilliseconds:N0} ms ({results.Count} judgements).");

                // nothing more to judge, and a whole playfield to let go of.
                drawableRuleset.Expire();
            }
        }

        private readonly BindableBool samplePlaybackDisabled = new BindableBool(true);

        IBindable<bool> ISamplePlaybackDisabler.SamplePlaybackDisabled => samplePlaybackDisabled;

        /// <summary>
        /// Stands where the playfield is to go, rather than running with the music.
        /// </summary>
        private class SimulationClock : IGameplayClock
        {
            public double Time;

            public double CurrentTime => Time;

            public double Rate => 1;

            public bool IsRunning => true;

            public double ElapsedFrameTime => 0;

            public double FramesPerSecond => 0;

            public void ProcessFrame()
            {
            }

            public double StartTime => 0;

            public double GameplayStartTime => 0;

            public IAdjustableAudioComponent AdjustmentsFromMods { get; } = new AudioAdjustments();

            public IBindable<bool> IsPaused { get; } = new BindableBool();

            public bool IsRewinding => false;
        }
    }
}
