// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Edit.Blueprints;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Utils;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class OsuSelectionHandler : EditorSelectionHandler
    {
        [Resolved]
        private OsuGridToolboxGroup gridToolbox { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private OsuHitObjectComposer? composer { get; set; }

        [Resolved(CanBeNull = true)]
        private StreamConversionPanel? streamPanel { get; set; }

        /// <summary>
        /// Puts the selection on whole coordinates, as one piece.
        /// </summary>
        /// <remarks>
        /// Rounding each object on its own would pull a pattern apart, since each moves by its own fraction, so the
        /// whole selection is moved by what the first object needs.
        ///
        /// What a snap decided is rounded too (2026-10-05). The fraction left on a slider's end used to stay, and was
        /// written as a whole number anyway, so stable read the stack from somewhere other than where it was made.
        /// Rounding moves it under a pixel, well inside the three that keep it on the stack.
        ///
        /// It is done on every step of a drag (2026-10-05). Until then it waited for the drag to be over, so a circle
        /// held on a slider's end moved when let go: on 2026-09-21 rounding every step had made one shake, held there.
        /// That came from the dragged object being measured where it is drawn, a stack's worth off where it is
        /// stored, which <see cref="OsuBlueprintContainer"/> no longer does; each step now lands on the same place.
        /// </remarks>
        /// <param name="snappedPoint">The point a snap brought onto what it was aimed at, if one did.</param>
        internal void RoundPositionsIfAsked(SnappedPoint? snappedPoint = null)
        {
            if (composer?.KeepsWholeCoordinates != true)
                return;

            var hitObjects = selectedMovableObjects;

            if (hitObjects.Length == 0)
                return;

            Vector2 delta;

            if (snappedPoint is SnappedPoint snapped && hitObjects.Contains(snapped.HitObject))
            {
                // the point that was snapped, rather than the object's position, keeps the whole number it was put
                // on (2026-10-05). A slider brought by its end onto a circle had its head cut on its own, which left
                // the end under a pixel short of the circle; the circle, snapped back onto that end, was cut a pixel
                // over, and going back and forth walked the two a pixel at a time. With the head placed so that the
                // end cuts to the circle's own whole number, the circle snapped back onto it stays where it is.
                Vector2 position = composer.RoundPositionIfAsked(snapped.HitObject.Position + snapped.Offset) - wholePartOf(snapped.Offset);
                delta = position - snapped.HitObject.Position;
            }
            else
                delta = composer.RoundPositionIfAsked(hitObjects[0].Position) - hitObjects[0].Position;

            if (delta == Vector2.Zero)
                return;

            foreach (var h in hitObjects)
                h.Position += delta;

            Scheduler.AddOnce(OsuBeatmapProcessor.ApplyStacking, EditorBeatmap);
        }

        /// <summary>
        /// The whole number below each coordinate, taking one a hair under a whole number as that number, as
        /// <see cref="OsuHitObjectComposer.RoundPositionIfAsked"/> does.
        /// </summary>
        private static Vector2 wholePartOf(Vector2 offset)
        {
            static float wholePart(float value)
            {
                float nearest = MathF.Round(value);
                return MathF.Abs(value - nearest) < 0.001f ? nearest : MathF.Floor(value);
            }

            return new Vector2(wholePart(offset.X), wholePart(offset.Y));
        }

        /// <summary>
        /// The point a snap brought onto what it was aimed at in the movement about to be handled: the object it is
        /// on, and how far it is from that object's position -- nothing for a circle, the path's end for a slider
        /// brought by its end. Set by <see cref="OsuBlueprintContainer"/> and used up by that movement.
        /// </summary>
        internal SnappedPoint? NextMovementSnappedPoint { get; set; }

        internal readonly record struct SnappedPoint(OsuHitObject HitObject, Vector2 Offset);

        private readonly Bindable<bool> allowOutsidePlayArea = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorAllowOutsidePlayArea, allowOutsidePlayArea);
        }

        /// <summary>
        /// The outlines of the selected objects that have no blueprint, which the selection box covers as well.
        /// </summary>
        internal SelectedObjectOutlines? Outlines { get; init; }

        protected override IEnumerable<Quad> GetSelectionQuadsWithoutBlueprints() => Outlines?.SelectionQuads ?? base.GetSelectionQuadsWithoutBlueprints();

        protected override void OnSelectionChanged()
        {
            base.OnSelectionChanged();

            Quad quad = selectedMovableObjects.Length > 0 ? GeometryUtils.GetSurroundingQuad(selectedMovableObjects) : new Quad();

            SelectionBox.CanFlipX = quad.Width > 0;
            SelectionBox.CanFlipY = quad.Height > 0;
            SelectionBox.CanReverse = EditorBeatmap.SelectedHitObjects.Count > 1 || EditorBeatmap.SelectedHitObjects.Any(s => s is Slider);
        }

        private bool nudgeMovementActive;

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == Key.M && e.ControlPressed && e.ShiftPressed)
            {
                mergeSelection();
                return true;
            }

            // Until the keys below are global actions, this will prevent conflicts with "seek between sample points"
            // which has a default of ctrl+shift+arrows.
            if (e.ShiftPressed)
                return false;

            if (e.ControlPressed)
            {
                switch (e.Key)
                {
                    case Key.Left:
                        return nudgeSelection(new Vector2(-1, 0));

                    case Key.Right:
                        return nudgeSelection(new Vector2(1, 0));

                    case Key.Up:
                        return nudgeSelection(new Vector2(0, -1));

                    case Key.Down:
                        return nudgeSelection(new Vector2(0, 1));
                }
            }

            return false;
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            base.OnKeyUp(e);

            if (nudgeMovementActive && !e.ControlPressed)
            {
                EditorBeatmap.EndChange();
                nudgeMovementActive = false;
            }
        }

        public override bool HandleMovement(MoveSelectionEvent<HitObject> moveEvent)
        {
            var snappedPoint = NextMovementSnappedPoint;
            NextMovementSnappedPoint = null;

            var hitObjects = selectedMovableObjects;

            var localDelta = this.ScreenSpaceDeltaToParentSpace(moveEvent.ScreenSpaceDelta);

            // this conditional is a rather ugly special case for stacks.
            // as it turns out, adding the `EditorBeatmap.Update()` call at the end of this would cause stacked objects to jitter when moved around
            // (they would stack and then unstack every frame).
            // the reason for that is that the selection handling abstractions are not aware of the distinction between "displayed" and "actual" position
            // which is unique to osu! due to stacking being applied as a post-processing step.
            // therefore, the following loop would occur:
            // - on frame 1 the blueprint is snapped to the stack's baseline position. `EditorBeatmap.Update()` applies stacking successfully,
            //   the blueprint moves up the stack from its original drag position.
            // - on frame 2 the blueprint's position is now the *stacked* position, which is interpreted higher up as *manually performing an unstack*
            //   to the blueprint's unstacked position (as the machinery higher up only cares about differences in screen space position).
            if (hitObjects.Any(h => Precision.AlmostEquals(localDelta, -h.StackOffset)))
                return true;

            moveObjects(hitObjects, localDelta);

            // on every step of a drag rather than once it is over, so that what is held is drawn where letting go
            // leaves it (2026-10-05).
            RoundPositionsIfAsked(snappedPoint);

            return true;
        }

        private void moveObjects(OsuHitObject[] hitObjects, Vector2 localDelta)
        {
            // this will potentially move the selection out of bounds...
            foreach (var h in hitObjects)
                h.Position += localDelta;

            // but this will be corrected, unless the selection is allowed outside the play area. The correction is
            // what cancels out a drag whenever the selection already reaches outside -- a slider ending past an edge,
            // or a whole screen's worth of pattern -- leaving it impossible to move at all.
            if (!allowOutsidePlayArea.Value)
                moveSelectionInBounds();

            // manually update stacking.
            // this intentionally bypasses the editor `UpdateState()` / beatmap processor flow for performance reasons,
            // as the entire flow is too expensive to run on every movement.
            Scheduler.AddOnce(OsuBeatmapProcessor.ApplyStacking, EditorBeatmap);
        }

        /// <summary>
        /// Move the current selection spatially by the specified delta, in gamefield coordinates (ie. the same coordinates as the blueprints).
        /// </summary>
        /// <param name="delta"></param>
        private bool nudgeSelection(Vector2 delta)
        {
            if (!nudgeMovementActive)
            {
                nudgeMovementActive = true;
                EditorBeatmap.BeginChange();
            }

            // the selection rather than its blueprints, which only exist for what is on screen.
            if (SelectedItems.Count == 0)
                return false;

            moveObjects(selectedMovableObjects, delta);
            RoundPositionsIfAsked();
            return true;
        }

        public override bool HandleReverse()
        {
            // the shape a run of circles is being laid along is what is reversed when the run is, and the run follows it.
            // The circles' times are counted from the shape's start: with only their order turned round, the first of them
            // was at the last one's time, and the run was taken to have been dragged that far along the timeline.
            if (streamPanel?.ShapeBehind(SelectedItems) is Slider shape)
            {
                shape.Path.Reverse(out Vector2 shapeOffset);
                shape.Position += shapeOffset;
                composer?.PutPathOnWholeCoordinatesIfAsked(shape);

                streamPanel.ShapeReversed();
                return true;
            }

            var hitObjects = EditorBeatmap.SelectedHitObjects
                                          .OfType<OsuHitObject>()
                                          .OrderBy(obj => obj.StartTime)
                                          .ToList();

            double endTime = hitObjects.Max(h => h.GetEndTime());
            double startTime = hitObjects.Min(h => h.StartTime);

            bool moreThanOneObject = hitObjects.Count > 1;

            // the expectation is that even if the objects themselves are reversed temporally,
            // the position of new combos in the selection should remain the same.
            // preserve it for later before doing the reversal.
            var newComboOrder = hitObjects.Select(obj => obj.NewCombo).ToList();

            foreach (var h in hitObjects)
            {
                if (moreThanOneObject)
                    h.StartTime = endTime - (h.GetEndTime() - startTime);

                if (h is Slider slider)
                {
                    slider.Path.Reverse(out Vector2 offset);
                    slider.Position += offset;

                    // the new head is where the path ended, which a slider cut short of its last anchor leaves on a
                    // fraction (2026-10-05).
                    composer?.PutPathOnWholeCoordinatesIfAsked(slider);
                }
            }

            // re-order objects by start time again after reversing, and restore new combo flag positioning
            hitObjects = hitObjects.OrderBy(obj => obj.StartTime).ToList();

            for (int i = 0; i < hitObjects.Count; ++i)
                hitObjects[i].NewCombo = newComboOrder[i];

            return true;
        }

        public override bool HandleFlip(Direction direction, bool flipOverOrigin)
        {
            // the shape a run of circles is being laid along is what is flipped when the run is, and the run follows it.
            var hitObjects = streamPanel?.ShapeBehind(SelectedItems) is Slider shape ? new OsuHitObject[] { shape } : selectedMovableObjects;

            // If we're flipping over the origin, we take the grid origin position from the grid toolbox.
            var flipQuad = flipOverOrigin ? new Quad(gridToolbox.StartPositionX.Value, gridToolbox.StartPositionY.Value, 0, 0) : GeometryUtils.GetSurroundingQuad(hitObjects);
            Vector2 flipAxis = direction == Direction.Vertical ? Vector2.UnitY : Vector2.UnitX;

            if (flipOverOrigin)
            {
                // If we're flipping over the origin, we take one of the axes of the grid.
                // Take the axis closest to the direction we want to flip over.
                switch (gridToolbox.GridType.Value)
                {
                    case PositionSnapGridType.Square:
                        flipAxis = GeometryUtils.RotateVector(Vector2.UnitX, -((gridToolbox.GridLinesRotation.Value + 360 + 45) % 90 - 45));
                        flipAxis = direction == Direction.Vertical ? flipAxis.PerpendicularLeft : flipAxis;
                        break;

                    case PositionSnapGridType.Triangle:
                        // Hex grid has 3 axes, so you can not directly flip over one of the axes,
                        // however it's still possible to achieve that flip by combining multiple flips over the other axes.
                        // Angle degree range for vertical = (-120, -60]
                        // Angle degree range for horizontal = [-30, 30)
                        flipAxis = direction == Direction.Vertical
                            ? GeometryUtils.RotateVector(Vector2.UnitX, -((gridToolbox.GridLinesRotation.Value + 360 + 30) % 60 + 60))
                            : GeometryUtils.RotateVector(Vector2.UnitX, -((gridToolbox.GridLinesRotation.Value + 360) % 60 - 30));
                        break;
                }
            }

            var controlPointFlipQuad = new Quad();

            bool didFlip = false;

            foreach (var h in hitObjects)
            {
                var flippedPosition = GeometryUtils.GetFlippedPosition(flipAxis, flipQuad, h.Position);

                // Clamp the flipped position inside the playfield bounds, because the flipped position might be outside the playfield bounds if the origin is not centered.
                flippedPosition = Vector2.Clamp(flippedPosition, Vector2.Zero, OsuPlayfield.BASE_SIZE);

                if (!Precision.AlmostEquals(flippedPosition, h.Position))
                {
                    h.Position = flippedPosition;
                    didFlip = true;
                }

                if (h is Slider slider)
                {
                    didFlip = true;

                    foreach (var cp in slider.Path.ControlPoints)
                        cp.Position = GeometryUtils.GetFlippedPosition(flipAxis, controlPointFlipQuad, cp.Position);
                }

                if (composer != null)
                {
                    h.Position = composer.RoundPositionIfAsked(h.Position);

                    // the anchors too: over a turned grid's axis they land on fractions.
                    if (h is Slider wholeSlider)
                        composer.PutPathOnWholeCoordinatesIfAsked(wholeSlider);
                }
            }

            return didFlip;
        }

        public override SelectionRotationHandler CreateRotationHandler() => new OsuSelectionRotationHandler();

        public override SelectionScaleHandler CreateScaleHandler() => new OsuSelectionScaleHandler();

        private void moveSelectionInBounds()
        {
            var hitObjects = selectedMovableObjects;

            Quad quad = GeometryUtils.GetSurroundingQuad(hitObjects, true);

            Vector2 delta = Vector2.Zero;

            if (quad.TopLeft.X < 0)
                delta.X -= quad.TopLeft.X;
            if (quad.TopLeft.Y < 0)
                delta.Y -= quad.TopLeft.Y;

            if (quad.BottomRight.X > DrawWidth)
                delta.X -= quad.BottomRight.X - DrawWidth;
            if (quad.BottomRight.Y > DrawHeight)
                delta.Y -= quad.BottomRight.Y - DrawHeight;

            foreach (var h in hitObjects)
                h.Position += delta;
        }

        /// <summary>
        /// All osu! hitobjects which can be moved/rotated/scaled.
        /// </summary>
        private OsuHitObject[] selectedMovableObjects => SelectedItems.OfType<OsuHitObject>()
                                                                      .Where(h => h is not Spinner)
                                                                      .ToArray();

        /// <summary>
        /// All osu! hitobjects which can be merged.
        /// </summary>
        private OsuHitObject[] selectedMergeableObjects => SelectedItems.OfType<OsuHitObject>()
                                                                        .Where(h => h is HitCircle or Slider)
                                                                        .OrderBy(h => h.StartTime)
                                                                        .ToArray();

        private void mergeSelection()
        {
            var mergeableObjects = selectedMergeableObjects;

            if (!canMerge(mergeableObjects))
                return;

            EditorBeatmap.BeginChange();

            // Have an initial slider object.
            var firstHitObject = mergeableObjects[0];
            var mergedHitObject = firstHitObject as Slider ?? new Slider
            {
                StartTime = firstHitObject.StartTime,
                Position = firstHitObject.Position,
                NewCombo = firstHitObject.NewCombo,
                Samples = firstHitObject.Samples,
            };

            if (mergedHitObject.Path.ControlPoints.Count == 0)
            {
                mergedHitObject.Path.ControlPoints.Add(new PathControlPoint(Vector2.Zero, PathType.LINEAR));
            }

            // Merge all the selected hit objects into one slider path.
            bool lastCircle = firstHitObject is HitCircle;

            foreach (var selectedMergeableObject in mergeableObjects.Skip(1))
            {
                if (selectedMergeableObject is IHasPath hasPath)
                {
                    var offset = lastCircle ? selectedMergeableObject.Position - mergedHitObject.Position : mergedHitObject.Path.ControlPoints[^1].Position;
                    float distanceToLastControlPoint = Vector2.Distance(mergedHitObject.Path.ControlPoints[^1].Position, offset);

                    // Calculate the distance required to travel to the expected distance of the merging slider.
                    mergedHitObject.Path.ExpectedDistance.Value = mergedHitObject.Path.CalculatedDistance + distanceToLastControlPoint + hasPath.Path.Distance;

                    // Remove the last control point if it sits exactly on the start of the next control point.
                    if (Precision.AlmostEquals(distanceToLastControlPoint, 0))
                    {
                        mergedHitObject.Path.ControlPoints.RemoveAt(mergedHitObject.Path.ControlPoints.Count - 1);
                    }

                    mergedHitObject.Path.ControlPoints.AddRange(hasPath.Path.ControlPoints.Select(o => new PathControlPoint(o.Position + offset, o.Type)));
                    lastCircle = false;
                }
                else
                {
                    // Turn the last control point into a linear type if this is the first merging circle in a sequence, so the subsequent control points can be inherited path type.
                    if (!lastCircle)
                    {
                        mergedHitObject.Path.ControlPoints.Last().Type = PathType.LINEAR;
                    }

                    mergedHitObject.Path.ControlPoints.Add(new PathControlPoint(selectedMergeableObject.Position - mergedHitObject.Position));
                    mergedHitObject.Path.ExpectedDistance.Value = null;
                    lastCircle = true;
                }
            }

            // Make sure only the merged hit object is in the beatmap.
            if (firstHitObject is Slider)
            {
                foreach (var selectedMergeableObject in mergeableObjects.Skip(1))
                {
                    EditorBeatmap.Remove(selectedMergeableObject);
                }
            }
            else
            {
                foreach (var selectedMergeableObject in mergeableObjects)
                {
                    EditorBeatmap.Remove(selectedMergeableObject);
                }

                EditorBeatmap.Add(mergedHitObject);
            }

            // Make sure the merged hitobject is selected.
            SelectedItems.Clear();
            SelectedItems.Add(mergedHitObject);

            EditorBeatmap.EndChange();
        }

        protected override IEnumerable<MenuItem> GetContextMenuItemsForSelection(IEnumerable<SelectionBlueprint<HitObject>> selection)
        {
            foreach (var item in base.GetContextMenuItemsForSelection(selection))
                yield return item;

            if (canMerge(selectedMergeableObjects))
                yield return new OsuMenuItem("Merge selection", MenuItemType.Destructive, mergeSelection);
        }

        private bool canMerge(IReadOnlyList<OsuHitObject> objects) =>
            objects.Count > 1
            && (objects.Any(h => h is Slider)
                || objects.Zip(objects.Skip(1), (h1, h2) => Precision.DefinitelyBigger(Vector2.DistanceSquared(h1.Position, h2.Position), 1)).Any(x => x));
    }
}
