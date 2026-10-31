// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using System.IO;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Notifications;
using osu.Game.Utils;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// What is used when something goes wrong: the logs to send by hand, and whether errors are sent on their own.
    /// </summary>
    public partial class BugReportSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.BugReportHeader;

        [Resolved]
        private Storage storage { get; set; } = null!;

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private INotificationOverlay? notifications { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsButton
                {
                    Text = LazerKaiSettingsStrings.ExportLogs,
                    TooltipText = LazerKaiSettingsStrings.ExportLogsDescription,
                    Action = exportLogs,
                },
                new SettingsCheckbox
                {
                    LabelText = LazerKaiSettingsStrings.ErrorReporting,
                    TooltipText = LazerKaiSettingsStrings.ErrorReportingDescription,
                    Current = config.GetBindable<bool>(OsuSetting.ErrorReporting),
                },
            };
        }

        /// <summary>
        /// Writes copies of the recent logs with the account name taken out, and opens the folder they went to.
        /// </summary>
        private void exportLogs()
        {
            try
            {
                (string destination, int written) = LogExport.WriteForSending(storage, Logger.Storage);

                notifications?.Post(new SimpleNotification
                {
                    Icon = FontAwesome.Solid.FileExport,
                    Text = LazerKaiSettingsStrings.ExportedLogs(written.ToString(NumberFormatInfo.CurrentInfo)),
                    Activated = () =>
                    {
                        gameHost.OpenFileExternally(destination.TrimDirectorySeparator() + Path.DirectorySeparatorChar);
                        return true;
                    },
                });

                gameHost.OpenFileExternally(destination.TrimDirectorySeparator() + Path.DirectorySeparatorChar);
            }
            catch (Exception ex)
            {
                Logger.Log(PathDisplay.MaskIn($@"Failed to export logs: {ex}"), LoggingTarget.Runtime);

                notifications?.Post(new SimpleErrorNotification
                {
                    Icon = FontAwesome.Solid.ExclamationTriangle,
                    Text = LazerKaiSettingsStrings.ExportLogsFailed,
                });
            }
        }
    }
}
