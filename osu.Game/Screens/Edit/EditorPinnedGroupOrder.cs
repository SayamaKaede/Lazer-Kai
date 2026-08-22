// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// How pinned toolbox groups are ordered among themselves.
    /// </summary>
    public enum EditorPinnedGroupOrder
    {
        /// <summary>
        /// Pinned groups keep the order they have in the sidebar, so pinning and unpinning never rearranges anything.
        /// </summary>
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.PinnedGroupOrderToolbox))]
        ToolboxOrder,

        /// <summary>
        /// Pinned groups stack in the order they were pinned, so the first one pinned stays on top.
        /// </summary>
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.PinnedGroupOrderPinned))]
        OrderPinned,
    }
}
