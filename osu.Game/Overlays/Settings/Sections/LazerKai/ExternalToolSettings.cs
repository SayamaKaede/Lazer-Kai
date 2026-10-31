// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Localisation;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    public partial class ExternalToolSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.ExternalToolHeader;

        private readonly BindableBool shown = new BindableBool();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            // left empty by default rather than filled in with the usual install: a path written down here is
            // one that has to keep being right, and the usual install is looked for anyway when this is empty.
            // It is here for an install that is somewhere else — a portable copy, another drive.
            var path = new MappingToolsPathTextBox
            {
                LabelText = LazerKaiSettingsStrings.MappingToolsPath,
                TooltipText = LazerKaiSettingsStrings.MappingToolsPathDescription,
                Current = config.GetBindable<string>(OsuSetting.MappingToolsPath),
            };

            var reveal = new SettingsButton
            {
                Action = () => shown.Toggle(),
            };

            Children = new Drawable[]
            {
                reveal,
                path,
            };

            // out of sight until it is asked for. A path very often carries the account name of whoever is at
            // the machine, and settings get opened on stream and in screenshots for reasons that have nothing
            // to do with this row. Hidden as a whole rather than with the characters covered up: what stands in
            // for a hidden value must never be something the setting could end up saving.
            shown.BindValueChanged(s =>
            {
                path.Alpha = s.NewValue ? 1 : 0;
                reveal.Text = s.NewValue ? LazerKaiSettingsStrings.HideMappingToolsPath : LazerKaiSettingsStrings.ShowMappingToolsPath;
            }, true);
        }
    }
}
