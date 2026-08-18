// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.UI;
using osu.Game.Tests.Beatmaps;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Osu.Tests.Editor
{
    /// <summary>
    /// Turning the selection with ctrl+shift+wheel. Ctrl on its own is taken by the beat snap divisor.
    /// </summary>
    public partial class TestSceneRotateSelectionByScroll : TestSceneOsuEditor
    {
        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => new TestBeatmap(ruleset);

        private Vector2[] positionsBefore = null!;

        [Test]
        public void TestSelectionTurnsOneStepPerNotch()
        {
            selectFirstThreeObjects();

            scroll(1, control: true, shift: true);
            AddAssert("selection turned", () => anglesTurnedBy(), () => Is.EqualTo(3).Within(0.01));

            selectFirstThreeObjects();
            scroll(-1, control: true, shift: true);
            AddAssert("selection turned the other way", () => anglesTurnedBy(), () => Is.EqualTo(-3).Within(0.01));
        }

        [Test]
        public void TestAltTakesAFinerStep()
        {
            selectFirstThreeObjects();

            scroll(1, control: true, shift: true, alt: true);
            AddAssert("selection turned by one degree", () => anglesTurnedBy(), () => Is.EqualTo(1).Within(0.01));
        }

        [Test]
        public void TestControlAloneLeavesTheSelectionWhereItIs()
        {
            selectFirstThreeObjects();

            scroll(1, control: true);
            AddAssert("selection unmoved", () => currentPositions().SequenceEqual(positionsBefore), () => Is.True);
        }

        /// <summary>
        /// A whole turn made without letting go of the modifiers has to come back in one undo, not three degrees at a time.
        /// </summary>
        [Test]
        public void TestWholeTurnUndoesAtOnce()
        {
            selectFirstThreeObjects();

            AddStep("hold ctrl+shift", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.PressKey(Key.ShiftLeft);
            });

            AddRepeatStep("turn", () => InputManager.ScrollVerticalBy(1), 3);
            AddAssert("turned three steps", () => anglesTurnedBy(), () => Is.EqualTo(9).Within(0.01));

            AddStep("let go", () =>
            {
                InputManager.ReleaseKey(Key.ShiftLeft);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddWaitStep("let the turn close", 2);

            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddUntilStep("back where it started", () => currentPositions().SequenceEqual(positionsBefore), () => Is.True);
        }

        private void selectFirstThreeObjects()
        {
            AddStep("hover the playfield", () => InputManager.MoveMouseTo(Editor.ChildrenOfType<OsuPlayfield>().Single()));
            AddStep("select first three objects", () =>
            {
                EditorBeatmap.SelectedHitObjects.Clear();
                EditorBeatmap.SelectedHitObjects.AddRange(EditorBeatmap.HitObjects.Take(3));
                positionsBefore = currentPositions();
            });
        }

        private void scroll(int delta, bool control = false, bool shift = false, bool alt = false)
            => AddStep($"scroll {delta} with {(control ? "ctrl " : "")}{(shift ? "shift " : "")}{(alt ? "alt" : "")}".Trim(), () =>
            {
                if (control) InputManager.PressKey(Key.ControlLeft);
                if (shift) InputManager.PressKey(Key.ShiftLeft);
                if (alt) InputManager.PressKey(Key.AltLeft);

                InputManager.ScrollVerticalBy(delta);

                if (alt) InputManager.ReleaseKey(Key.AltLeft);
                if (shift) InputManager.ReleaseKey(Key.ShiftLeft);
                if (control) InputManager.ReleaseKey(Key.ControlLeft);
            });

        /// <summary>
        /// Read from the beatmap rather than the selection: an undo restores the beatmap from a saved state, which
        /// replaces the objects and leaves nothing selected.
        /// </summary>
        private Vector2[] currentPositions() => EditorBeatmap.HitObjects.OfType<OsuHitObject>().Take(3).Select(o => o.Position).ToArray();

        /// <summary>
        /// The angle the selection turned through, measured from the object furthest from the selection's centre so
        /// that the reading does not depend on which object is picked.
        /// </summary>
        private double anglesTurnedBy()
        {
            var now = currentPositions();
            Vector2 centreBefore = positionsBefore.Aggregate(Vector2.Zero, (a, b) => a + b) / positionsBefore.Length;
            Vector2 centreNow = now.Aggregate(Vector2.Zero, (a, b) => a + b) / now.Length;

            int furthest = 0;

            for (int i = 1; i < positionsBefore.Length; i++)
            {
                if ((positionsBefore[i] - centreBefore).LengthSquared > (positionsBefore[furthest] - centreBefore).LengthSquared)
                    furthest = i;
            }

            Vector2 before = positionsBefore[furthest] - centreBefore;
            Vector2 after = now[furthest] - centreNow;

            return MathHelper.RadiansToDegrees(System.Math.Atan2(after.Y, after.X) - System.Math.Atan2(before.Y, before.X));
        }
    }
}
