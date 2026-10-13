// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using ManagedBass;
using osu.Framework.Audio.Callbacks;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// How long a file of sound plays for.
    /// </summary>
    public static class SoundLength
    {
        /// <summary>
        /// The length of <paramref name="file"/> in milliseconds, or null if it cannot be read as sound.
        /// </summary>
        public static double? Of(byte[] file)
        {
            using (var data = new MemoryStream(file))
            {
                var fileCallbacks = new FileCallbacks(new DataStreamFileProcedures(data));
                int decodeStream = Bass.CreateStream(StreamSystem.NoBuffer, BassFlags.Decode | BassFlags.Prescan, fileCallbacks.Callbacks, fileCallbacks.Handle);

                if (decodeStream == 0)
                    return null;

                try
                {
                    double seconds = Bass.ChannelBytes2Seconds(decodeStream, Bass.ChannelGetLength(decodeStream));

                    return seconds < 0 ? null : seconds * 1000;
                }
                finally
                {
                    Bass.StreamFree(decodeStream);
                }
            }
        }
    }
}
