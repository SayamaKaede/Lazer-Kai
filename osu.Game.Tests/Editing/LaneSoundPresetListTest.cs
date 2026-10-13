// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Testing;
using osu.Game.Audio;
using osu.Game.Screens.Edit.Hitsounds;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The presets the lanes of a difficulty made to hitsound in are kept as, and how they are kept (2026-10-13).
    /// </summary>
    [TestFixture]
    public class LaneSoundPresetListTest
    {
        private static readonly LaneSound kick = new LaneSound { File = @"HS/kick.wav", Source = @"kick.wav" };

        private static readonly LaneSound snare = new LaneSound
        {
            File = @"HS/snare.wav",
            Source = @"drums/snare.wav",
            Bank = HitSampleInfo.BANK_DRUM,
            Sound = HitSampleInfo.HIT_CLAP,
        };

        private static readonly LaneSound none = new LaneSound();

        private static LaneSoundPreset preset(string name, params LaneSound[] lanes) => LaneSoundPreset.From(name, lanes, new LaneSound { Bank = HitSampleInfo.BANK_SOFT });

        private static (string, string, string)[] lanesOf(LaneSoundPreset? preset) => preset!.Lanes.Select(l => (l.Source, l.Bank, l.Sound)).ToArray();

        [Test]
        public void TestALaneIsKeptByWhereItsSoundCameFromAndWhatItIsToBe()
        {
            var made = preset("kit", kick, none, snare);

            Assert.That(lanesOf(made), Is.EqualTo(new[]
            {
                (@"kick.wav", HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                // a lane with no sound keeps its place, for the one after it to be the third still.
                (string.Empty, HitSampleInfo.BANK_NORMAL, HitSampleInfo.HIT_NORMAL),
                (@"drums/snare.wav", HitSampleInfo.BANK_DRUM, HitSampleInfo.HIT_CLAP),
            }));
            Assert.That(made.Lanes.Select(l => l.DisplayName), Is.EqualTo(new[] { @"kick", string.Empty, @"snare" }));
        }

        [Test]
        public void TestTheHitSoundOfACircleWithNoneIsKeptToo()
        {
            var made = LaneSoundPreset.From("kit", new[] { none }, new LaneSound { File = @"HS/kick.wav", Source = @"kick.wav", Bank = HitSampleInfo.BANK_DRUM });

            Assert.That((made.Fallback.Source, made.Fallback.Bank), Is.EqualTo((@"kick.wav", HitSampleInfo.BANK_DRUM)));
            Assert.That(made.HasSounds);
        }

        [Test]
        public void TestLanesWithNoSoundAtAllAreNothingToKeep()
        {
            Assert.That(preset("empty", none, none).HasSounds, Is.False);
            Assert.That(preset("one", none, kick).HasSounds);
        }

        [Test]
        public void TestANewNameIsAddedAtTheEnd()
        {
            var list = new LaneSoundPresetList();

            list.Save(preset("drums", kick));
            list.Save(preset("keys", snare));

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "drums", "keys" }));
            Assert.That(lanesOf(list.Find("keys")).Single().Item1, Is.EqualTo(@"drums/snare.wav"));
        }

        [Test]
        public void TestATakenNameIsOverwrittenWhereItStands()
        {
            var list = new LaneSoundPresetList();

            list.Save(preset("drums", kick));
            list.Save(preset("keys", snare));
            list.Save(preset("drums", snare, kick));

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "drums", "keys" }));
            Assert.That(lanesOf(list.Find("drums")).Select(l => l.Item1), Is.EqualTo(new[] { @"drums/snare.wav", @"kick.wav" }));
        }

        [Test]
        public void TestSpacesAroundANameAreNotPartOfIt()
        {
            var list = new LaneSoundPresetList();

            list.Save(preset("drums", kick));
            list.Save(preset("  drums ", snare));

            Assert.That(list.Presets.Select(p => p.Name), Is.EqualTo(new[] { "drums" }));
            Assert.That(lanesOf(list.Find(" drums")).Single().Item1, Is.EqualTo(@"drums/snare.wav"));
        }

        [Test]
        public void TestNothingIsFoundUnderANameNeverSaved()
        {
            var list = new LaneSoundPresetList();

            list.Save(preset("drums", kick));

            Assert.That(list.Find("keys"), Is.Null);
        }

        [Test]
        public void TestWhatIsWrittenIsReadBackAsItWas()
        {
            using (var storage = new TemporaryNativeStorage($"{nameof(LaneSoundPresetListTest)}-{Guid.NewGuid()}"))
            {
                var store = new LaneSoundPresetStore(storage);

                Assert.That(store.Load().Presets, Is.Empty);

                var list = new LaneSoundPresetList();
                list.Save(preset("drums", kick, none, snare));
                list.Save(preset("keys", snare));
                store.Save(list);

                var read = store.Load();

                Assert.That(read.Presets.Select(p => p.Name), Is.EqualTo(new[] { "drums", "keys" }));
                Assert.That(lanesOf(read.Find("drums")), Is.EqualTo(lanesOf(list.Find("drums"))));
                Assert.That(read.Find("drums")!.Fallback.Bank, Is.EqualTo(HitSampleInfo.BANK_SOFT));
            }
        }

        [Test]
        public void TestAFileThatCannotBeReadIsAnEmptyList()
        {
            using (var storage = new TemporaryNativeStorage($"{nameof(LaneSoundPresetListTest)}-{Guid.NewGuid()}"))
            {
                using (var stream = storage.CreateFileSafely(LaneSoundPresetStore.FILENAME))
                using (var writer = new System.IO.StreamWriter(stream))
                    writer.Write(@"{ not json");

                Assert.That(new LaneSoundPresetStore(storage).Load().Presets, Is.Empty);
            }
        }
    }
}
