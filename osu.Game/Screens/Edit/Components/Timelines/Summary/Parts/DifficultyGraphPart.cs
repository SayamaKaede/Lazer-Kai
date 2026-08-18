// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
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
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Skinning;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osuTK.Graphics;

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

        private Container graph = null!;
        private Color4 defaultColour;
        private Container<Box> bars = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();
        private readonly Bindable<EditorSelectionColour> colour = new Bindable<EditorSelectionColour>();
        private readonly Bindable<float> opacity = new Bindable<float>();

        /// <summary>
        /// Whether the graph currently has anything to show, so appearance changes do not reveal an empty one.
        /// </summary>
        private bool hasContent;

        private CancellationTokenSource? calculationCancellation;
        private ScheduledDelegate? scheduledRecalculation;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            defaultColour = colourProvider.Highlight1;

            InternalChild = graph = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                Child = bars = new Container<Box> { RelativeSizeAxes = Axes.Both },
            };

            config.BindWith(OsuSetting.EditorShowDifficultyGraph, enabled);
            config.BindWith(OsuSetting.EditorDifficultyGraphColour, colour);
            config.BindWith(OsuSetting.EditorDifficultyGraphOpacity, opacity);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.BeatmapReprocessed += scheduleRecalculation;
            enabled.BindValueChanged(_ => RecalculateNow(), true);

            colour.BindValueChanged(_ => updateAppearance());
            opacity.BindValueChanged(_ => updateAppearance(), true);
        }

        /// <summary>
        /// Recalculates after a pause, so a burst of edits costs one pass rather than one each.
        /// </summary>
        private void scheduleRecalculation()
        {
            scheduledRecalculation?.Cancel();

            if (!enabled.Value)
            {
                hasContent = false;

                updateAppearance();
                return;
            }

            scheduledRecalculation = Scheduler.AddDelayed(recalculate, recalculate_debounce);
        }

        /// <summary>
        /// Recalculates now.
        /// </summary>
        /// <remarks>
        /// Used when opening a beatmap and on save. Neither is a moment worth making the user wait through: opening
        /// has nothing to coalesce, and a save is a deliberate stop.
        /// </remarks>
        public void RecalculateNow()
        {
            scheduledRecalculation?.Cancel();

            if (!enabled.Value)
                return;

            recalculate();
        }

        private void recalculate()
        {
            calculationCancellation?.Cancel();
            calculationCancellation = new CancellationTokenSource();

            var token = calculationCancellation.Token;
            var rulesetInstance = ruleset.Value.CreateInstance();
            // taken from the editor rather than the working beatmap: the latter re-reads what is on disk, so the curve
            // would be missing everything placed or deleted since the beatmap was opened.
            var beatmap = new EditorStateWorkingBeatmap(editorBeatmap.PlayableBeatmap);

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
                hasContent = false;

                updateAppearance();
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
                hasContent = false;

                updateAppearance();
                return;
            }

            for (int i = 0; i < resolution; i++)
            {
                if (buckets[i] <= 0)
                    continue;

                bars.Add(new Box
                {
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    X = (float)i / resolution,
                    // slightly wider than its slot so neighbours meet and the curve reads as one shape.
                    Width = 1.5f / resolution,
                    Height = (float)(buckets[i] / peak),
                });
            }

            hasContent = true;
            updateAppearance();
        }

        private void updateAppearance()
        {
            graph.Colour = colour.Value.ToColour() ?? defaultColour;
            graph.FadeTo(hasContent ? opacity.Value : 0, 400, Easing.OutQuint);
        }

        /// <summary>
        /// Hands the difficulty calculator the beatmap as it currently stands in the editor.
        /// </summary>
        /// <remarks>
        /// The objects are copied into a list of their own first, because the calculation runs off the update thread
        /// and the editor is free to keep changing its own list while it does.
        /// </remarks>
        // this exists for the same reason as the identical wrapper in `PerformancePointsCounter`: a difficulty
        // calculator can only be handed an `IWorkingBeatmap`, never an `IBeatmap`.
        private class EditorStateWorkingBeatmap : WorkingBeatmap
        {
            private readonly IBeatmap snapshot;

            public EditorStateWorkingBeatmap(IBeatmap editorState)
                : base(editorState.BeatmapInfo, null)
            {
                var copy = new Beatmap
                {
                    BeatmapInfo = editorState.BeatmapInfo,
                    ControlPointInfo = editorState.ControlPointInfo.DeepClone(),
                };

                copy.HitObjects.AddRange(editorState.HitObjects);
                snapshot = copy;
            }

            public override IBeatmap GetPlayableBeatmap(IRulesetInfo ruleset, IReadOnlyList<Mod> mods, CancellationToken cancellationToken) => snapshot;

            protected override IBeatmap GetBeatmap() => snapshot;

            public override Texture GetBackground() => throw new NotImplementedException();

            protected override Track GetBeatmapTrack() => throw new NotImplementedException();

            protected internal override ISkin GetSkin() => throw new NotImplementedException();

            public override Stream GetStream(string storagePath) => throw new NotImplementedException();
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
