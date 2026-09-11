// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Localisation;
using System.Globalization;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class BackupSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.BackupHeader;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Storage storage)
        {
            config.BindWith(OsuSetting.EditorAutoBackup, enabled);

            var interval = new FormSliderBar<double>
            {
                Caption = EditorStrings.AutoBackupInterval,
                Current = config.GetBindable<double>(OsuSetting.EditorAutoBackupInterval),
                LabelFormat = v => LazerKaiSettingsStrings.Minutes(v.ToString("N0", NumberFormatInfo.CurrentInfo)),
            };

            var keepCount = new FormSliderBar<int>
            {
                Caption = EditorStrings.AutoBackupKeepCount,
                Current = config.GetBindable<int>(OsuSetting.EditorAutoBackupKeepCount),
            };

            var openFolder = new SettingsButton
            {
                // straight under a checkbox, which it would otherwise touch.
                Margin = new MarginPadding { Top = BUTTON_GAP },
                Text = EditorStrings.OpenBackupFolder,
                Action = () => storage.GetStorageForDirectory(EditorAutoBackup.BACKUP_DIRECTORY).PresentExternally(),
            };

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AutoBackupEnabled,
                    Current = enabled
                },
                new SettingsItemV2(interval),
                new SettingsItemV2(keepCount),
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.NotifyOnBackup,
                    Current = config.GetBindable<bool>(OsuSetting.EditorNotifyOnBackup)
                },
                openFolder,
            };

            // the interval and count say nothing while backups are off, so they follow the switch above them.
            enabled.BindValueChanged(e =>
            {
                interval.Current.Disabled = !e.NewValue;
                keepCount.Current.Disabled = !e.NewValue;
            }, true);
        }
    }
}
