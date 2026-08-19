// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.ComboColours;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Writing a colour layout into the beatmap being edited.
    /// </summary>
    public partial class TestSceneComboColourApply : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        private const int colours = 4;

        [Test]
        public void TestApplyWritesSkipsAndUndoesInOneStep()
        {
            int[] before = null!;

            AddStep("lay out four circles, each its own combo", () =>
            {
                EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray());

                for (int i = 0; i < 4; i++)
                    EditorBeatmap.Add(new HitCircle { StartTime = 1000 + i * 500, Position = new Vector2(100 + i * 20, 100), NewCombo = true });

                before = offsets();
            });

            AddStep("apply a two colour layout", () =>
            {
                var project = new ComboColourProject();
                var point = new ComboColourPoint { Time = 0, Mode = ComboColourPointMode.Normal };
                point.Colours.AddRange([2, 3]);
                project.Points.Add(point);

                ComboColourApplier.Apply(EditorBeatmap, project, colours);
            });

            AddAssert("combos take the asked-for colours", () => coloursOfCombos(), () => Is.EqualTo(new[] { 2, 3, 2, 3 }));

            AddStep("undo", () => Editor.Undo());
            AddUntilStep("back to what it was", () => offsets(), () => Is.EqualTo(before));
        }

        /// <summary>
        /// A layout has to clear what a previous one left behind, or an old skip further down the map throws the
        /// colours off from there on.
        /// </summary>
        [Test]
        public void TestApplyClearsSkipsLeftByAnEarlierLayout()
        {
            AddStep("lay out four circles with skips already on them", () =>
            {
                EditorBeatmap.RemoveRange(EditorBeatmap.HitObjects.ToArray());

                for (int i = 0; i < 4; i++)
                    EditorBeatmap.Add(new HitCircle { StartTime = 1000 + i * 500, Position = new Vector2(100 + i * 20, 100), NewCombo = true, ComboOffset = 2 });
            });

            AddStep("apply an empty layout", () => ComboColourApplier.Apply(EditorBeatmap, new ComboColourProject(), colours));

            AddAssert("every skip cleared", () => offsets(), () => Is.All.Zero);
        }

        private int[] offsets() => EditorBeatmap.HitObjects.OfType<IHasComboInformation>().Select(o => o.ComboOffset).ToArray();

        /// <summary>
        /// The colour each combo lands on, walked forward the way the game does it.
        /// </summary>
        private int[] coloursOfCombos()
        {
            int colour = 0;

            return offsets().Select(skip => colour = (colour + skip + 1) % colours).ToArray();
        }
    }
}
