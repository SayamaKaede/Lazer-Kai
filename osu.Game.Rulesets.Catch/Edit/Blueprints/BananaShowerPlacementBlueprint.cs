// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
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
    public partial class BananaShowerPlacementBlueprint : CatchPlacementBlueprint<BananaShower>
    {
        private readonly TimeSpanOutline outline;

        private double placementStartTime;
        private double placementEndTime;

        private Bindable<bool> swapRightAndMiddleMouse = null!;

        /// <summary>
        /// The button that finishes the shower.
        /// </summary>
        /// <remarks>
        /// Deliberately tied to the physical right button rather than to <see cref="MouseButton.Right"/>, the same way
        /// osu!'s spinners are: trading right and middle happens where input comes in, so that button arrives here as
        /// <see cref="MouseButton.Middle"/>.
        /// </remarks>
        private MouseButton finishButton => swapRightAndMiddleMouse.Value ? MouseButton.Middle : MouseButton.Right;

        protected override bool IsValidForPlacement => base.IsValidForPlacement && (PlacementActive == PlacementState.Waiting || Precision.DefinitelyBigger(HitObject.Duration, 0));

        public BananaShowerPlacementBlueprint()
        {
            Child = outline = new TimeSpanOutline();
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            swapRightAndMiddleMouse = config.GetBindable<bool>(OsuSetting.SwapRightAndMiddleMouse);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            BeginPlacement();
        }

        protected override void Update()
        {
            base.Update();

            outline.UpdateFrom(HitObjectContainer, HitObject);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            switch (PlacementActive)
            {
                case PlacementState.Waiting:
                    if (e.Button != MouseButton.Left) break;

                    BeginPlacement(true);
                    return true;

                case PlacementState.Active:
                    if (e.Button != finishButton) break;

                    EndPlacement(true);
                    return true;
            }

            return base.OnMouseDown(e);
        }

        public override SnapResult UpdateTimeAndPosition(Vector2 screenSpacePosition, double fallbackTime)
        {
            var result = Composer?.FindSnappedPositionAndTime(screenSpacePosition) ?? new SnapResult(screenSpacePosition, fallbackTime);

            base.UpdateTimeAndPosition(result.ScreenSpacePosition, result.Time ?? fallbackTime);

            if (!(result.Time is double time)) return result;

            switch (PlacementActive)
            {
                case PlacementState.Waiting:
                    placementStartTime = placementEndTime = time;
                    break;

                case PlacementState.Active:
                    placementEndTime = time;
                    break;
            }

            HitObject.StartTime = Math.Min(placementStartTime, placementEndTime);
            HitObject.EndTime = Math.Max(placementStartTime, placementEndTime);
            return result;
        }
    }
}
