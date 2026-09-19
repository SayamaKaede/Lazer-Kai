// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints
{
    /// <summary>
    /// The outlines of selected objects that have no blueprint at the moment, because their time is not on screen.
    /// </summary>
    /// <remarks>
    /// A blueprint brings the object's drawable with it, along with input, hit animations and control points. Giving one
    /// to every selected object is what made selecting a whole beatmap take seconds, and most of every frame afterwards,
    /// so only what is on screen has one. The rest of the selection is still outlined here, and nothing else.
    ///
    /// Slider bodies and spinners use the same pieces as their blueprints. Circles do not: the blueprint's ring is a
    /// masked container, and masking is drawn one ring at a time, which with thousands of them held the frame rate
    /// down on its own. Here every ring is the same picture of a ring, drawn together. The handle for dragging a
    /// slider's length is left out, since nothing here can be dragged.
    /// </remarks>
    public partial class SelectedObjectOutlines : CompositeDrawable
    {
        /// <summary>
        /// Matches the ring a circle's blueprint draws (<see cref="Skinning.Default.RingPiece"/>'s default).
        /// </summary>
        private const float default_ring_thickness = 9;

        private readonly Dictionary<HitObject, Outline> outlines = new Dictionary<HitObject, Outline>();
        private readonly HashSet<HitObject> selected = new HashSet<HitObject>();
        private readonly HashSet<HitObject> withBlueprint = new HashSet<HitObject>();
        private readonly BindableList<HitObject> selectedHitObjects = new BindableList<HitObject>();

        private readonly Container bodies;
        private readonly Container<Sprite> rings;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private IRenderer renderer { get; set; } = null!;

        private EditorSelectionAppearance appearance = null!;
        private Texture? ringTexture;
        private float ringTextureThickness;

        public SelectedObjectOutlines()
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                bodies = new Container { RelativeSizeAxes = Axes.Both },
                rings = new Container<Sprite> { RelativeSizeAxes = Axes.Both },
            };
        }

        // only something to look at. What is selected is picked up and moved through the blueprints on screen.
        public override bool PropagatePositionalInputSubTree => false;
        public override bool PropagateNonPositionalInputSubTree => false;

        /// <summary>
        /// The screen-space extents of the outlines, which the selection box covers along with the blueprints.
        /// </summary>
        public IEnumerable<Quad> SelectionQuads => outlines.Values.Select(o => o.SelectionQuad);

        /// <summary>
        /// How many objects are outlined at the moment.
        /// </summary>
        public int Count => outlines.Count;

        /// <summary>
        /// Changes whenever an outline is added or removed.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>
        /// Finds the outlined object at a screen-space position, preferring the one nearest to <paramref name="time"/>
        /// where outlines overlap.
        /// </summary>
        public HitObject? FindAt(Vector2 screenSpacePosition, double time)
        {
            HitObject? found = null;

            foreach (var (hitObject, outline) in outlines)
            {
                if (found != null && Math.Abs(hitObject.StartTime - time) >= Math.Abs(found.StartTime - time))
                    continue;

                if (outline.Contains(screenSpacePosition))
                    found = hitObject;
            }

            return found;
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OsuConfigManager config)
        {
            appearance = new EditorSelectionAppearance(config);

            void updateAppearance()
            {
                rings.Colour = appearance.GetObjectColour(colours.Yellow);
                rings.Alpha = appearance.ObjectOpacity;

                float thickness = appearance.GetObjectThickness(default_ring_thickness);

                if (ringTexture != null && thickness == ringTextureThickness)
                    return;

                ringTexture?.Dispose();
                ringTexture = createRingTexture(thickness);
                ringTextureThickness = thickness;

                foreach (var ring in rings)
                    ring.Texture = ringTexture;
            }

            appearance.Changed += updateAppearance;
            updateAppearance();
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            selectedHitObjects.BindTo(editorBeatmap.SelectedHitObjects);
            selectedHitObjects.BindCollectionChanged(selectionChanged, true);
        }

        protected override void Update()
        {
            base.Update();

            foreach (var outline in outlines.Values)
                outline.UpdateFromHitObject();
        }

        public void BlueprintAdded(HitObject hitObject)
        {
            withBlueprint.Add(hitObject);
            refresh(hitObject);
        }

        public void BlueprintRemoved(HitObject hitObject)
        {
            withBlueprint.Remove(hitObject);
            refresh(hitObject);
        }

        private void selectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                foreach (var hitObject in selected.ToArray())
                {
                    selected.Remove(hitObject);
                    refresh(hitObject);
                }

                foreach (var hitObject in selectedHitObjects)
                {
                    selected.Add(hitObject);
                    refresh(hitObject);
                }

                return;
            }

            if (e.OldItems != null)
            {
                foreach (HitObject hitObject in e.OldItems)
                {
                    selected.Remove(hitObject);
                    refresh(hitObject);
                }
            }

            if (e.NewItems != null)
            {
                foreach (HitObject hitObject in e.NewItems)
                {
                    selected.Add(hitObject);
                    refresh(hitObject);
                }
            }
        }

        private void refresh(HitObject hitObject)
        {
            bool wanted = selected.Contains(hitObject) && !withBlueprint.Contains(hitObject);

            if (!wanted)
            {
                if (outlines.Remove(hitObject, out var existing))
                {
                    existing.Remove();
                    Version++;
                }

                return;
            }

            if (outlines.ContainsKey(hitObject))
                return;

            Outline? outline = hitObject switch
            {
                HitCircle circle => new CircleOutline(this, circle),
                Slider slider => new SliderOutline(this, slider),
                Spinner spinner => new SpinnerOutline(this, spinner),
                _ => null,
            };

            if (outline == null)
                return;

            outline.UpdateFromHitObject();
            outlines[hitObject] = outline;
            Version++;
        }

        private Sprite addRing()
        {
            var ring = new Sprite
            {
                Origin = Anchor.Centre,
                Size = OsuHitObject.OBJECT_DIMENSIONS,
                Texture = ringTexture,
                // until it has been put in place.
                Alpha = 0,
            };

            rings.Add(ring);
            return ring;
        }

        private void placeRing(Sprite ring, OsuHitObject hitObject)
        {
            ring.Position = hitObject.StackedPosition;
            ring.Scale = new Vector2(hitObject.Scale);
            ring.Alpha = 1;
        }

        /// <summary>
        /// A white ring as wide as <see cref="OsuHitObject.OBJECT_DIMENSIONS"/>, with a border of <paramref name="thickness"/>
        /// in the same units, and edges softened by a pixel the way a masked container's are.
        /// </summary>
        private Texture createRingTexture(float thickness)
        {
            const int size = 256;

            float outerRadius = size / 2f;
            float innerRadius = outerRadius * (1 - Math.Clamp(thickness / OsuHitObject.OBJECT_RADIUS, 0, 1));

            var image = new SixLabors.ImageSharp.Image<Rgba32>(size, size);

            image.ProcessPixelRows(pixels =>
            {
                for (int y = 0; y < size; y++)
                {
                    var row = pixels.GetRowSpan(y);

                    for (int x = 0; x < size; x++)
                    {
                        float distance = MathF.Sqrt(MathF.Pow(x + 0.5f - outerRadius, 2) + MathF.Pow(y + 0.5f - outerRadius, 2));
                        float coverage = Math.Clamp(outerRadius - distance, 0, 1) * Math.Clamp(distance - innerRadius, 0, 1);

                        row[x] = new Rgba32(255, 255, 255, (byte)MathF.Round(coverage * 255));
                    }
                }
            });

            var texture = renderer.CreateTexture(size, size, filteringMode: TextureFilteringMode.Linear);
            texture.SetData(new TextureUpload(image));
            return texture;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            ringTexture?.Dispose();
        }

        private abstract class Outline
        {
            protected readonly SelectedObjectOutlines Owner;

            protected Outline(SelectedObjectOutlines owner)
            {
                Owner = owner;
            }

            public abstract void UpdateFromHitObject();

            public abstract Quad SelectionQuad { get; }

            /// <summary>
            /// Whether a screen-space position is where the object's blueprint would take hold of it.
            /// </summary>
            public abstract bool Contains(Vector2 screenSpacePosition);

            public abstract void Remove();

            protected static bool RingContains(Sprite ring, Vector2 screenSpacePosition)
            {
                if (!ring.ScreenSpaceDrawQuad.Contains(screenSpacePosition))
                    return false;

                // the whole circle, as a circle's hit area is, rather than only the ring that is drawn.
                return Vector2.Distance(ring.ToLocalSpace(screenSpacePosition), ring.DrawSize / 2) <= ring.DrawWidth / 2;
            }
        }

        private class CircleOutline : Outline
        {
            private readonly HitCircle circle;
            private readonly Sprite ring;

            public CircleOutline(SelectedObjectOutlines owner, HitCircle circle)
                : base(owner)
            {
                this.circle = circle;
                ring = owner.addRing();
            }

            public override void UpdateFromHitObject() => Owner.placeRing(ring, circle);

            public override Quad SelectionQuad => ring.ScreenSpaceDrawQuad;

            public override bool Contains(Vector2 screenSpacePosition) => RingContains(ring, screenSpacePosition);

            public override void Remove() => Owner.rings.Remove(ring, true);
        }

        private class SliderOutline : Outline
        {
            private readonly Slider slider;
            private readonly SliderBodyPiece body;
            private readonly Sprite head;
            private readonly Sprite tail;

            public SliderOutline(SelectedObjectOutlines owner, Slider slider)
                : base(owner)
            {
                this.slider = slider;

                owner.bodies.Add(body = new SliderBodyPiece());
                head = owner.addRing();
                tail = owner.addRing();
            }

            public override void UpdateFromHitObject()
            {
                body.UpdateFrom(slider);

                // a slider that has just been pasted is selected before the beatmap has processed it, and has no circles at
                // its ends until then. A blueprint never sees that, since it waits for the drawable, which waits for them.
                if (slider.HeadCircle == null)
                    return;

                // the same circles SliderCircleOverlay puts its rings on.
                Owner.placeRing(head, slider.HeadCircle);
                Owner.placeRing(tail, slider.RepeatCount % 2 == 0 ? slider.TailCircle : slider.LastRepeat!);
            }

            public override Quad SelectionQuad
            {
                get
                {
                    var result = body.ScreenSpaceDrawQuad.AABBFloat;

                    // not in place yet; see UpdateFromHitObject.
                    if (head.Alpha == 0)
                        return result;

                    result = RectangleF.Union(result, head.ScreenSpaceDrawQuad.AABBFloat);

                    // SliderCircleOverlay.VisibleQuad makes room for the length handle around the tail, so the selection
                    // box does not change size as the slider's blueprint comes and goes.
                    var tailRect = tail.ScreenSpaceDrawQuad.AABBFloat;
                    result = RectangleF.Union(result, new RectangleF(tailRect.TopLeft - tailRect.Size * 0.2f, tailRect.Size * 1.4f));

                    return result;
                }
            }

            // the body alone, as SliderSelectionBlueprint takes hold; its ends are inside the body anyway.
            public override bool Contains(Vector2 screenSpacePosition) =>
                body.ScreenSpaceDrawQuad.AABBFloat.Contains(screenSpacePosition) && body.ReceivePositionalInputAt(screenSpacePosition);

            public override void Remove()
            {
                Owner.bodies.Remove(body, true);
                Owner.rings.Remove(head, true);
                Owner.rings.Remove(tail, true);
            }
        }

        private class SpinnerOutline : Outline
        {
            private readonly Spinner spinner;
            private readonly SpinnerPiece piece;

            public SpinnerOutline(SelectedObjectOutlines owner, Spinner spinner)
                : base(owner)
            {
                this.spinner = spinner;
                owner.bodies.Add(piece = new SpinnerPiece());
            }

            public override void UpdateFromHitObject() => piece.UpdateFrom(spinner);

            public override Quad SelectionQuad => piece.ScreenSpaceDrawQuad;

            public override bool Contains(Vector2 screenSpacePosition) => piece.ReceivePositionalInputAt(screenSpacePosition);

            public override void Remove() => Owner.bodies.Remove(piece, true);
        }
    }
}
