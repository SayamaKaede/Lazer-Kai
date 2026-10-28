// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Text.RegularExpressions;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// What is there for working on this game, and not for mapping (2026-10-28). Not offered in a distributed build.
    /// </summary>
    public partial class DevelopmentSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.DevelopmentHeader;

        /// <summary>
        /// Whether <paramref name="version"/> is that of a distributed build: a date and a revision, as publish.ps1 numbers
        /// one. A portable build carries the date and the commit, and a local build no version at all.
        /// </summary>
        public static bool IsDistributedVersion(string version) => Regex.IsMatch(version, @"^\d{8}\.\d+$");

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, Storage storage)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormCheckBox
                {
                    Caption = LazerKaiSettingsStrings.EditorOperationLog,
                    HintText = LazerKaiSettingsStrings.EditorOperationLogDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorOperationLog),
                }),
                new SettingsButton
                {
                    Margin = new MarginPadding { Top = BUTTON_GAP },
                    Text = LazerKaiSettingsStrings.OpenOperationLogs,
                    Action = () => storage.GetStorageForDirectory(EditorOperationLog.DIRECTORY).PresentExternally(),
                },
            };
        }
    }
}
