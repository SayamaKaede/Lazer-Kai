// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// The cut-off on how long an object stays selectable is about what a drag can pick up. Something the user has
    /// already got hold of must stay held, however far the playhead has moved on since.
    /// </summary>
    public partial class TestSceneSelectionSurvivesSeekingAway : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        [Test]
        public void TestSelectedObjectStaysGrabbableAfterSeekingPastIt()
        {
            AddStep("set a short window", () => config.SetValue(OsuSetting.EditorSelectableAfterEndTime, 100.0));

            AddStep("select the first object", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.Add(EditorBeatmap.HitObjects.First());
            });
            AddUntilStep("it is grabbable", () => selectable(), () => Is.True);

            AddStep("seek well past it", () => EditorClock.Seek(EditorBeatmap.HitObjects.First().GetEndTime() + 5000));
            AddUntilStep("still selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(1));
            AddAssert("still grabbable", () => selectable(), () => Is.True);
        }

        [Test]
        public void TestUnselectedObjectStopsBeingGrabbable()
        {
            AddStep("set a short window", () => config.SetValue(OsuSetting.EditorSelectableAfterEndTime, 100.0));
            AddStep("select nothing", () => EditorBeatmap.SelectedHitObjects.Clear());

            AddStep("sit on the first object", () => EditorClock.Seek(EditorBeatmap.HitObjects.First().StartTime));
            AddUntilStep("grabbable while it is there", () => selectable(), () => Is.True);

            AddStep("seek well past it", () => EditorClock.Seek(EditorBeatmap.HitObjects.First().GetEndTime() + 5000));
            AddUntilStep("no longer grabbable", () => selectable(), () => Is.False);
        }

        /// <summary>
        /// An object with no blueprint at all is not grabbable either, so a missing one counts as false.
        /// </summary>
        private bool selectable()
        {
            var first = EditorBeatmap.HitObjects.First();

            return Editor.ChildrenOfType<HitObjectSelectionBlueprint>()
                         .SingleOrDefault(b => ReferenceEquals(b.Item, first))?.IsSelectable == true;
        }
    }
}
