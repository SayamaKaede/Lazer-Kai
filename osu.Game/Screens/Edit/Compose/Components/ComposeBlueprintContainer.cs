// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Screens.Edit.Compose.Components
{
    /// <summary>
    /// A blueprint container generally displayed as an overlay to a ruleset's playfield.
    /// </summary>
    public abstract partial class ComposeBlueprintContainer : EditorBlueprintContainer, IFineDragArea
    {
        private DependencyContainer dependencies = null!;

        private readonly Container<PlacementBlueprint> placementBlueprintContainer;

        public new EditorSelectionHandler SelectionHandler => (EditorSelectionHandler)base.SelectionHandler;

        public PlacementBlueprint CurrentPlacement { get; private set; }

        public HitObjectPlacementBlueprint CurrentHitObjectPlacement => CurrentPlacement as HitObjectPlacementBlueprint;

        [Resolved(canBeNull: true)]
        private EditorScreenWithTimeline editorScreen { get; set; }

        /// <remarks>
        /// Positional input must be received outside the container's bounds,
        /// in order to handle composer blueprints which are partially offscreen.
        /// </remarks>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => editorScreen?.MainContent.ReceivePositionalInputAt(screenSpacePos) ?? base.ReceivePositionalInputAt(screenSpacePos);

        protected override IEnumerable<SelectionBlueprint<HitObject>> ApplySelectionOrder(IEnumerable<SelectionBlueprint<HitObject>> blueprints) =>
            base.ApplySelectionOrder(blueprints)
                .OrderBy(b => Math.Min(Math.Abs(EditorClock.CurrentTime - b.Item.GetEndTime()), Math.Abs(EditorClock.CurrentTime - b.Item.StartTime)));

        protected ComposeBlueprintContainer(HitObjectComposer composer)
            : base(composer)
        {
            placementBlueprintContainer = new Container<PlacementBlueprint>
            {
                RelativeSizeAxes = Axes.Both
            };
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            return dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(new DrawableRulesetDependenciesProvidingContainer(Composer.Ruleset)
            {
                Child = placementBlueprintContainer
            });

            dependencies.CacheAs(SelectionHandler);
        }

        /// <summary>
        /// Degrees the selection turns per wheel notch, and the finer step taken while alt is held.
        /// </summary>
        private const float rotation_step = 3;

        private const float rotation_step_fine = 1;

        /// <summary>
        /// Whether a turn is being made with the wheel right now, and how far it has come so far.
        /// </summary>
        /// <remarks>
        /// Every notch of the wheel is part of one turn, ended by letting go of the modifiers. Committing each notch
        /// on its own would leave the user undoing a long turn three degrees at a time.
        /// </remarks>
        private bool rotatingByScroll;

        private float rotationSoFar;

        protected override bool OnScroll(ScrollEvent e)
        {
            // ctrl alone already cycles the beat snap divisor. Global actions match modifiers exactly, so adding
            // shift here does not also trigger that.
            if (e.ControlPressed && e.ShiftPressed)
            {
                var rotationHandler = SelectionHandler.RotationHandler;

                // holding shift makes the framework hand the notch over on the horizontal axis instead, so both are
                // read. `Editor.OnScroll` sums them for the same reason.
                int direction = Math.Sign(e.ScrollDelta.X + e.ScrollDelta.Y);

                if (direction != 0 && rotationHandler.CanRotateAroundSelectionOrigin.Value)
                {
                    float step = e.AltPressed ? rotation_step_fine : rotation_step;

                    // never joins a turn someone else started, such as a drag of the rotation handle.
                    if (!rotatingByScroll && rotationHandler.OperationInProgress.Value)
                        return true;

                    if (!rotatingByScroll)
                    {
                        rotatingByScroll = true;
                        rotationSoFar = 0;
                        rotationHandler.Begin();
                    }

                    rotationSoFar += step * direction;
                    rotationHandler.Update(rotationSoFar);
                    return true;
                }
            }

            return base.OnScroll(e);
        }

        /// <summary>
        /// Ends the turn started by the wheel, writing it to the undo history as a single step.
        /// </summary>
        private void commitScrollRotation()
        {
            if (!rotatingByScroll)
                return;

            rotatingByScroll = false;
            SelectionHandler.RotationHandler.Commit();
        }

        /// <summary>
        /// Whether the modifiers which hold a wheel turn open are still down. Read from the input state rather than
        /// from key events, which only arrive while this container has focus.
        /// </summary>
        private bool rotationModifiersHeld
        {
            get
            {
                var keyboard = GetContainingInputManager()?.CurrentState.Keyboard;

                return keyboard != null && keyboard.ControlPressed && keyboard.ShiftPressed;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Beatmap.HitObjectAdded += hitObjectAdded;
        }

        protected override void TransferBlueprintFor(HitObject hitObject, DrawableHitObject drawableObject)
        {
            base.TransferBlueprintFor(hitObject, drawableObject);

            var blueprint = (HitObjectSelectionBlueprint)GetBlueprintFor(hitObject);
            blueprint.DrawableObject = drawableObject;
        }

        #region Placement

        /// <summary>
        /// Refreshes the current placement tool.
        /// </summary>
        private void refreshPlacement()
        {
            CurrentPlacement?.EndPlacement(false);
            CurrentPlacement?.Expire();
            CurrentPlacement = null;

            ensurePlacementCreated();
        }

        private void updatePlacementTimeAndPosition()
        {
            CurrentPlacement.UpdateTimeAndPosition(InputManager.CurrentState.Mouse.Position, Beatmap.SnapTime(EditorClock.CurrentTime, null));
        }

        #endregion

        protected override void Update()
        {
            base.Update();

            if (rotatingByScroll && !rotationModifiersHeld)
                commitScrollRotation();

            if (CurrentPlacement != null)
            {
                switch (CurrentPlacement.PlacementActive)
                {
                    case PlacementBlueprint.PlacementState.Waiting:
                        if (!Composer.CursorInPlacementArea)
                            CurrentPlacement.Hide();
                        else
                            CurrentPlacement.Show();

                        break;

                    case PlacementBlueprint.PlacementState.Active:
                        CurrentPlacement.Show();
                        break;

                    case PlacementBlueprint.PlacementState.Finished:
                        refreshPlacement();
                        break;
                }

                // updates the placement with the latest editor clock time.
                updatePlacementTimeAndPosition();
            }
        }

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            // updates the placement with the latest mouse position.
            if (CurrentPlacement != null)
                updatePlacementTimeAndPosition();

            return base.OnMouseMove(e);
        }

        protected sealed override SelectionBlueprint<HitObject> CreateBlueprintFor(HitObject item)
        {
            var drawable = Composer.HitObjects.FirstOrDefault(d => d.HitObject == item);

            if (drawable == null)
                return null;

            return CreateHitObjectBlueprintFor(item)?.With(b => b.DrawableObject = drawable);
        }

        [CanBeNull]
        public virtual HitObjectSelectionBlueprint CreateHitObjectBlueprintFor(HitObject hitObject) => null;

        private void hitObjectAdded(HitObject obj)
        {
            // refresh the tool to handle the case of placement completing.
            refreshPlacement();

            // on successful placement, the new combo button should be reset as this is the most common user interaction.
            if (Beatmap.SelectedHitObjects.Count == 0 && Composer.SelectionNewComboState != null)
                Composer.SelectionNewComboState.Value = TernaryState.False;
        }

        private void ensurePlacementCreated()
        {
            if (CurrentPlacement != null) return;

            var blueprint = CurrentTool?.CreatePlacementBlueprint();

            if (blueprint != null)
            {
                placementBlueprintContainer.Child = CurrentPlacement = blueprint;

                // Fixes a 1-frame position discrepancy due to the first mouse move event happening in the next frame
                updatePlacementTimeAndPosition();
            }
        }

        public void CommitIfPlacementActive()
        {
            CurrentPlacement?.EndPlacement(CurrentPlacement.PlacementActive == PlacementBlueprint.PlacementState.Active);
            refreshPlacement();
        }

        private CompositionTool currentTool;

        /// <summary>
        /// The current placement tool.
        /// </summary>
        public CompositionTool CurrentTool
        {
            get => currentTool;

            set
            {
                if (currentTool == value)
                    return;

                currentTool = value;

                SelectionHandler.RightClickAlwaysQuickDeletes = currentTool is not SelectTool;

                // As per stable editor, when changing tools, we should forcefully commit any pending placement.
                CommitIfPlacementActive();
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            // leaving a turn open would leave the editor's change handler mid-transaction for good.
            commitScrollRotation();

            base.Dispose(isDisposing);

            if (Beatmap.IsNotNull())
                Beatmap.HitObjectAdded -= hitObjectAdded;
        }
    }
}
