// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Testing;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Osu;
using osu.Game.Screens.Edit.Collab;

namespace osu.Game.Tests.Visual.Editing
{
    /// <summary>
    /// The collab panel's rows are drawables, and a change to the beatmap reaches the panel once per mouse movement while
    /// an object is being dragged. Making them all again every time froze the editor for tens of seconds on a real
    /// beatmap (2026-09-11: 2563 rebuilds at 13.5ms each, with the panel closed).
    /// </summary>
    public partial class TestSceneCollabOverlayRebuild : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new OsuRuleset();

        private CollabOverlay collab => Editor.ChildrenOfType<CollabOverlay>().Single();

        private CollabBlockRow[] rows() => collab.ChildrenOfType<CollabBlockRow>().ToArray();

        [SetUpSteps]
        public void SetUpCollab()
        {
            AddStep("cut the beatmap in three", () =>
            {
                double[] times = EditorBeatmap.HitObjects.Select(h => h.StartTime).ToArray();

                // the stretches are between bookmarks, so the beatmap's two ends are marked as well.
                EditorBeatmap.Bookmarks.AddRange(new[] { (int)times[0], (int)times[times.Length / 3], (int)times[2 * times.Length / 3], (int)times[^1] });
            });
        }

        /// <summary>
        /// The total of what nobody has yet is named in the game's language: it was written out as the English text
        /// before it reached the screen, so it said "unassigned" in Japanese as well (2026-10-18).
        /// </summary>
        [Test]
        public void TestUnassignedIsTranslated()
        {
            AddStep("open the panel", () => collab.Show());
            AddUntilStep("unassigned is named by its translation",
                () => collab.ChildrenOfType<CollabTotal>().SelectMany(t => t.ChildrenOfType<SpriteText>()).Any(t => t.Text.Equals(CollabStrings.Unassigned)));
        }

        [Test]
        public void TestThereIsARowForEachStretchWhenOpened()
        {
            AddStep("open the panel", () => collab.Show());
            AddUntilStep("a row for each stretch", () => rows().Length, () => Is.EqualTo(3));
        }

        /// <summary>
        /// Nobody sees the rows while the panel is closed, so an edit made then has no reason to make them again.
        /// </summary>
        [Test]
        public void TestAnEditWhileClosedLeavesTheRowsAlone()
        {
            CollabBlockRow[] before = null!;

            AddStep("open the panel", () => collab.Show());
            AddUntilStep("the rows are made", () => rows().Length, () => Is.EqualTo(3));
            AddStep("close it", () => collab.Hide());
            AddStep("remember the rows", () => before = rows());

            AddStep("edit an object", nudge);
            AddWaitStep("let the edit be processed", 3);

            AddAssert("the rows were not made again", rows, () => Is.EqualTo(before));
        }

        /// <summary>
        /// And opening it afterwards brings them up to date with whatever changed in the meantime.
        /// </summary>
        [Test]
        public void TestOpeningCatchesUpWithWhatChangedWhileClosed()
        {
            AddStep("open and close the panel", () =>
            {
                collab.Show();
                collab.Hide();
            });
            AddStep("cut once more while closed", () =>
            {
                double[] times = EditorBeatmap.HitObjects.Select(h => h.StartTime).ToArray();
                EditorBeatmap.Bookmarks.Add((int)times[times.Length / 6]);
            });

            AddStep("open the panel", () => collab.Show());
            AddUntilStep("a row for each of the four stretches", () => rows().Length, () => Is.EqualTo(4));
        }

        /// <summary>
        /// While open, a burst of edits — which is what a drag sends — makes the rows once afterwards rather than once per
        /// edit while they are arriving.
        /// </summary>
        [Test]
        public void TestABurstOfEditsWhileOpenMakesTheRowsOnceAfterwards()
        {
            CollabBlockRow[] before = null!;
            bool sameStraightAfter = false;

            AddStep("open the panel", () => collab.Show());
            AddUntilStep("the rows are made", () => rows().Length, () => Is.EqualTo(3));

            AddStep("twenty edits in one go, one change each, the way a drag sends them", () =>
            {
                before = rows();

                for (int i = 0; i < 20; i++)
                {
                    EditorBeatmap.BeginChange();
                    nudge();
                    EditorBeatmap.EndChange();
                }

                sameStraightAfter = rows().SequenceEqual(before);
            });

            AddAssert("none were made while the edits arrived", () => sameStraightAfter);
            AddUntilStep("they are made afterwards", () => !rows().SequenceEqual(before));
        }

        private void nudge()
        {
            var hitObject = EditorBeatmap.HitObjects[1];
            hitObject.StartTime += 1;
            EditorBeatmap.Update(hitObject);
        }
    }
}
