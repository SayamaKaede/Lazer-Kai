// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Sits beside the buttons of the sound folder, and opens the presets the lanes can be saved to and taken from.
    /// </summary>
    public partial class LaneSoundPresetButton : RoundedButton, IHasPopover
    {
        private readonly LaneSounds laneSounds;
        private readonly Action applied;

        /// <param name="laneSounds">The lanes, which is what gets saved and what a preset is put on.</param>
        /// <param name="applied">Called once a preset has been put on the lanes, for what shows them to show it.</param>
        public LaneSoundPresetButton(LaneSounds laneSounds, Action applied)
        {
            this.laneSounds = laneSounds;
            this.applied = applied;

            Text = LazerKaiHitsoundStrings.LanePresets;
            Action = this.ShowPopover;
        }

        public Popover GetPopover() => new LaneSoundPresetPopover(laneSounds, applied);
    }
}
