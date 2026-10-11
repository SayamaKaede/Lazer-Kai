// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Testing;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Objects;
using osu.Game.Rulesets.Mania.Objects.Drawables;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI.Scrolling;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Mania.Tests.Editor
{
    /// <summary>
    /// The osu!mania editor as stable has it: no keys under the judgement line, the lanes going on there with the notes
    /// that have gone by, notes placed there as anywhere else, and the ticks drawn the whole way (2026-10-12).
    /// </summary>
    public partial class TestSceneManiaEditorBelowTheLine : EditorTestScene
    {
        protected override Ruleset CreateEditorRuleset() => new ManiaRuleset();

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        private Column column => this.ChildrenOfType<Column>().First();

        public override void SetUpSteps()
        {
            base.SetUpSteps();

            AddStep("as it comes", () =>
            {
                config.GetBindable<bool>(OsuSetting.EditorManiaHideKeys).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorManiaTickThickness).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorManiaTickOpacity).SetDefault();
                config.SetValue(OsuSetting.EditorAutoSeekOnPlacement, false);
            });
            AddStep("empty", () => EditorBeatmap.Clear());
            AddStep("seek to a tick near 5000", () => EditorClock.Seek(playhead = EditorBeatmap.SnapTime(5000, null)));
            AddUntilStep("arrived", () => EditorClock.CurrentTime, () => Is.EqualTo(playhead).Within(1));
        }

        /// <summary>
        /// Where the editor is as a test starts: on a tick, which is where a note at the playhead is put.
        /// </summary>
        private double playhead;

        [Test]
        public void TestTheKeysAreHiddenUnlessAskedFor()
        {
            AddAssert("no keys", () => this.ChildrenOfType<Column>().Select(c => c.KeyArea.Alpha), () => Is.All.EqualTo(0));

            AddStep("ask for them", () => config.SetValue(OsuSetting.EditorManiaHideKeys, false));
            AddAssert("keys", () => this.ChildrenOfType<Column>().Select(c => c.KeyArea.Alpha), () => Is.All.EqualTo(1));
        }

        /// <summary>
        /// A note goes where the cursor is, whatever osu!'s setting for placing at the playhead says.
        /// </summary>
        /// <remarks>
        /// A mapper of osu! has that setting on. Lazer has osu!mania go by it too, where it puts every note on the
        /// judgement line: reported as notes that could be placed nowhere else.
        /// </remarks>
        [TestCase(true)]
        [TestCase(false)]
        public void TestANoteGoesWhereTheCursorIs(bool osuPlacesAtThePlayhead)
        {
            AddStep($"osu! places at the playhead: {osuPlacesAtThePlayhead}", () => config.SetValue(OsuSetting.EditorLimitedDistanceSnap, osuPlacesAtThePlayhead));
            AddStep("take the note tool", () => InputManager.Key(Key.Number2));

            AddStep("move below the judgement line", () => InputManager.MoveMouseTo(belowTheLine()));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("a note is placed before the playhead", () => EditorBeatmap.HitObjects.Select(h => h.StartTime), () => Has.Exactly(1).LessThan(playhead - 1));

            AddStep("move well above it", () => InputManager.MoveMouseTo(new Vector2(column.ScreenSpaceDrawQuad.Centre.X, lineY() - 200)));
            AddStep("click", () => InputManager.Click(MouseButton.Left));
            AddAssert("and one after it", () => EditorBeatmap.HitObjects.Select(h => h.StartTime), () => Has.Exactly(1).GreaterThan(playhead + 1));

            AddStep("setting back", () => config.GetBindable<bool>(OsuSetting.EditorLimitedDistanceSnap).SetDefault());
        }

        [Test]
        public void TestANoteThatHasGoneByIsStillShown()
        {
            Note note = null!;

            AddStep("a note a little before the playhead", () => EditorBeatmap.Add(note = new Note { Column = 0, StartTime = timeBelowTheLine() }));

            AddUntilStep("it is shown", () => drawableOf(note)?.Alpha, () => Is.EqualTo(1));
            AddAssert("below the judgement line", () => drawableOf(note)!.ScreenSpaceDrawQuad.Centre.Y, () => Is.GreaterThan(lineY()));

            // played up to, rather than sought past: it is hit as it reaches the line, and then goes on down.
            AddStep("seek to before it", () => EditorClock.Seek(note.StartTime - 500));
            AddUntilStep("arrived", () => EditorClock.CurrentTime, () => Is.EqualTo(note.StartTime - 500).Within(1));
            AddStep("play", () => EditorClock.Start());
            AddUntilStep("it has gone by", () => EditorClock.CurrentTime, () => Is.GreaterThan(note.StartTime + 20));
            AddStep("stop", () => EditorClock.Stop());
            AddStep("seek to where it is just below the line", () => EditorClock.Seek(playhead));
            AddUntilStep("arrived", () => EditorClock.CurrentTime, () => Is.EqualTo(playhead).Within(1));
            AddUntilStep("still shown", () => drawableOf(note)?.Alpha, () => Is.EqualTo(1));
        }

        [Test]
        public void TestTheTicksAreShownWithNothingSelected()
        {
            AddAssert("nothing is selected", () => EditorBeatmap.SelectedHitObjects, () => Is.Empty);
            AddStep("move off the playfield", () => InputManager.MoveMouseTo(Editor.ScreenSpaceDrawQuad.TopLeft + new Vector2(5)));

            AddUntilStep("ticks are drawn in the lane", () => ticks().Count(), () => Is.GreaterThan(4));
            AddAssert("none of them fainter than the rest", () => ticks().Select(t => t.Alpha), () => Is.All.EqualTo(0.5f));
            AddAssert("above the line and below it", () => (ticks().Any(t => t.ScreenSpaceDrawQuad.Centre.Y < lineY() - 50), ticks().Any(t => t.ScreenSpaceDrawQuad.Centre.Y > lineY() + 5)),
                () => Is.EqualTo((true, true)));
        }

        [Test]
        public void TestTheTicksAreAsThickAndAsOpaqueAsAskedFor()
        {
            AddUntilStep("ticks are drawn", () => ticks().Count(), () => Is.GreaterThan(4));
            AddAssert("a pixel thick as they come", () => ticks().Select(t => t.DrawHeight), () => Is.All.EqualTo(1));
            AddAssert("at half", () => ticks().Select(t => t.Alpha), () => Is.All.EqualTo(0.5f));

            AddStep("ask for four pixels at a quarter", () =>
            {
                config.SetValue(OsuSetting.EditorManiaTickThickness, 4f);
                config.SetValue(OsuSetting.EditorManiaTickOpacity, 0.25f);
            });

            AddUntilStep("four pixels thick", () => ticks().Length > 4 && ticks().All(t => t.DrawHeight == 4));
            AddAssert("at a quarter", () => ticks().Select(t => t.Alpha), () => Is.All.EqualTo(0.25f));

            AddStep("settings back", () =>
            {
                config.GetBindable<float>(OsuSetting.EditorManiaTickThickness).SetDefault();
                config.GetBindable<float>(OsuSetting.EditorManiaTickOpacity).SetDefault();
            });
        }

        /// <summary>
        /// The ticks of the stage that are on screen. They are drawn where its bar lines are.
        /// </summary>
        private DrawableHitObject[] ticks()
        {
            var stage = this.ChildrenOfType<Stage>().First();

            return stage.HitObjectContainer.Parent!.ChildrenOfType<ScrollingHitObjectContainer>().Single(c => c != stage.HitObjectContainer).AliveObjects
                        .Where(t => stage.ScreenSpaceDrawQuad.AABBFloat.Contains(t.ScreenSpaceDrawQuad.Centre)).ToArray();
        }

        private DrawableNote? drawableOf(Note note) => this.ChildrenOfType<DrawableNote>().SingleOrDefault(d => d.HitObject == note && d.IsAlive);

        /// <summary>
        /// Where the judgement line is on screen.
        /// </summary>
        private float lineY() => column.HitObjectContainer.ScreenSpaceDrawQuad.BottomLeft.Y;

        /// <summary>
        /// A point of the first lane halfway between the judgement line and the bottom.
        /// </summary>
        private Vector2 belowTheLine() => new Vector2(column.ScreenSpaceDrawQuad.Centre.X, (lineY() + column.ScreenSpaceDrawQuad.BottomLeft.Y) / 2);

        /// <summary>
        /// The time that is drawn there.
        /// </summary>
        private double timeBelowTheLine() => column.TimeAtScreenSpacePosition(belowTheLine());
    }
}
