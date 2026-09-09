// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Osu.Beatmaps;
using osu.Game.Rulesets.Osu.Edit;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Screens.Edit;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// That the cursor the editor draws is the size the cursor would actually be played at.
    /// </summary>
    /// <remarks>
    /// Built on its own rather than inside an editor: the question is only what the cursor reads its size from, and
    /// a whole editor around it would answer it no better.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneEditorAutoCursor : OsuTestScene
    {
        private const float default_circle_size = 5;

        [Cached]
        private readonly EditorBeatmap editorBeatmap = new EditorBeatmap(new OsuBeatmap
        {
            BeatmapInfo = new BeatmapInfo
            {
                Ruleset = new OsuRuleset().RulesetInfo,
                Difficulty = new BeatmapDifficulty { CircleSize = default_circle_size },
            },
        });

        private EditorAutoCursor cursor = null!;

        private float scale => cursor.CursorScale.Value;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("reset the difficulty", () => editorBeatmap.Difficulty.CircleSize = default_circle_size);
            AddStep("draw a cursor", () => Child = cursor = new EditorAutoCursor
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
            });
            AddUntilStep("wait for it", () => cursor.IsLoaded);
        }

        [TearDownSteps]
        public void TearDownSteps()
        {
            AddStep("put the settings back", () =>
            {
                var config = Dependencies.Get<OsuConfigManager>();

                config.SetValue(OsuSetting.GameplayCursorSize, 1f);
                config.SetValue(OsuSetting.AutoCursorSize, false);
            });
        }

        [Test]
        public void TestItFollowsTheCursorSizeSetting()
        {
            setSize(1.4f);
            AddAssert("the cursor grew with it", () => scale, () => Is.EqualTo(1.4f).Within(0.001f));

            setSize(0.6f);
            AddAssert("and shrank with it", () => scale, () => Is.EqualTo(0.6f).Within(0.001f));
        }

        [Test]
        public void TestFollowingCircleSizeIsOffUntilItIsAskedFor()
        {
            setSize(1);
            setFollowsCircleSize(false);
            AddAssert("circle size is not in it", () => scale, () => Is.EqualTo(1).Within(0.001f));

            setFollowsCircleSize(true);
            AddAssert("now it is", () => scale, () => Is.EqualTo(OsuCursor.GetScaleForCircleSize(default_circle_size)).Within(0.001f));
        }

        /// <remarks>
        /// A beatmap in play cannot change its circle size partway through. The one being edited can, and the
        /// cursor has to follow it there -- which is the half nothing else was going to notice.
        /// </remarks>
        [Test]
        public void TestItFollowsTheCircleSizeChanging()
        {
            setSize(1);
            setFollowsCircleSize(true);

            AddStep("make the circles bigger", () => editorBeatmap.Difficulty.CircleSize = 2);
            AddUntilStep("the cursor grew too", () => scale, () => Is.EqualTo(OsuCursor.GetScaleForCircleSize(2)).Within(0.001f));

            AddStep("make them smaller", () => editorBeatmap.Difficulty.CircleSize = 7);
            AddUntilStep("the cursor shrank too", () => scale, () => Is.EqualTo(OsuCursor.GetScaleForCircleSize(7)).Within(0.001f));

            AddAssert("and the two differ, so this proved something",
                () => Precision.AlmostEquals(OsuCursor.GetScaleForCircleSize(2), OsuCursor.GetScaleForCircleSize(7)), () => Is.False);
        }

        private void setSize(float size) =>
            AddStep($"cursor size {size}", () => Dependencies.Get<OsuConfigManager>().SetValue(OsuSetting.GameplayCursorSize, size));

        private void setFollowsCircleSize(bool follows) =>
            AddStep($"follow circle size: {follows}", () => Dependencies.Get<OsuConfigManager>().SetValue(OsuSetting.AutoCursorSize, follows));
    }
}
