// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Asked before a save of a beatmap kept in stable's format goes to lazer's instead, since the file does not come
    /// back from that.
    /// </summary>
    public partial class CannotStayStableDialog : PopupDialog
    {
        public CannotStayStableDialog(Action saveInLazerFormat, Action exportLogs)
        {
            HeaderText = LazerKaiStableCompatibilityStrings.CannotStayStableHeader;
            BodyText = LazerKaiStableCompatibilityStrings.CannotStayStableBody;

            Icon = FontAwesome.Solid.ExclamationTriangle;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogDangerousButton
                {
                    Text = LazerKaiStableCompatibilityStrings.SaveInLazerFormat,
                    Action = saveInLazerFormat
                },
                new PopupDialogOkButton
                {
                    Text = LazerKaiSettingsStrings.ExportLogs,
                    Action = exportLogs
                },
                new PopupDialogCancelButton
                {
                    Text = LazerKaiStableCompatibilityStrings.DoNotSave,
                },
            };
        }
    }
}
