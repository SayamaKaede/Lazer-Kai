// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Which rows the composer's status panel carries. Its own fixture, because those settings live in the config and
    /// would otherwise carry into the next test.
    /// </summary>
    [TestFixture]
    public partial class TestSceneComposeStatusRows : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private static readonly OsuSetting[] all_rows =
        {
            OsuSetting.EditorStatusShowStarRating,
            OsuSetting.EditorStatusShowMaxPerformance,
            OsuSetting.EditorStatusShowScore,
            OsuSetting.EditorStatusShowLegacyScore,
            OsuSetting.EditorStatusShowSliderVelocity,
            OsuSetting.EditorStatusShowNormalBank,
            OsuSetting.EditorStatusShowAdditionBank,
            OsuSetting.EditorStatusShowSampleVolume,
            OsuSetting.EditorStatusShowKiai,
            OsuSetting.EditorStatusShowCombo,
            OsuSetting.EditorStatusShowMods,
            // there while stable compatibility is on, which it is from the start (2026-10-23).
            OsuSetting.EditorStatusShowSaveFormat,
        };

        [SetUpSteps]
        public void SetUpStatusSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("the panel is up", () => Editor.ChildrenOfType<ComposeStatusDisplay>().Any());

            // the mods row steps out of the flow when nothing is selected, so there has to be something selected for
            // its own setting to be the thing deciding.
            AddStep("select a test play mod", () => Editor.TestPlayMods.Mods.Value = new Mod[] { new OsuModHidden() });
        }

        [TearDownSteps]
        public void TearDownStatusSteps()
        {
            AddStep("restore the rows", () =>
            {
                foreach (var row in all_rows)
                    config.GetBindable<bool>(row).SetDefault();
            });
        }

        private ComposeStatusDisplay status => Editor.ChildrenOfType<ComposeStatusDisplay>().Single();

        private int visibleRows => status.ChildrenOfType<osu.Framework.Graphics.Containers.FillFlowContainer>()
                                         .First()
                                         .Children.Count(c => c.IsPresent);

        [Test]
        public void TestEveryRowCanBeTurnedOffOnItsOwn()
        {
            int all = 0;

            AddStep("count the rows", () => all = visibleRows);
            AddAssert("they are all there", () => all, () => Is.EqualTo(all_rows.Length));

            foreach (var row in all_rows)
            {
                var hidden = row;

                AddStep($"turn off {hidden}", () => config.SetValue(hidden, false));
                AddUntilStep("one fewer row", () => visibleRows, () => Is.EqualTo(all - 1));

                AddStep($"turn {hidden} back on", () => config.SetValue(hidden, true));
                AddUntilStep("it came back", () => visibleRows, () => Is.EqualTo(all));
            }
        }

        [Test]
        public void TestTheWholePanelCanBeEmptied()
        {
            AddStep("turn every row off", () =>
            {
                foreach (var row in all_rows)
                    config.SetValue(row, false);
            });

            AddUntilStep("nothing is left", () => visibleRows, () => Is.Zero);
        }
    }
}
