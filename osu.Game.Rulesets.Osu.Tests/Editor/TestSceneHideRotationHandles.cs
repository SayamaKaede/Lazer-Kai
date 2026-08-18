// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Screens.Edit.Compose.Components;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    public partial class TestSceneHideRotationHandles : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// The grips on the corners of a selection can be turned off without taking the ability to rotate with them.
        /// </summary>
        [Test]
        public void TestHandlesFollowTheSetting()
        {
            AddStep("show the handles", () => config.SetValue(OsuSetting.EditorHideRotationHandles, false));
            AddStep("select first three objects", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects.Take(3));
            });

            AddUntilStep("a handle on every corner", handleCount, () => Is.EqualTo(4));

            AddStep("hide the handles", () => config.SetValue(OsuSetting.EditorHideRotationHandles, true));
            AddUntilStep("no handles", handleCount, () => Is.Zero);

            AddStep("show them again", () => config.SetValue(OsuSetting.EditorHideRotationHandles, false));
            AddUntilStep("handles back", handleCount, () => Is.EqualTo(4));
        }

        private int handleCount() => Editor.ChildrenOfType<SelectionBoxRotationHandle>().Count();
    }
}
