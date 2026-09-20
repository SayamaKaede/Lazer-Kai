// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The anchors of a stream's shape, shown while the panel is open on one.
    /// </summary>
    /// <remarks>
    /// The shape itself is never drawn and never goes into the beatmap. What is being made is a run of circles, and
    /// a slider drawn among them says nothing about how that run reads -- but the shape still has to be reachable,
    /// so its anchors and the line between them are put up on their own, the way they are while it is being drawn.
    ///
    /// Ctrl-click adds an anchor, which a selected slider does for itself. Nothing is taken from the buttons here:
    /// deleting an anchor is how a shape is tidied up, and holding the right button back to save the run took that
    /// away too. The run looks after itself instead -- the panel puts back anything that is deleted out from under
    /// it, wherever the deleting was done from.
    /// </remarks>
    public partial class StreamShapeEditor : CompositeDrawable
    {
        [Resolved]
        private StreamConversionPanel panel { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private IEditorChangeHandler? changeHandler { get; set; }

        private readonly Bindable<Slider?> shape = new Bindable<Slider?>();

        private readonly Bindable<bool> stableSliderControls = new Bindable<bool>();
        private readonly Bindable<SliderAnchorType> anchorType = new Bindable<SliderAnchorType>();

        private PathControlPointVisualiser<Slider>? visualiser;

        private PathControlPoint? placing;

        public StreamShapeEditor()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorStableSliderControls, stableSliderControls);
            config.BindWith(OsuSetting.EditorSliderAnchorType, anchorType);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            shape.BindTo(panel.Shape);
            shape.BindValueChanged(s => show(s.NewValue), true);
        }

        private void show(Slider? slider)
        {
            ClearInternal();
            visualiser = null;
            placing = null;

            if (slider != null)
                AddInternal(visualiser = new PathControlPointVisualiser<Slider>(slider, true));
        }

        // in the way of the playfield only while there is a shape being shaped, and then only to keep hold of the
        // buttons that would otherwise reach past it.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => shape.Value != null;

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (shape.Value == null)
                return false;

            switch (e.Button)
            {
                case MouseButton.Left when e.ControlPressed:
                    changeHandler?.BeginChange();
                    placing = addAnchor(shape.Value, e.MousePosition);
                    visualiser?.SetSelectionTo(placing);
                    return true;
            }

            return false;
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            if (placing == null)
                return false;

            visualiser?.DragStarted(placing);
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            base.OnDrag(e);

            if (placing != null)
                visualiser?.DragInProgress(e);
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            if (placing == null)
                return;

            if (IsDragged)
                visualiser?.DragEnded();

            placing = null;
            changeHandler?.EndChange();
        }

        /// <summary>
        /// Puts an anchor into the stretch of path the click landed nearest to.
        /// </summary>
        private PathControlPoint addAnchor(Slider slider, Vector2 position)
        {
            var controlPoints = slider.Path.ControlPoints;

            position -= slider.Position;

            int insertionIndex = 0;
            float nearest = float.MaxValue;

            for (int i = 0; i < controlPoints.Count - 1; i++)
            {
                float distance = new Line(controlPoints[i].Position, controlPoints[i + 1].Position).DistanceToPoint(position);

                if (distance >= nearest)
                    continue;

                insertionIndex = i + 1;
                nearest = distance;
            }

            var anchor = new PathControlPoint { Position = position };

            controlPoints.Insert(insertionIndex, anchor);

            if (stableSliderControls.Value)
                SliderAnchorInsertion.ShapeAroundInsertion(controlPoints, insertionIndex, anchorType.Value.ToPathType());

            visualiser?.EnsureValidPathTypes();

            // as long as it is drawn, and no longer: the shape is not a slider with a length of its own.
            slider.Path.ExpectedDistance.Value = null;

            return anchor;
        }
    }
}
