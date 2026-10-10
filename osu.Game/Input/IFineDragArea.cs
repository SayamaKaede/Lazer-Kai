// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;

namespace osu.Game.Input
{
    /// <summary>
    /// An area where a left-button drag should begin the moment the mouse moves, rather than after the travel distance
    /// the framework normally waits for.
    /// </summary>
    /// <remarks>
    /// Nudging an object, or a slider control point, a couple of pixels is impossible while a drag has to travel ten
    /// first, and so is moving the handle of a slider bar by a step or two. Everywhere else — buttons, scroll bars,
    /// menus — keeps the usual threshold, so a click made with an unsteady hand is still a click.
    ///
    /// Only has an effect while the user has asked for it; see <see cref="OsuUserInputManager.RemoveDragThreshold"/>.
    /// </remarks>
    public interface IFineDragArea : IDrawable
    {
    }
}
