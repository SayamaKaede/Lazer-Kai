// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Extensions;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Scoring;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Edit.BeatmapFiles;
using osu.Game.Tests.Resources;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// In the game, the objects the editor edits are gone through by the editor alone.
    /// </summary>
    /// <remarks>
    /// What the difficulty is worth is worked out in the background from the very beatmap the editor was opened on,
    /// until the first save: the beatmap manager hands out the one it has. That is no object of the editor's all the
    /// same, since what is read from a file is made into the ruleset's objects anew each time it is asked for
    /// (2026-10-14: the editor's objects were thought to be shared here as they were in <see cref="EditorTestScene"/>,
    /// see <see cref="TestSceneEditorObjectsStayItsOwn"/>).
    /// </remarks>
    public partial class TestSceneEditorObjectsStayItsOwnInGame : EditorSavingTestScene
    {
        protected override Ruleset CreateRuleset() => new OsuRuleset();

        /// <summary>
        /// The objects of the editor that were made ready again by anything but the update thread.
        /// </summary>
        private readonly ConcurrentQueue<string> touched = new ConcurrentQueue<string>();

        private int updateThread;

        private long? before;

        private EditorDifficultyTracker tracker => Editor.ChildrenOfType<EditorDifficultyTracker>().Single();

        public override void SetUpSteps()
        {
            CreateInitialBeatmap = () =>
            {
                var set = BeatmapSetFiles.Read(Game.Realm, Game.BeatmapManager.Import(new ImportTask(TestResources.GetTestBeatmapForImport())).GetResultSafely()!.ID)!;

                return Game.BeatmapManager.GetWorkingBeatmap(set.Beatmaps.First(b => b.Ruleset.ShortName == @"osu"), true);
            };

            base.SetUpSteps();
        }

        public override void TearDownSteps()
        {
            AddStep("setting back to default", () => Game.LocalConfig.GetBindable<ScoringMode>(OsuSetting.ScoreDisplayMode).SetDefault());

            base.TearDownSteps();
        }

        [Test]
        public void TestWorkingOutWhatTheDifficultyIsWorthLeavesThem()
        {
            AddAssert("the difficulty has objects with others inside them", () => EditorBeatmap.HitObjects.Any(h => h.NestedHitObjects.Count > 0));
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
                Game.LocalConfig.SetValue(OsuSetting.ScoreDisplayMode, Game.LocalConfig.Get<ScoringMode>(OsuSetting.ScoreDisplayMode) == ScoringMode.Classic ? ScoringMode.Standardised : ScoringMode.Classic);
            });

            AddUntilStep("worked out again", () => tracker.MaxScore.Value != null && tracker.MaxScore.Value != before);
            AddAssert("nothing but the editor went through its objects", () => touched, () => Is.Empty);
        }

        [Test]
        public void TestTheBeatmapAsSavedIsMadeOfOtherObjects()
        {
            AddAssert("it is the beatmap the editor was opened on", () => Game.BeatmapManager.GetWorkingBeatmap(EditorBeatmap.BeatmapInfo), () => Is.SameAs(Game.Beatmap.Value));

            AddAssert("none of its objects is one of the editor's", () =>
            {
                var editors = new HashSet<HitObject>(EditorBeatmap.HitObjects, ReferenceEqualityComparer.Instance);

                return Game.BeatmapManager.GetWorkingBeatmap(EditorBeatmap.BeatmapInfo)
                           .GetPlayableBeatmap(EditorBeatmap.BeatmapInfo.Ruleset)
                           .HitObjects.Count(editors.Contains);
            }, () => Is.Zero);
        }
    }
}
