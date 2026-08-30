// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using NUnit.Framework;
using osu.Game.Utils;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// Keeping an account name off the screen when a path goes on it.
    /// </summary>
    [TestFixture]
    public class PathDisplayTest
    {
        [Test]
        public void TestTheNameAfterUsersIsMasked()
        {
            Assert.That(PathDisplay.Mask(@"C:\Users\Kaede\Documents\Mapping Tools\Mapping Tools.exe"),
                Is.EqualTo($@"C:\Users\{PathDisplay.MASK}\Documents\Mapping Tools\Mapping Tools.exe"));
        }

        /// <summary>
        /// Somebody else's name is somebody's name too.
        /// </summary>
        [Test]
        public void TestAnotherMachinesNameIsMaskedToo()
        {
            Assert.That(PathDisplay.Mask(@"D:\Users\SomeoneElse\tools\Mapping Tools.exe"),
                Is.EqualTo($@"D:\Users\{PathDisplay.MASK}\tools\Mapping Tools.exe"));
        }

        [Test]
        public void TestThisAccountsNameIsMaskedWhereverItAppears()
        {
            string account = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).Split('\\', '/')[^1];

            if (string.IsNullOrEmpty(account))
                Assert.Ignore(@"No account name to check against on this machine.");

            Assert.That(PathDisplay.Mask($@"D:\backups\{account}\Mapping Tools.exe"),
                Is.EqualTo($@"D:\backups\{PathDisplay.MASK}\Mapping Tools.exe"));
        }

        /// <summary>
        /// The usual install has nobody's name in it and must come through untouched — masking a path that says
        /// nothing about anybody only makes it harder to read.
        /// </summary>
        [Test]
        public void TestAPathWithNoNameInItIsLeftAlone()
        {
            const string path = @"C:\Program Files\Mapping Tools\Mapping Tools.exe";

            Assert.That(PathDisplay.Mask(path), Is.EqualTo(path));
        }

        [Test]
        public void TestTheSeparatorSurvives()
        {
            Assert.That(PathDisplay.Mask(@"/home/Users/Kaede/tools/mt"), Is.EqualTo($@"/home/Users/{PathDisplay.MASK}/tools/mt"));
        }

        [TestCase("")]
        [TestCase(null)]
        public void TestNothingIsNothing(string? path)
        {
            Assert.That(PathDisplay.Mask(path!), Is.EqualTo(path));
        }

        /// <summary>
        /// Only what is drawn is masked. Everything the setting does with the path afterwards — comparing it,
        /// opening it — has to be done with the real one.
        /// </summary>
        [Test]
        public void TestMaskingDoesNotTouchTheOriginal()
        {
            const string path = @"C:\Users\Kaede\Mapping Tools.exe";

            PathDisplay.Mask(path);

            Assert.That(path, Is.EqualTo(@"C:\Users\Kaede\Mapping Tools.exe"));
        }
    }
}
