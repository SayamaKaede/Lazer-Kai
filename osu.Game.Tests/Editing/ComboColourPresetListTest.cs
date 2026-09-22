// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Testing;
using osu.Game.Screens.Edit.Setup;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The combo colour presets offered on the setup screen, and how they are kept.
    /// </summary>
    [TestFixture]
    public class ComboColourPresetListTest
    {
        private static readonly Colour4[] warm = { Colour4.FromHex("#FF0000"), Colour4.FromHex("#FF8000") };
        private static readonly Colour4[] cool = { Colour4.FromHex("#0000FF"), Colour4.FromHex("#00FFFF"), Colour4.FromHex("#8000FF") };

        [Test]
        public void TestANewNameIsAddedAtTheEnd()
        {
            var list = new ComboColourPresetList();

            list.Save("warm", warm);
            list.Save("cool", cool);

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "warm", "cool" }));
            Assert.That(list.Find("cool")!.ToColours(), Is.EqualTo(cool));
        }

        [Test]
        public void TestATakenNameIsOverwrittenWhereItStands()
        {
            var list = new ComboColourPresetList();

            list.Save("warm", warm);
            list.Save("cool", cool);
            list.Save("warm", cool);

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "warm", "cool" }));
            Assert.That(list.Find("warm")!.ToColours(), Is.EqualTo(cool));
        }

        [Test]
        public void TestSpacesAroundANameAreNotPartOfIt()
        {
            var list = new ComboColourPresetList();

            list.Save("warm", warm);
            list.Save("  warm ", cool);

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "warm" }));
            Assert.That(list.Find(" warm")!.ToColours(), Is.EqualTo(cool));
        }

        [Test]
        public void TestNothingIsFoundUnderANameNeverSaved()
        {
            var list = new ComboColourPresetList();

            list.Save("warm", warm);

            Assert.That(list.Find("cool"), Is.Null);
        }

        [Test]
        public void TestTheListIsReadBackAsItWasWritten()
        {
            using (var storage = new TemporaryNativeStorage(nameof(ComboColourPresetListTest)))
            {
                var store = new ComboColourPresetStore(storage);
                var list = new ComboColourPresetList();

                list.Save("warm", warm);
                list.Save("cool", cool);
                store.Save(list);

                var read = new ComboColourPresetStore(storage).Load();

                Assert.That(read.Presets.Select(p => p.Name), Is.EqualTo(new[] { "warm", "cool" }));
                Assert.That(read.Find("warm")!.ToColours(), Is.EqualTo(warm));
                Assert.That(read.Find("cool")!.ToColours(), Is.EqualTo(cool));
            }
        }

        [Test]
        public void TestNothingSavedYetReadsAsEmpty()
        {
            using (var storage = new TemporaryNativeStorage(nameof(ComboColourPresetListTest)))
                Assert.That(new ComboColourPresetStore(storage).Load().Presets, Is.Empty);
        }
    }
}
