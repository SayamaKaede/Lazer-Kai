// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Collab;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Dividing a beatmap between collaborators, and saying who took how much.
    /// </summary>
    [TestFixture]
    public class CollabBlocksTest
    {
        /// <summary>
        /// The number each collaborator is given has to add back up to the number the beatmap is judged by, or they
        /// are arguing over a figure neither of them can reproduce.
        /// </summary>
        [Test]
        public void TestTheBlocksAddBackUpToTheBeatmapsDrainTime()
        {
            var beatmap = withBreaks();
            var blocks = CollabBlocks.For(beatmap, new[] { 2_400, 5_000, 8_000 });

            Assert.That(blocks.Sum(b => b.DrainTime),
                Is.EqualTo(ComposeStatusDisplay.DrainTimeFor(beatmap)).Within(0.0001));
        }

        [Test]
        public void TestOneBlockPerBookmarkPlusTheOneBeforeThem()
        {
            var blocks = CollabBlocks.For(withBreaks(), new[] { 2_400, 8_000 });

            Assert.That(blocks, Has.Count.EqualTo(3));
            Assert.That(blocks[0].Key, Is.EqualTo(CollabBlock.HEAD));
            Assert.That(blocks[1].Key, Is.EqualTo(2_400));
            Assert.That(blocks[2].Key, Is.EqualTo(8_000));
        }

        /// <summary>
        /// A bookmark before the beatmap starts or after it ends has no play on the far side of it to hand over.
        /// </summary>
        [Test]
        public void TestBookmarksOutsideTheBeatmapDoNotCut()
        {
            var blocks = CollabBlocks.For(withBreaks(), new[] { -5_000, 500, 20_000 });

            Assert.That(blocks, Has.Count.EqualTo(1));
        }

        [Test]
        public void TestABeatmapWithNoObjectsHasNothingToDivide()
        {
            Assert.That(CollabBlocks.For(new Beatmap(), new[] { 1_000 }), Is.Empty);
        }

        /// <summary>
        /// Every object belongs to exactly one block, counted by where it was hit.
        /// </summary>
        [Test]
        public void TestEveryObjectIsCountedOnce()
        {
            var beatmap = withBreaks();
            var blocks = CollabBlocks.For(beatmap, new[] { 2_400, 8_000 });

            Assert.That(blocks.Sum(b => b.ObjectCount), Is.EqualTo(beatmap.HitObjects.Count));
        }

        /// <summary>
        /// What is written out has to come back the same, or a split shared in a chat is a split lost.
        /// </summary>
        [Test]
        public void TestASplitSurvivesBeingWrittenOutAndReadBack()
        {
            var blocks = CollabBlocks.For(withBreaks(), new[] { 2_400, 8_000 });

            var assignment = new Dictionary<int, string>
            {
                [CollabBlock.HEAD] = "くろ",
                [2_400] = "しろ",
                [8_000] = "くろ",
            };

            string text = CollabClipboard.Write("Artist - Title [test collab]", blocks, assignment);

            Assert.That(CollabClipboard.Read(text, blocks), Is.EqualTo(assignment));
        }

        [Test]
        public void TestTheBeatmapIsNamedOnTheFirstLine()
        {
            var blocks = CollabBlocks.For(withBreaks(), new[] { 2_400 });
            var assignment = new Dictionary<int, string> { [2_400] = "しろ" };

            string[] lines = CollabClipboard.Write("Artist - Title [test collab]", blocks, assignment).Split('\n');

            Assert.That(lines[0], Is.EqualTo("Artist - Title [test collab]"));
            Assert.That(lines[1], Is.EqualTo("00:02:400 - しろ"));
        }

        /// <summary>
        /// A split of a beatmap whose bookmarks have moved since names moments that are no longer the start of
        /// anything. Those lines have nowhere to go rather than somewhere wrong.
        /// </summary>
        [Test]
        public void TestATimestampNamingNoBlockIsLeftOut()
        {
            var blocks = CollabBlocks.For(withBreaks(), new[] { 2_400 });

            var read = CollabClipboard.Read("Artist - Title [test collab]\n0:02:400 - しろ\n0:07:777 - だれか", blocks);

            Assert.That(read, Has.Count.EqualTo(1));
            Assert.That(read[2_400], Is.EqualTo("しろ"));
        }

        [Test]
        public void TestTimestampsAreWrittenTheWayADiscussionWritesThem()
        {
            Assert.That(CollabClipboard.TryReadTimestamp("1:02:345", out int ms), Is.True);
            Assert.That(ms, Is.EqualTo(62_345));

            // minutes keep counting past sixty rather than rolling over.
            Assert.That(CollabClipboard.TryReadTimestamp("75:00:000", out int marathon), Is.True);
            Assert.That(marathon, Is.EqualTo(4_500_000));

            Assert.That(CollabClipboard.TryReadTimestamp("not a time", out _), Is.False);

            // a moment past a minute, written out the same way it is read back.
            Assert.That(CollabClipboard.Timestamp(62_345), Is.EqualTo("01:02:345"));
            Assert.That(CollabClipboard.Timestamp(2_400), Is.EqualTo("00:02:400"));
        }

        /// <summary>
        /// A split is agreed as letters and shared as names, so the letters have to become names without the split
        /// having to be agreed again.
        /// </summary>
        [Test]
        public void TestRenamingACollaboratorCarriesOverWhatTheyHave()
        {
            var project = new CollabProject { Names = { "A", "B" } };

            project.Assignment[2_400] = "A";
            project.Assignment[8_000] = "B";

            Assert.That(project.Rename("A", "くろ"), Is.True);

            Assert.That(project.Names, Is.EqualTo(new[] { "くろ", "B" }));
            Assert.That(project.Assignment[2_400], Is.EqualTo("くろ"));
            Assert.That(project.Assignment[8_000], Is.EqualTo("B"));
        }

        /// <summary>
        /// Two collaborators under one name cannot be told apart, in the totals or in a shared split.
        /// </summary>
        [Test]
        public void TestANameSomebodyElseHasIsRefused()
        {
            var project = new CollabProject { Names = { "A", "B" } };

            project.Assignment[2_400] = "A";

            Assert.That(project.Rename("A", "B"), Is.False);

            Assert.That(project.Names, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(project.Assignment[2_400], Is.EqualTo("A"));
        }

        [Test]
        public void TestANameOfNothingIsRefused()
        {
            var project = new CollabProject { Names = { "A" } };

            Assert.That(project.Rename("A", "   "), Is.False);
            Assert.That(project.Names, Is.EqualTo(new[] { "A" }));
        }

        private static Beatmap withBreaks()
        {
            var beatmap = new Beatmap();

            beatmap.HitObjects.Add(new TestObject { StartTime = 1_000 });
            beatmap.HitObjects.Add(new TestDurationObject { StartTime = 3_000, Duration = 500 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 8_000 });
            beatmap.HitObjects.Add(new TestObject { StartTime = 10_000 });

            beatmap.Breaks.Add(new BreakPeriod(3_700, 7_400));

            return beatmap;
        }

        private class TestObject : HitObject
        {
        }

        private class TestDurationObject : HitObject, IHasDuration
        {
            public double Duration { get; set; }

            public double EndTime => StartTime + Duration;
        }
    }
}
