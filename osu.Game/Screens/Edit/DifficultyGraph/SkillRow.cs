// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// One skill's difficulty across the beatmap, on a scale of its own.
    /// </summary>
    /// <remarks>
    /// The stack above shows how the skills compare to one another, which buries the small ones — osu!'s reading
    /// comes to a couple of per cent of the whole, and a band that thin says nothing about where reading is hard.
    /// Given a row and its own scale, it does.
    /// </remarks>
    public partial class SkillRow : CompositeDrawable
    {
        private readonly string name;
        private readonly Color4 colour;
        private readonly DifficultyGraphDetailOverlay.SkillColumn[] columns;
        private readonly int skill;

        public SkillRow(string name, Color4 colour, DifficultyGraphDetailOverlay.SkillColumn[] columns, int skill)
        {
            this.name = name;
            this.colour = colour;
            this.columns = columns;
            this.skill = skill;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            Masking = true;
            CornerRadius = 3;

            var bars = new BufferedContainer<Box>(cachedFrameBuffer: true)
            {
                RelativeSizeAxes = Axes.Both,
                RedrawOnScale = false,
                Alpha = 0.85f,
            };

            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6,
                },
                bars,
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(7, 0),
                    Margin = new MarginPadding { Left = 8, Top = 7 },
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Size = new Vector2(9),
                            Colour = colour,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new OsuSpriteText
                        {
                            Text = name,
                            Font = OsuFont.Default.With(size: 11.5f, weight: FontWeight.SemiBold),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                },
            };

            double top = columns.Length == 0 ? 0 : columns.Max(c => skill < c.Skills.Length ? c.Skills[skill] : 0);

            if (top <= 0)
                return;

            float width = 1.02f / columns.Length;

            for (int i = 0; i < columns.Length; i++)
            {
                double value = skill < columns[i].Skills.Length ? columns[i].Skills[skill] : 0;

                if (value <= 0)
                    continue;

                bars.Add(new Box
                {
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    X = (float)i / columns.Length,
                    Width = width,
                    Height = (float)(value / top),
                    Colour = colour,
                });
            }

            bars.ForceRedraw();
        }
    }
}
