// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Bindables;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// What the collaboration panel and the timeline along the bottom are pointing out to each other.
    /// </summary>
    /// <remarks>
    /// The list says who has what, but not where any of it is; the timeline says where everything is, but not whose
    /// it is. Pointing at either lights the other, which is what puts the two together.
    ///
    /// Colouring the whole song at once would say both without any pointing, and is what a split of three or four
    /// people wants, but past that the strip becomes a row of slivers nobody can read. Lighting only what is being
    /// pointed at answers the question actually being asked, and stays legible however many people are on the
    /// beatmap.
    /// </remarks>
    public class CollabHighlight
    {
        /// <summary>The stretches lit on the timeline.</summary>
        public readonly Bindable<CollabBlock[]> Blocks = new Bindable<CollabBlock[]>(Array.Empty<CollabBlock>());

        /// <summary>The colour they are lit in, which is the colour of whoever has them.</summary>
        public readonly Bindable<Color4> Colour = new Bindable<Color4>(Color4.White);

        /// <summary>Where on the song the pointer is resting on the timeline, or null when it is not on it.</summary>
        public readonly Bindable<double?> PointedAt = new Bindable<double?>();

        /// <summary>
        /// Whether the panel is open, which is the only time any of this applies.
        /// </summary>
        /// <remarks>
        /// The timeline along the bottom is the editor's, not this feature's. It answers the pointer while a split is
        /// being read and goes back to being a plain timeline the moment the panel is closed.
        /// </remarks>
        public readonly BindableBool Open = new BindableBool();

        /// <summary>Whoever asked for the current light, so that only they can put it out.</summary>
        private object? owner;

        public void Show(object owner, IEnumerable<CollabBlock> blocks, Color4 colour)
        {
            this.owner = owner;
            Colour.Value = colour;
            Blocks.Value = blocks.ToArray();
        }

        /// <summary>
        /// Puts the light out, unless somebody else has since claimed it.
        /// </summary>
        /// <remarks>
        /// Rows and readings hand over as the pointer crosses between them, and the one being left is told after the
        /// one being entered has already taken over. Clearing regardless would leave the strip dark while pointing.
        /// </remarks>
        public void Hide(object owner)
        {
            if (ReferenceEquals(this.owner, owner))
                Clear();
        }

        /// <summary>Puts the light out whoever has it, for when the panel itself goes away.</summary>
        public void Clear()
        {
            owner = null;
            Blocks.Value = Array.Empty<CollabBlock>();
        }
    }
}
