// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Screens.Edit;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// The pieces the timeline borrows from the skin, so that an object reads there the way it will read in play.
    /// </summary>
    /// <remarks>
    /// lazer's timeline draws every object as the same rounded bar, which says where things are but not what they
    /// are: a slider's ends are not drawn at all, and a reverse is a dot. Stable's timeline is the skin's own
    /// circles, which is what a mapper is looking at while placing.
    ///
    /// Only stable-format skins are read. The lookups are the plain legacy names rather than the game's skinnable
    /// components, because those belong to the osu! ruleset and the timeline belongs to every ruleset.
    /// </remarks>
    public static class TimelineSkinPieces
    {
        /// <summary>The size a legacy skin draws a hit circle at, which its numbers are drawn to match.</summary>
        public const float LEGACY_CIRCLE_DIAMETER = 128;

        public const string CIRCLE_LOOKUP = @"hitcircle";

        public const string OVERLAY_LOOKUP = @"hitcircleoverlay";

        public const string REVERSE_LOOKUP = @"reversearrow";

        /// <summary>
        /// Whether the current skin draws in the format these pieces are read from.
        /// </summary>
        public static bool Available(ISkin skin) => skin.GetTexture(CIRCLE_LOOKUP) != null;
    }

    /// <summary>
    /// One end of an object, drawn as the skin draws a hit circle.
    /// </summary>
    /// <remarks>
    /// Both ends are the hit circle rather than the skin's slider start and end pieces. That is what stable's
    /// timeline does, and it is what keeps the ends readable at all: a skin whose slider end is a transparent image
    /// leaves nothing to see, while the ring over it is drawn by every skin.
    ///
    /// The ring is drawn over the number, which is what a legacy skin does unless it says otherwise. The setting that
    /// says otherwise is the osu! ruleset's, and this is read from every ruleset.
    /// </remarks>
    public partial class TimelineSkinCircle : CompositeDrawable
    {
        /// <summary>A step thicker than the 4 a selected object's outline is drawn at.</summary>
        private const float grip_thickness = 6;

        private readonly int? comboIndex;
        private readonly bool reverse;

        private Sprite circle = null!;

        /// <summary>What is drawn in the middle of it: a number, a reverse arrow, or nothing.</summary>
        private Drawable? content;

        /// <summary>
        /// The colour of the circle underneath. The ring over it is left alone, the way it is in play.
        /// </summary>
        public ColourInfo CircleColour
        {
            set => circle.Colour = value;
        }

        /// <summary>
        /// Whether this end is being held. With no handle drawn on it there is otherwise nothing to say that a drag
        /// has hold of it rather than of the timeline behind.
        /// </summary>
        public bool Grabbed
        {
            set => grip.FadeTo(value ? 1 : 0, 100, Easing.OutQuint);
        }

        private Container grip = null!;

        private float circleSize;

        /// <summary>
        /// The height objects are drawn at, which this is drawn to fill.
        /// </summary>
        public float CircleSize
        {
            set
            {
                circleSize = value;
                Size = new Vector2(value);
                updateContent();
            }
        }

        // the skin's numbers and arrows are drawn for a full-sized circle, so they come down with this one.
        // Stable's own blanket 0.8 on the numbers is kept on top of that.
        private void updateContent()
        {
            if (content == null)
                return;

            // the arrow is the skin's own size; stable's blanket 0.8 on the numbers is kept on top of theirs.
            float scale = reverse ? 1 : 0.8f;

            content.Scale = new Vector2(scale * circleSize / TimelineSkinPieces.LEGACY_CIRCLE_DIAMETER);
        }

        /// <param name="comboIndex">The number to draw on it, or null for an end that carries no number.</param>
        /// <param name="reverse">Whether the skin's reverse arrow is drawn on it in place of a number.</param>
        public TimelineSkinCircle(int? comboIndex, bool reverse = false)
        {
            this.comboIndex = comboIndex;
            this.reverse = reverse;
        }

        [BackgroundDependencyLoader]
        private void load(ISkinSource skin, OsuConfigManager config, OsuColour colours)
        {
            circle = new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = skin.GetTexture(TimelineSkinPieces.CIRCLE_LOOKUP),
            };

            var pieces = new List<Drawable> { circle };

            if (reverse)
            {
                pieces.Add(content = new TimelineSkinReverseArrow());
            }
            else if (comboIndex != null && skin.HasFont(LegacyFont.HitCircle))
            {
                pieces.Add(content = new LegacySpriteText(LegacyFont.HitCircle)
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = comboIndex.Value.ToString(),
                });
            }

            pieces.Add(new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                FillMode = FillMode.Fit,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = skin.GetTexture(TimelineSkinPieces.OVERLAY_LOOKUP),
            });

            // over the skin's own ring and a step thicker than the outline a selected object gets, so that having
            // hold of an end reads at a glance from the end itself.
            pieces.Add(grip = new CircularContainer
            {
                RelativeSizeAxes = Axes.Both,
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Masking = true,
                BorderThickness = grip_thickness,
                Alpha = 0,
                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true,
                },
            });

            InternalChildren = pieces.ToArray();

            var appearance = new EditorSelectionAppearance(config);

            void updateGrip() => grip.BorderColour = appearance.GetColour(colours.Yellow);

            appearance.Changed += updateGrip;
            updateGrip();

            updateContent();
        }
    }

    /// <summary>
    /// A reverse, drawn as the skin's own arrow.
    /// </summary>
    /// <remarks>
    /// Drawn at the size the skin drew it, which is what makes it sit inside the circle the way it does in play.
    /// It points back down the timeline without being turned: the texture already points the way a reverse sends
    /// the player, which is against the direction the timeline runs in.
    /// </remarks>
    public partial class TimelineSkinReverseArrow : CompositeDrawable
    {
        [BackgroundDependencyLoader]
        private void load(ISkinSource skin)
        {
            AutoSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;

            InternalChild = new Sprite
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Texture = skin.GetTexture(TimelineSkinPieces.REVERSE_LOOKUP),
            };
        }
    }
}
