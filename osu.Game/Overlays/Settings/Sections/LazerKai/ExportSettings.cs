// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class ExportSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ExportHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsEnumDropdown<BeatmapExportFormat>
                {
                    LabelText = BeatmapExportStrings.Format,
                    Current = config.GetBindable<BeatmapExportFormat>(OsuSetting.BeatmapExportFormat)
                },
                new SettingsCheckbox
                {
                    LabelText = LazerKaiStableCompatibilityStrings.ImproveCompatibility,
                    TooltipText = LazerKaiStableCompatibilityStrings.ImproveCompatibilityDescription,
                    Current = config.GetBindable<bool>(OsuSetting.StableCompatibility)
                },
            };
        }
    }
}
