// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// A curve drawn by hand, in place of picking one from a list.
    /// </summary>
    /// <remarks>
    /// The square reads left to right as the way through the stream and bottom to top as the way along the
    /// slider, so the straight diagonal is an even stream and anything else is not.
    ///
    /// Points are added by clicking where one is wanted and taken away by right-clicking one, so the shape is
    /// as simple or as involved as it needs to be. The two ends cannot be moved or removed.
    /// </remarks>
    public partial class StreamCurveEditor : CompositeDrawable
    {
        /// <summary>
        /// The points the curve goes through, both axes running 0 to 1, not counting the fixed ends.
        /// </summary>
        public readonly BindableList<Vector2> Points = new BindableList<Vector2>();

        private SmoothPath curve = null!;
        private Container<PointHandle> handles = null!;

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Masking = true;
            CornerRadius = 5;

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                curve = new SmoothPath
                {
                    PathRadius = 1.5f,
                    Colour = colours.Yellow,
                },
                handles = new Container<PointHandle> { RelativeSizeAxes = Axes.Both },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // only when there are more or fewer of them. Rebuilding on every change destroyed the handle that
            // was being dragged, which ended the drag a frame after it started — the point barely moved.
            Points.BindCollectionChanged((_, _) =>
            {
                if (handles.Count != Points.Count)
                    rebuildHandles();
            }, true);
        }

        private List<Vector2> drawnFor = new List<Vector2>();
        private Vector2 drawnAt;

        protected override void Update()
        {
            base.Update();

            if (DrawSize == Vector2.Zero)
                return;

            // redrawn when the points move or the box is resized, and not otherwise. Leaving out the first of
            // those is what made dragging a handle do nothing to the line it was dragging.
            if (DrawSize == drawnAt && drawnFor.SequenceEqual(Points))
                return;

            drawnAt = DrawSize;
            drawnFor = Points.ToList();

            var walked = StreamCurve.Walk(Points);

            curve.Vertices = walked.Select(toLocal).ToArray();
            curve.Position = -curve.PositionInBoundingBox(Vector2.Zero);

            foreach (var handle in handles)
                handle.Position = toLocal(Points[handle.Index]);
        }

        private void rebuildHandles()
        {
            handles.Clear();

            for (int i = 0; i < Points.Count; i++)
            {
                handles.Add(new PointHandle(i)
                {
                    Moved = moved,
                    Removed = removed,
                    Colour = colours.Yellow,
                    Position = toLocal(Points[i]),
                });
            }

            drawnFor = new List<Vector2> { new Vector2(float.NaN) };
        }

        private void moved(PointHandle handle, Vector2 screenSpacePosition)
        {
            if (handle.Index >= Points.Count)
                return;

            Points[handle.Index] = fromLocal(ToLocalSpace(screenSpacePosition));
        }

        private void removed(PointHandle handle)
        {
            if (handle.Index < Points.Count)
                Points.RemoveAt(handle.Index);
        }

        private Vector2 toLocal(Vector2 point) => new Vector2(point.X * DrawWidth, (1 - point.Y) * DrawHeight);

        private Vector2 fromLocal(Vector2 point) => new Vector2(
            Math.Clamp(point.X / DrawWidth, 0, 1),
            Math.Clamp(1 - point.Y / DrawHeight, 0, 1));

        protected override bool OnClick(ClickEvent e)
        {
            Points.Add(fromLocal(ToLocalSpace(e.ScreenSpaceMouseDownPosition)));
            return true;
        }

        /// <summary>
        /// One point on the curve, dragged to move it and right-clicked to take it away.
        /// </summary>
        private partial class PointHandle : CircularContainer
        {
            /// <summary>
            /// Which of the points this one is. Held rather than the point's position, which two of them can
            /// share — and does not change under a drag, which the position does.
            /// </summary>
            public readonly int Index;

            public Action<PointHandle, Vector2>? Moved;
            public Action<PointHandle>? Removed;

            public PointHandle(int index)
            {
                Index = index;

                Size = new Vector2(12);
                Origin = Anchor.Centre;
                Masking = true;

                Child = new Box { RelativeSizeAxes = Axes.Both };
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button == MouseButton.Right)
                {
                    Removed?.Invoke(this);
                    return true;
                }

                return true;
            }

            // swallowed so that letting go over a handle is not also read as putting a new point there.
            protected override bool OnClick(ClickEvent e) => true;

            protected override bool OnDragStart(DragStartEvent e) => e.Button == MouseButton.Left;

            protected override void OnDrag(DragEvent e) => Moved?.Invoke(this, e.ScreenSpaceMousePosition);
        }
    }
}
