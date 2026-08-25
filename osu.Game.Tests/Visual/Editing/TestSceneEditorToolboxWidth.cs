// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// How wide the composer's toolboxes open, which is set from the settings screen rather than the editor's menus.
    /// </summary>
    /// <remarks>
    /// Its own fixture, because the widths live in the config and would otherwise carry into the next test.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneEditorToolboxWidth : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        // told apart by where they sit rather than by the order they were built in, which is not this test's to know.
        private ExpandingToolboxContainer[] toolboxes => Editor.ChildrenOfType<ExpandingToolboxContainer>()
                                                               .OrderBy(t => t.ScreenSpaceDrawQuad.TopLeft.X)
                                                               .ToArray();

        private ExpandingToolboxContainer left => toolboxes.First();

        private ExpandingToolboxContainer right => toolboxes.Last();

        [SetUpSteps]
        public void SetUpWidthSteps()
        {
            AddStep("go to compose", () => Editor.Mode.Value = EditorScreenMode.Compose);
            AddUntilStep("both toolboxes are up", () => toolboxes.Length, () => Is.EqualTo(2));
        }

        [TearDownSteps]
        public void TearDownWidthSteps()
        {
            AddStep("restore the widths", () =>
            {
                config.GetBindable<float>(OsuSetting.EditorLeftToolboxWidth).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorRightToolboxWidth).SetDefault();
                config.GetBindable<bool>(OsuSetting.EditorAlwaysExpandLeftToolbox).SetDefault();
            });
        }

        [Test]
        public void TestEachToolboxTakesItsOwnWidth()
        {
            AddStep("widen the left one", () => config.SetValue(OsuSetting.EditorLeftToolboxWidth, 320f));
            AddUntilStep("the left one took it", () => left.ExpandedWidth, () => Is.EqualTo(320f));
            AddAssert("the right one did not", () => right.ExpandedWidth, () => Is.EqualTo(250f));

            AddStep("narrow the right one", () => config.SetValue(OsuSetting.EditorRightToolboxWidth, 180f));
            AddUntilStep("the right one took it", () => right.ExpandedWidth, () => Is.EqualTo(180f));
            AddAssert("the left one is where it was", () => left.ExpandedWidth, () => Is.EqualTo(320f));
        }

        /// <remarks>
        /// The width is dragged in the settings screen with the editor behind it, so a toolbox already open has to
        /// follow rather than wait for the next time it opens.
        /// </remarks>
        [Test]
        public void TestAToolboxAlreadyOpenFollowsAtOnce()
        {
            AddStep("hold the left one open", () => config.SetValue(OsuSetting.EditorAlwaysExpandLeftToolbox, true));
            AddUntilStep("it is open", () => left.Expanded.Value);
            AddUntilStep("at the width it starts with", () => left.DrawWidth, () => Is.EqualTo(200f).Within(1));

            AddStep("widen it", () => config.SetValue(OsuSetting.EditorLeftToolboxWidth, 320f));
            AddUntilStep("it grew without being closed", () => left.DrawWidth, () => Is.EqualTo(320f).Within(1));
        }
    }
}
