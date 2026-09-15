// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Containers;
using osu.Framework.Testing;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections;
using osu.Game.Overlays.Settings.Sections.LazerKai;

namespace osu.Game.Tests.Visual.Settings
{
    /// <summary>
    /// The fork's settings sections list their subsections in the sidebar while being read.
    /// </summary>
    public partial class TestSceneSettingsSidebarSubsections : OsuManualInputManagerTestScene
    {
        private SettingsOverlay settings = null!;

        private SidebarSubsectionButton[] rowsOf<TSection>() where TSection : SettingsSection
            => settings.ChildrenOfType<SidebarSubsectionButton>().Where(b => b.Section is TSection).ToArray();

        private SidebarIconButton buttonOf<TSection>() where TSection : SettingsSection
            => settings.ChildrenOfType<SidebarIconButton>().Single(b => b.Section is TSection);

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("open settings", () => Child = settings = new SettingsOverlay { State = { Value = Visibility.Visible } });
            AddUntilStep("sidebar loaded", () => settings.ChildrenOfType<SidebarIconButton>().Any(b => b.Section is LazerKaiSection));
        }

        [Test]
        public void TestReadSectionListsItsSubsections()
        {
            AddStep("go to Lazer-Kai", () => buttonOf<LazerKaiSection>().TriggerClick());
            AddUntilStep("Lazer-Kai is being read", () => settings.SectionsContainer.SelectedSection.Value is LazerKaiSection);

            AddUntilStep("every Lazer-Kai subsection is listed", () =>
            {
                var section = settings.SectionsContainer.OfType<LazerKaiSection>().Single();
                var rows = rowsOf<LazerKaiSection>();

                return rows.Length > 0
                       && rows.Length == section.Children.OfType<SettingsSubsection>().Count()
                       && rows.All(r => r.IsPresent);
            });
            AddAssert("the beatmap section's subsections are not listed", () => rowsOf<LazerKaiBeatmapSection>().All(r => !r.IsPresent));

            AddStep("go to the beatmap section", () => buttonOf<LazerKaiBeatmapSection>().TriggerClick());
            AddUntilStep("the beatmap section's subsections are listed", () => rowsOf<LazerKaiBeatmapSection>().Any(r => r.IsPresent));
            AddUntilStep("Lazer-Kai's are not", () => rowsOf<LazerKaiSection>().All(r => !r.IsPresent));
        }

        [Test]
        public void TestRowGoesToItsSubsection()
        {
            AddStep("go to Lazer-Kai", () => buttonOf<LazerKaiSection>().TriggerClick());
            AddUntilStep("status row listed", () => rowsOf<LazerKaiSection>().Any(r => r.Subsection is EditorStatusSettings && r.IsPresent));

            AddStep("press the status row", () => rowsOf<LazerKaiSection>().Single(r => r.Subsection is EditorStatusSettings).TriggerClick());
            AddUntilStep("status is the row being read", () => rowsOf<LazerKaiSection>().Single(r => r.Selected).Subsection is EditorStatusSettings);

            // with Lazer-Kai's rows listed the sidebar is taller than the test window, and status is near the end.
            AddUntilStep("the status row is in view", () =>
            {
                var sidebar = settings.ChildrenOfType<SettingsSidebar>().First(s => s.Parent == settings);
                var row = rowsOf<LazerKaiSection>().Single(r => r.Subsection is EditorStatusSettings);

                return sidebar.ScreenSpaceDrawQuad.Contains(row.ScreenSpaceDrawQuad.TopLeft)
                       && sidebar.ScreenSpaceDrawQuad.Contains(row.ScreenSpaceDrawQuad.BottomLeft);
            });
        }
    }
}
