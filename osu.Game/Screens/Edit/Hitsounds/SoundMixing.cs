// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using ManagedBass;
using ManagedBass.Mix;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Makes one sound file of several, all heard from its start: a circle has one sound of each kind, and lanes that
    /// come to the same one at the same time are to be heard together in it.
    /// </summary>
    /// <remarks>
    /// As Mapping Tools does it where it is left to choose the format (its <c>NaudioAudioClipMixer</c> and
    /// <c>NaudioAudioExporter</c>): the sounds are added up as they are, none made quieter for the others, at the
    /// highest sample rate and the most channels among them, and written as a 32-bit floating point wave file. What
    /// goes over full volume is so kept as it is, and is only cut off as it is played, if it is still over by then: a
    /// circle at 60% plays a file that reaches 1.4 without any of it lost (2026-10-14).
    /// </remarks>
    public static class SoundMixing
    {
        /// <summary>
        /// The sounds as one wave file, or null if one of them is not a sound that can be read.
        /// </summary>
        /// <param name="sounds">The sound files, each whole.</param>
        public static byte[]? Mix(IReadOnlyList<byte[]> sounds)
        {
            var pins = new List<GCHandle>();
            var streams = new List<int>();
            int mixer = 0;

            try
            {
                int rate = 0;
                int channels = 0;

                foreach (byte[] sound in sounds)
                {
                    // read from where it is, which it is kept at until the reading is over.
                    var pin = GCHandle.Alloc(sound, GCHandleType.Pinned);
                    pins.Add(pin);

                    int stream = Bass.CreateStream(pin.AddrOfPinnedObject(), 0, sound.Length, BassFlags.Decode | BassFlags.Float);

                    if (stream == 0)
                        return null;

                    streams.Add(stream);

                    var info = Bass.ChannelGetInfo(stream);

                    rate = Math.Max(rate, info.Frequency);
                    channels = Math.Max(channels, Math.Min(info.Channels, 2));
                }

                if (streams.Count == 0)
                    return null;

                mixer = BassMix.CreateMixerStream(rate, channels, BassFlags.Decode | BassFlags.Float | BassFlags.MixerEnd);

                if (mixer == 0)
                    return null;

                foreach (int stream in streams)
                {
                    // a sound added to a mixer is faded in over a moment, which would take the start off a hit sound.
                    if (!BassMix.MixerAddChannel(mixer, stream, BassFlags.MixerChanNoRampin | BassFlags.MixerChanDownMix))
                        return null;
                }

                using (var data = new MemoryStream())
                {
                    byte[] buffer = new byte[65536];

                    while (true)
                    {
                        int read = Bass.ChannelGetData(mixer, buffer, buffer.Length);

                        if (read <= 0)
                            break;

                        data.Write(buffer, 0, read);
                    }

                    return data.Length == 0 ? null : waveFile(data.ToArray(), rate, channels);
                }
            }
            finally
            {
                if (mixer != 0)
                    Bass.StreamFree(mixer);

                foreach (int stream in streams)
                    Bass.StreamFree(stream);

                foreach (var pin in pins)
                    pin.Free();
            }
        }

        private const int bytes_per_sample = 4;

        /// <summary>
        /// What a wave file says its samples are where they are floating point numbers.
        /// </summary>
        private const short ieee_float = 3;

        private static byte[] waveFile(byte[] samples, int rate, int channels)
        {
            using (var file = new MemoryStream())
            using (var writer = new BinaryWriter(file, Encoding.ASCII))
            {
                writer.Write(Encoding.ASCII.GetBytes(@"RIFF"));
                writer.Write(50 + samples.Length);
                writer.Write(Encoding.ASCII.GetBytes(@"WAVE"));

                // with the size of what follows the format, which is nothing, and how many samples there are: both are
                // asked of a file that is not plain whole numbers.
                writer.Write(Encoding.ASCII.GetBytes(@"fmt "));
                writer.Write(18);
                writer.Write(ieee_float);
                writer.Write((short)channels);
                writer.Write(rate);
                writer.Write(rate * channels * bytes_per_sample);
                writer.Write((short)(channels * bytes_per_sample));
                writer.Write((short)(bytes_per_sample * 8));
                writer.Write((short)0);

                writer.Write(Encoding.ASCII.GetBytes(@"fact"));
                writer.Write(4);
                writer.Write(samples.Length / (channels * bytes_per_sample));

                writer.Write(Encoding.ASCII.GetBytes(@"data"));
                writer.Write(samples.Length);
                writer.Write(samples);

                writer.Flush();
                return file.ToArray();
            }
        }
    }
}
