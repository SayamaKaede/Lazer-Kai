// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The shape the editor's placement grid is drawn in.
    /// </summary>
    /// <remarks>
    /// A copy of osu!'s own <c>PositionSnapGridType</c>, which cannot be used here: it lives in the ruleset, and
    /// a ruleset is something this project is referenced by rather than the other way round. The two are matched
    /// by position, and a test holds them to it.
    /// </remarks>
    public enum EditorGridShape
    {
        Square,
        Triangle,
        Circle,
    }
}
