// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The export menu writes one difficulty out in stable's format or in lazer's; a file kept in stable's format has
    /// nothing in lazer's to write, so that item is not offered for it.
    /// </summary>
    public partial class TestSceneExportingOneDifficulty : EditorSavingTestScene
    {
        private const string difficulty_file = @"Soleily - Renatus (Gamu) [Hard].osu";

        protected override Ruleset CreateRuleset() => new OsuRuleset();

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                Game.LocalConfig.SetValue(OsuSetting.StableCompatibility, true);

                var importedSet = Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely();
                return Game.BeatmapManager.GetWorkingBeatmap(importedSet!.Value.Beatmaps.Single(b => b.Path == difficulty_file));
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("settings back to default", () => Game.LocalConfig.GetBindable<bool>(OsuSetting.StableCompatibility).SetDefault());
            base.TearDownSteps();
        }

        [Test]
        public void TestLazerExportIsDisabledForAStableFile()
        {
            AddAssert("stable export is offered", () => item(EditorStrings.ExportBeatmapFile).Action.Disabled, () => Is.False);
            AddAssert("lazer export is not", () => item(EditorStrings.ExportLazerBeatmapFile).Action.Disabled, () => Is.True);
        }

        private MenuItem item(Framework.Localisation.LocalisableString text)
            => flatten(Editor.ChildrenOfType<EditorMenuBar>().Single().Items).Single(i => i.Text.Value.Equals(text));

        private static IEnumerable<MenuItem> flatten(IEnumerable<MenuItem> items)
            => items.SelectMany(i => new[] { i }.Concat(flatten(i.Items)));
    }
}
