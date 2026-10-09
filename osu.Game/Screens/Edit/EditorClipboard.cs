// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Bindables;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Wraps the contents of the editor clipboard.
    /// </summary>
    public class EditorClipboard
    {
        public Bindable<string> Content { get; } = new Bindable<string>();

        /// <summary>
        /// The hitsounds copied on the hitsound screen, which copies those rather than the objects (2026-10-08).
        /// </summary>
        public Bindable<IReadOnlyList<CopiedHitsound>> Hitsounds { get; } = new Bindable<IReadOnlyList<CopiedHitsound>>(Array.Empty<CopiedHitsound>());
    }
}
