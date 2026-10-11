// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Pooling;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI.Scrolling;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components
{
    /// <summary>
    /// A grid which displays coloured beat divisor lines in proximity to the selection or placement cursor.
    /// </summary>
    public abstract partial class BeatSnapGrid : CompositeComponent
    {
        private const double visible_range = 750;

        /// <summary>
        /// The range of time values of the current selection.
        /// </summary>
        public (double start, double end)? SelectionTimeRange
        {
            set
            {
                if (value == selectionTimeRange)
                    return;

                selectionTimeRange = value;

                // drawn all the way along at all times, the lines do not depend on it.
                if (AlwaysShownTimeSpan == null)
                    lineCache.Invalidate();
            }
        }

        /// <summary>
        /// How much time is on screen from one end of the playfield to the other, for a grid whose lines are drawn all
        /// the way along it at all times, as stable's editor draws them; null for one that draws them near the selection
        /// or the cursor only.
        /// </summary>
        protected virtual double? AlwaysShownTimeSpan => null;

        /// <summary>
        /// How thick the lines are, in pixels.
        /// </summary>
        public float LineThickness
        {
            get => lineThickness;
            set
            {
                if (value == lineThickness)
                    return;

                lineThickness = value;
                lineCache.Invalidate();
            }
        }

        private float lineThickness = DrawableGridLine.DEFAULT_THICKNESS;

        /// <summary>
        /// How opaque the lines of a grid that is always shown are. One that draws them near the selection or the cursor
        /// fades them by how far from it they are.
        /// </summary>
        public float LineOpacity
        {
            get => lineOpacity;
            set
            {
                if (value == lineOpacity)
                    return;

                lineOpacity = value;
                lineCache.Invalidate();
            }
        }

        private float lineOpacity = 1;

        /// <summary>
        /// The times the lines were last made for when they are always shown, and the time span they were made with.
        /// </summary>
        private (double start, double end, double span)? shown;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        /// <summary>
        /// Makes the lines anew as the playhead nears an end of what they were made for. Making them is a pooled object a
        /// line, which is not for every frame the playhead moves.
        /// </summary>
        private void updateAlwaysShown(double span)
        {
            double now = editorClock.CurrentTime;

            // what is on screen is within the span either side of the playhead.
            if (shown is (double start, double end, double madeWith) && madeWith == span && now - span >= start && now + span <= end)
                return;

            shown = (now - span * 2, now + span * 3, span);
            lineCache.Invalidate();
        }

        [Resolved]
        private EditorBeatmap beatmap { get; set; } = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved]
        private BindableBeatDivisor beatDivisor { get; set; } = null!;

        private readonly List<ScrollingHitObjectContainer> grids = new List<ScrollingHitObjectContainer>();

        private readonly DrawablePool<DrawableGridLine> linesPool = new DrawablePool<DrawableGridLine>(50);

        private readonly Cached lineCache = new Cached();

        private (double start, double end)? selectionTimeRange;

        [BackgroundDependencyLoader]
        private void load(HitObjectComposer composer)
        {
            AddInternal(linesPool);

            foreach (var target in GetTargetContainers(composer))
            {
                var lineContainer = new ScrollingHitObjectContainer();

                grids.Add(lineContainer);
                target.Add(lineContainer);
            }

            beatDivisor.BindValueChanged(_ => createLines(), true);
        }

        protected abstract IEnumerable<Container> GetTargetContainers(HitObjectComposer composer);

        protected override void Update()
        {
            base.Update();

            if (AlwaysShownTimeSpan is double span)
                updateAlwaysShown(span);

            if (!lineCache.IsValid)
            {
                lineCache.Validate();
                createLines();
            }
        }

        private void createLines()
        {
            foreach (var grid in grids)
                grid.Clear();

            bool alwaysShown = AlwaysShownTimeSpan != null;

            (double start, double end)? made = alwaysShown ? shown == null ? null : (shown.Value.start, shown.Value.end) : selectionTimeRange;

            if (made == null)
                return;

            var range = made.Value;

            var timingPoint = beatmap.ControlPointInfo.TimingPointAt(range.start - visible_range);

            double time = timingPoint.Time;
            int beat = 0;

            // progress time until in the visible range.
            while (time < range.start - visible_range)
            {
                time += timingPoint.BeatLength / beatDivisor.Value;
                beat++;
            }

            while (time < range.end + visible_range)
            {
                var nextTimingPoint = beatmap.ControlPointInfo.TimingPointAt(time);

                // switch to the next timing point if we have reached it.
                if (nextTimingPoint.Time > timingPoint.Time)
                {
                    beat = 0;
                    time = nextTimingPoint.Time;
                    timingPoint = nextTimingPoint;
                }

                Color4 colour = BindableBeatDivisor.GetColourFor(
                    BindableBeatDivisor.GetDivisorForBeatIndex(beat, beatDivisor.Value), colours);

                foreach (var grid in grids)
                {
                    var line = linesPool.Get();

                    // kept until it has gone off the far end, which is less than the playfield's length past the playhead.
                    line.ShownFor = alwaysShown ? shown!.Value.span : visible_range;

                    line.Apply(new HitObject
                    {
                        StartTime = time
                    });

                    line.Colour = colour;
                    line.Thickness = lineThickness;

                    // a line taken from the pool is as faded as it last was.
                    if (alwaysShown)
                        line.Alpha = lineOpacity;

                    grid.Add(line);
                }

                beat++;
                time += timingPoint.BeatLength / beatDivisor.Value;
            }

            foreach (var grid in grids)
            {
                // required to update ScrollingHitObjectContainer's cache.
                grid.UpdateSubTree();

                // always shown, none of them fades.
                if (alwaysShown)
                    continue;

                foreach (var line in grid.Objects.OfType<DrawableGridLine>())
                {
                    time = line.HitObject.StartTime;

                    if (time >= range.start && time <= range.end)
                        line.Alpha = 1;
                    else
                    {
                        double timeSeparation = time < range.start ? range.start - time : time - range.end;
                        line.Alpha = (float)Math.Max(0, 1 - timeSeparation / visible_range);
                    }
                }
            }
        }

        private partial class DrawableGridLine : DrawableHitObject
        {
            [Resolved]
            private IScrollingInfo scrollingInfo { get; set; } = null!;

            private readonly IBindable<ScrollingDirection> direction = new Bindable<ScrollingDirection>();

            public const float DEFAULT_THICKNESS = 2;

            /// <summary>
            /// How long after its time the line is kept.
            /// </summary>
            public double ShownFor = visible_range;

            /// <summary>
            /// How thick the line is, in pixels.
            /// </summary>
            public float Thickness
            {
                get => thickness;
                set
                {
                    thickness = value;

                    // which way it is thick depends on the way it scrolls, which is known once its dependencies are in:
                    // a line of the pool has those, and has been made as thick as it was then, before it is first shown.
                    if (LoadState >= LoadState.Ready)
                        applyThickness();
                }
            }

            private float thickness = DEFAULT_THICKNESS;

            private void applyThickness()
            {
                if (RelativeSizeAxes == Axes.Y)
                    Width = thickness;
                else
                    Height = thickness;
            }

            public DrawableGridLine()
                : base(new HitObject())
            {
                AddInternal(new Box { RelativeSizeAxes = Axes.Both });
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                direction.BindTo(scrollingInfo.Direction);
                direction.BindValueChanged(onDirectionChanged, true);
            }

            private void onDirectionChanged(ValueChangedEvent<ScrollingDirection> direction)
            {
                switch (direction.NewValue)
                {
                    case ScrollingDirection.Up:
                        Anchor = Anchor.TopLeft;
                        Origin = Anchor.CentreLeft;
                        break;

                    case ScrollingDirection.Down:
                        Anchor = Anchor.BottomLeft;
                        Origin = Anchor.CentreLeft;
                        break;

                    case ScrollingDirection.Left:
                        Anchor = Anchor.TopLeft;
                        Origin = Anchor.TopCentre;
                        break;

                    case ScrollingDirection.Right:
                        Anchor = Anchor.TopRight;
                        Origin = Anchor.TopCentre;
                        break;
                }

                bool isHorizontal = direction.NewValue == ScrollingDirection.Left || direction.NewValue == ScrollingDirection.Right;

                if (isHorizontal)
                {
                    RelativeSizeAxes = Axes.Y;
                    Width = thickness;
                }
                else
                {
                    RelativeSizeAxes = Axes.X;
                    Height = thickness;
                }
            }

            protected override void UpdateInitialTransforms()
            {
                // don't perform any fading – we are handling that ourselves.
                LifetimeEnd = HitObject.StartTime + ShownFor;
            }
        }
    }
}
