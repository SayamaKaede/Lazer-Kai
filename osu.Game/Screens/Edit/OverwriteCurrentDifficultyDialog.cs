// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Asked before a dropped file replaces the difficulty currently open.
    /// </summary>
    /// <remarks>
    /// Only this case is asked about. Replacing another difficulty in the set costs nothing that is not already
    /// on disk, but replacing this one throws away whatever is on screen and unsaved.
    /// </remarks>
    public partial class OverwriteCurrentDifficultyDialog : DangerousActionDialog
    {
        public OverwriteCurrentDifficultyDialog(string difficultyName, Action overwriteAction, Action cancelAction)
        {
            HeaderText = EditorDialogsStrings.OverwriteCurrentDifficultyHeader;
            BodyText = EditorDialogsStrings.OverwriteCurrentDifficultyDetails(difficultyName);
            Icon = FontAwesome.Solid.FileImport;

            DangerousAction = overwriteAction;
            CancelAction = cancelAction;
        }
    }
}
