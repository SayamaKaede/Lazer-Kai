// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// What to know about stable compatibility, shown when it is turned on: what it covers, the backups it takes, and
    /// what happens when a save cannot stay in stable's format.
    /// </summary>
    public partial class StableCompatibilityNoticeDialog : PopupDialog
    {
        public StableCompatibilityNoticeDialog()
        {
            HeaderText = LazerKaiStableCompatibilityStrings.NoticeHeader;
            BodyText = LocalisableString.Interpolate($"{LazerKaiStableCompatibilityStrings.NoticeSupportedModes}\n{LazerKaiStableCompatibilityStrings.NoticeBackups}\n{LazerKaiStableCompatibilityStrings.NoticeWarning}");

            Icon = FontAwesome.Solid.InfoCircle;

            Buttons = new PopupDialogButton[]
            {
                new PopupDialogOkButton
                {
                    Text = LazerKaiStableCompatibilityStrings.NoticeOk,
                },
            };
        }
    }
}
