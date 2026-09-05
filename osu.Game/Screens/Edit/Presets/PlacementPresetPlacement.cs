// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics.Primitives;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// Works out where a preset's objects go before they are handed to the beatmap.
    /// </summary>
    public static class PlacementPresetPlacement
    {
        /// <summary>
        /// The area the objects cover, taking the drawn curve into account rather than only where each starts.
        /// </summary>
        /// <remarks>
        /// A slider's position is its head, so a shape that curves away from it is nowhere near centred on that
        /// point. What is being centred is what can be seen.
        /// </remarks>
        public static RectangleF Extent(IEnumerable<HitObject> hitObjects)
        {
            var points = new List<Vector2>();
            var pathPoints = new List<Vector2>();

            foreach (var hitObject in hitObjects)
            {
                if (hitObject is not IHasPosition position)
                    continue;

                points.Add(position.Position);

                if (hitObject is not IHasPath path)
                    continue;

                path.Path.GetPathToProgress(pathPoints, 0, 1);
                points.AddRange(pathPoints.Select(p => position.Position + p));
            }

            if (points.Count == 0)
                return RectangleF.Empty;

            float minX = points.Min(p => p.X);
            float minY = points.Min(p => p.Y);
            float maxX = points.Max(p => p.X);
            float maxY = points.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }

        /// <summary>
        /// Moves the objects so the first of them starts at <paramref name="startTime"/> and what they cover is
        /// centred on <paramref name="centre"/>.
        /// </summary>
        /// <remarks>
        /// The middle of the playfield rather than under the cursor: the list this was chosen from is what the
        /// cursor is over at the moment the choice is made.
        /// </remarks>
        public static void LayOut(IList<HitObject> hitObjects, double startTime, Vector2 centre)
        {
            if (hitObjects.Count == 0)
                return;

            double timeOffset = startTime - hitObjects.Min(h => h.StartTime);

            foreach (var hitObject in hitObjects)
                hitObject.StartTime += timeOffset;

            var extent = Extent(hitObjects);

            if (extent == RectangleF.Empty)
                return;

            var positionOffset = centre - extent.Centre;

            foreach (var hitObject in hitObjects)
            {
                if (hitObject is IHasPosition position)
                    position.Position += positionOffset;
            }
        }

        /// <summary>
        /// Takes off everything a preset does not carry, leaving the shape.
        /// </summary>
        /// <remarks>
        /// A preset is a shape, not a phrase: the sounds and the combo belong to wherever it is being put, the same
        /// way they do for an object drawn by hand. The one sample left is the one every placement starts with.
        /// </remarks>
        public static void StripReading(IEnumerable<HitObject> hitObjects)
        {
            foreach (var hitObject in hitObjects)
            {
                hitObject.Samples.Clear();
                hitObject.Samples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL));

                if (hitObject is IHasRepeats withRepeats)
                {
                    foreach (var nodeSamples in withRepeats.NodeSamples)
                    {
                        nodeSamples.Clear();
                        nodeSamples.Add(new HitSampleInfo(HitSampleInfo.HIT_NORMAL));
                    }
                }

                if (hitObject is IHasComboInformation combo)
                    combo.NewCombo = false;
            }
        }
    }
}
