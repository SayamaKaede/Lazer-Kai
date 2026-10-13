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
    /// Which files of a beatmap set are its sounds, and what bringing one into another set would do there (2026-10-13).
    /// </summary>
    [TestFixture]
    public class BeatmapSetSoundsTest
    {
        private static BeatmapSetInfo set(string song, params (string name, string hash)[] files)
        {
            var made = new BeatmapSetInfo();

            made.Beatmaps.Add(new BeatmapInfo(metadata: new BeatmapMetadata { AudioFile = song }));
            made.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = @"song" }, song));

            foreach (var (name, hash) in files)
                made.Files.Add(new RealmNamedFileUsage(new RealmFile { Hash = hash }, name));

            return made;
        }

        private static ImportStanding standing(BeatmapSetInfo target, string name, string hash = @"theirs") =>
            BeatmapSetSounds.ToBringInto(target, set(@"their song.mp3", (name, hash))).Single().Standing;

        [Test]
        public void TestTheSoundsOfASetAreItsAudioFilesButItsSong()
        {
            var theirs = set(@"Song.mp3", (@"soft-hitclap2.wav", @"a"), (@"drums/snare.ogg", @"b"), (@"bg.jpg", @"c"), (@"Artist - Title (them) [Hard].osu", @"d"), (@"Kick.WAV", @"e"));

            Assert.That(BeatmapSetSounds.Of(theirs).Select(f => f.Filename), Is.EqualTo(new[] { @"drums/snare.ogg", @"Kick.WAV", @"soft-hitclap2.wav" }));
        }

        [Test]
        public void TestTheSongIsLeftOutHoweverItsNameIsWritten()
        {
            var theirs = set(@"song.mp3");

            // as a file of stable's may have it: the name in the difficulty is not always written as the file's is.
            theirs.Beatmaps[0].Metadata.AudioFile = @"SONG.MP3";

            Assert.That(BeatmapSetSounds.Of(theirs), Is.Empty);
        }

        [Test]
        public void TestASoundOfNoNameHereIsNew()
        {
            Assert.That(standing(set(@"audio.mp3", (@"snare.wav", @"ours")), @"kick.wav"), Is.EqualTo(ImportStanding.New));
        }

        [Test]
        public void TestTheVeryFileIsThereAlready()
        {
            Assert.That(standing(set(@"audio.mp3", (@"Kick.wav", @"same")), @"kick.wav", @"same"), Is.EqualTo(ImportStanding.ThereAlready));
        }

        [Test]
        public void TestAnotherSoundOfTheNameIsWhatItWouldBePutInPlaceOf()
        {
            Assert.That(standing(set(@"audio.mp3", (@"Kick.wav", @"ours")), @"kick.wav"), Is.EqualTo(ImportStanding.InPlaceOfAnother));
        }

        [Test]
        public void TestAnotherKindOfFileOfTheNameIsTheSameSound()
        {
            // stable finds a sound by its name whatever kind of file it is.
            Assert.That(standing(set(@"audio.mp3", (@"kick.ogg", @"ours")), @"kick.wav"), Is.EqualTo(ImportStanding.InPlaceOfAnother));
        }

        [Test]
        public void TestASoundInAnotherFolderIsAnotherSound()
        {
            Assert.That(standing(set(@"audio.mp3", (@"kick.wav", @"ours")), @"drums/kick.wav"), Is.EqualTo(ImportStanding.New));
        }

        [Test]
        public void TestNothingIsPutInPlaceOfTheSong()
        {
            Assert.That(standing(set(@"audio.mp3"), @"audio.wav"), Is.EqualTo(ImportStanding.NameOfTheSong));
            Assert.That(standing(set(@"audio.mp3"), @"Audio.mp3"), Is.EqualTo(ImportStanding.NameOfTheSong));
        }
    }
}
