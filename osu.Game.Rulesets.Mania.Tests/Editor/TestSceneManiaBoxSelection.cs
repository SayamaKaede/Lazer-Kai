// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Mania.Tests.Editor
{
    /// <summary>
    /// A box dragged over the lanes while the wheel seeks on, which is how a long stretch of notes is selected: the box
    /// goes on with the notes, and what it has selected leaves the screen behind it (2026-10-13).
    /// </summary>
    public partial class TestSceneManiaBoxSelection : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new ManiaRuleset();

        /// <summary>
        /// What the box has selected stays selected once it is no longer drawn. A note that had just stopped being drawn
        /// was asked where it was through what had drawn it, which by then was nowhere: it was found outside the box and
        /// taken out of the selection, so that of a long stretch only the end of it was copied.
        /// </summary>
        [Test]
        public void TestWhatLeavesTheScreenStaysSelectedByABoxStillDragged()
        {
            HitObject[] selectedAtFirst = null!;

            AddStep("put notes on every half beat, in all but the outer lanes", () =>
            {
                var timing = EditorBeatmap.ControlPointInfo.TimingPoints.First();
                int lanes = this.ChildrenOfType<Column>().Count();

                EditorBeatmap.Clear();
                EditorBeatmap.BeginChange();

                for (int i = 0; i < 300; i++)
                    EditorBeatmap.Add(new Note { Column = 1 + i % (lanes - 2), StartTime = timing.Time + i * timing.BeatLength / 2 });

                EditorBeatmap.EndChange();
            });

            AddStep("seek to a little way in", () => EditorClock.Seek(EditorBeatmap.HitObjects[20].StartTime));
            AddStep("take the selection tool", () => InputManager.Key(Key.Number1));

            AddStep("press in the last lane", () =>
            {
                var lane = this.ChildrenOfType<Column>().OrderBy(c => c.Index).Last().ScreenSpaceDrawQuad;

                InputManager.MoveMouseTo(new Vector2(lane.Centre.X, lane.TopLeft.Y + lane.Height * 0.7f));
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("drag to the first lane, further up", () =>
            {
                var lane = this.ChildrenOfType<Column>().OrderBy(c => c.Index).First().ScreenSpaceDrawQuad;

                InputManager.MoveMouseTo(new Vector2(lane.Centre.X, lane.TopLeft.Y + lane.Height * 0.3f));
            });

            AddWaitStep("let the box take what is in it", 3);
            AddStep("note what it took", () => selectedAtFirst = EditorBeatmap.SelectedHitObjects.ToArray());
            AddAssert("some notes", () => selectedAtFirst, () => Is.Not.Empty);

            for (int i = 0; i < 60; i++)
            {
                AddStep("turn the wheel fast", () =>
                {
                    for (int tick = 0; tick < 6; tick++)
                        InputManager.ScrollVerticalBy(-1);
                });
                AddWaitStep("a frame or two", 2);
            }

            AddUntilStep("seeking is over", () => !EditorClock.IsSeeking);
            AddWaitStep("a few frames", 5);

            AddAssert("what it took at first is no longer drawn", () => composer.HitObjects.Select(d => d.HitObject).Intersect(selectedAtFirst), () => Is.Empty);
            AddAssert("and is selected still", () => selectedAtFirst.Except(EditorBeatmap.SelectedHitObjects), () => Is.Empty);

            AddStep("let go", () => InputManager.ReleaseButton(MouseButton.Left));
            AddWaitStep("a few frames", 5);

            AddAssert("still, with the box gone", () => selectedAtFirst.Except(EditorBeatmap.SelectedHitObjects), () => Is.Empty);
            AddAssert("no note of the stretch selected is left out", () =>
            {
                double first = EditorBeatmap.SelectedHitObjects.Min(h => h.StartTime);
                double last = EditorBeatmap.SelectedHitObjects.Max(h => h.StartTime);

                return EditorBeatmap.HitObjects.Where(h => h.StartTime >= first && h.StartTime <= last).Except(EditorBeatmap.SelectedHitObjects);
            }, () => Is.Empty);
        }

        private HitObjectComposer composer => Editor.ChildrenOfType<HitObjectComposer>().Single();
    }
}
