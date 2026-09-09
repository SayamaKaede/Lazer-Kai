// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Lines;
using osu.Framework.Input.Events;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders
{
    public partial class SliderEndDragMarker : SmoothPath
    {
        public Action<DragStartEvent>? StartDrag { get; set; }
        public Action<DragEvent>? Drag { get; set; }
        public Action? EndDrag { get; set; }

        /// <summary>
        /// The slider this marker adjusts the length of, once it is known.
        /// </summary>
        /// <remarks>
        /// Only used to leave it out of <see cref="ReceivePositionalInputAt"/>'s reckoning below, so the marker is
        /// not made to stand aside for the very object it belongs to.
        /// </remarks>
        public HitObject? Owner { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private HitObjectComposer? composer { get; set; }

        /// <summary>
        /// The thickness the marker is drawn at before the user's setting scales it.
        /// </summary>
        private const float default_path_radius = 5;

        private readonly Bindable<EditorSelectionColour> markerColour = new Bindable<EditorSelectionColour>();
        private readonly Bindable<float> markerScale = new Bindable<float>(1);
        private readonly Bindable<float> markerOpacity = new Bindable<float>(1);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorSliderEndDragMarkerColour, markerColour);
            config.BindWith(OsuSetting.EditorSliderEndDragMarkerScale, markerScale);
            config.BindWith(OsuSetting.EditorSliderEndDragMarkerOpacity, markerOpacity);

            markerColour.BindValueChanged(_ => updateState());
            markerOpacity.BindValueChanged(_ => updateState());
            markerScale.BindValueChanged(scale => PathRadius = default_path_radius * scale.NewValue, true);

            var path = PathApproximator.CircularArcToPiecewiseLinear([
                new Vector2(0, OsuHitObject.OBJECT_RADIUS),
                new Vector2(OsuHitObject.OBJECT_RADIUS, 0),
                new Vector2(0, -OsuHitObject.OBJECT_RADIUS)
            ]);

            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;
            Vertices = path;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            updateState();
        }

        /// <summary>
        /// Stands aside where a selected object is under the cursor.
        /// </summary>
        /// <remarks>
        /// The marker is drawn on top of everything behind the slider's tail and takes the mouse for itself, so a
        /// circle sitting under that tail could be seen and selected but not picked up -- every attempt caught the
        /// slider instead. What is selected is what the user has already said they are working on, so it wins.
        ///
        /// Declining positional input rather than the click itself, so the marker does not light up under the
        /// cursor either: offering a grab that will not happen is the same problem one step earlier.
        ///
        /// The slider this marker belongs to does not count. When it is the selected one, the marker is the point.
        /// </remarks>
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
        {
            if (!base.ReceivePositionalInputAt(screenSpacePos))
                return false;

            var selected = composer?.BlueprintContainer.SelectionHandler.SelectedBlueprints;

            if (selected == null)
                return true;

            foreach (var blueprint in selected)
            {
                if (blueprint.Item != Owner && blueprint.ReceivePositionalInputAt(screenSpacePos))
                    return false;
            }

            return true;
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return true;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        protected override bool OnDragStart(DragStartEvent e)
        {
            updateState();
            StartDrag?.Invoke(e);
            return true;
        }

        protected override void OnDrag(DragEvent e)
        {
            updateState();
            base.OnDrag(e);
            Drag?.Invoke(e);
        }

        protected override void OnDragEnd(DragEndEvent e)
        {
            updateState();
            EndDrag?.Invoke();
            base.OnDragEnd(e);
        }

        protected override bool OnMouseDown(MouseDownEvent e) => e.Button == MouseButton.Left;

        protected override bool OnClick(ClickEvent e) => e.Button == MouseButton.Left;

        private void updateState()
        {
            // the dragged colour is left alone: it is what tells the user the marker has hold of the slider.
            Colour = IsHovered || IsDragged ? colours.Red : markerColour.Value.ToColour() ?? colours.Yellow;
            Alpha = markerOpacity.Value;
        }
    }
}
