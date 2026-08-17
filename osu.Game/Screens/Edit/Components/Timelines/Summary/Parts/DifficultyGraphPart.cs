// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Logging;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Plots how hard the beatmap is over its length, behind the rest of the summary timeline.
    /// </summary>
    /// <remarks>
    /// The curve adds up what every skill made of each object. Skills are not on a shared scale and their sum is not
    /// a star rating, so the height carries no unit: it is there to show where the dense and quiet parts of a beatmap
    /// are relative to one another.
    /// </remarks>
    public partial class DifficultyGraphPart : CompositeDrawable
    {
        /// <summary>
        /// How long to wait after the last edit before recalculating.
        /// </summary>
        /// <remarks>
        /// A full difficulty calculation is far too expensive to run per keystroke, and the shape barely moves for a
        /// single object anyway.
        /// </remarks>
        private const double recalculate_debounce = 2000;

        /// <summary>
        /// Number of columns the curve is drawn with. Enough to read the shape, few enough to stay cheap.
        /// </summary>
        private const int resolution = 400;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        private BufferedContainer graph = null!;
        private Container<Bar> bars = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        private CancellationTokenSource? calculationCancellation;
        private ScheduledDelegate? scheduledRecalculation;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChild = graph = new BufferedContainer(cachedFrameBuffer: true)
            {
                RelativeSizeAxes = Axes.Both,
                Colour = colourProvider.Highlight1,
                Alpha = 0,
                Child = bars = new Container<Bar> { RelativeSizeAxes = Axes.Both },
            };

            config.BindWith(OsuSetting.EditorShowDifficultyGraph, enabled);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.BeatmapReprocessed += scheduleRecalculation;
            enabled.BindValueChanged(_ => scheduleRecalculation(), true);
        }

        private void scheduleRecalculation()
        {
            scheduledRecalculation?.Cancel();

            if (!enabled.Value)
            {
                graph.FadeOut(200, Easing.OutQuint);
                return;
            }

            scheduledRecalculation = Scheduler.AddDelayed(recalculate, recalculate_debounce);
        }

        private void recalculate()
        {
            calculationCancellation?.Cancel();
            calculationCancellation = new CancellationTokenSource();

            var token = calculationCancellation.Token;
            var rulesetInstance = ruleset.Value.CreateInstance();
            var beatmap = workingBeatmap.Value;

            Task.Run(() =>
            {
                try
                {
                    // no mods: the graph describes the beatmap as written, which is what is being edited.
                    var breakdown = rulesetInstance.CreateDifficultyCalculator(beatmap).CalculateBreakdown([], token);

                    Schedule(() =>
                    {
                        if (!token.IsCancellationRequested)
                            display(breakdown);
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not calculate the difficulty graph");
                }
            }, token);
        }

        private void display(DifficultyBreakdown breakdown)
        {
            bars.Clear();

            double[] difficulties = breakdown.GetCombinedDifficulties();

            if (difficulties.Length == 0)
            {
                graph.FadeOut(200, Easing.OutQuint);
                return;
            }

            double trackLength = workingBeatmap.Value.Track.Length;

            if (trackLength <= 0)
                return;

            // the curve is bucketed by time rather than by object, so a dense burst reads as tall rather than wide.
            double[] buckets = new double[resolution];

            for (int i = 0; i < difficulties.Length; i++)
            {
                int bucket = (int)(breakdown.Times[i] / trackLength * resolution);

                if (bucket < 0 || bucket >= resolution)
                    continue;

                buckets[bucket] = Math.Max(buckets[bucket], difficulties[i]);
            }

            double peak = buckets.Max();

            if (peak <= 0)
            {
                graph.FadeOut(200, Easing.OutQuint);
                return;
            }

            for (int i = 0; i < resolution; i++)
            {
                if (buckets[i] <= 0)
                    continue;

                bars.Add(new Bar
                {
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    X = (float)i / resolution,
                    Width = 1f / resolution,
                    Height = (float)(buckets[i] / peak),
                });
            }

            graph.FadeTo(0.35f, 400, Easing.OutQuint);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            calculationCancellation?.Cancel();

            if (editorBeatmap.IsNotNull())
                editorBeatmap.BeatmapReprocessed -= scheduleRecalculation;
        }
    }
}
