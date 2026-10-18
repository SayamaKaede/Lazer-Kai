// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.IO;
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
    ///
    /// Each of the four also lists its own subsections in the sidebar while it is being read
    /// (<see cref="SettingsSection.ListSubsectionsInSidebar"/>): even split four ways, a group was a long scroll away.
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
            ListSubsectionsInSidebar = true;

            var groups = new List<Drawable>();

            // the guide, the manual and the changelogs are only beside the executable in a build that was published,
            // and a row that opens nothing is worse than no row at all. Decided here rather than inside the group: a
            // subsection takes its alpha from the settings search, so one that faded itself out would be faded back
            // in the moment the panel filtered anything.
            string? guide = LazerKaiManual.FindGuide(RuntimeInfo.StartupDirectory);
            string? manual = LazerKaiManual.Find(RuntimeInfo.StartupDirectory);
            var changelog = LazerKaiChangelog.Read(RuntimeInfo.StartupDirectory);

            if (guide != null || manual != null || changelog.Count > 0)
                groups.Add(new ManualSettings(guide, manual, changelog));

            groups.AddRange(new Drawable[]
            {
                // first, for somebody opening these for the first time: the recommended settings are one click away.
                new SettingsPresetSettings(),
                new AppearanceSettings(),
                new RulesetSkinSettings(),
                new EditorDisplaySettings(),
                new EditorDifficultyGraphSettings(),
                new EditorGhostDifficultySettings(),
                new EditorToolboxSettings(),
                new EditorButtonShapeSettings(),
                new EditorTimelineSettings(),
                new EditorSelectionSettings(),
                new EditorStatusSettings(),
            });

            Children = groups;
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
            ListSubsectionsInSidebar = true;

            Children = new Drawable[]
            {
                new EditorBeatmapSkinSettings(),
                new EditorHitsoundSettings(),
                new EditorManiaSettings(),
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
            ListSubsectionsInSidebar = true;

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
            ListSubsectionsInSidebar = true;

            Children = new Drawable[]
            {
                new BackupSettings(),
                new SongSelectSettings(),
                new ExternalToolSettings(),
            };
        }
    }
}
