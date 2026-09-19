// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Moving a whole selection in time, as dragging it along the timeline does on every frame.
    /// </summary>
    /// <remarks>
    /// Everything that follows a move — putting the objects back in start time order, reconnecting the follow points,
    /// and measuring the selection for the scale handles — used to be done once per object, and each of those searched
    /// or measured the whole selection. Dragging a whole beatmap of 1800 objects along the timeline took over a second
    /// a frame. They are done once for however many objects moved now, so the cost below is a multiple of the objects
    /// rather than of their square.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneMovingLargeSelectionInTime : TestSceneOsuEditor
    {
        private const int object_count = 1000;

        /// <summary>
        /// Generous next to the tens of milliseconds this takes, and far under the seconds it took per frame before.
        /// </summary>
        private const double budget_ms = 500;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset)
        {
            var beatmap = new TestBeatmap(ruleset, false);
            var objects = new List<HitObject>();

            for (int i = 0; i < object_count; i++)
                objects.Add(new HitCircle { StartTime = 1000 + i * 200, Position = new Vector2(40 + i * 37 % 432, 40 + i * 53 % 304) });

            beatmap.HitObjects = objects;
            return beatmap;
        }

        [Test]
        public void TestMovingEverythingInTimeCostsLittle()
        {
            double elapsed = 0;

            AddStep("select everything", () => EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects));
            AddUntilStep("everything is selected", () => EditorBeatmap.SelectedHitObjects.Count, () => Is.EqualTo(object_count));

            AddStep("move the selection in time", () =>
            {
                var stopwatch = Stopwatch.StartNew();

                EditorBeatmap.PerformOnSelection(h => h.StartTime += 50);

                elapsed = stopwatch.Elapsed.TotalMilliseconds;
            });

            AddAssert("everything moved", () => EditorBeatmap.HitObjects.First().StartTime, () => Is.EqualTo(1050));
            AddAssert("the objects are still in time order",
                () => EditorBeatmap.HitObjects.Zip(EditorBeatmap.HitObjects.Skip(1)).All(pair => pair.First.StartTime <= pair.Second.StartTime));
            AddAssert($"it took under {budget_ms}ms", () => elapsed, () => Is.LessThan(budget_ms));
        }
    }
}
