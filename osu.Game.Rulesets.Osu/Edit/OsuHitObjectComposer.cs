// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Framework.Caching;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Cursor;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Edit.Tools;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Input;

using osu.Game.Input.Bindings;

namespace osu.Game.Rulesets.Osu.Edit
{
    [Cached]
    public partial class OsuHitObjectComposer : HitObjectComposer<OsuHitObject, OsuAction>
    {
        /// <summary>
        /// Amount the slider velocity shortcuts move the velocity by, and the smaller step used when holding the fine modifier.
        /// </summary>
        private const double velocity_adjust_step = 0.1;

        private const double velocity_adjust_step_fine = 0.01;

        // not present in test scenes which build the composer without a game around it.
        [Resolved(CanBeNull = true)]
        private OnScreenDisplay onScreenDisplay { get; set; }

        public OsuHitObjectComposer(Ruleset ruleset)
            : base(ruleset)
        {
        }

        public override Bindable<TernaryState> SelectionNewComboState { get; } = new Bindable<TernaryState>();

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.EditorDecreaseSliderVelocity:
                    adjustSliderVelocity(-velocity_adjust_step);
                    return true;

                case GlobalAction.EditorIncreaseSliderVelocity:
                    adjustSliderVelocity(velocity_adjust_step);
                    return true;

                case GlobalAction.EditorDecreaseSliderVelocityFine:
                    adjustSliderVelocity(-velocity_adjust_step_fine);
                    return true;

                case GlobalAction.EditorIncreaseSliderVelocityFine:
                    adjustSliderVelocity(velocity_adjust_step_fine);
                    return true;
            }

