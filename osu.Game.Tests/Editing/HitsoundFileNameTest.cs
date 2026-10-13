// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Models;
using osu.Game.Screens.Edit.BeatmapFiles;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The names a sound file of a beatmap set is played as a hitsound by, and which of them a file can be given
    /// (2026-10-13).
    /// </summary>
    [TestFixture]
    public class HitsoundFileNameTest
    {
        [TestCase(@"soft-hitclap2.wav", @"soft", @"hitclap", 2)]
        [TestCase(@"normal-hitnormal.wav", @"normal", @"hitnormal", 1)]
        [TestCase(@"drum-hitfinish15.ogg", @"drum", @"hitfinish", 15)]
        [TestCase(@"normal-hitwhistle3.mp3", @"normal", @"hitwhistle", 3)]
        [TestCase(@"soft-slidertick.wav", @"soft", @"slidertick", 1)]
        [TestCase(@"soft-sliderwhistle4.wav", @"soft", @"sliderwhistle", 4)]
        // stable does not mind how a name is written.
        [TestCase(@"Drum-SliderSlide10.OGG", @"drum", @"sliderslide", 10)]
        public void TestANameOfTheFormSaysWhatTheSoundIs(string filename, string bank, string sound, int index)
        {
            Assert.That(HitsoundFileName.Of(filename), Is.EqualTo(new HitsoundFileName(bank, sound, index)));
        }

        [TestCase(@"kick.wav")]
        [TestCase(@"soft-hitclap.txt")]
        [TestCase(@"soft-hitclap-2.wav")]
        [TestCase(@"soft-hitclap2b.wav")]
        [TestCase(@"soft-kick2.wav")]
        [TestCase(@"loud-hitclap2.wav")]
        // the first custom set is the name with no number, and there is none before it.
        [TestCase(@"soft-hitclap1.wav")]
        [TestCase(@"soft-hitclap0.wav")]
        [TestCase(@"soft-hitclap02.wav")]
        // found beside the difficulties only.
        [TestCase(@"HS/soft-hitclap2.wav")]
        public void TestAnyOtherNameIsNoHitsound(string filename)
        {
            Assert.That(HitsoundFileName.Of(filename), Is.Null);
        }

        [Test]
        public void TestTheFirstCustomSetIsNamedWithNoNumber()
        {
            Assert.That(new HitsoundFileName(@"normal", @"hitnormal", 1).WithExtension(@".wav"), Is.EqualTo(@"normal-hitnormal.wav"));
            Assert.That(new HitsoundFileName(@"soft", @"hitclap", 2).WithExtension(@".ogg"), Is.EqualTo(@"soft-hitclap2.ogg"));
            Assert.That(new HitsoundFileName(@"drum", @"sliderslide", 12).WithExtension(@".wav"), Is.EqualTo(@"drum-sliderslide12.wav"));
        }

        [Test]
        public void TestEveryNameThatCanBeGivenIsOfTheForm()
        {
            foreach (string bank in HitsoundFileName.BANKS)
            {
                foreach (string sound in HitsoundFileName.SOUNDS)
                {
                    var name = new HitsoundFileName(bank, sound, 7);

                    Assert.That(HitsoundFileName.Of(name.WithExtension(@".wav")), Is.EqualTo(name));
                }
            }
        }

        private static BeatmapSetInfo set(params string[] files)
        {
            var made = new BeatmapSetInfo();

            made.Beatmaps.Add(new BeatmapInfo(metadata: new BeatmapMetadata { AudioFile = @"audio.mp3" }));

            foreach (string name in files.Append(@"audio.mp3"))
                made.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = name }, name));

            return made;
        }

        [Test]
        public void TestANameNoFileHasIsFree()
        {
            Assert.That(BeatmapSetSounds.TakenBy(set(@"kick.wav", @"soft-hitclap3.wav"), @"soft-hitclap2.wav", @"kick.wav"), Is.Null);
        }

        [Test]
        public void TestANameAnotherSoundHasIsTaken()
        {
            Assert.That(BeatmapSetSounds.TakenBy(set(@"kick.wav", @"Soft-HitClap2.wav"), @"soft-hitclap2.wav", @"kick.wav")?.Filename, Is.EqualTo(@"Soft-HitClap2.wav"));

            // stable finds a sound by its name whatever kind of file it is.
            Assert.That(BeatmapSetSounds.TakenBy(set(@"kick.wav", @"soft-hitclap2.ogg"), @"soft-hitclap2.wav", @"kick.wav")?.Filename, Is.EqualTo(@"soft-hitclap2.ogg"));
        }

        [Test]
        public void TestTheNameASoundHasIsNotTakenFromItself()
        {
            Assert.That(BeatmapSetSounds.TakenBy(set(@"soft-hitclap2.wav"), @"soft-hitclap2.wav", @"soft-hitclap2.wav"), Is.Null);
        }
    }
}
