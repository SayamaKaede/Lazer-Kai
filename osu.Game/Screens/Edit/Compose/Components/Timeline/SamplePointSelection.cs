// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// Which of the timeline's sound points are selected, so that a change made through one of them goes to all of them.
    /// </summary>
    /// <remarks>
    /// A drag box selects the points inside it, which can be part of a slider: its head and body but not its tail. Any
    /// other way of selecting — clicking an object, selecting all — selects every point of the objects selected.
    ///
    /// A point is an object's own sounds (<see cref="Body"/>), or one of its nodes by index.
    /// </remarks>
    public partial class SamplePointSelection : Component
    {
        /// <summary>
        /// The index standing for an object's own sounds, rather than one of its nodes'.
        /// </summary>
        public const int Body = -1;

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        /// <summary>
        /// The points the last drag box took in; null when the selection was made some other way.
        /// </summary>
        private HashSet<(HitObject hitObject, int point)>? boxed;

        private bool selectingFromBox;

        /// <summary>
        /// Fired whenever which points are selected may have changed.
        /// </summary>
        public event Action? Changed;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            beatmap.SelectedHitObjects.BindCollectionChanged((_, _) =>
            {
                // a selection made any other way is of whole objects again.
                if (!selectingFromBox)
                    boxed = null;

                Changed?.Invoke();
            });
        }

        /// <summary>
        /// Selects the points a drag box took in, alongside the objects it selects.
        /// </summary>
        /// <param name="points">The points in the box.</param>
        /// <param name="selectObjects">Selects the objects the box does, which is not to undo the points'.</param>
        public void SelectFromBox(IEnumerable<(HitObject hitObject, int point)> points, Action selectObjects)
        {
            selectingFromBox = true;

            try
            {
                selectObjects();
                boxed = points.ToHashSet();
            }
            finally
            {
                selectingFromBox = false;
            }

            Changed?.Invoke();
        }

        /// <summary>
        /// Selects a point on its own, or adds it to the points selected or takes it out of them, with the objects they
        /// belong to.
        /// </summary>
        /// <param name="hitObject">The object the point belongs to.</param>
        /// <param name="point">The point.</param>
        /// <param name="add">Whether to add it to the points selected, or take it out if it is already one of them.</param>
        public void SelectPoint(HitObject hitObject, int point, bool add) => SelectPoint(hitObject, point, add, All);

        /// <param name="hitObject">The object the point belongs to.</param>
        /// <param name="point">The point.</param>
        /// <param name="add">Whether to add it to <paramref name="selected"/> or take it out, rather than select it alone.</param>
        /// <param name="selected">The points selected before: a click on an object selects all of it as the mouse goes
        /// down, before it is known to be a click.</param>
        public void SelectPoint(HitObject hitObject, int point, bool add, IEnumerable<(HitObject hitObject, int point)> selected)
        {
            var points = add ? selected.ToHashSet() : new HashSet<(HitObject hitObject, int point)>();

            if (!points.Remove((hitObject, point)))
                points.Add((hitObject, point));

            SelectPoints(points);
        }

        /// <summary>
        /// Selects <paramref name="points"/> and nothing else, with the objects they belong to.
        /// </summary>
        public void SelectPoints(IEnumerable<(HitObject hitObject, int point)> points)
        {
            var selecting = points.ToHashSet();

            SelectFromBox(selecting, () =>
            {
                var objects = selecting.Select(p => p.hitObject).Distinct().ToList();

                beatmap.SelectedHitObjects.RemoveAll(h => !objects.Contains(h));
                beatmap.SelectedHitObjects.AddRange(objects.Except(beatmap.SelectedHitObjects).ToList());
            });
        }

        public bool Contains(HitObject hitObject, int point) => boxed?.Contains((hitObject, point)) ?? beatmap.SelectedHitObjects.Contains(hitObject);

        /// <summary>
        /// Every selected point.
        /// </summary>
        public IEnumerable<(HitObject hitObject, int point)> All => boxed ?? beatmap.SelectedHitObjects.SelectMany(PointsOf);

        /// <summary>
        /// Every point an object has: its own sounds, and each of its nodes'.
        /// </summary>
        public static IEnumerable<(HitObject hitObject, int point)> PointsOf(HitObject hitObject)
        {
            yield return (hitObject, Body);

            if (hitObject is IHasRepeats repeats)
            {
                for (int node = 0; node < repeats.NodeSamples.Count; node++)
                    yield return (hitObject, node);
            }
        }

        /// <summary>
        /// The sounds a point stands for.
        /// </summary>
        public static IList<HitSampleInfo> SamplesOf(HitObject hitObject, int point) =>
            point != Body && hitObject is IHasRepeats repeats && point < repeats.NodeSamples.Count ? repeats.NodeSamples[point] : hitObject.Samples;

        /// <summary>
        /// Where a point is shown on the timeline: a node where it is, an object's own sounds halfway through its first
        /// span if it has repeats, at its end if it sounds there, and otherwise at its start.
        /// </summary>
        public static double TimeOf(HitObject hitObject, int point, IBeatmap beatmap)
        {
            if (hitObject is IHasRepeats repeats)
            {
                double span = repeats.Duration / repeats.SpanCount();
                return point == Body ? hitObject.StartTime + span / 2 : hitObject.StartTime + point * span;
            }

            return SamplePointPiece.SoundsAtItsEnd(hitObject, beatmap) ? hitObject.GetEndTime() : hitObject.StartTime;
        }
    }
}
