// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// The <see cref="LineVolumePointPiece"/>s inside one slider or spinner: shown while it is selected and its green lines
    /// can be edited, one on every tick of the beat divisor that has no sound point of its own.
    /// </summary>
    public partial class LineVolumeTicks : CompositeDrawable
    {
        /// <summary>
        /// The most ticks one object is given, so that a long spinner at a fine divisor does not fill the timeline.
        /// </summary>
        private const int max_ticks = 256;

        private readonly HitObject hitObject;
        private readonly Func<bool> isSelected;

        /// <summary>
        /// Whether the points go above the object, as the sound points do in stable's layout.
        /// </summary>
        public bool Above { get; set; }

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private BindableBeatDivisor beatDivisor { get; set; } = null!;

        [Resolved]
        private Editor? editor { get; set; }

        /// <summary>
        /// What the ticks were last made for, so that they are only made again when that changes.
        /// </summary>
        private string? madeFor;

        public LineVolumeTicks(HitObject hitObject, Func<bool> isSelected)
        {
            this.hitObject = hitObject;
            this.isSelected = isSelected;

            RelativeSizeAxes = Axes.Both;
        }

        protected override void Update()
        {
            base.Update();

            bool shown = isSelected() && editor?.KeptStableVersion.Value != null && beatmap.BeatmapInfo.Ruleset.OnlineID == 0;
            string key = shown ? FormattableString.Invariant($"{hitObject.StartTime}|{hitObject.GetEndTime()}|{beatDivisor.Value}|{Above}") : string.Empty;

            if (key == madeFor)
                return;

            madeFor = key;
            ClearInternal();

            if (!shown)
                return;

            double start = hitObject.StartTime;
            double duration = hitObject.GetEndTime() - start;

            foreach (double time in ticks())
            {
                AddInternal(new LineVolumePointPiece(hitObject, time)
                {
                    RelativePositionAxes = Axes.X,
                    X = (float)((time - start) / duration),
                    Anchor = Above ? Anchor.TopLeft : Anchor.BottomLeft,
                    Origin = Above ? Anchor.BottomCentre : Anchor.TopCentre,
                });
            }
        }

        /// <summary>
        /// Every tick of the beat divisor strictly inside the object that no sound point stands on already.
        /// </summary>
        private IEnumerable<double> ticks()
        {
            double start = hitObject.StartTime;
            double end = hitObject.GetEndTime();

            var timingPoint = beatmap.ControlPointInfo.TimingPointAt(start);
            double step = timingPoint.BeatLength / beatDivisor.Value;

            if (step <= 0 || end <= start)
                yield break;

            var taken = new List<double>();

            foreach (var point in SamplePointSelection.PointsOf(hitObject))
                taken.Add(SamplePointSelection.TimeOf(point.hitObject, point.point, beatmap));

            int count = 0;

            for (double time = timingPoint.Time + (Math.Floor((start - timingPoint.Time) / step + 0.001) + 1) * step; time < end - 1 && count < max_ticks; time += step)
            {
                if (taken.Exists(t => Math.Abs(t - time) < 1))
                    continue;

                count++;
                yield return time;
            }
        }
    }
}
