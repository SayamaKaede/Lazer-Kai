// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Rulesets;

namespace osu.Game.Overlays.Settings.Sections.Input
{
    public partial class KeyBindingPanel : SettingsSubPanel
    {
        protected override Drawable CreateHeader() => new SettingsHeader(InputSettingsStrings.KeyBindingPanelHeader, InputSettingsStrings.KeyBindingPanelDescription);

        [BackgroundDependencyLoader(permitNulls: true)]
        private void load(RulesetStore rulesets)
        {
            AddSection(new GlobalKeyBindingsSection(FontAwesome.Solid.Globe, InputSettingsStrings.GlobalKeyBindingHeader)
            {
                Children = new[]
                {
                    new GlobalKeyBindingsSubsection(CommonStrings.General, GlobalActionCategory.General),
                    new GlobalKeyBindingsSubsection(InputSettingsStrings.AudioSection, GlobalActionCategory.AudioControl),
                    new GlobalKeyBindingsSubsection(InputSettingsStrings.OverlaysSection, GlobalActionCategory.Overlays),
                }
            });

            AddSection(new GlobalKeyBindingsSection(OsuIcon.GameplayC, GameplaySettingsStrings.GameplaySectionHeader)
            {
                Children = new[]
                {
                    new GlobalKeyBindingsSubsection(CommonStrings.General, GlobalActionCategory.InGame),
                    new GlobalKeyBindingsSubsection(InputSettingsStrings.ReplaySection, GlobalActionCategory.Replay),
                }
            });

            foreach (var ruleset in rulesets.AvailableRulesets)
                AddSection(new RulesetBindingsSection(ruleset));

            AddSection(new GlobalKeyBindingsSection(OsuIcon.Beatmap, InputSettingsStrings.SongSelectSection)
            {
                Children = new[]
                {
                    new GlobalKeyBindingsSubsection(CommonStrings.General, GlobalActionCategory.SongSelect),
                }
            });

            List<KeyBindingsSubsection> rulesetEditorVariants = new List<KeyBindingsSubsection>();

            foreach (var ruleset in rulesets.AvailableRulesets)
                rulesetEditorVariants.Add(new RulesetEditorBindingsSubsection(ruleset));

            AddSection(new GlobalKeyBindingsSection(OsuIcon.EditorSelect, InputSettingsStrings.EditorSection)
            {
                ChildrenEnumerable = new KeyBindingsSubsection[]
                {
                    new GlobalKeyBindingsSubsection(CommonStrings.General, GlobalActionCategory.Editor),
                    new GlobalKeyBindingsSubsection(InputSettingsStrings.EditorTestPlaySection, GlobalActionCategory.EditorTestPlay),
                }.Concat(rulesetEditorVariants),
            });

            // a section of its own rather than one heading under the editor's, with its headings listed in the sidebar
            // the way this fork's settings are: there were 26 bindings under one heading, and finding one meant reading
            // all of them (2026-10-18).
            AddSection(new GlobalKeyBindingsSection(FontAwesome.Solid.PenNib, LazerKaiSettingsStrings.SectionHeader)
            {
                ListSubsectionsInSidebar = true,
                Children = new[]
                {
                    new GlobalKeyBindingsSubsection(LazerKaiSettingsStrings.DisplayHeader, GlobalActionCategory.EditorLazerKaiDisplay),
                    new GlobalKeyBindingsSubsection(LazerKaiSettingsStrings.KeyBindingPlaybackHeader, GlobalActionCategory.EditorLazerKaiPlayback),
                    new GlobalKeyBindingsSubsection(LazerKaiSettingsStrings.KeyBindingEditingHeader, GlobalActionCategory.EditorLazerKaiEditing),
                    new GlobalKeyBindingsSubsection(LazerKaiSettingsStrings.KeyBindingHitsoundModeHeader, GlobalActionCategory.EditorLazerKaiHitsoundMode),
                    new GlobalKeyBindingsSubsection(LazerKaiSettingsStrings.OtherSectionHeader, GlobalActionCategory.EditorLazerKaiOther),
                }
            });
        }
    }
}
