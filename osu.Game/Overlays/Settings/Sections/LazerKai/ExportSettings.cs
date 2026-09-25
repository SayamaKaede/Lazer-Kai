// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
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

        [Resolved]
        private SessionStatics sessionStatics { get; set; } = null!;

        [Resolved]
        private IDialogOverlay? dialogOverlay { get; set; }

        private Bindable<bool> stableCompatibility = null!;
        private Bindable<bool> noticeShown = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            stableCompatibility = config.GetBindable<bool>(OsuSetting.StableCompatibility);

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
                    Current = stableCompatibility
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            noticeShown = sessionStatics.GetBindable<bool>(Static.StableCompatibilityNoticeShown);

            // turned on, not found on: somebody who had it on already has been using it.
            stableCompatibility.BindValueChanged(on =>
            {
                if (!on.NewValue || noticeShown.Value)
                    return;

                noticeShown.Value = true;
                dialogOverlay?.Push(new StableCompatibilityNoticeDialog());
            });
        }
    }
}
