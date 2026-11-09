// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Compose.Components.Timeline;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The bookmarks, drawn across the top timeline the way the saved green lines are (2026-11-09).
    /// </summary>
    public partial class TestSceneTimelineBookmarks : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private double[] drawnTimes =>
            Editor.ChildrenOfType<TimelineBookmarkDisplay>()
                  .SelectMany(d => d.ChildrenOfType<TimelineBookmarkDisplay.BookmarkPiece>())
                  .Select(p => p.BookmarkTime)
                  .ToArray();

        [TearDownSteps]
        public void PutTheSettingsBack()
        {
            AddStep("bookmarks shown again", () => config.SetValue(OsuSetting.EditorTimelineShowBookmarks, true));
            AddStep("lazer's timeline again", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Lazer));
        }

        [Test]
        public void TestOnFromTheStartAndInTheRecommendedSettings()
        {
            AddAssert("on by default", () => config.GetBindable<bool>(OsuSetting.EditorTimelineShowBookmarks).Default, () => Is.True);
            AddAssert("on in the recommended settings", () => LazerKaiSettingsPreset.RECOMMENDED[OsuSetting.EditorTimelineShowBookmarks], () => Is.EqualTo(@"True"));
        }

        [Test]
        public void TestTheBookmarksAreDrawnWhereTheyStand()
        {
            AddStep("stable's timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddStep("two bookmarks", () => EditorBeatmap.Bookmarks.AddRange(new[] { 1000, 1500 }));
            AddStep("look at them", () => EditorClock.Seek(1200));

            AddUntilStep("both are drawn", () => drawnTimes, () => Is.EquivalentTo(new[] { 1000d, 1500d }));

            AddStep("take one away", () => EditorBeatmap.Bookmarks.Remove(1000));
            AddUntilStep("only the other is drawn", () => drawnTimes, () => Is.EquivalentTo(new[] { 1500d }));

            AddStep("add one", () => EditorBeatmap.Bookmarks.Add(1300));
            AddUntilStep("it is drawn too", () => drawnTimes, () => Is.EquivalentTo(new[] { 1300d, 1500d }));
        }

        [Test]
        public void TestNothingIsDrawnWhileHidden()
        {
            AddStep("stable's timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, EditorTimelineStyle.Stable));
            AddStep("bookmarks hidden", () => config.SetValue(OsuSetting.EditorTimelineShowBookmarks, false));
            AddStep("a bookmark", () => EditorBeatmap.Bookmarks.Add(1000));
            AddStep("look at it", () => EditorClock.Seek(1000));
            AddWaitStep("give it time", 5);

            AddAssert("nothing drawn", () => drawnTimes, () => Is.Empty);

            AddStep("bookmarks shown", () => config.SetValue(OsuSetting.EditorTimelineShowBookmarks, true));
            AddUntilStep("drawn", () => drawnTimes, () => Is.EquivalentTo(new[] { 1000d }));
        }

        /// <summary>
        /// Lazer's own timeline runs its ticks the full height, and a quarter-beat tick is the same blue as a bookmark.
        /// </summary>
        [TestCase(EditorTimelineStyle.Lazer, false)]
        [TestCase(EditorTimelineStyle.Stable, true)]
        [TestCase(EditorTimelineStyle.LazerKai, true)]
        public void TestDrawnOnlyWhereTheTicksStayDown(EditorTimelineStyle style, bool drawn)
        {
            AddStep($"{style} timeline", () => config.SetValue(OsuSetting.EditorTimelineStyle, style));
            AddStep("a bookmark", () => EditorBeatmap.Bookmarks.Add(1000));
            AddStep("look at it", () => EditorClock.Seek(1000));
            AddWaitStep("give it time", 5);

            AddAssert(drawn ? "drawn" : "not drawn", () => drawnTimes.Length, () => Is.EqualTo(drawn ? 1 : 0));
        }
    }
}
