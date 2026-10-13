// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mania.Edit.Blueprints;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Mania.Edit
{
    public partial class ManiaBlueprintContainer : ComposeBlueprintContainer, IHasTooltip
    {
        public new ManiaHitObjectComposer Composer => (ManiaHitObjectComposer)base.Composer;

        public ManiaBlueprintContainer(ManiaHitObjectComposer composer)
            : base(composer)
        {
        }

        public override HitObjectSelectionBlueprint? CreateHitObjectBlueprintFor(HitObject hitObject)
        {
            switch (hitObject)
            {
                case Note note:
                    return new NoteSelectionBlueprint(note);

                case HoldNote holdNote:
                    return new HoldNoteSelectionBlueprint(holdNote);
            }

            return base.CreateHitObjectBlueprintFor(hitObject);
        }

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new ManiaSelectionHandler();

        #region Selecting the notes of a lane by what it sounds as

        /// <summary>
        /// Whether the mouse went down on what a lane sounds as, at its top, and the notes of the lane were selected for
        /// it: what a click or a drag does otherwise is then left out until the button is up.
        /// </summary>
        private bool laneSelected;

        /// <summary>
        /// The lane whose sound the mouse is on, which is lit and says what a click on it does. The lanes themselves
        /// have no mouse in the editor: this is over them and has it.
        /// </summary>
        private int? laneUnderMouse;

        public LocalisableString TooltipText => laneUnderMouse != null ? LazerKaiHitsoundStrings.SelectLaneNotes : default;

        private InputManager inputManager = null!;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager()!;
        }

        protected override void Update()
        {
            base.Update();

            int? lane = IsHovered ? Composer.LaneLabelAt(inputManager.CurrentState.Mouse.Position) : null;

            if (lane == laneUnderMouse)
                return;

            laneUnderMouse = lane;
            Composer.LightLaneLabel(lane);
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (e.Button == MouseButton.Left && Composer.LaneLabelAt(e.ScreenSpaceMouseDownPosition) is int lane)
            {
                var notes = Beatmap.HitObjects.Where(h => h is ManiaHitObject note && note.Column == lane).ToArray();

                Beatmap.SelectedHitObjects.Clear();
                Beatmap.SelectedHitObjects.AddRange(notes);

                laneSelected = true;
                return true;
            }

            return base.OnMouseDown(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            // a click on nothing takes the selection away.
            if (laneSelected)
                return true;

            return base.OnClick(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            // nor is a box started by a mouse that moved a little as it clicked.
            if (laneSelected)
                return false;

            return base.OnDragStart(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (laneSelected && e.Button == MouseButton.Left)
            {
                // after the click, which comes once the button is up.
                Schedule(() => laneSelected = false);
                return;
            }

            base.OnMouseUp(e);
        }

        #endregion

        protected sealed override DragBox CreateDragBox() => new ScrollingDragBox(Composer.Playfield);

        protected override bool TryMoveBlueprints(DragEvent e, IList<(SelectionBlueprint<HitObject> blueprint, Vector2[] originalSnapPositions)> blueprints)
        {
            Vector2 distanceTravelled = e.ScreenSpaceMousePosition - e.ScreenSpaceMouseDownPosition;

            // The final movement position, relative to movementBlueprintOriginalPosition.
            Vector2 movePosition = blueprints.First().originalSnapPositions.First() + distanceTravelled;

            // Retrieve a snapped position.
            var result = Composer.FindSnappedPositionAndTime(movePosition);

            var referenceBlueprint = blueprints.First().blueprint;
            bool moved = SelectionHandler.HandleMovement(new MoveSelectionEvent<HitObject>(referenceBlueprint, result.ScreenSpacePosition - referenceBlueprint.ScreenSpaceSelectionPoint));
            if (moved)
                ApplySnapResultTime(result, referenceBlueprint.Item.StartTime);
            return moved;
        }
    }
}
