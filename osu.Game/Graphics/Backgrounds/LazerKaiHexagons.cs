// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.Utils;
using osu.Game.IO;
using osuTK;

namespace osu.Game.Graphics.Backgrounds
{
    /// <summary>
    /// Hexagon outlines drifting upwards, the fork's own pattern for the title screen's logo.
    /// Moves the way <see cref="TrianglesV2"/> does, so the logo can drive it the same way.
    /// </summary>
    public partial class LazerKaiHexagons : CompositeDrawable
    {
        private const float base_velocity = 50;

        /// <summary>sqrt(3) / 2, the height of a flat-topped hexagon over its width.</summary>
        private const float height_ratio = 0.866f;

        /// <summary>The width of each hexagon. They are all the same size, so their outlines are all the same thickness.</summary>
        public float HexagonWidth { get; set; } = 100;

        /// <summary>The amount of hexagons compared to the default distribution.</summary>
        public float SpawnRatio { get; set; } = 1;

        /// <summary>The relative velocity of the hexagons. Default is 1.</summary>
        public float Velocity = 1;

        private Texture? texture;

        private bool filled;

        [BackgroundDependencyLoader]
        private void load(LazerKaiArtwork artwork)
        {
            texture = artwork.GetTexture(@"hexagon");
        }

        protected override void Update()
        {
            base.Update();

            if (DrawWidth == 0 || DrawHeight == 0)
                return;

            int aimCount = Math.Max(1, (int)(DrawWidth * 0.02f * SpawnRatio));

            // the first ones are spread over the whole area, so it does not start empty; later ones come in from below.
            while (InternalChildren.Count < aimCount)
                AddInternal(createHexagon(!filled));

            filled = true;

            float movedDistance = -(float)Time.Elapsed / 1000 * Velocity * base_velocity / DrawHeight;

            foreach (var hexagon in InternalChildren.OfType<Hexagon>().ToArray())
            {
                hexagon.Y += Math.Max(0.5f, hexagon.SpeedMultiplier) * movedDistance;

                if (hexagon.Y + hexagon.Height / DrawHeight < 0)
                    RemoveInternal(hexagon, true);
            }
        }

        private Hexagon createHexagon(bool randomY)
        {
            float relativeHeight = HexagonWidth * height_ratio / DrawHeight;
            float relativeWidth = HexagonWidth / DrawWidth;

            return new Hexagon
            {
                Texture = texture,
                Size = new Vector2(HexagonWidth, HexagonWidth * height_ratio),
                RelativePositionAxes = Axes.Both,
                Position = new Vector2(
                    Interpolation.ValueAt(RNG.NextSingle(), -relativeWidth / 2, 1 - relativeWidth / 2, 0f, 1f),
                    randomY ? Interpolation.ValueAt(RNG.NextSingle(), -relativeHeight, 1f, 0f, 1f) : 1),
                SpeedMultiplier = nextSpeedMultiplier(),
            };
        }

        /// <summary>A random speed around half, picked the way <see cref="TrianglesV2"/> picks one.</summary>
        private static float nextSpeedMultiplier()
        {
            const float std_dev = 0.16f;
            const float mean = 0.5f;

            float u1 = 1 - RNG.NextSingle();
            float u2 = 1 - RNG.NextSingle();
            float randStdNormal = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2));
            return Math.Max(mean + std_dev * randStdNormal, 0.1f);
        }

        private partial class Hexagon : Sprite
        {
            public float SpeedMultiplier;
        }
    }
}
