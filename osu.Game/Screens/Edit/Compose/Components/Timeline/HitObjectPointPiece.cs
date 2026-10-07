// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    public partial class HitObjectPointPiece : CircularContainer
    {
        protected OsuSpriteText Label { get; private set; }

        protected Container LabelContainer { get; private set; }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            AutoSizeAxes = Axes.Both;

            Color4 colour = GetRepresentingColour(colours);

            InternalChildren = new Drawable[]
            {
                LabelContainer = new Container
                {
                    AutoSizeAxes = Axes.X,
                    // a touch shorter than upstream's 16, so that two of these stacked over an object in the stable
                    // layout leave the object more of the timeline's height.
                    Height = TimelineHitObjectBlueprint.POINT_ROW_HEIGHT,
                    Masking = true,
                    CornerRadius = TimelineHitObjectBlueprint.POINT_ROW_HEIGHT / 2,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Colour = colour,
                            RelativeSizeAxes = Axes.Both,
                        },
                        Label = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Padding = new MarginPadding(5),
                            Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                            Colour = colours.B5,
                        }
                    }
                },
            };
        }

        protected virtual Color4 GetRepresentingColour(OsuColour colours)
        {
            return colours.Yellow;
        }

        /// <summary>
        /// Outlines the label, for a point that a change made through another will also go to.
        /// </summary>
        protected void SetHighlighted(bool highlighted) => SetHighlighted(highlighted, Color4.White, 2);

        /// <param name="highlighted">Whether to outline the label.</param>
        /// <param name="colour">The outline's colour, opacity included.</param>
        /// <param name="thickness">The outline's thickness.</param>
        protected void SetHighlighted(bool highlighted, Color4 colour, float thickness)
        {
            LabelContainer.BorderColour = colour;
            LabelContainer.BorderThickness = highlighted ? thickness : 0;
        }
    }
}
