// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Structure used to convey the general state of an <see cref="Editor"/> instance.
    /// </summary>
    public class EditorState
    {
        /// <summary>
        /// The current editor mode.
        /// </summary>
        public EditorScreenMode Mode { get; set; }

        /// <summary>
        /// The current audio time.
        /// </summary>
        public double Time { get; set; }

        /// <summary>
        /// The editor clipboard content.
        /// </summary>
        public string ClipboardContent { get; set; } = string.Empty;

        /// <summary>
        /// The hitsounds copied on the hitsound screen, kept as the objects are when switching difficulty: those of the
        /// hitsound difficulty can be pasted into another.
        /// </summary>
        public IReadOnlyList<CopiedHitsound> HitsoundClipboard { get; set; } = Array.Empty<CopiedHitsound>();
    }
}
