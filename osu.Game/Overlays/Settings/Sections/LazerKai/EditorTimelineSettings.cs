// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Screens.Edit;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class EditorTimelineSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.TimelineHeader;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsItemV2(new FormEnumDropdown<EditorTimelineStyle>
                {
                    Caption = EditorStrings.TimelineStyle,
                    HintText = EditorStrings.TimelineStyleDescription,
                    Current = config.GetBindable<EditorTimelineStyle>(OsuSetting.EditorTimelineStyle),
                }),
                new SettingsCheckbox
                {
                    LabelText = LazerKaiSavedLinesStrings.ShowStableGreenLines,
                    TooltipText = LazerKaiSavedLinesStrings.ShowStableGreenLinesDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorTimelineShowSavedLines)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.TimelineInspector,
                    TooltipText = EditorStrings.TimelineInspectorDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorTimelineInspector)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.TimelineDragToSeek,
                    TooltipText = EditorStrings.TimelineDragToSeekDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorTimelineDragToSeek)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.TimelineUseSkin,
                    TooltipText = EditorStrings.TimelineUseSkinDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorTimelineUseSkin)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.AlwaysExpandTimelinePointLabels,
                    TooltipText = EditorStrings.AlwaysExpandTimelinePointLabelsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandTimelinePointLabels)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideTimelinePoints,
                    TooltipText = EditorStrings.HideTimelinePointsDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideTimelinePoints)
                },
                new SettingsCheckbox
                {
                    LabelText = EditorStrings.HideTimelineVolume,
                    TooltipText = EditorStrings.HideTimelineVolumeDescription,
                    Current = config.GetBindable<bool>(OsuSetting.EditorHideTimelineVolume)
                },
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.TimelineDragAreaScale,
                    HintText = EditorStrings.TimelineDragAreaScaleDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorTimelineDragAreaScale),
                    DisplayAsPercentage = true,
                }),
                new SettingsItemV2(new FormSliderBar<float>
                {
                    Caption = EditorStrings.TimelineRepeatDragThreshold,
                    HintText = EditorStrings.TimelineRepeatDragThresholdDescription,
                    Current = config.GetBindable<float>(OsuSetting.EditorTimelineRepeatDragThreshold),
                    DisplayAsPercentage = true,
                }),
            };
        }
    }
}
