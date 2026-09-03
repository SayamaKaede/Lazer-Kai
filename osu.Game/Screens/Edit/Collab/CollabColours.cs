// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// A colour per collaborator, so a split can be read at a glance rather than by name.
    /// </summary>
    /// <remarks>
    /// Eight of them, which is more collaborators than a beatmap tends to have. Past that they repeat: two people
    /// sharing a colour is confusing, but a ninth person with no colour at all cannot be seen.
    /// </remarks>
    public static class CollabColours
    {
        private static readonly Color4[] colours =
        {
            Color4Extensions.FromHex("7ecbff"),
            Color4Extensions.FromHex("ffc46b"),
            Color4Extensions.FromHex("a1e37a"),
            Color4Extensions.FromHex("ff8fb3"),
            Color4Extensions.FromHex("c9a3ff"),
            Color4Extensions.FromHex("6be0c8"),
            Color4Extensions.FromHex("ffe066"),
            Color4Extensions.FromHex("ff9b6b"),
        };

        public static Color4 For(int index) => index < 0 ? Color4.White : colours[index % colours.Length];
    }
}
