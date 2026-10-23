// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Screens.Edit;
using osu.Game.Screens.Select;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// A file of Lazer-Kai's own that cannot be read is put aside rather than written over (2026-10-23).
    /// </summary>
    /// <remarks>
    /// One that could not be read was taken as empty, and the next save wrote what was saved then in its place: months
    /// of time spent mapping, or every preset, for the one stretch or preset of that moment.
    /// </remarks>
    [TestFixture]
    public class UnreadableStoreFileTest
    {
        private const string unreadable = "{ this is not what was written";

        private TemporaryNativeStorage storage = null!;

        [SetUp]
        public void SetUp() => storage = new TemporaryNativeStorage("unreadable-store-file");

        [TearDown]
        public void TearDown() => storage.Dispose();

        [Test]
        public void TestMappingTimeKeptAsideAndCarriedOn()
        {
            write(MappingTimeStore.FILENAME, unreadable);

            var store = new MappingTimeStore(storage);
            var beatmap = new BeatmapInfo { DifficultyName = "Hard" };
            var now = DateTimeOffset.Now;

            store.Save(new MappingSpan(Guid.NewGuid(), beatmap.ID, Guid.Empty, "a", "b", "Hard", now, now.AddMinutes(1)));

            Assert.That(keptAside(MappingTimeStore.FILENAME), Is.EqualTo(unreadable));
            Assert.That(store.Load(), Has.Count.EqualTo(1));
        }

        [Test]
        public void TestSearchPresetsKeptAsideAndCarriedOn()
        {
            write(SearchPresetStore.FILENAME, unreadable);

            var store = new SearchPresetStore(storage);
            store.Add("mine", "stars>5");

            Assert.That(keptAside(SearchPresetStore.FILENAME), Is.EqualTo(unreadable));
            Assert.That(store.Load().Single().Name, Is.EqualTo("mine"));
        }

        private void write(string filename, string contents) => File.WriteAllText(storage.GetFullPath(filename), contents);

        /// <summary>
        /// What the file put aside next to <paramref name="filename"/> holds, or null if there is none.
        /// </summary>
        private string? keptAside(string filename)
        {
            string? aside = storage.GetFiles(string.Empty, $"{Path.GetFileNameWithoutExtension(filename)}.broken-*").SingleOrDefault();
            return aside == null ? null : File.ReadAllText(storage.GetFullPath(aside));
        }
    }
}
