// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Rulesets.Catch.Edit.Blueprints.Components;
using osu.Game.Rulesets.Catch.Objects;
using osu.Game.Rulesets.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Catch.Edit.Blueprints
{
    public partial class JuiceStreamPlacementBlueprint : CatchPlacementBlueprint<JuiceStream>
    {
        private readonly ScrollingPath scrollingPath;

        private readonly NestedOutlineContainer nestedOutlineContainer;

        private readonly PlacementEditablePath editablePath;

        private int lastEditablePathId = -1;

        private InputManager inputManager = null!;

        private Bindable<bool> swapRightAndMiddleMouse = null!;

        /// <summary>
        /// The button that finishes the stream.
        /// </summary>
        /// <remarks>
        /// Deliberately tied to the physical right button rather than to <see cref="MouseButton.Right"/>, the same way
        /// osu!'s sliders are: trading right and middle happens where input comes in, so that button arrives here as
        /// <see cref="MouseButton.Middle"/>. Without this, trading them left a stream that only the wheel could finish.
        /// </remarks>
        private MouseButton finishButton => swapRightAndMiddleMouse.Value ? MouseButton.Middle : MouseButton.Right;

        protected override bool IsValidForPlacement => base.IsValidForPlacement && (PlacementActive == PlacementState.Waiting || Precision.DefinitelyBigger(HitObject.Duration, 0));

        public JuiceStreamPlacementBlueprint()
        {
            Children = new Drawable[]
            {
                scrollingPath = new ScrollingPath(),
                nestedOutlineContainer = new NestedOutlineContainer(),
                editablePath = new PlacementEditablePath(positionToTime)
            };
        }

        protected override void Update()
        {
            base.Update();

            if (PlacementActive == PlacementState.Active)
                editablePath.UpdateFrom(HitObjectContainer, HitObject);
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            swapRightAndMiddleMouse = config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager()!;

            BeginPlacement();
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            switch (PlacementActive)
            {
                case PlacementState.Waiting:
                    if (e.Button != MouseButton.Left) break;

                    editablePath.AddNewVertex();
                    BeginPlacement(true);
                    return true;

                case PlacementState.Active:
                    if (e.Button == MouseButton.Left)
                    {
                        editablePath.AddNewVertex();
                        return true;
                    }

                    if (e.Button == finishButton)
                    {
                        EndPlacement(true);
                        return true;
                    }

                    break;
            }

            return base.OnMouseDown(e);
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var gridSnapResult = Composer?.FindSnappedPositionAndTime(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);
            gridSnapResult.ScreenSpacePosition.X = screenSpacePosition.X;
            var distanceSnapResult = Composer?.TryDistanceSnap(gridSnapResult.ScreenSpacePosition);

            var result = distanceSnapResult != null && Vector2.Distance(gridSnapResult.ScreenSpacePosition, distanceSnapResult.ScreenSpacePosition) < CatchHitObjectComposer.DISTANCE_SNAP_RADIUS
                ? distanceSnapResult
                : gridSnapResult;

            switch (PlacementActive)
            {
                case PlacementState.Waiting:
                    HitObject.OriginalX = ToLocalSpace(result.ScreenSpacePosition).X;
                    if (result.Time is double snappedTime)
                        HitObject.StartTime = snappedTime;
                    break;

                case PlacementState.Active:
                    Vector2 unsnappedPosition = inputManager.CurrentState.Mouse.Position;
                    editablePath.MoveLastVertex(unsnappedPosition);
                    break;

                default:
                    return result;
            }

            // Make sure the up-to-date position is used for outlines.
            Vector2 startPosition = CatchHitObjectUtils.GetStartPosition(HitObjectContainer, HitObject);
            editablePath.Position = nestedOutlineContainer.Position = scrollingPath.Position = startPosition;

            if (lastEditablePathId != editablePath.PathId)
                editablePath.UpdateHitObjectFromPath(HitObject);
            lastEditablePathId = editablePath.PathId;

            ApplyDefaultsToHitObject();
            scrollingPath.UpdatePathFrom(HitObjectContainer, HitObject);
            nestedOutlineContainer.UpdateNestedObjectsFrom(HitObjectContainer, HitObject);
            return result;
        }

        private double positionToTime(float relativeYPosition)
        {
            double time = HitObjectContainer.TimeAtPosition(relativeYPosition, HitObject.StartTime);
            return time - HitObject.StartTime;
        }
    }
}
