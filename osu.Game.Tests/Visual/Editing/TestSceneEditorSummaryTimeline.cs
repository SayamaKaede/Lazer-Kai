// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.Components.Timelines.Summary;
using osu.Game.Screens.Edit.Discussion;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    [TestFixture]
    public partial class TestSceneEditorSummaryTimeline : EditorClockTestScene
    {
        [Cached(typeof(EditorBeatmap))]
        private readonly EditorBeatmap editorBeatmap;

        /// <summary>
        /// Cached by the editor in the real thing: the strip lights the stretch a collaboration panel is pointing at.
        /// </summary>
        [Cached]
        private readonly CollabHighlight collabHighlight = new CollabHighlight();

        /// <summary>
        /// Cached by the editor in the real thing: the strip marks where the discussion panel's threads are.
        /// </summary>
        [Cached]
        private readonly DiscussionMarks discussionMarks = new DiscussionMarks();

        public TestSceneEditorSummaryTimeline()
        {
            var beatmap = CreateBeatmap(new OsuRuleset().RulesetInfo);

            beatmap.ControlPointInfo.Add(100000, new TimingControlPoint { BeatLength = 100 });
            beatmap.ControlPointInfo.Add(50000, new DifficultyControlPoint { SliderVelocity = 2 });
            beatmap.ControlPointInfo.Add(80000, new EffectControlPoint { KiaiMode = true });
            beatmap.ControlPointInfo.Add(110000, new EffectControlPoint { KiaiMode = false });
            beatmap.Bookmarks = new[] { 75000, 125000 };
            beatmap.Breaks.Add(new ManualBreakPeriod(90000, 120000));

            editorBeatmap = new EditorBeatmap(beatmap);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            AddStep("create timeline", () =>
            {
                // required for track
                Beatmap.Value = CreateWorkingBeatmap(editorBeatmap.PlayableBeatmap);

                Add(new SummaryTimeline
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(500, 50)
                });
            });
        }
    }
}
