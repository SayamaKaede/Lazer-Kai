// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;

namespace osu.Game.Screens.Edit.Compose.Components
{
    public abstract partial class EditorBlueprintContainer : BlueprintContainer<HitObject>
    {
        [Resolved]
        protected EditorClock EditorClock { get; private set; }

        [Resolved]
        protected EditorBeatmap Beatmap { get; private set; }

        protected readonly HitObjectComposer Composer;

        private HitObjectUsageEventBuffer usageEventBuffer;

        protected InputManager InputManager { get; private set; }

        protected EditorBlueprintContainer(HitObjectComposer composer)
        {
            Composer = composer;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            SelectedItems.BindTo(Beatmap.SelectedHitObjects);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            InputManager = GetContainingInputManager();

            Beatmap.HitObjectAdded += AddBlueprintFor;
            Beatmap.HitObjectRemoved += RemoveBlueprintFor;

            if (Composer != null)
            {
                foreach (var obj in Composer.HitObjects)
                    AddBlueprintFor(obj.HitObject);

                usageEventBuffer = new HitObjectUsageEventBuffer(Composer.Playfield);
                usageEventBuffer.HitObjectUsageBegan += AddBlueprintFor;
                // an object leaving the time on screen stays selected; see RemoveBlueprintKeepingSelection.
                usageEventBuffer.HitObjectUsageFinished += RemoveBlueprintKeepingSelection;
                usageEventBuffer.HitObjectUsageTransferred += TransferBlueprintFor;
            }
        }

        protected override void Update()
        {
            base.Update();
            usageEventBuffer?.Update();
        }

        protected override IEnumerable<SelectionBlueprint<HitObject>> SortForMovement(IReadOnlyList<SelectionBlueprint<HitObject>> blueprints)
            => blueprints.OrderBy(b => b.Item.StartTime);

        protected void ApplySnapResultTime(SnapResult result, double referenceTime)
        {
            if (!result.Time.HasValue)
                return;

            // Apply the start time at the newly snapped-to position
            double offset = result.Time.Value - referenceTime;

            if (offset != 0)
                Beatmap.PerformOnSelection(obj => obj.StartTime += offset);
        }

        protected override void AddBlueprintFor(HitObject item)
        {
            if (item is IBarLine)
                return;

            base.AddBlueprintFor(item);
        }

        /// <summary>
        /// Invoked when a <see cref="HitObject"/> has been transferred to another <see cref="DrawableHitObject"/>.
        /// </summary>
        /// <param name="hitObject">The hit object which has been assigned to a new drawable.</param>
        /// <param name="drawableObject">The new drawable that is representing the hit object.</param>
        protected virtual void TransferBlueprintFor(HitObject hitObject, DrawableHitObject drawableObject)
        {
        }

        protected override void DragOperationCompleted()
        {
            base.DragOperationCompleted();

            // handle positional change etc.
            // the selection moves as a whole, including what has no blueprint because its time is not on screen.
            foreach (var item in SelectionBlueprints.Select(b => b.Item).Union(SelectedItems))
                Beatmap.Update(item);
        }

        protected override bool OnDoubleClick(DoubleClickEvent e)
        {
            if (!base.OnDoubleClick(e))
                return false;

            EditorClock?.SeekSmoothlyTo(ClickedBlueprint.Item.StartTime);
            return true;
        }

        protected override SelectionBlueprintContainer CreateSelectionBlueprintContainer() => new HitObjectOrderedSelectionContainer { RelativeSizeAxes = Axes.Both };

        protected override SelectionHandler<HitObject> CreateSelectionHandler() => new EditorSelectionHandler();

        protected override void SelectAll()
        {
            SelectedItems.AddRange(Beatmap.HitObjects.Except(SelectedItems).ToArray());
        }

        private readonly HashSet<HitObject> keptForMovement = new HashSet<HitObject>();
        private HitObject keptForPointer;

        protected override void OnMovementBlueprintsChanged(IReadOnlyList<HitObject> items)
        {
            base.OnMovementBlueprintsChanged(items);

            // what is being dragged keeps its blueprint until the drag is over, even if its time leaves the screen meanwhile.
            var previous = keptForMovement.ToArray();

            keptForMovement.Clear();
            keptForMovement.UnionWith(items);

            foreach (var item in previous.Concat(items))
                updateKeepAlive(item);
        }

        /// <summary>
        /// Gives one object a blueprint while the pointer is over it, even though its time is not on screen.
        /// </summary>
        /// <remarks>
        /// Only what is on screen has a blueprint, but a selected object stays where it can be taken hold of however far
        /// the timeline has moved on: selecting a pattern, moving to another part of the beatmap and dragging it from
        /// there is an ordinary way to work. A drag can only start from a blueprint, so one is made for whatever
        /// selected object the pointer rests on, before the button goes down.
        /// </remarks>
        /// <param name="item">The object, or null to let go of the last one.</param>
        protected void KeepBlueprintUnderPointer(HitObject item)
        {
            if (ReferenceEquals(keptForPointer, item))
                return;

            var previous = keptForPointer;
            keptForPointer = item;

            if (previous != null)
                updateKeepAlive(previous);
            if (item != null)
                updateKeepAlive(item);
        }

        /// <summary>
        /// The object given a blueprint by <see cref="KeepBlueprintUnderPointer"/>, if any.
        /// </summary>
        protected HitObject KeptUnderPointer => keptForPointer;

        private void updateKeepAlive(HitObject item) =>
            Composer?.Playfield.SetKeepAlive(item, keptForMovement.Contains(item) || ReferenceEquals(keptForPointer, item));

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (Beatmap != null)
            {
                Beatmap.HitObjectAdded -= AddBlueprintFor;
                Beatmap.HitObjectRemoved -= RemoveBlueprintFor;
            }

            usageEventBuffer?.Dispose();
        }
    }
}
