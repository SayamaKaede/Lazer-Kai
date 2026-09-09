// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.IO;
using osuTK;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// This fork's mark: the cog, with 改 sitting in the hole through the middle of it.
    /// </summary>
    /// <remarks>
    /// Two files rather than one, so that how big the character sits inside the cog can be changed without redrawing
    /// anything, and so that either can be replaced on its own -- see <see cref="LazerKaiArtwork"/>.
    ///
    /// Where the hole is gets read off the drawing every time rather than written down here. The cog was redrawn
    /// twice in one day: a ring was added around it, which made the picture half as wide again, and then the ring
    /// was redrawn, which moved the cog off the middle of it. Any number measured by hand would have been wrong by
    /// the second version and wrong differently by the third, and nothing would have said so -- the character would
    /// simply have sat somewhere other than in the hole.
    ///
    /// Drawn additively: the artwork is a dark grey, so composited normally over the dark settings panel it reads
    /// as a smudge. Added to what is already lit, it reads as a sheen.
    /// </remarks>
    public partial class LazerKaiMark : CompositeDrawable
    {
        /// <summary>
        /// What can be read off a drawing of the cog: how tall it is against its width, and where the hole through
        /// it is, both as fractions of the picture.
        /// </summary>
        public readonly record struct Measurements(float Aspect, Vector2 HoleCentre, float HoleWidth)
        {
            /// <summary>Nothing found. Drawn to nothing rather than drawn wrong.</summary>
            public static Measurements None => new Measurements(1, Vector2.Zero, 0);

            public bool Found => HoleWidth > 0;
        }

        /// <summary>
        /// How the mark sits against the top right corner of the group it belongs to.
        /// </summary>
        /// <remarks>
        /// All four are about the cog itself -- how wide its hole is drawn, where the middle of that hole sits, and
        /// how much of the hole 改 fills -- rather than about the picture around it, so they went on meaning the
        /// same thing across three redraws of the cog. Settled by eye on 2026-09-09.
        /// </remarks>
        private const float hole_size = 46;

        private const float right_of_hole = 17;

        private const float below_the_line = 16;

        private const float kai_fills = 0.55f;

        private Sprite cog = null!;
        private Sprite kai = null!;

        private Measurements measurements = Measurements.None;

        [BackgroundDependencyLoader]
        private void load(LazerKaiArtwork artwork)
        {
            Blending = BlendingParameters.Additive;

            measurements = Measure(artwork.GetBytes(@"main"));

            InternalChildren = new Drawable[]
            {
                cog = new Sprite
                {
                    RelativeSizeAxes = Axes.Both,
                    Texture = artwork.GetTexture(@"main"),
                },
                kai = new Sprite
                {
                    // placed by where the hole was found rather than by anchor: the cog is not necessarily in the
                    // middle of its own picture, and in the current drawing it is not.
                    Origin = Anchor.Centre,
                    RelativePositionAxes = Axes.Both,
                    Position = measurements.HoleCentre,
                    Texture = artwork.GetTexture(@"kai"),
                },
            };

            updateLayout();
        }

        /// <summary>
        /// Reads the shape of a cog off a drawing of one.
        /// </summary>
        /// <remarks>
        /// The picture is expected to be cropped to the artwork, and the hole to be a round gap that the drawing
        /// closes all the way around -- so the widest run of see-through pixels that does not reach either side is
        /// the hole, and being round, that run lies across the middle of it. Runs that do reach a side are the space
        /// around the cog, not the hole through it.
        /// </remarks>
        public static Measurements Measure(byte[]? drawing)
        {
            if (drawing == null)
                return Measurements.None;

            try
            {
                using (var picture = SixLabors.ImageSharp.Image.Load<Rgba32>(drawing))
                {
                    int widest = 0, holeStart = 0, holeRow = 0;

                    for (int y = 0; y < picture.Height; y++)
                    {
                        int start = -1;

                        for (int x = 0; x <= picture.Width; x++)
                        {
                            bool clear = x < picture.Width && picture[x, y].A == 0;

                            if (clear && start < 0)
                                start = x;
                            else if (!clear && start >= 0)
                            {
                                if (start > 0 && x < picture.Width && x - start > widest)
                                    (widest, holeStart, holeRow) = (x - start, start, y);

                                start = -1;
                            }
                        }
                    }

                    if (widest == 0)
                        return Measurements.None;

                    return new Measurements(
                        (float)picture.Height / picture.Width,
                        new Vector2((holeStart + widest / 2f) / picture.Width, holeRow / (float)picture.Height),
                        widest / (float)picture.Width);
                }
            }
            catch (Exception e)
            {
                // a replacement drawing is whatever somebody dropped in the folder, so this is reachable by
                // ordinary means. Logged rather than swallowed: a mark that quietly never appears is the hardest
                // kind of thing to work out afterwards.
                Logger.Log($"The mark's drawing could not be read: {e.Message}", level: LogLevel.Important);
                return Measurements.None;
            }
        }

        private void updateLayout()
        {
            if (!measurements.Found)
            {
                Alpha = 0;
                return;
            }

            Alpha = 1;

            float width = hole_size / measurements.HoleWidth;
            float height = width * measurements.Aspect;

            Size = new Vector2(width, height);
            Position = new Vector2(-right_of_hole - measurements.HoleCentre.X * width, below_the_line - measurements.HoleCentre.Y * height);

            // sized in pixels rather than relatively: the picture is not square, and a relative size would stretch
            // the character with it.
            kai.Size = new Vector2(kai_fills * hole_size);
        }

        /// <summary>Whether both halves of the mark were actually found. Read by the tests.</summary>
        internal bool IsComplete => cog.Texture != null && kai.Texture != null && measurements.Found;
    }
}
