// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.IO;

namespace osu.Game.Tests.NonVisual
{
    /// <summary>
    /// Reading the changelogs shipped beside the game, and choosing the ones to show after an update.
    /// </summary>
    [TestFixture]
    public class LazerKaiChangelogTest
    {
        private string directory = null!;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(Path.Combine(directory, LazerKaiChangelog.DIRECTORY));
        }

        [TearDown]
        public void TearDown() => Directory.Delete(directory, true);

        [Test]
        public void TestReadAsItIsWritten()
        {
            var release = LazerKaiChangelog.Parse(@"20261005.0", string.Join('\n',
                @"**Lazer-Kai 20261005.0**",
                @"",
                @"- Tachyon 2026.1001.0がリリースされたので、ベースのバージョンを変えました",
                @"",
                @"**機能追加**",
                @"- F6の緑線の一覧に「選択した線を現在位置から貼り付け」を追加しました",
                @"  間隔を保ったまま貼り付けます",
                @"  貼り付けた範囲にあった緑線は置き換わります",
                @"- Shift+F12で、通常のスクリーンショットを撮影するようにしました",
                @"",
                @"**移行のしかた**",
                @"1. LazerKai-win-Portable.zip を好きな場所に展開する",
                @"今までのフォルダはそのまま残ります。"));

            Assert.That(release.Version, Is.EqualTo(@"20261005.0"));
            Assert.That(release.Lines.Select(l => (l.Kind, l.Text)), Is.EqualTo(new[]
            {
                (LazerKaiChangelog.LineKind.Change, @"Tachyon 2026.1001.0がリリースされたので、ベースのバージョンを変えました"),
                (LazerKaiChangelog.LineKind.Heading, @"機能追加"),
                (LazerKaiChangelog.LineKind.Change, @"F6の緑線の一覧に「選択した線を現在位置から貼り付け」を追加しました"),
                (LazerKaiChangelog.LineKind.Change, @"Shift+F12で、通常のスクリーンショットを撮影するようにしました"),
                (LazerKaiChangelog.LineKind.Heading, @"移行のしかた"),
                (LazerKaiChangelog.LineKind.Text, @"1. LazerKai-win-Portable.zip を好きな場所に展開する"),
                (LazerKaiChangelog.LineKind.Text, @"今までのフォルダはそのまま残ります。"),
            }));

            Assert.That(release.Lines[2].Details, Is.EqualTo(new[] { @"間隔を保ったまま貼り付けます", @"貼り付けた範囲にあった緑線は置き換わります" }));
            Assert.That(release.Lines[3].Details, Is.Empty);
        }

        /// <summary>
        /// One of the early changelogs was saved with Windows line endings, and an editor may add a byte order mark.
        /// </summary>
        [Test]
        public void TestWindowsLineEndingsAndAByteOrderMark()
        {
            File.WriteAllText(Path.Combine(directory, LazerKaiChangelog.DIRECTORY, @"20260819-1.txt"),
                "**Lazer-Kai 20260819.1**\r\n\r\n**修正**\r\n- 一つ目\r\n  その説明\r\n", new UTF8Encoding(true));

            var release = LazerKaiChangelog.Read(directory).Single();

            Assert.That(release.Lines.Select(l => (l.Kind, l.Text)), Is.EqualTo(new[]
            {
                (LazerKaiChangelog.LineKind.Heading, @"修正"),
                (LazerKaiChangelog.LineKind.Change, @"一つ目"),
            }));
            Assert.That(release.Lines[1].Details, Is.EqualTo(new[] { @"その説明" }));
        }

        [Test]
        public void TestOnlyReleasesAreReadNewestFirst()
        {
            write(@"20260916-0.txt");
            write(@"20260916-0-書き直し.txt");
            write(@"20261008-0.txt");
            write(@"20261008-10.txt");
            write(@"20261008-9.txt");
            write(@"notes.txt");

            Assert.That(LazerKaiChangelog.Read(directory).Select(r => r.Version),
                Is.EqualTo(new[] { @"20261008.10", @"20261008.9", @"20261008.0", @"20260916.0" }));
        }

        [Test]
        public void TestNothingToRead()
        {
            Directory.Delete(Path.Combine(directory, LazerKaiChangelog.DIRECTORY));

            Assert.That(LazerKaiChangelog.Read(directory), Is.Empty);
            Assert.That(LazerKaiChangelog.Read(null), Is.Empty);
            Assert.That(LazerKaiChangelog.Read(string.Empty), Is.Empty);
        }

        [Test]
        public void TestASkippedReleaseIsShownToo()
        {
            Assert.That(unseen(@"20260929.0", @"20261008.0"), Is.EqualTo(new[] { @"20261008.0", @"20261005.0" }));
        }

        [Test]
        public void TestTheNextReleaseOfTheSameDay()
        {
            Assert.That(unseen(@"20261008.0", @"20261008.1"), Is.EqualTo(new[] { @"20261008.1" }));
        }

        [Test]
        public void TestComingFromAPortableBuildShowsOnlyTheRunningRelease()
        {
            Assert.That(unseen(@"20261010 0f4a20+", @"20261008.1"), Is.EqualTo(new[] { @"20261008.1" }));
        }

        [TestCase(@"", @"20261008.0", Description = "a first run")]
        [TestCase(@"20261008.0", @"20261008.0", Description = "the same release again")]
        [TestCase(@"20261008.0", @"20261010 0f4a20+", Description = "a portable build")]
        [TestCase(@"20261008.0", @"local debug", Description = "a build out of the repository")]
        [TestCase(@"20261008.1", @"20261005.0", Description = "an older release")]
        public void TestNothingToShow(string last, string running)
        {
            Assert.That(unseen(last, running), Is.Empty);
        }

        private static string[] unseen(string last, string running)
        {
            var releases = new[] { @"20260926.0", @"20260929.0", @"20261005.0", @"20261008.0", @"20261008.1" }
                .Select(v => new LazerKaiChangelog.Release(v, []));

            return LazerKaiChangelog.Unseen(releases, last, running).Select(r => r.Version).ToArray();
        }

        private void write(string name) =>
            File.WriteAllText(Path.Combine(directory, LazerKaiChangelog.DIRECTORY, name), "**Lazer-Kai**\n- 変更\n");
    }
}
