// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Overlays;
using osu.Game.Overlays.Mods;

namespace osu.Game.Screens.Edit.GameplayTest
{
    /// <summary>
    /// The song select mod screen, opened from the editor to choose what a test play runs with.
    /// </summary>
    /// <remarks>
    /// Reused rather than rebuilt as a menu: mod settings, incompatibility handling, search and presets all come with
    /// it, and none of them fit into a list of toggles. The selection it writes to is the editor's own, so the mods
    /// picked at song select are left untouched.
    /// </remarks>
    public partial class EditorModSelectOverlay : UserModSelectOverlay
    {
        public EditorModSelectOverlay(OverlayColourScheme colourScheme)
            : base(colourScheme)
        {
            ShowPresets = true;
        }
    }
}
