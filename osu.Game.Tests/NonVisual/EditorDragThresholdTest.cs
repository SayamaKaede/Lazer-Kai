// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Input;

namespace osu.Game.Tests.NonVisual
{
    [TestFixture]
    public class EditorDragThresholdTest
    {
        private const float framework_default = OsuUserInputManager.DEFAULT_DRAG_DISTANCE;
        private const float minimal = OsuUserInputManager.MINIMAL_DRAG_DISTANCE;

        [TestCase(false, false, framework_default)]
        [TestCase(false, true, framework_default, Description = "over an area that would take it, but the user has not asked for it")]
        [TestCase(true, false, framework_default, Description = "asked for, but the pointer is over a button or a menu")]
        [TestCase(true, true, minimal)]
        public void TestThresholdOnlyDropsWhereBothAgree(bool removeDragThreshold, bool overFineDragArea, float expected)
            => Assert.That(OsuUserInputManager.ClickDragDistanceFor(removeDragThreshold, overFineDragArea), Is.EqualTo(expected));
    }
}
