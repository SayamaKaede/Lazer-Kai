// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// One stretch of the song and the colour of whoever has it.
    /// </summary>
    /// <remarks>
    /// What the timeline needs to follow the playhead with, and no more: the panel is where a stretch is a block
    /// with a drain time and an owner, and the strip along the bottom only ever draws where and what colour.
    /// </remarks>
    public readonly struct CollabBand
    {
        public readonly double Start;
        public readonly double End;
        public readonly Color4 Colour;

        public CollabBand(double start, double end, Color4 colour)
        {
            Start = start;
            End = end;
            Colour = colour;
        }

        public bool Contains(double time) => time >= Start && time < End;
    }
}
