// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Overlays.FirstRunSetup
{
    /// <summary>
    /// Where somebody who has just installed the build starts: shown once the first-run setup is closed, whether it
    /// was finished or not.
    /// </summary>
    /// <remarks>
    /// There are enough features that the manual on its own left people new to the build lost (2026-10-14). This
    /// points at the getting-started guide, which is short and leads on into the manual, and offers the recommended
    /// settings, asked about and applied as they are from the settings.
    /// </remarks>
    public partial class LazerKaiWelcomeDialog : PopupDialog
    {
        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        [Resolved]
        private INotificationOverlay? notifications { get; set; }

        /// <param name="openGuide">Opens the getting-started guide, or null when there is none beside the game.</param>
        public LazerKaiWelcomeDialog(Action? openGuide)
        {
            HeaderText = LazerKaiWelcomeStrings.Header;
            BodyText = LazerKaiWelcomeStrings.Body;

            Icon = FontAwesome.Solid.BookOpen;

            var buttons = new List<PopupDialogButton>();

            if (openGuide != null)
            {
                buttons.Add(new PopupDialogOkButton
                {
                    Text = LazerKaiSettingsStrings.OpenGuide,
                    // the guide opens outside the game, and the recommended settings are still here to apply on
                    // coming back to it.
                    HideDialogOnAction = false,
                    Action = openGuide,
                });
            }

            buttons.Add(new PopupDialogOkButton
            {
                Text = LazerKaiWelcomeStrings.ApplyRecommendedSettings,
                Action = () => dialogOverlay?.Push(SettingsPresetSettings.AskToApplyRecommended(storage, config, notifications)),
            });

            buttons.Add(new PopupDialogCancelButton
            {
                Text = LazerKaiWelcomeStrings.Close,
            });

            Buttons = buttons;
        }
    }
}
