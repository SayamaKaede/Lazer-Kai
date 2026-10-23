// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Screens.Edit;

namespace osu.Game.Tests.Editing
{
    [TestFixture]
    public class EditorAutoBackupTest
    {
        /// <summary>
        /// Backups are written a folder per difficulty. Anything offering to open "this difficulty's backups" has to
        /// arrive at the same folder they were written to, or it opens somewhere empty and reads as nothing was kept.
        /// </summary>
        [Test]
        public void TestDirectoryIsTheDifficultysOwnFolder()
        {
            var info = beatmap("Nanahira", "Fukkura Fuwaama Cake", "Hard");
            info.ID = Guid.Parse("1a2b3c4d-0000-0000-0000-000000000000");

            string directory = EditorAutoBackup.DirectoryFor(info);

            Assert.That(directory, Is.EqualTo(Path.Combine(EditorAutoBackup.BACKUP_DIRECTORY, "Nanahira - Fukkura Fuwaama Cake [Hard] (1a2b3c4d)")));
        }

        /// <summary>
        /// A copy of a beatmap set, or another version of it, has the same names. Sharing a folder, each would count the
        /// other's backups among its own and delete them as the oldest (2026-10-23).
        /// </summary>
        [Test]
        public void TestDifficultiesWithTheSameNamesDoNotShareAFolder()
        {
            string one = EditorAutoBackup.DirectoryFor(beatmap("Nanahira", "Fukkura Fuwaama Cake", "Hard"));
            string copy = EditorAutoBackup.DirectoryFor(beatmap("Nanahira", "Fukkura Fuwaama Cake", "Hard"));

            Assert.That(one, Is.Not.EqualTo(copy));
        }

        /// <summary>
        /// Two difficulties of one beatmap are backed up separately, so restoring one never offers the other's files.
        /// </summary>
        [Test]
        public void TestDifficultiesOfOneBeatmapDoNotShareAFolder()
        {
            string hard = EditorAutoBackup.DirectoryFor(beatmap("Nanahira", "Fukkura Fuwaama Cake", "Hard"));
            string insane = EditorAutoBackup.DirectoryFor(beatmap("Nanahira", "Fukkura Fuwaama Cake", "Fuwafuwa"));

            Assert.That(hard, Is.Not.EqualTo(insane));
        }

        /// <summary>
        /// Metadata is whatever the mapper typed, and a title with a slash in it would otherwise be read as a folder
        /// separator and scatter the backups.
        /// </summary>
        [Test]
        public void TestCharactersAFolderNameCannotHoldAreRemoved()
        {
            string directory = EditorAutoBackup.DirectoryFor(beatmap("A/B", "C:D", "E?F"));

            Assert.That(Path.GetFileName(directory), Does.Not.Contain("/").And.Not.Contain(":").And.Not.Contain("?"));
            Assert.That(Path.GetDirectoryName(directory), Is.EqualTo(EditorAutoBackup.BACKUP_DIRECTORY));
        }

        private static BeatmapInfo beatmap(string artist, string title, string difficulty) =>
            new BeatmapInfo
            {
                DifficultyName = difficulty,
                Metadata =
                {
                    Artist = artist,
                    Title = title,
                },
            };
    }
}
