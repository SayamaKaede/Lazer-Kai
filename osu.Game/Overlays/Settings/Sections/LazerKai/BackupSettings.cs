// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.LazerKai.Controls;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class BackupSettings : SettingsSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.BackupHeader;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Storage storage)
        {
            config.BindWith(OsuSetting.EditorAutoBackup, enabled);

            var interval = new SettingsSlider<double, MinutesSlider>
            {
                LabelText = EditorStrings.AutoBackupInterval,
                Current = config.GetBindable<double>(OsuSetting.EditorAutoBackupInterval)
            };

            var keepCount = new SettingsSlider<int>
            {
                LabelText = EditorStrings.AutoBackupKeepCount,
                Current = config.GetBindable<int>(OsuSetting.EditorAutoBackupKeepCount)
            };

            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AutoBackupEnabled,
                    Current = enabled
                },
                interval,
                keepCount,
                new SettingsButton
                {
                    Text = EditorStrings.OpenBackupFolder,
                    Action = () => storage.GetStorageForDirectory(EditorAutoBackup.BACKUP_DIRECTORY).PresentExternally(),
                },
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
