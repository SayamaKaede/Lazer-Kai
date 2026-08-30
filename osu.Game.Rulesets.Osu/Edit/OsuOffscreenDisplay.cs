// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Rulesets.UI;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Outlines the area objects must stay inside to remain visible during gameplay, and marks the objects which
    /// currently break out of it.
    /// </summary>
    /// <remarks>
    /// Only objects the playfield is currently showing are considered. Marking an object the user cannot see achieves
    /// nothing, and the handful on screen can be re-checked every frame, so a marker follows an object being dragged
    /// instead of waiting for the edit to be committed.
    ///
    /// The bounds and the offscreen test are shared with <see cref="CheckOffscreenObjects"/>, so what is drawn here
    /// always agrees with what beatmap verification reports.
    /// </remarks>
    public partial class OsuOffscreenDisplay : CompositeDrawable
    {
        /// <summary>
        /// How far outside an object's own radius the warning ring is drawn.
        /// </summary>
        private const float ring_padding = 6;

        private const float ring_thickness = 3;

        private const float border_alpha = 0.35f;

        private readonly Playfield playfield;

        private Container<Drawable> markers = null!;
        private Container playAreaBorder = null!;

        private readonly Bindable<bool> showBorder = new Bindable<bool>();
        private readonly Bindable<bool> detectOffscreen = new Bindable<bool>();

        private readonly Dictionary<OsuHitObject, Drawable> currentMarkers = new Dictionary<OsuHitObject, Drawable>();

        /// <summary>
        /// Reused each frame so the sweep below does not allocate.
        /// </summary>
        private readonly HashSet<OsuHitObject> seen = new HashSet<OsuHitObject>();

        private readonly List<OsuHitObject> toRemove = new List<OsuHitObject>();

        public OsuOffscreenDisplay(Playfield playfield)
        {
            this.playfield = playfield;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                playAreaBorder = new Container
                {
                    Masking = true,
                    BorderThickness = 2,
                    // matches the composer's grid rather than the warning colour: this is a reference line, not a problem.
                    BorderColour = Color4.White,
                    Alpha = border_alpha,
                    // positioned in playfield coordinates, which is what the bounds are expressed in.
                    Position = new Vector2(CheckOffscreenObjects.MIN_X, CheckOffscreenObjects.MIN_Y),
                    Size = new Vector2(CheckOffscreenObjects.MAX_X - CheckOffscreenObjects.MIN_X,
                        CheckOffscreenObjects.MAX_Y - CheckOffscreenObjects.MIN_Y),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        AlwaysPresent = true,
                        Alpha = 0,
                    }
                },
                markers = new Container { RelativeSizeAxes = Axes.Both },
            };

            config.BindWith(OsuSetting.EditorShowPlayAreaBorder, showBorder);
            config.BindWith(OsuSetting.EditorDetectOffscreen, detectOffscreen);
        }

        protected override void Update()
        {
            base.Update();

            playAreaBorder.Alpha = showBorder.Value ? border_alpha : 0;

            if (!detectOffscreen.Value)
            {
                if (currentMarkers.Count > 0)
                {
                    markers.Clear();
                    currentMarkers.Clear();
                }

                return;
            }

            seen.Clear();

            foreach (var drawable in playfield.HitObjectContainer.AliveObjects)
            {
                if (drawable.HitObject is not OsuHitObject hitObject)
                    continue;

                if (!isOffscreen(hitObject))
                    continue;

                seen.Add(hitObject);

                if (!currentMarkers.TryGetValue(hitObject, out var marker))
                {
                    marker = createMarker(hitObject);

                    currentMarkers[hitObject] = marker;
                    markers.Add(marker);
                }
                else
                    ((IOffscreenMarker)marker).UpdateFrom(hitObject);

                // an object stays alive well past the point it has faded from view, so the marker takes the object's
                // own transparency instead of its lifetime. That way the two appear and disappear together.
                marker.Alpha = drawable.Alpha;
            }

            toRemove.Clear();

            foreach (var (hitObject, marker) in currentMarkers)
            {
                if (seen.Contains(hitObject))
                    continue;

                markers.Remove(marker, true);
                toRemove.Add(hitObject);
            }

            foreach (var hitObject in toRemove)
                currentMarkers.Remove(hitObject);
        }

        private static Drawable createMarker(OsuHitObject hitObject)
        {
            Drawable marker = hitObject is Slider slider
                // the whole slider is outlined, not just the end which happens to be out. Marking one end alone reads
                // as if only that end were at fault.
                ? new OffscreenSliderOutline(slider)
                : new OffscreenRing((float)hitObject.Radius + ring_padding);

            ((IOffscreenMarker)marker).UpdateFrom(hitObject);
            return marker;
        }

        /// <summary>
        /// Whether any part of the object leaves the visible area.
        /// </summary>
        /// <remarks>
        /// Asked of <see cref="CheckOffscreenObjects"/> rather than worked out again here. This used to measure a
        /// slider from the bounds of its <see cref="SliderPath.CalculatedPath"/>, which is the curve the control
        /// points describe and not the body that is drawn: a slider whose length runs past the end of that curve
        /// carries on beyond it, and the part past the end was never looked at. A slider arcing up out of the
        /// play area could go unmarked while the verifier called it a problem.
        /// </remarks>
        private static bool isOffscreen(OsuHitObject hitObject) => CheckOffscreenObjects.IsOffscreen(hitObject);

        /// <summary>
        /// Lets a marker follow the object it belongs to while that object is being edited.
        /// </summary>
        private interface IOffscreenMarker
        {
            void UpdateFrom(OsuHitObject hitObject);
        }

        /// <summary>
        /// Traces a slider's path so the whole object reads as offscreen, matching how selection outlines it.
        /// </summary>
        internal partial class OffscreenSliderOutline : CompositeDrawable, IOffscreenMarker
        {
            private readonly ManualSliderBody body;

            private int? lastPathVersion;

            public OffscreenSliderOutline(Slider slider)
            {
                // both of these are what `SliderBodyPiece` does for the same job, and both are needed here for
                // the same reason. Without a size this is a drawable one point wide sitting wherever the origin
                // put it, and a point outside the masking bounds is a drawable that never gets drawn — which is
                // exactly what happens to a slider whose body reaches a long way out of the play area.
                AutoSizeAxes = Axes.Both;
                AlwaysPresent = true;

                InternalChild = body = new ManualSliderBody
                {
                    AccentColour = Color4.Transparent,
                    BorderColour = Color4.Red,
                    BorderSize = ring_thickness,
                    PathRadius = slider.Scale * OsuHitObject.OBJECT_RADIUS,
                };
            }

            public void UpdateFrom(OsuHitObject hitObject)
            {
                var slider = (Slider)hitObject;

                body.PathRadius = slider.Scale * OsuHitObject.OBJECT_RADIUS;

                // rebuilding the path is the expensive part, so it only happens when the shape actually changed.
                if (lastPathVersion != slider.Path.Version.Value)
                {
                    lastPathVersion = slider.Path.Version.Value;
                    body.SetVertices(slider.Path.CalculatedPath);
                }

                Position = slider.StackedPosition;
                OriginPosition = body.PathOffset;
            }
        }

        private partial class OffscreenRing : CircularContainer, IOffscreenMarker
        {
            public OffscreenRing(float radius)
            {
                Origin = Anchor.Centre;
                Size = new Vector2(radius * 2);

                Masking = true;
                BorderThickness = ring_thickness;
                BorderColour = Color4.Red;

                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    AlwaysPresent = true,
                    Alpha = 0,
                };
            }

            public void UpdateFrom(OsuHitObject hitObject)
            {
                Size = new Vector2(((float)hitObject.Radius + ring_padding) * 2);
                Position = hitObject.StackedPosition;
            }
        }
    }
}
