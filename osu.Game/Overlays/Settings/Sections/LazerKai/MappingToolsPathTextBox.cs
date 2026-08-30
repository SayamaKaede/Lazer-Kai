// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;
using osu.Game.Utils;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// A path box that, while empty, shows in grey the Mapping Tools that was found without being told.
    /// </summary>
    /// <remarks>
    /// An empty box is otherwise the same sight whether the usual install was found or nothing was, and that
    /// difference is the whole question being asked of this setting. Shown rather than filled in, and greyed
    /// rather than typed, because it is what would be used and not what was chosen — and the box has to stay
    /// writable for somebody who has a copy installed and means to use a different one.
    /// </remarks>
    public partial class MappingToolsPathTextBox : SettingsTextBox
    {
        /// <summary>
        /// What is shown while nothing has been typed.
        /// </summary>
        /// <remarks>
        /// Masked, because this is a path the game puts on screen without being asked to: nobody typed it, so
        /// nobody chose to show it. A portable copy of Mapping Tools very often sits under an account folder.
        /// </remarks>
        public static LocalisableString Detected =>
            MappingToolsHandoff.FindExecutable(string.Empty) is string found ? PathDisplay.Mask(found) : LazerKaiSettingsStrings.MappingToolsNotDetected;

        protected override Drawable CreateControl()
        {
            var control = (OutlinedTextBox)base.CreateControl();

            control.PlaceholderText = Detected;

            return control;
        }
    }
}
