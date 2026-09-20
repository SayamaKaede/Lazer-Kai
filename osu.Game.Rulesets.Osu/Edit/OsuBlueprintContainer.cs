// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    public partial class OsuBlueprintContainer : ComposeBlueprintContainer
    {
        private Bindable<bool> limitedDistanceSnap { get; set; } = null!;

        public new OsuHitObjectComposer Composer => (OsuHitObjectComposer)base.Composer;

        public OsuBlueprintContainer(OsuHitObjectComposer composer)
            : base(composer)
        {
        }

        // behind the blueprints and the selection box. Created up front, since the selection handler is created during the
        // base class's loading and needs it.
        private readonly SelectedObjectOutlines selectedObjectOutlines = new SelectedObjectOutlines { Depth = 1 };

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            limitedDistanceSnap = config.GetBindable<bool>(OsuSetting.EditorLimitedDistanceSnap);

            AddInternal(selectedObjectOutlines);
        }

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new OsuSelectionHandler { Outlines = selectedObjectOutlines };

        protected override void OnBlueprintAdded(HitObject item)
        {
            base.OnBlueprintAdded(item);
            selectedObjectOutlines.BlueprintAdded(item);
        }

        protected override void OnBlueprintRemoved(HitObject item)
        {
            base.OnBlueprintRemoved(item);
            selectedObjectOutlines.BlueprintRemoved(item);
        }

        private Vector2? lastPointerPosition;
        private int lastOutlinesVersion;

        protected override void Update()
        {
            base.Update();
            updateBlueprintUnderPointer();
        }

        /// <summary>
        /// An outline cannot be dragged, so the selected object under the pointer is given a blueprint for as long as the
        /// pointer stays on it. See <see cref="EditorBlueprintContainer.KeepBlueprintUnderPointer"/>.
        /// </summary>
        private void updateBlueprintUnderPointer()
        {
            var mouse = InputManager.CurrentState.Mouse;

            // whatever has been taken hold of stays, while a button is down: it may be in the middle of being dragged.
            if (mouse.Buttons.HasAnyButtonPressed)
                return;

            if (KeptUnderPointer != null)
            {
                // once its blueprint is there, its outline is gone, so the blueprint is what says the pointer is still on it.
                var blueprint = TryGetBlueprintFor(KeptUnderPointer);

                if (blueprint != null ? blueprint.ReceivePositionalInputAt(mouse.Position) : selectedObjectOutlines.FindAt(mouse.Position, EditorClock.CurrentTime) == KeptUnderPointer)
                    return;
            }

            // the search goes through every outline, so only when something it depends on has changed.
            if (lastPointerPosition == mouse.Position && lastOutlinesVersion == selectedObjectOutlines.Version && KeptUnderPointer == null)
                return;

            lastPointerPosition = mouse.Position;
            lastOutlinesVersion = selectedObjectOutlines.Version;
            KeepBlueprintUnderPointer(selectedObjectOutlines.Count > 0 ? selectedObjectOutlines.FindAt(mouse.Position, EditorClock.CurrentTime) : null);
        }

        public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(HitObject hitObject)
        {
            switch (hitObject)
            {
                case HitCircle circle:
                    return new HitCircleSelectionBlueprint(circle);

                case Slider slider:
                    return new SliderSelectionBlueprint(slider);

                case Spinner spinner:
                    return new SpinnerSelectionBlueprint(spinner);
            }

            return base.CreateHitObjectBlueprintFor(hitObject);
        }

        protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
        {
            Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;

            for (int i = 0; i < blueprints.Count; i++)
            {
                if (checkSnappingBlueprintToNearbyObjects(blueprints[i].blueprint, distanceTravelled, blueprints[i].originalSnapPositions))
                    return true;
            }

            // if no positional snapping could be performed, try unrestricted snapping from the earliest
            // item in the selection.

            // The final movement position, relative to movementBlueprintOriginalPosition.
            Vector2 movePosition = blueprints.First().originalSnapPositions.First() + distanceTravelled;
            var referenceBlueprint = blueprints.First().blueprint;

            // Retrieve a snapped position.
            var result = Composer.TrySnapToNearbyObjects(movePosition);
            result ??= Composer.TrySnapToDistanceGrid(movePosition, limitedDistanceSnap.Value ? referenceBlueprint.Item.StartTime : null);
            if (Composer.TrySnapToPositionGrid(result?.ScreenSpacePosition ?? movePosition, result?.Time) is SnapResult gridSnapResult)
                result = gridSnapResult;

            // a position a snap decided is already the one asked for, and is not rounded afterwards.
            snapped = result != null;
            result ??= new SnapResult(movePosition, null);

            bool moved = SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(referenceBlueprint, result.ScreenSpacePosition - referenceBlueprint.ScreenSpaceSelectionPoint));
            if (moved)
                ApplySnapResultTime(result, referenceBlueprint.Item.StartTime);
            return moved;
        }

        protected override void DragOperationCompleted()
        {
            base.DragOperationCompleted();

            // while the change this drag made is still open, so undo takes it with the rest of the drag.
            if (SelectionHandler is OsuSelectionHandler osuSelectionHandler)
                osuSelectionHandler.RoundPositionsIfAsked();
        }

        /// <summary>
        /// Tells the selection handler whether a snap decided where the movement it is about to handle lands, so that
        /// a position asked for by a snap is not rounded off it afterwards.
        /// </summary>
        private bool snapped
        {
            set
            {
                if (SelectionHandler is OsuSelectionHandler osuSelectionHandler)
                    osuSelectionHandler.MovementCameFromSnap = value;
            }
        }

        /// <summary>
        /// Check for positional snap for given blueprint.
        /// </summary>
        /// <param name="blueprint">The blueprint to check for snapping.</param>
        /// <param name="distanceTravelled">Distance travelled since start of dragging action.</param>
        /// <param name="originalPositions">The snap positions of blueprint before start of dragging action.</param>
        /// <returns>Whether an object to snap to was found.</returns>
        private bool checkSnappingBlueprintToNearbyObjects(SelectionBlueprint<HitObject> blueprint, Vector2 distanceTravelled, Vector2[] originalPositions)
        {
            var currentPositions = blueprint.ScreenSpaceSnapPoints;

            for (int i = 0; i < originalPositions.Length; i++)
            {
                Vector2 originalPosition = originalPositions[i];
                var testPosition = originalPosition + distanceTravelled;

                var positionalResult = Composer.TrySnapToNearbyObjects(testPosition);

                if (positionalResult == null || positionalResult.ScreenSpacePosition == testPosition) continue;

                var delta = positionalResult.ScreenSpacePosition - currentPositions[i];

                snapped = true;

                // attempt to move the objects, and apply any time based snapping if we can.
                if (SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(blueprint, delta)))
                {
                    ApplySnapResultTime(positionalResult, blueprint.Item.StartTime);
                    return true;
                }
            }

            return false;
        }
    }
}
