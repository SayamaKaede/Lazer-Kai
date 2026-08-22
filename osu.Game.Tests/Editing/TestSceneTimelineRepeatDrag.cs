// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using static osu.Game.Screens.Edit.Compose.Components.Timeline.TimelineHitObjectBlueprint;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class TestSceneTimelineRepeatDrag
    {
        private const double repeat_length = 1000;

        /// <summary>
        /// Dragging the end of a slider in the timeline snaps to the beat, so the halfway point between one repeat and
        /// the next is a position the user lands on exactly. It has to be the threshold it claims to be.
        /// </summary>
        [Test]
        public void TestThresholdIsWhereItSaysItIs()
        {
            // one pass, dragged to exactly half of a second pass.
            Assert.That(DragArea.RepeatCountFor(repeat_length * 1.5, repeat_length, 0.5f), Is.EqualTo(1));

            // just short of it.
            Assert.That(DragArea.RepeatCountFor(repeat_length * 1.49, repeat_length, 0.5f), Is.EqualTo(0));

            // three quarters of the way, which is the next beat along at 1/4 snapping.
            Assert.That(DragArea.RepeatCountFor(repeat_length * 1.75, repeat_length, 0.8f), Is.EqualTo(0));
            Assert.That(DragArea.RepeatCountFor(repeat_length * 1.8, repeat_length, 0.8f), Is.EqualTo(1));
        }

        [Test]
        public void TestNotDraggedLeavesCountAlone()
        {
            Assert.That(DragArea.RepeatCountFor(repeat_length, repeat_length, 0.5f), Is.EqualTo(0));
            Assert.That(DragArea.RepeatCountFor(repeat_length * 3, repeat_length, 0.5f), Is.EqualTo(2));
        }

        [Test]
        public void TestDraggedBackToNothing()
        {
            Assert.That(DragArea.RepeatCountFor(repeat_length * 0.4, repeat_length, 0.5f), Is.EqualTo(0));
            Assert.That(DragArea.RepeatCountFor(-repeat_length, repeat_length, 0.5f), Is.EqualTo(0));
        }
    }
}
