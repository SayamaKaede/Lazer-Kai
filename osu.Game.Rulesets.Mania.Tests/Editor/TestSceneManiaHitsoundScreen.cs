// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Localisation;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Components.TernaryButtons;
using osu.Game.Screens.Edit.Hitsounds;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Mania.Tests.Editor
{
    /// <summary>
    /// The hitsound screen of an osu!mania difficulty that was not made to hitsound in, such as one hitsounded in stable:
    /// the same buttons as an osu! difficulty has, but for the ones that put a circle in.
    /// </summary>
    public partial class TestSceneManiaHitsoundScreen : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new ManiaRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new ManiaBeatmap(new StageDefinition(4))
        {
            BeatmapInfo = { Ruleset = ruleset },
        };

        /// <summary>
        /// The buttons that put a circle in are not shown where there is no circle to put in, and are not to be pressed
        /// there either. They could be: pressed where a note sounded as a file, one gave it sounds the difficulty could
        /// not be written with, and the game went down (DOTNET-G, 2026-10-13).
        /// </summary>
        [Test]
        public void TestTheButtonsThatPutACircleInCannotBePressed()
        {
            Note note = null!;

            AddStep("a note that sounds as a file, as one hitsounded in stable does", () => EditorBeatmap.Add(note = new Note
            {
                Column = 0,
                StartTime = 0,
                Samples = { new ConvertHitObjectParser.FileHitSampleInfo(@"soft-hitwhistle", 100) },
            }));

            AddStep("switch to the hitsound screen", () => Editor.Mode.Value = EditorScreenMode.Hitsounds);
            AddUntilStep("its buttons are up", () => placingButtons().Length, () => Is.EqualTo(2));
            AddStep("go to the note", () => EditorClock.Seek(0));

            AddAssert("the buttons are not shown", () => placingButtons().Select(b => b.Parent!.Parent!.Alpha), () => Is.All.EqualTo(0));
            AddAssert("nor to be pressed", () => placingButtons().Select(b => b.Enabled.Value), () => Is.All.False);

            AddStep("press them anyway", () =>
            {
                foreach (var button in placingButtons())
                    button.TriggerClick();
            });

            AddAssert("the note sounds as it did", () => note.Samples.Select(s => s.Name), () => Is.EqualTo(new[] { HitSampleInfo.HIT_NORMAL }));
        }

        private DrawableTernaryButton[] placingButtons() =>
            Editor.ChildrenOfType<HitsoundPanel>().SingleOrDefault()?.ChildrenOfType<DrawableTernaryButton>()
                  .Where(b => b.Description.Equals(LazerKaiHitsoundStrings.PlaceCircle) || b.Description.Equals(LazerKaiHitsoundStrings.PlaceCircleWithPrevious))
                  .ToArray() ?? System.Array.Empty<DrawableTernaryButton>();
    }
}
