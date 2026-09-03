// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Screens.Edit.Collab;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Lights the stretches of the song being pointed at in the collaboration panel, and says back where on the song
    /// the pointer is resting here.
    /// </summary>
    /// <remarks>
    /// Added rather than drawn over, so the markers, breaks and bookmarks underneath stay readable through it: what
    /// is being asked is where a stretch sits among them, which would be lost if it covered them.
    /// </remarks>
    public partial class CollabPart : TimelinePart
    {
        private const float lit_alpha = 0.35f;

        /// <summary>
        /// Nothing here is clicked, but the pointer resting on it is read, and a part outside the positional input
        /// queue is never told it is being pointed at.
        /// </summary>
        public override bool HandlePositionalInput => true;

        [Resolved]
        private CollabHighlight highlight { get; set; } = null!;

        /// <summary>The moment of the song the pointer is resting on here, or null while it is not being followed.</summary>
        public double? PointedAt => highlight.PointedAt.Value;

        private readonly IBindable<CollabBlock[]> blocks = new Bindable<CollabBlock[]>();
        private readonly IBindable<Color4> colour = new Bindable<Color4>();
        private readonly IBindable<bool> open = new BindableBool();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            colour.BindTo(highlight.Colour);
            colour.BindValueChanged(c => this.FadeColour(c.NewValue, 100, Easing.OutQuint), true);

            blocks.BindTo(highlight.Blocks);
            blocks.BindValueChanged(lit =>
            {
                // faded out rather than dropped, so that moving off the panel does not snap the strip dark.
                foreach (var band in Children.ToArray())
                    band.FadeOut(200, Easing.OutQuint).Expire();

                foreach (var block in lit.NewValue)
                    Add(new Band(block));
            }, true);

            open.BindTo(highlight.Open);
        }

        protected override void Update()
        {
            base.Update();

            highlight.PointedAt.Value = pointedAt();
        }

        /// <summary>
        /// The moment of the song under the pointer, or null when the pointer is elsewhere.
        /// </summary>
        /// <remarks>
        /// Read from <see cref="Drawable.IsHovered"/> rather than the pointer's position alone, so that the panel
        /// being dragged over the strip does not have the strip answering through it.
        /// </remarks>
        private double? pointedAt()
        {
            if (!open.Value || !IsHovered || DrawWidth <= 0)
                return null;

            var inputManager = GetContainingInputManager();

            if (inputManager == null)
                return null;

            // the content is scaled so that a child's X is its time, which is the scale to read back out of.
            return ToLocalSpace(inputManager.CurrentState.Mouse.Position).X / DrawWidth * Content.RelativeChildSize.X;
        }

        private partial class Band : Box
        {
            public Band(CollabBlock block)
            {
                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Both;
                Blending = BlendingParameters.Additive;
                Alpha = 0;

                X = (float)block.Start;
                Width = (float)(block.End - block.Start);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                this.FadeTo(lit_alpha, 100, Easing.OutQuint);
            }
        }
    }
}
