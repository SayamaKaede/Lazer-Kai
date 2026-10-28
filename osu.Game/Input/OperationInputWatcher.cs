// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Game.Input
{
    /// <summary>
    /// Passes on every input the game receives to whoever listens, handling none of it (2026-10-28).
    /// </summary>
    /// <remarks>
    /// In front of everything, as <see cref="TouchInputInterceptor"/> is, so that it sees what dialogs and menus are given
    /// as well as what the screen beneath them is. For the editor's operation log.
    /// </remarks>
    public partial class OperationInputWatcher : Component
    {
        /// <summary>
        /// Raised with each input as it comes in, before anything else handles it.
        /// </summary>
        public event Action<UIEvent>? Received;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Received != null;

        protected override bool Handle(UIEvent e)
        {
            Received?.Invoke(e);
            return false;
        }
    }
}
