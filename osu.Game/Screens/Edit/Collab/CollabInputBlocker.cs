// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input.Events;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// Takes whatever lands on it that its own controls did not, so a click meant for the panel cannot reach the
    /// beatmap behind it.
    /// </summary>
    /// <remarks>
    /// The composer, the toolboxes and the timelines are all still in the queue underneath, and would otherwise
    /// answer the same click. Nothing outside these bounds is blocked: the panel is read alongside the beatmap, not
    /// instead of it.
    /// </remarks>
    public partial class CollabInputBlocker : Container
    {
        /// <summary>Raised whenever a click lands here, wherever within it it landed.</summary>
        /// <remarks>
        /// osu!'s buttons answer the click rather than the press, so a press on one of them still arrives here on
        /// its way through. That is what makes this the one place a click anywhere on the panel can be read from.
        /// </remarks>
        public Action? Pressed;

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            Pressed?.Invoke();
            return true;
        }

        protected override bool OnClick(ClickEvent e) => true;

        protected override bool OnDoubleClick(DoubleClickEvent e) => true;

        protected override bool OnDragStart(DragStartEvent e) => true;

        protected override bool OnScroll(ScrollEvent e) => true;

        protected override bool OnHover(HoverEvent e) => true;
    }
}
