// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets.Osu.UI.Cursor;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Osu.Tests
{
    /// <summary>
    /// That something other than the mouse can lead the cursor trail.
    /// </summary>
    /// <remarks>
    /// The editor draws autoplay's cursor, and the trail belongs to that rather than to the mapper's pointer, which
    /// is off doing editing work. Both leading it at once would draw two trails.
    /// </remarks>
    [TestFixture]
    public partial class TestSceneCursorTrailLeading : OsuManualInputManagerTestScene
    {
        private static readonly Vector2 led_to = new Vector2(120, 90);

        private TestCursorTrail trail = null!;

        [SetUpSteps]
        public void SetUpSteps()
        {
            AddStep("draw a trail", () => Child = trail = new TestCursorTrail
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(300),
            });

            AddUntilStep("wait for it", () => trail.IsLoaded);
            AddStep("forget what it has seen", () => trail.Moves.Clear());
        }

        [Test]
        public void TestTheMouseLeadsItWhenNothingElseDoes()
        {
            AddStep("move the mouse over it", () => InputManager.MoveMouseTo(trail));

            AddUntilStep("it followed the mouse", () => trail.Moves.Count, () => Is.GreaterThan(0));
        }

        [Test]
        public void TestASourceLeadsItInsteadOfTheMouse()
        {
            AddStep("lead it somewhere of our own", () => trail.ScreenSpacePositionSource = () => trail.ToScreenSpace(led_to));

            AddUntilStep("it went there", () => trail.Moves.Count, () => Is.GreaterThan(0));

            AddStep("forget what it has seen", () => trail.Moves.Clear());
            AddStep("move the mouse somewhere else", () => InputManager.MoveMouseTo(trail.ToScreenSpace(new Vector2(10, 10))));

            AddAssert("every point is the one we led it to",
                () => trail.Moves.TrueForAll(m => Precision.AlmostEquals(trail.ToLocalSpace(m), led_to, 0.5f)));
        }

        [Test]
        public void TestASourceWithNowhereToPointLeadsItNowhere()
        {
            AddStep("lead it by nothing at all", () => trail.ScreenSpacePositionSource = () => null);
            // cleared after the source is set, not before: the frames in between still belong to the mouse.
            AddStep("forget what it has seen", () => trail.Moves.Clear());
            AddWaitStep("let it run", 5);
            AddStep("move the mouse over it", () => InputManager.MoveMouseTo(trail));
            AddWaitStep("let it run", 5);

            AddAssert("it went nowhere", () => trail.Moves, () => Is.Empty);
        }

        private partial class TestCursorTrail : CursorTrail
        {
            public readonly List<Vector2> Moves = new List<Vector2>();

            protected override void CursorMovedTo(Vector2 screenSpacePosition)
            {
                Moves.Add(screenSpacePosition);
                base.CursorMovedTo(screenSpacePosition);
            }
        }
    }
}