            return base.OnPressed(e);
        }

        /// <summary>
        /// The velocity control lives in the right-hand toolbox, which is not where the eye is while placing objects,
        /// so the value landed on is called out over the playfield as well.
        /// </summary>
        private void adjustSliderVelocity(double delta)
            => onScreenDisplay?.Display(new EditorToast(EditorStrings.SliderVelocityAdjusted(sliderVelocityToolboxGroup.AdjustVelocity(delta))));

        protected override DrawableRuleset<OsuHitObject> CreateDrawableRuleset(Ruleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new DrawableOsuEditorRuleset(ruleset, beatmap, mods);

        /// <summary>
        /// Held rather than created inline so its button can be found again when the user asks for it to be hidden.
        /// </summary>
        private readonly GridFromPointsTool gridFromPointsTool = new GridFromPointsTool();

        protected override IReadOnlyList<CompositionTool<OsuAction>> CompositionTools => new CompositionTool<OsuAction>[]
        {
            new HitCircleCompositionTool(),
            new SliderCompositionTool(),
            new SpinnerCompositionTool(),
            gridFromPointsTool,
            new StreamCompositionTool()
        };

        private readonly Bindable<bool> hideGridFromPointsTool = new Bindable<bool>();

        private readonly Bindable<bool> swapRightAndMiddleMouse = new Bindable<bool>();

        /// <summary>
        /// The button which turns new combo on and off.
        /// </summary>
        /// <remarks>
        /// Deliberately tied to the physical right button rather than to <see cref="MouseButton.Right"/>, the same way
        /// a slider's path is finished: this is stable's muscle memory and belongs on the button the hand reaches for,
        /// whether or not the two are swapped. The swap happens at the input source, so that button arrives here as
        /// <see cref="MouseButton.Middle"/>.
        /// </remarks>
        private MouseButton newComboButton => swapRightAndMiddleMouse.Value ? MouseButton.Middle : MouseButton.Right;

        private readonly Bindable<TernaryState> rectangularGridSnapToggle = new Bindable<TernaryState>();

        protected override Drawable CreateHitObjectInspector() => new OsuHitObjectInspector(DistanceSnapProvider);

        /// <summary>
        /// A position as the beatmap should hold it: on whole coordinates while the setting asks for that.
        /// </summary>
        /// <remarks>
        /// The game keeps a fraction of a pixel and writes it out, but stable and the website read a whole number,
        /// so a position half a pixel off lands somewhere else the moment the beatmap leaves the editor -- onto a
        /// stack it was never meant to be on, or off one it was.
        ///
        /// The fraction is cut off, as stable does, rather than rounded (2026-10-05). Two circles stacked on a slider's
        /// end at (416.59, 195.76) were put on (417, 196) here, and stable, stacking them on it again, put them on
        /// (416, 195): read from what stable showed for both before and after, with the stack's offset taken off.
        /// </remarks>
        public Vector2 RoundPositionIfAsked(Vector2 position) =>
            integerPositions.Value ? new Vector2(cutFraction(position.X), cutFraction(position.Y)) : position;

        /// <summary>
        /// Whether positions are kept on whole coordinates, as <see cref="RoundPositionIfAsked"/> does.
        /// </summary>
        public bool KeepsWholeCoordinates => integerPositions.Value;

        /// <summary>
        /// Puts a slider's head and every anchor of its path on whole coordinates, while the setting asks for that.
        /// </summary>
        /// <remarks>
        /// The anchors, not only the head (2026-10-05). They are held here as offsets from the head and were left as
        /// fractions, while stable holds each as whole coordinates of its own and the save writes them that way; the
        /// path, its end and whatever was stacked on that end moved once the beatmap was opened again. un!stable
        /// does the same ("slider paths use truncated coordinates"). Each anchor is cut where it is, so one that was
        /// already whole stays exactly where it was whatever happens to the head.
        /// </remarks>
        public void PutPathOnWholeCoordinatesIfAsked<T>(T hitObject)
            where T : OsuHitObject, IHasPath
        {
            if (!integerPositions.Value)
                return;

            Vector2 head = RoundPositionIfAsked(hitObject.Position);
            var points = hitObject.Path.ControlPoints;

            for (int i = 0; i < points.Count; i++)
            {
                Vector2 whole = RoundPositionIfAsked(hitObject.Position + points[i].Position) - head;

                if (points[i].Position != whole)
                    points[i].Position = whole;
            }

            if (hitObject.Position != head)
                hitObject.Position = head;
        }

        /// <summary>
        /// A value a hair under a whole number is that number. What a snap decides goes through screen space and
        /// back, and a slider's end on 192 comes out as 191.99998, which cutting the fraction off would put on 191.
        /// </summary>
        private static float cutFraction(float value)
        {
            float nearest = MathF.Round(value);
            return MathF.Abs(value - nearest) < 0.001f ? nearest : MathF.Truncate(value);
        }

        private readonly Bindable<bool> integerPositions = new Bindable<bool>();


        public override EditorInspector CreateCompactInspector(float textSize) =>
            new OsuHitObjectInspector(DistanceSnapProvider) { Compact = true, TextSize = textSize };

        protected override IEnumerable<Drawable> CreateTernaryButtons()
            => base.CreateTernaryButtons()
                   .Append(new DrawableTernaryButton<OsuAction>
                   {
                       Current = rectangularGridSnapToggle,
                       Description = "Grid Snap",
                       CreateIcon = () => new SpriteIcon { Icon = OsuIcon.EditorGridSnap },
                       Action = OsuAction.EditorToggleGridSnap,
                       Hotkey = HotkeyForAction(OsuAction.EditorToggleGridSnap)
                   })
                   .Concat(DistanceSnapProvider.CreateTernaryButtons());

        private BindableList<HitObject> selectedHitObjects;

        private Bindable<HitObject> placementObject;

        [Cached(typeof(IDistanceSnapProvider))]
        public readonly OsuDistanceSnapProvider DistanceSnapProvider = new OsuDistanceSnapProvider();

        [Cached]
        private readonly OsuSliderVelocityToolboxGroup sliderVelocityToolboxGroup = new OsuSliderVelocityToolboxGroup();

        [Cached]
        protected readonly OsuGridToolboxGroup OsuGridToolboxGroup = new OsuGridToolboxGroup();

        [Cached]
        protected readonly FreehandSliderToolboxGroup FreehandSliderToolboxGroup = new FreehandSliderToolboxGroup();

        /// <summary>
        /// The settings shown while a slider is being turned into a stream. Lives here rather than with the
        /// slider it is about, which stops existing partway through the conversion.
        /// </summary>
        [Cached]
        protected readonly StreamConversionPanel StreamConversionPanel = new StreamConversionPanel();

        /// <summary>
        /// Where autoplay's cursor would be. Kept because flashlight reads its position as well as drawing it.
        /// </summary>
        private AutoCursorDisplay autoCursor;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            // the toolbox buttons are added by the base composer, which has already run by this point.
            config.BindWith(OsuSetting.EditorHideGridFromPointsTool, hideGridFromPointsTool);
            config.BindWith(OsuSetting.SwapRightAndMiddleMouse, swapRightAndMiddleMouse);
            config.BindWith(OsuSetting.EditorStableSliderControls, stableSliderControls);
            config.BindWith(OsuSetting.EditorSliderAnchorType, anchorType);
            config.BindWith(OsuSetting.EditorAllowOutsidePlayArea, allowOutsidePlayArea);
            config.BindWith(OsuSetting.EditorIntegerPositions, integerPositions);
            hideGridFromPointsTool.BindValueChanged(hidden => SetToolVisibility(gridFromPointsTool, !hidden.NewValue), true);

            AddInternal(DistanceSnapProvider);
            AddInternal(StreamConversionPanel);

            // in the playfield's own space, above everything else there, so the anchors sit where the shape is.
            PlayfieldContentContainer.Add(DrawableRuleset.CreatePlayfieldAdjustmentContainer().WithChild(new StreamShapeEditor()));

            // over the objects rather than under them, the way a cursor sits over what it is pointing at.
            PlayfieldContentContainer.Add(DrawableRuleset.CreatePlayfieldAdjustmentContainer().WithChild(autoCursor = new AutoCursorDisplay()));
            DistanceSnapProvider.AttachToToolbox(RightToolbox);

            // Give a bit of breathing room around the playfield content.
            PlayfieldContentContainer.Padding = new MarginPadding(10);

            LayerBelowRuleset.Add(new OsuOffscreenDisplay(Playfield));

            LayerBelowRuleset.Add(
                distanceSnapGridContainer = new Container
                {
                    RelativeSizeAxes = Axes.Both
                }
            );

            selectedHitObjects = EditorBeatmap.SelectedHitObjects.GetBoundCopy();
            selectedHitObjects.CollectionChanged += (_, _) => updateDistanceSnapGrid();

            placementObject = EditorBeatmap.PlacementObject.GetBoundCopy();
            placementObject.ValueChanged += _ => updateDistanceSnapGrid();
            DistanceSnapProvider.DistanceSnapToggle.ValueChanged += _ => updateDistanceSnapGrid();

            // we may be entering the screen with a selection already active
            updateDistanceSnapGrid();

            OsuGridToolboxGroup.GridType.BindValueChanged(updatePositionSnapGrid, true);

            RightToolbox.AddRange(new Drawable[]
                {
                    new OsuContextMenuContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Child = sliderVelocityToolboxGroup,
                    },
                    OsuGridToolboxGroup,
                    new TransformToolboxGroup
                    {
                        RotationHandler = BlueprintContainer.SelectionHandler.RotationHandler,
                        ScaleHandler = (OsuSelectionScaleHandler)BlueprintContainer.SelectionHandler.ScaleHandler,
                        GridToolbox = OsuGridToolboxGroup,
                    },
                    new GenerateToolboxGroup(),
                    FreehandSliderToolboxGroup
                }
            );
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // the overlays have been handed to the editor by now, which is where the flashlight ends up.
            leadFlashlight();
        }

        /// <summary>
        /// Points the previewed flashlight at autoplay's cursor while it is shown, and at the mouse otherwise.
        /// </summary>
        /// <remarks>
        /// Autoplay's cursor on screen is what the mapper is watching, so the lit area goes with it. Without it there is
        /// nothing else to follow, and following autoplay's path anyway lit the objects wherever the mapper looked
        /// (2026-09-26). The mouse is read every frame, rather than on its moves as in play, so that the lit area
        /// catches up with a pointer that has stopped.
        /// </remarks>
        private void leadFlashlight()
        {
            var flashlight = ModOverlays.SelectMany(o => o.ChildrenOfType<ModFlashlight<OsuHitObject>.Flashlight>()).FirstOrDefault();

            if (flashlight == null)
                return;

            flashlight.ScreenSpacePositionSource = () => autoCursor.ScreenSpacePosition ?? GetContainingInputManager()?.CurrentState.Mouse.Position;
        }

        private void updatePositionSnapGrid(ValueChangedEvent<PositionSnapGridType> obj)
        {
            if (positionSnapGrid != null)
                LayerBelowRuleset.Remove(positionSnapGrid, true);

            switch (obj.NewValue)
            {
                case PositionSnapGridType.Square:
                    var rectangularPositionSnapGrid = new RectangularPositionSnapGrid();

                    rectangularPositionSnapGrid.Spacing.BindTo(OsuGridToolboxGroup.SpacingVector);
                    rectangularPositionSnapGrid.GridLineRotation.BindTo(OsuGridToolboxGroup.GridLinesRotation);

                    positionSnapGrid = rectangularPositionSnapGrid;
                    break;

                case PositionSnapGridType.Triangle:
                    var triangularPositionSnapGrid = new TriangularPositionSnapGrid();

                    triangularPositionSnapGrid.Spacing.BindTo(OsuGridToolboxGroup.GridLineSpacing);
                    triangularPositionSnapGrid.GridLineRotation.BindTo(OsuGridToolboxGroup.GridLinesRotation);

                    positionSnapGrid = triangularPositionSnapGrid;
                    break;

                case PositionSnapGridType.Circle:
                    var circularPositionSnapGrid = new CircularPositionSnapGrid();

                    circularPositionSnapGrid.Spacing.BindTo(OsuGridToolboxGroup.GridLineSpacing);

                    positionSnapGrid = circularPositionSnapGrid;
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(OsuGridToolboxGroup.GridType), OsuGridToolboxGroup.GridType, "Unsupported grid type.");
            }

            // Bind the start position to the toolbox sliders.
            positionSnapGrid.StartPosition.BindTo(OsuGridToolboxGroup.StartPosition);

            positionSnapGrid.RelativeSizeAxes = Axes.Both;
            LayerBelowRuleset.Add(positionSnapGrid);
        }

        protected override ComposeBlueprintContainer CreateBlueprintContainer()
            => new OsuBlueprintContainer(this);

        public override bool SupportsPlacementPresets => true;

        public override Vector2 PlacementPresetCentre => OsuPlayfield.BASE_SIZE / 2;

        public override float PlacementPresetObjectRadius
            => OsuHitObject.OBJECT_RADIUS * LegacyRulesetExtensions.CalculateScaleFromCircleSize(EditorBeatmap.Difficulty.CircleSize, true);

        public override void ApplyPlacementDefaults(HitObject hitObject)
        {
            // the velocity is what decides how long the shape lasts, and a preset carries the shape rather than the
            // reading. Taking the toolbox's value is what the slider tool does, so a preset and a shape drawn by
            // hand come out the same length.
            if (hitObject is IHasSliderVelocity hasSliderVelocity)
                hasSliderVelocity.SliderVelocityMultiplier = sliderVelocityToolboxGroup.SliderVelocity.Value;
        }

        public override string ConvertSelectionToString()
            => string.Join(',', selectedHitObjects.Cast<OsuHitObject>().OrderBy(h => h.StartTime)
                                                  .Select(h => (h.IndexInCurrentCombo + 1).ToString(CultureInfo.InvariantCulture)));

        // 1,2,3,4 ...
        private static readonly Regex selection_regex = new Regex(@"^\d+(,\d+)*$", RegexOptions.Compiled);

        public override void SelectFromTimestamp(double timestamp, string objectDescription)
        {
            if (!selection_regex.IsMatch(objectDescription))
                return;

            List<OsuHitObject> remainingHitObjects = EditorBeatmap.HitObjects.Cast<OsuHitObject>().Where(h => h.StartTime >= timestamp).ToList();
            string[] splitDescription = objectDescription.Split(',');

            for (int i = 0; i < splitDescription.Length; i++)
            {
                if (!int.TryParse(splitDescription[i], out int combo) || combo < 1)
                    continue;

                OsuHitObject current = remainingHitObjects.FirstOrDefault(h => h.IndexInCurrentCombo + 1 == combo);

                if (current == null)
                    continue;

                EditorBeatmap.SelectedHitObjects.Add(current);

                if (i < splitDescription.Length - 1)
                    remainingHitObjects = remainingHitObjects.Where(h => h != current && h.StartTime >= current.StartTime).ToList();
            }
        }

        private DistanceSnapGrid distanceSnapGrid;
        private Container distanceSnapGridContainer;

        private readonly Cached distanceSnapGridCache = new Cached();
        private double? lastDistanceSnapGridTime;

        private PositionSnapGrid positionSnapGrid;

        protected override void Update()
        {
            base.Update();

            if (!(BlueprintContainer.CurrentTool is SelectTool))
            {
                if (EditorClock.CurrentTime != lastDistanceSnapGridTime)
                {
                    distanceSnapGridCache.Invalidate();
                    lastDistanceSnapGridTime = EditorClock.CurrentTime;
                }

                if (!distanceSnapGridCache.IsValid)
                    updateDistanceSnapGrid();
            }
        }

        [CanBeNull]
        public SnapResult TrySnapToNearbyObjects(Vector2 screenSpacePosition, double? fallbackTime = null)
        {
            if (!snapToVisibleBlueprints(screenSpacePosition, out var snapResult))
                return null;

            if (DistanceSnapProvider.DistanceSnapToggle.Value != TernaryState.True || distanceSnapGrid == null)
                return snapResult;

            // In the case of snapping to nearby objects, a time value is not provided.
            // This matches the stable editor (which also uses current time), but with the introduction of time-snapping distance snap
            // this could result in unexpected behaviour when distance snapping is turned on and a user attempts to place an object that is
            // BOTH on a valid distance snap ring, and also at the same position as a previous object.
            //
            // We want to ensure that in this particular case, the time-snapping component of distance snap is still applied.
            // The easiest way to ensure this is to attempt application of distance snap after a nearby object is found, and copy over
            // the time value if the proposed positions are roughly the same.
            (Vector2 distanceSnappedPosition, double distanceSnappedTime) = distanceSnapGrid.GetSnappedPosition(distanceSnapGrid.ToLocalSpace(snapResult.ScreenSpacePosition));
            snapResult.Time = Precision.AlmostEquals(distanceSnapGrid.ToScreenSpace(distanceSnappedPosition), snapResult.ScreenSpacePosition, 1)
                ? distanceSnappedTime
                : fallbackTime;

            return snapResult;
        }

        [CanBeNull]
        public SnapResult TrySnapToDistanceGrid(Vector2 screenSpacePosition, double? fixedTime = null)
        {
            if (DistanceSnapProvider.DistanceSnapToggle.Value != TernaryState.True || distanceSnapGrid?.IsLoaded != true)
                return null;

            var playfield = PlayfieldAtScreenSpacePosition(screenSpacePosition);
            (Vector2 pos, double time) = distanceSnapGrid.GetSnappedPosition(distanceSnapGrid.ToLocalSpace(screenSpacePosition), fixedTime);

            if (pos.X < 0 || pos.X > OsuPlayfield.BASE_SIZE.X || pos.Y < 0 || pos.Y > OsuPlayfield.BASE_SIZE.Y)
                return null;

            return new SnapResult(distanceSnapGrid.ToScreenSpace(pos), time, playfield);
        }

        [CanBeNull]
        public SnapResult TrySnapToPositionGrid(Vector2 screenSpacePosition, double? fallbackTime = null)
        {
            if (rectangularGridSnapToggle.Value != TernaryState.True)
                return null;

            Vector2 pos = positionSnapGrid.GetSnappedPosition(positionSnapGrid.ToLocalSpace(screenSpacePosition));

            // A grid which doesn't perfectly fit the playfield can produce a position that is outside of the playfield.
            // We need to clamp the position to the playfield bounds to ensure that the snapped position is always in bounds.
            // The grid carries on past the play area, so while objects are allowed out there, the snap follows it out.
            if (!allowOutsidePlayArea.Value)
                pos = Vector2.Clamp(pos, Vector2.Zero, OsuPlayfield.BASE_SIZE);

            var playfield = PlayfieldAtScreenSpacePosition(screenSpacePosition);
            return new SnapResult(positionSnapGrid.ToScreenSpace(pos), fallbackTime, playfield);
        }

        private bool snapToVisibleBlueprints(Vector2 screenSpacePosition, out SnapResult snapResult)
        {
            // check other on-screen objects for snapping/stacking
            var blueprints = BlueprintContainer.SelectionBlueprints.AliveChildren;

            var playfield = PlayfieldAtScreenSpacePosition(screenSpacePosition);

            float inScreenSpace(float gamefieldPixels) =>
                playfield.GamefieldToScreenSpace(new Vector2(gamefieldPixels)).X -
                playfield.GamefieldToScreenSpace(Vector2.Zero).X;

            float snapRadius = inScreenSpace(OsuHitObject.OBJECT_RADIUS * 0.10f);

            foreach (var b in blueprints)
            {
                if (b.IsSelected)
                    continue;

                var snapPositions = b.ScreenSpaceSnapPoints;

                if (!snapPositions.Any())
                    continue;

                // if the snap target is a stacked object, snap to its unstacked position rather than its stacked position.
                // this is intended to make working with stacks easier (because thanks to this, you can drag an object to any
                // of the items on the stack to add an object to it, rather than having to drag to the position of the *first* object on it at all times).
                Vector2 unstacked(Vector2 p) => b.Item is OsuHitObject osuObject && osuObject.StackOffset != Vector2.Zero
                    ? b.ToScreenSpace(b.ToLocalSpace(p) - osuObject.StackOffset)
                    : p;

                // in reach of where it is drawn or of where it is stored (2026-10-05). An object that comes to be stacked
                // on what is being brought to it is drawn a stack's worth away the moment it is; measured only from where
                // it is drawn, it went out of reach, the snap let go, the stack came off and the snap took hold again, on
                // every movement -- a slider held with its end beside a circle shook between the two places.
                float reach(Vector2 p) => MathF.Min(Vector2.Distance(p, screenSpacePosition), Vector2.Distance(unstacked(p), screenSpacePosition));

                var closestSnapPosition = snapPositions.MinBy(reach);

                if (reach(closestSnapPosition) < snapRadius)
                {
                    var snapPosition = unstacked(closestSnapPosition);

                    // only return distance portion, since time is not really valid
                    snapResult = new SnapResult(snapPosition, null, playfield);
                    return true;
                }
            }

            snapResult = null;
            return false;
        }

        private void updateDistanceSnapGrid()
        {
            distanceSnapGridContainer.Clear();
            distanceSnapGridCache.Invalidate();
            distanceSnapGrid = null;

            if (DistanceSnapProvider.DistanceSnapToggle.Value != TernaryState.True)
                return;

            switch (BlueprintContainer.CurrentTool)
            {
                case SelectTool:
                    if (!EditorBeatmap.SelectedHitObjects.Any())
                        return;

                    distanceSnapGrid = createDistanceSnapGrid(EditorBeatmap.SelectedHitObjects);
                    break;

                default:
                    if (!CursorInPlacementArea)
                        return;

                    distanceSnapGrid = createDistanceSnapGrid(Enumerable.Empty<HitObject>());
                    break;
            }

            if (distanceSnapGrid != null)
            {
                distanceSnapGridContainer.Add(distanceSnapGrid);
                distanceSnapGridCache.Validate();
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            // Why is this logic here and not in `OsuSelectionHandler`?
            // Because we only want to handle this toggle after all other right-click handling completes.
            //
            // Consider that input is handled from the most nested child first:
            //
            // ComposeScreen
            //  |- OsuContextMenuContainer                 // right click for context
            //     |- TimelineBlueprintContainer
            //        |- TimelineSelectionHandler
            //     |- (Osu)HitObjectComposer               // right click for toggle new combo
            //        |- (Osu)EditorBlueprintContainer     // right click for select
            //           |- (Osu)EditorSelectionHandler    // right click for delete
            if (e.Button == newComboButton)
            {
                var osuSelectionHandler = (OsuSelectionHandler)BlueprintContainer.SelectionHandler;

                if (!osuSelectionHandler.SelectedItems.Any())
                {
                    SelectionNewComboState!.Value = SelectionNewComboState.Value == TernaryState.False ? TernaryState.True : TernaryState.False;
                    return true;
                }
            }

            return base.OnMouseDown(e);
        }

        private readonly Bindable<bool> stableSliderControls = new Bindable<bool>();

        private readonly Bindable<bool> allowOutsidePlayArea = new Bindable<bool>();
        private readonly Bindable<SliderAnchorType> anchorType = new Bindable<SliderAnchorType>();

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Repeat)
                return false;

            if (tryPickAnchorType(e))
                return true;

            handleToggleViaKey(e);
            return base.OnKeyDown(e);
        }

        /// <summary>
        /// Ctrl and a number pick the shape a new anchor makes, which is otherwise a trip to the settings.
        /// </summary>
        /// <remarks>
        /// The shape is only used while sliders are shaped stable's way, so the keys do nothing while that is off
        /// rather than quietly setting something that has no effect. Alt and a number is the editor's own shortcut
        /// for the type of an anchor already placed, and is left alone.
        /// </remarks>
        private bool tryPickAnchorType(KeyDownEvent e)
        {
            if (!stableSliderControls.Value || !e.ControlPressed || e.ShiftPressed || e.AltPressed)
                return false;

            if (e.Key < Key.Number1 || e.Key > Key.Number4)
                return false;

            anchorType.Value = (SliderAnchorType)(e.Key - Key.Number1);
            onScreenDisplay?.Display(new EditorToast(EditorStrings.AnchorAddTypeChanged(anchorType.Value.GetLocalisableDescription())));
            return true;
        }

        protected override void OnKeyUp(KeyUpEvent e)
        {
            handleToggleViaKey(e);
            base.OnKeyUp(e);
        }

        private bool gridSnapMomentary;

        private void handleToggleViaKey(KeyboardEvent key)
        {
            bool shiftPressed = key.ShiftPressed;

            if (shiftPressed != gridSnapMomentary)
            {
                gridSnapMomentary = shiftPressed;
                rectangularGridSnapToggle.Value = rectangularGridSnapToggle.Value == TernaryState.False ? TernaryState.True : TernaryState.False;
            }

            DistanceSnapProvider.HandleToggleViaKey(key);
        }

        private DistanceSnapGrid createDistanceSnapGrid(IEnumerable<HitObject> selectedHitObjects)
        {
            if (BlueprintContainer.CurrentTool is SpinnerCompositionTool)
                return null;

            var objects = selectedHitObjects.ToList();

            if (objects.Count == 0)
                // use accurate time value to give more instantaneous feedback to the user.
                return createGrid(h => h.StartTime <= EditorClock.CurrentTimeAccurate);

            double minTime = objects.Min(h => h.StartTime);
            return createGrid(h => h.StartTime < minTime, objects.Count + 1);
        }

        /// <summary>
        /// Creates a grid from the last <see cref="HitObject"/> matching a predicate to a target <see cref="HitObject"/>.
        /// </summary>
        /// <param name="sourceSelector">A predicate that matches <see cref="HitObject"/>s where the grid can start from.
        /// Only the last <see cref="HitObject"/> matching the predicate is used.</param>
        /// <param name="targetOffset">An offset from the <see cref="HitObject"/> selected via <paramref name="sourceSelector"/> at which the grid should stop.</param>
        /// <returns>The <see cref="OsuDistanceSnapGrid"/> from a selected <see cref="HitObject"/> to a target <see cref="HitObject"/>.</returns>
        private OsuDistanceSnapGrid createGrid(Func<HitObject, bool> sourceSelector, int targetOffset = 1)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(targetOffset);

            int positionSourceObjectIndex = -1;
            IHasSliderVelocity sliderVelocitySource = null;

            for (int i = 0; i < EditorBeatmap.HitObjects.Count; i++)
            {
                if (!sourceSelector(EditorBeatmap.HitObjects[i]))
                    break;

                positionSourceObjectIndex = i;

                if (EditorBeatmap.HitObjects[i] is IHasSliderVelocity hasSliderVelocity)
                    sliderVelocitySource = hasSliderVelocity;
            }

            if (positionSourceObjectIndex == -1)
                return null;

            HitObject sourceObject = EditorBeatmap.HitObjects[positionSourceObjectIndex];

            int targetIndex = positionSourceObjectIndex + targetOffset;
            HitObject targetObject = null;

            // Keep advancing the target object while its start time falls before the end time of the source object
            while (true)
            {
                if (targetIndex >= EditorBeatmap.HitObjects.Count)
                    break;

                if (EditorBeatmap.HitObjects[targetIndex].StartTime >= sourceObject.GetEndTime())
                {
                    targetObject = EditorBeatmap.HitObjects[targetIndex];
                    break;
                }

                targetIndex++;
            }

            if (sourceObject is Spinner)
                return null;

            return new OsuDistanceSnapGrid((OsuHitObject)sourceObject, (OsuHitObject)targetObject, sliderVelocitySource);
        }
    }
}
