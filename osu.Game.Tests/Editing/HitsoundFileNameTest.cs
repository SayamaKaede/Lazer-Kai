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

        [TestCase(@"soft-hitclap2.wav", 3, @"soft-hitclap5.wav")]
        // the first custom set is the name with no number.
        [TestCase(@"normal-hitnormal.wav", 3, @"normal-hitnormal4.wav")]
        [TestCase(@"drum-sliderslide10.ogg", 1, @"drum-sliderslide11.ogg")]
        [TestCase(@"soft-hitclap2.wav", 0, @"soft-hitclap2.wav")]
        // a name of no custom set has none to be another.
        [TestCase(@"kick.wav", 3, @"kick.wav")]
        [TestCase(@"HS/soft-hitclap2.wav", 3, @"HS/soft-hitclap2.wav")]
        public void TestANameIsOfTheCustomSetSoManyOn(string filename, int by, string expected)
        {
            Assert.That(HitsoundFileName.Shifted(filename, by), Is.EqualTo(expected));
        }

        [Test]
        public void TestTheHighestCustomSetOfABeatmapSet()
        {
            Assert.That(BeatmapSetSounds.HighestCustomIndex(set(@"kick.wav")), Is.EqualTo(0));
            Assert.That(BeatmapSetSounds.HighestCustomIndex(set(@"soft-hitclap.wav")), Is.EqualTo(1));
            Assert.That(BeatmapSetSounds.HighestCustomIndex(set(@"soft-hitclap.wav", @"drum-hitfinish7.ogg", @"normal-hitnormal3.wav")), Is.EqualTo(7));
        }

        [Test]
        public void TestASoundBroughtInUnderAnotherCustomSetIsNotInPlaceOfTheOneThere()
        {
            var ours = set(@"soft-hitclap2.wav", @"kick.wav");
            var theirs = set(@"soft-hitclap2.wav", @"kick.wav");

            // told apart by what is in them, which here is their names: the same in both.
            theirs.Files.Single(f => f.Filename == @"soft-hitclap2.wav").File.Hash = @"another clap";
            theirs.Files.Single(f => f.Filename == @"kick.wav").File.Hash = @"another kick";

            Assert.That(BeatmapSetSounds.ToBringInto(ours, theirs).Select(s => (s.Source, s.Name, s.Standing)), Is.EqualTo(new[]
            {
                (@"kick.wav", @"kick.wav", ImportStanding.InPlaceOfAnother),
                (@"soft-hitclap2.wav", @"soft-hitclap2.wav", ImportStanding.InPlaceOfAnother),
            }));

            Assert.That(BeatmapSetSounds.ToBringInto(ours, theirs, 2).Select(s => (s.Source, s.Name, s.Standing)), Is.EqualTo(new[]
            {
                // a name of no custom set is brought in as it is, and is in place of the one there still.
                (@"kick.wav", @"kick.wav", ImportStanding.InPlaceOfAnother),
                (@"soft-hitclap2.wav", @"soft-hitclap4.wav", ImportStanding.New),
            }));
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
