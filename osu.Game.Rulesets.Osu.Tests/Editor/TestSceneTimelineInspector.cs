// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The strip above the beat divisor, with nothing selected. It says what the inspector would about the object
    /// the playhead is on, so the numbers are there while playing through without having to click anything.
    /// </summary>
    /// <remarks>
    /// The object shown is the last one to have started, and it stays until the next one starts: between two
    /// objects there is nothing at the playhead, and a strip that went blank there flickered the whole way through.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneTimelineInspector : TestSceneOsuEditor
    {
        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(Ruleset.Value, false);

        [SetUpSteps]
        public void ShowTheStrip()
        {
            AddStep("show the inspector above the divisor", () => config.SetValue(OsuSetting.EditorTimelineInspector, true));
        }

        [TearDownSteps]
        public void PutTheSettingBack()
        {
            AddStep("back to the stock answer", () => config.SetValue(OsuSetting.EditorTimelineInspector, false));
        }

        [Test]
        public void TestTheObjectAtThePlayheadIsShown()
        {
            AddStep("add three circles", () =>
            {
                EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = 2000, Position = new Vector2(200, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(300, 100) });
                EditorBeatmap.SelectedHitObjects.Clear();
            });

            AddStep("seek onto the first", () => EditorClock.Seek(1000));
            AddUntilStep("the first is shown", () => stripText(), () => Does.Contain("1,000").And.Contain("(100, 100)"));

            // halfway to the next: nothing is at the playhead, and the first stays rather than the strip going blank.
            AddStep("seek between the first and the second", () => EditorClock.Seek(1500));
            AddUntilStep("the first is still shown", () => stripText(), () => Does.Contain("1,000").And.Contain("(100, 100)"));

            AddStep("seek onto the second", () => EditorClock.Seek(2000));
            AddUntilStep("the second is shown", () => stripText(), () => Does.Contain("2,000").And.Contain("(200, 100)"));
        }

        [Test]
        public void TestASelectionComesFirst()
        {
            AddStep("add three circles, and select the last", () =>
            {
                EditorBeatmap.Add(new HitCircle { StartTime = 1000, Position = new Vector2(100, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = 2000, Position = new Vector2(200, 100) });
                EditorBeatmap.Add(new HitCircle { StartTime = 3000, Position = new Vector2(300, 100) });
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.Last());
            });

            AddStep("seek onto the first", () => EditorClock.Seek(1000));
            AddUntilStep("the selection is shown", () => stripText(), () => Does.Contain("3,000").And.Contain("(300, 100)"));
        }

        private string stripText()
        {
            var strip = Editor.ChildrenOfType<EditorInspector>().Single(i => i.Compact);

            // the text is laid out a word to a sprite, each carrying its own space.
            string text = string.Concat(strip.ChildrenOfType<SpriteText>().Select(s => s.Text.ToString()));
            return Regex.Replace(text, @"\s+", " ");
        }
    }
}
