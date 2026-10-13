// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The objects an editor test edits are gone through by the editor alone, as they are in the game.
    /// </summary>
    /// <remarks>
    /// The editor works out what the difficulty is worth in the background, from the beatmap as saved
    /// (<see cref="EditorDifficultyTracker"/>). In the game that is read from its file, and made into objects of its
    /// own each time. A test's beatmap made of the ruleset's own objects was handed over as it was, objects and all:
    /// the editor's were given their nested objects again from another thread, while the playfield was going through
    /// them as it loaded ("Collection was modified", every test of <see cref="TestSceneComposeStatusSamples"/> at
    /// once, 2026-09-08 and 2026-10-13).
    /// </remarks>
    public partial class TestSceneEditorObjectsStayItsOwn : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        /// <summary>
        /// The objects of the editor that were made ready again by anything but the update thread.
        /// </summary>
        private readonly ConcurrentQueue<string> touched = new ConcurrentQueue<string>();

        private int updateThread;

        private long? before;

        private EditorDifficultyTracker tracker => Editor.ChildrenOfType<EditorDifficultyTracker>().Single();

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => config.GetBindable<ScoringMode>(OsuSetting.ScoreDisplayMode).SetDefault());

            base.TearDownSteps();
        }

        [Test]
        public void TestWorkingOutWhatTheDifficultyIsWorthLeavesThem()
        {
            AddUntilStep("worked out once", () => tracker.MaxScore.Value != null);

            AddStep("watch the objects", () =>
            {
                touched.Clear();
                updateThread = Environment.CurrentManagedThreadId;

                foreach (var hitObject in EditorBeatmap.HitObjects)
                {
                    hitObject.DefaultsApplied += h =>
                    {
                        if (Environment.CurrentManagedThreadId != updateThread)
                            touched.Enqueue($@"{h.GetType().Name} at {h.StartTime}");
                    };
                }
            });

            AddStep("have it worked out again", () =>
            {
                before = tracker.MaxScore.Value;

                // read out another way, it is another number: which is how it is known to have been worked out.
                config.SetValue(OsuSetting.ScoreDisplayMode, config.Get<ScoringMode>(OsuSetting.ScoreDisplayMode) == ScoringMode.Classic ? ScoringMode.Standardised : ScoringMode.Classic);
            });

            AddUntilStep("worked out again", () => tracker.MaxScore.Value != null && tracker.MaxScore.Value != before);
            AddAssert("nothing but the editor went through its objects", () => touched, () => Is.Empty);
        }

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new Beatmap
            {
                BeatmapInfo = { Ruleset = ruleset },
                HitObjects =
                {
                    new HitCircle { StartTime = 500 },
                    new Slider
                    {
                        StartTime = 1000,
                        Path = new SliderPath(PathType.LINEAR, [Vector2.Zero, new Vector2(100, 0)]),
                        RepeatCount = 1,
                    },
                    new Spinner { StartTime = 2500, Duration = 1000 },
                },
            };

            beatmap.ControlPointInfo.Add(0, new TimingControlPoint { BeatLength = 400 });

            // a slider's length comes from its velocity, which is nothing until the objects have been told about the
            // beatmap — and a slider of infinite length gives the test harness a track of infinite length.
            foreach (var hitObject in beatmap.HitObjects)
                hitObject.ApplyDefaults(beatmap.ControlPointInfo, beatmap.Difficulty);

            return beatmap;
        }
    }
}
