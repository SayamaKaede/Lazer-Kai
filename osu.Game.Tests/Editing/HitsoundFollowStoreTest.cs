// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Extensions;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Which difficulty's hitsounds a difficulty always takes, and which difficulty that is kept for.
    /// </summary>
    [TestFixture]
    public class HitsoundFollowStoreTest
    {
        private static BeatmapInfo difficulty() => new BeatmapInfo
        {
            DifficultyName = "Insane",
            Metadata = new BeatmapMetadata { Artist = "Artist", Title = "Title" },
        };

        /// <summary>
        /// A copy of the set, or another version of it, has the same names but is not the same difficulty.
        /// </summary>
        [Test]
        public void TestAnotherSetWithTheSameNamesDoesNotShareIt()
        {
            using (var storage = new TemporaryNativeStorage(nameof(HitsoundFollowStoreTest)))
            {
                var store = new HitsoundFollowStore(storage);

                store.Save(difficulty(), "Hitsounds");

                Assert.That(store.Load(difficulty()), Is.Null);
            }
        }

        [Test]
        public void TestRenamingTheDifficultyKeepsIt()
        {
            using (var storage = new TemporaryNativeStorage(nameof(HitsoundFollowStoreTest)))
            {
                var store = new HitsoundFollowStore(storage);
                var info = difficulty();

                store.Save(info, "Hitsounds");
                info.DifficultyName = "Extra";

                Assert.That(store.Load(info), Is.EqualTo("Hitsounds"));
            }
        }

        /// <summary>
        /// What was kept under the names before (2026-10-23 to 11-01) is taken over by the first difficulty to look for
        /// it, and not by another with the same names after that.
        /// </summary>
        [Test]
        public void TestWhatWasKeptUnderTheNamesIsTakenOverOnce()
        {
            using (var storage = new TemporaryNativeStorage(nameof(HitsoundFollowStoreTest)))
            {
                var store = new HitsoundFollowStore(storage);
                var info = difficulty();

                string old = Path.Combine(HitsoundFollowStore.DIRECTORY, "Artist - Title [Insane]".GetValidFilename(), "follow.json");

                using (var stream = storage.CreateFileSafely(old))
                using (var writer = new StreamWriter(stream))
                    writer.Write(@"{""Source"":""Hitsounds""}");

                Assert.That(store.Load(info), Is.EqualTo("Hitsounds"));
                Assert.That(store.Load(info), Is.EqualTo("Hitsounds"));
                Assert.That(store.Load(difficulty()), Is.Null);
            }
        }
    }
}
