// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Lines;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Overlays;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// A kept shape, drawn small enough to pick out of a list.
    /// </summary>
    /// <remarks>
    /// Not the ruleset's own drawables: those want a skin, a working clock and a playfield around them, none of
    /// which a settings-sized square has. What a shape is recognised by is where it goes, which is the path and
    /// the points along it.
    /// </remarks>
    public partial class PlacementPresetThumbnail : CompositeDrawable
    {
        private readonly IList<HitObject> hitObjects;
        private readonly float objectRadius;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private Vector2 drawnAt;

        public PlacementPresetThumbnail(IList<HitObject> hitObjects, float objectRadius)
        {
            this.hitObjects = hitObjects;
            this.objectRadius = objectRadius;
        }

        protected override void Update()
        {
            base.Update();

            // the square is only known once the list around it has been laid out, and it changes again if the
            // panel does. Redrawing on every frame would rebuild the whole list while it is being scrolled.
            if (DrawSize != drawnAt)
            {
                drawnAt = DrawSize;
                rebuild();
            }
        }

        /// <summary>
        /// How much of the square is left around the shape, so that a thick shape is not cut off at the edge.
        /// </summary>
        private const float padding = 4;

        private void rebuild()
        {
            ClearInternal();

            if (DrawWidth <= 0 || DrawHeight <= 0)
                return;

            var extent = PlacementPresetPlacement.Extent(hitObjects);

            if (extent == RectangleF.Empty)
                return;

            // the shape is as wide as the objects are, so the area it takes up is the path grown by the object's
            // radius on every side rather than the path itself.
            float grow = objectRadius;

            float width = extent.Width + grow * 2;
            float height = extent.Height + grow * 2;

            float scale = Math.Min((DrawWidth - padding * 2) / Math.Max(width, 1),
                (DrawHeight - padding * 2) / Math.Max(height, 1));

            var origin = new Vector2(extent.Left - grow, extent.Top - grow);
            var offset = new Vector2((DrawWidth - width * scale) / 2, (DrawHeight - height * scale) / 2);

            Vector2 toLocal(Vector2 position) => (position - origin) * scale + offset;

            // drawn at the weight the objects will be placed at, decided by looking at a list of them (2026-09-05).
            float drawnRadius = objectRadius * scale;

            var pathPoints = new List<Vector2>();

            foreach (var hitObject in hitObjects)
            {
                if (hitObject is not IHasPosition position)
                    continue;

                if (hitObject is IHasPath path)
                {
                    path.Path.GetPathToProgress(pathPoints, 0, 1);

                    if (pathPoints.Count > 1)
                    {
                        AddInternal(new SmoothPath
                        {
                            PathRadius = drawnRadius,
                            Alpha = 0.4f,
                            Colour = colourProvider.Highlight1,
                            Vertices = pathPoints.Select(p => toLocal(position.Position + p)).ToArray(),
                        });
                    }
                }

                AddInternal(new Circle
                {
                    Origin = Anchor.Centre,
                    Position = toLocal(position.Position),
                    Size = new Vector2(drawnRadius * 2),
                    Colour = colourProvider.Content1,
                });
            }
        }
    }
}
