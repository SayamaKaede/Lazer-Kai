// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit.GreenLines
{
    public partial class RemoveUnneededGreenLinesDialog : DangerousActionDialog
    {
        public RemoveUnneededGreenLinesDialog(Action removeAction, LocalisableString? header = null, LocalisableString? body = null)
        {
            HeaderText = header ?? LazerKaiGreenLineStrings.RemoveUnneededHeader;
            BodyText = body ?? LazerKaiGreenLineStrings.RemoveUnneededBody;
            DangerousAction = removeAction;
        }
    }
}
