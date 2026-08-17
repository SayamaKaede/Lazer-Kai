// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Edit.Components
{
    /// <summary>
    /// Temporary: records which drawable last took a drag start, so it can be confirmed whether a press on a toolbox
    /// button reaches the button or is taken by the toolbox scrolling behind it. Not meant to ship.
    /// </summary>
    public static class EditorButtonDragProbe
    {
        public static string LastHandler = "(none yet)";
    }
}
