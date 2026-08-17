// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Overlays.Settings.Sections
{
    /// <summary>
    /// Gathers everything this fork adds, so it can all be found in one place.
    /// </summary>
    /// <remarks>
    /// These settings also appear in the editor's own menus, where they are reachable while working. This section is
    /// for finding out what exists in the first place.
    /// </remarks>
    public partial class LazerKaiSection : SettingsSection
    {
        public override LocalisableString Header => LazerKaiSettingsStrings.SectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.PenNib
        };

        public LazerKaiSection()
        {
            Children = new Drawable[]
            {
                new EditorDisplaySettings(),
                new EditorSelectionSettings(),
                new ComposingSettings(),
                new EditorInputSettings(),
                new TestPlaySettings(),
                new BackupSettings(),
            };
        }
    }
}
