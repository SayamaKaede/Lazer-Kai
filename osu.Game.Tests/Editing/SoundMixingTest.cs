// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using System.Linq;
using System.Text;
using ManagedBass;
using NUnit.Framework;
using osu.Game.Screens.Edit.Hitsounds;
using osuTK.Audio;

namespace osu.Game.Tests.Editing
{
    /// <summary>
    /// The one file made of the sounds of lanes that come to the same sound of a circle at the same time (2026-10-14).
    /// </summary>
    [TestFixture]
    public class SoundMixingTest
    {
        [OneTimeSetUp]
        public void SetUp()
        {
            if (!Bass.Init(0) && Bass.LastError != Errors.Already)
                throw new AudioException("Could not initialize Bass.");
        }

        /// <summary>
        /// From the very first sample: a sound added to a mixer is faded in unless it is asked not to be, and a hit sound
        /// is its start.
        /// </summary>
        [Test]
        public void TestTheSoundsAreAddedUpFromTheFirstSample()
        {
            var mixed = read(SoundMixing.Mix(new[] { wave(44100, 1, 1000, 0.25f), wave(44100, 1, 1000, 0.25f) }));

            Assert.That((mixed.rate, mixed.channels), Is.EqualTo((44100, 1)));
            Assert.That(mixed.samples, Has.Length.EqualTo(1000));
            Assert.That(mixed.samples, Is.All.EqualTo(0.5f).Within(0.001f));
        }

        [Test]
        public void TestTheLongerSoundGoesOnAlone()
        {
            var mixed = read(SoundMixing.Mix(new[] { wave(44100, 1, 100, 0.25f), wave(44100, 1, 300, 0.5f) }));

            Assert.That(mixed.samples, Has.Length.EqualTo(300));
            Assert.That(mixed.samples.Take(100), Is.All.EqualTo(0.75f).Within(0.001f));
            Assert.That(mixed.samples.Skip(100), Is.All.EqualTo(0.5f).Within(0.001f));
        }

        /// <summary>
        /// As Mapping Tools leaves it: the file can say more than full volume, so nothing is lost of two loud sounds
        /// played quietly.
        /// </summary>
        [Test]
        public void TestWhatGoesOverFullVolumeIsKept()
        {
            var mixed = read(SoundMixing.Mix(new[] { wave(44100, 1, 500, 0.75f), wave(44100, 1, 500, 0.75f) }));

            Assert.That(mixed.samples, Is.All.EqualTo(1.5f).Within(0.001f));
        }

        [Test]
        public void TestTheFileIsAtTheHighestRateWithTheMostChannels()
        {
            // a tenth of a second each, the one with two channels saying opposite things in them.
            var mixed = read(SoundMixing.Mix(new[] { wave(22050, 1, 2205, 0.25f), wave(44100, 2, 4410, 0.5f, -0.5f) }));

            Assert.That((mixed.rate, mixed.channels), Is.EqualTo((44100, 2)));
            Assert.That(mixed.samples.Length / 2, Is.EqualTo(4410).Within(64));

            // away from the ends, where bringing the slower one up to the rate rings.
            var middle = mixed.samples.Skip(2 * 1000).Take(2 * 2000).ToArray();

            Assert.That(middle.Where((_, i) => i % 2 == 0), Is.All.EqualTo(0.75f).Within(0.01f), "one channel of the one, and the other in both");
            Assert.That(middle.Where((_, i) => i % 2 == 1), Is.All.EqualTo(-0.25f).Within(0.01f));
        }

        [Test]
        public void TestSomethingThatIsNotASoundIsNotMixed()
        {
            Assert.That(SoundMixing.Mix(new[] { wave(44100, 1, 100, 0.25f), Encoding.ASCII.GetBytes(@"not a sound at all, whatever it is called") }), Is.Null);
        }

        /// <summary>
        /// A 16-bit wave file saying the same thing all through, each channel its own.
        /// </summary>
        private static byte[] wave(int rate, int channels, int frames, params float[] levels)
        {
            using (var file = new MemoryStream())
            using (var writer = new BinaryWriter(file))
            {
                int length = frames * channels * 2;

                writer.Write(Encoding.ASCII.GetBytes(@"RIFF"));
                writer.Write(36 + length);
                writer.Write(Encoding.ASCII.GetBytes(@"WAVEfmt "));
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channels);
                writer.Write(rate);
                writer.Write(rate * channels * 2);
                writer.Write((short)(channels * 2));
                writer.Write((short)16);
                writer.Write(Encoding.ASCII.GetBytes(@"data"));
                writer.Write(length);

                for (int i = 0; i < frames; i++)
                {
                    for (int channel = 0; channel < channels; channel++)
                        writer.Write((short)Math.Round(levels[channel] * 32768));
                }

                writer.Flush();
                return file.ToArray();
            }
        }

        /// <summary>
        /// What a file that was mixed says: a floating point wave file, as it is written.
        /// </summary>
        private static (int rate, int channels, float[] samples) read(byte[]? file)
        {
            Assert.That(file, Is.Not.Null);
            Assert.That(Encoding.ASCII.GetString(file!, 0, 4) + Encoding.ASCII.GetString(file!, 8, 8), Is.EqualTo(@"RIFFWAVEfmt "));
            Assert.That(BitConverter.ToInt32(file!, 4), Is.EqualTo(file!.Length - 8), "the length it says it has");
            Assert.That((BitConverter.ToInt16(file, 20), BitConverter.ToInt16(file, 34)), Is.EqualTo(((short)3, (short)32)), "floating point, 32 bits");

            int channels = BitConverter.ToInt16(file, 22);
            int rate = BitConverter.ToInt32(file, 24);

            Assert.That(Encoding.ASCII.GetString(file, 50, 4), Is.EqualTo(@"data"));

            int length = BitConverter.ToInt32(file, 54);

            Assert.That(length, Is.EqualTo(file.Length - 58));
            Assert.That(BitConverter.ToInt32(file, 46), Is.EqualTo(length / (channels * 4)), "how many samples it says it has");

            // and it is a sound that is read back as one.
            Assert.That(SoundMixing.Mix(new[] { file }), Is.Not.Null);

            return (rate, channels, Enumerable.Range(0, length / 4).Select(i => BitConverter.ToSingle(file, 58 + i * 4)).ToArray());
        }
    }
}
