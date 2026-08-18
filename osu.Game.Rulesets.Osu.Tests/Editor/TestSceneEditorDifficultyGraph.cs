// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    [TestFixture]
    public partial class TestSceneEditorDifficultyGraph : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// The graph has to describe the beatmap being edited. Reading it back through the working beatmap re-converts
        /// what was loaded from disk, so everything placed or deleted since then would be missing from the curve —
        /// including at the moment of a save, which is exactly when a mapper looks at it.
        /// </summary>
        [Test]
        public void TestGraphFollowsEditsRatherThanTheLoadedFile()
        {
            AddStep("show the graph", () => config.SetValue(OsuSetting.EditorShowDifficultyGraph, true));

            AddStep("empty the beatmap", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray()));
            recalculate();
            AddUntilStep("nothing drawn", () => barCount(), () => Is.Zero);

            AddStep("place a stream", () =>
            {
                for (int i = 0; i < 30; i++)
                    EditorBeatmap.Add(new HitCircle { StartTime = 1000 + i * 125, Position = new Vector2(100 + i * 5, 100) });
            });
            recalculate();
            AddUntilStep("curve drawn", () => barCount(), () => Is.GreaterThan(0));

            AddStep("empty the beatmap again", () => EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray()));
            recalculate();
            AddUntilStep("nothing drawn again", () => barCount(), () => Is.Zero);
        }

        private void recalculate() => AddStep("recalculate", () => graph().RecalculateNow());

        private DifficultyGraphPart graph() => Editor.ChildrenOfType<DifficultyGraphPart>().Single();

        private int barCount() => graph().ChildrenOfType<Box>().Count();
    }
}
