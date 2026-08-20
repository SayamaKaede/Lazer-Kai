// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Screens;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// Song select's mod selection and the editor's test play mods being one and the same.
    /// </summary>
    /// <remarks>
    /// A fixture of its own because the setting has to be on before the editor is first loaded, and because it lives in
    /// config, where it would otherwise carry into every test run after it.
    /// </remarks>
    public partial class TestSceneEditorTestPlayModCarryOver : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        public override void SetUpSteps()
        {
            AddStep("share the mod selection", () => config.SetValue(OsuSetting.EditorCarryOverSongSelectMods, true));

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("stop sharing the mod selection", () => config.SetValue(OsuSetting.EditorCarryOverSongSelectMods, false));

            base.TearDownSteps();
        }

        /// <summary>
        /// Set here rather than as a step of its own: exiting the previous screens returns their lease on the
        /// selection, which reverts it, so it has to be put in place immediately before the editor is loaded.
        /// </summary>
        protected override void LoadEditor()
        {
            SelectedMods.Value = new[] { new OsuModHardRock() };
            base.LoadEditor();
        }

        [Test]
        public void TestEditorStartsFromSongSelectMods()
        {
            AddAssert("test play took hard rock", () => Editor.TestPlayMods.Mods.Value.OfType<OsuModHardRock>().Any(), () => Is.True);

            // the editor screen itself still runs unmodded: it is only the test play selection that inherits.
            AddAssert("the editor is not running it", () => Editor.Mods.Value, () => Is.Empty);
        }

        /// <summary>
        /// And the other way round, in spite of the lease on the selection reverting as the editor goes away.
        /// </summary>
        [Test]
        public void TestEditorModsAreLeftAtSongSelect()
        {
            Editor firstEditor = null!;

            AddStep("pick double time at 1.25x in the editor", () =>
            {
                firstEditor = Editor;
                Editor.TestPlayMods.Mods.Value = new[] { new OsuModDoubleTime { SpeedChange = { Value = 1.25 } } };
            });

            AddStep("exit editor", () => Editor.Exit());
            AddUntilStep("wait for editor exit", () => !firstEditor.IsCurrentScreen());

            AddUntilStep("song select ended up on double time", () => SelectedMods.Value.OfType<OsuModDoubleTime>().SingleOrDefault()?.SpeedChange.Value,
                () => Is.EqualTo(1.25));
        }
    }
}
