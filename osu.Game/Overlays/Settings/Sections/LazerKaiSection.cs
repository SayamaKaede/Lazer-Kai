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
    /// What this fork adds, as one sidebar entry with the rest folded under it.
    /// </summary>
    /// <remarks>
    /// It was one section until there were nine subsections in it — more than any of the game's own — and
    /// finding a setting meant scrolling past everything else on the way.
    ///
    /// The groups below sit under this one in the sidebar the way the rulesets sit under gameplay:
    /// <see cref="SettingsSection.UseSmallerSidebarButton"/> is what indents them. They are separate sections
    /// rather than subsections because the sidebar only lists sections, and being able to jump to a group is the
    /// whole point.
    ///
    /// Split by when a setting is reached for rather than by what it changes: what the screen looks like, what
    /// the beatmap is made of, testing it, and the things that are neither.
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
                new EditorDifficultyGraphSettings(),
                new EditorGhostDifficultySettings(),
                new EditorToolboxSettings(),
                new EditorButtonShapeSettings(),
                new EditorTimelineSettings(),
                new EditorSelectionSettings(),
                new EditorStatusSettings(),
            };
        }
    }

    public partial class LazerKaiBeatmapSection : SettingsSection
    {
        public override LocalisableString Header => LazerKaiSettingsStrings.BeatmapSectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.Music
        };

        public LazerKaiBeatmapSection()
        {
            UseSmallerSidebarButton = true;

            Children = new Drawable[]
            {
                new EditorBeatmapSkinSettings(),
                new EditorHitsoundSettings(),
                new EditorInputSettings(),
                new EditorOtherCustomisationSettings(),
                new ExportSettings(),
            };
        }
    }

    public partial class LazerKaiTestPlaySection : SettingsSection
    {
        public override LocalisableString Header => LazerKaiSettingsStrings.TestPlaySectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.Play
        };

        public LazerKaiTestPlaySection()
        {
            UseSmallerSidebarButton = true;

            Children = new Drawable[]
            {
                new TestPlaySettings(),
            };
        }
    }

    public partial class LazerKaiOtherSection : SettingsSection
    {
        public override LocalisableString Header => LazerKaiSettingsStrings.OtherSectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = FontAwesome.Solid.Toolbox
        };

        public LazerKaiOtherSection()
        {
            UseSmallerSidebarButton = true;

            Children = new Drawable[]
            {
                new BackupSettings(),
                new ExternalToolSettings(),
            };
        }
    }
}
