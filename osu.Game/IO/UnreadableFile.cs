// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.IO
{
    /// <summary>
    /// Puts aside a file of Lazer-Kai's own that could not be read, so that what is saved next does not go in its place.
    /// </summary>
    /// <remarks>
    /// The stores carry on as if there were nothing there, which keeps the game going, but they write their whole file
    /// on every save: months of time spent mapping, or every preset, were written over with what was saved at that
    /// moment (2026-10-23). The file is kept beside the new one, renamed, so that it can still be looked at or put back.
    /// </remarks>
    public static class UnreadableFile
    {
        /// <summary>
        /// Renames <paramref name="path"/> to <c>name.broken-date-time.ext</c> beside it.
        /// </summary>
        /// <returns>
        /// Whether the file is out of the way: false if it is still where it was, in which case nothing should be written
        /// over it.
        /// </returns>
        public static bool SetAside(Storage storage, string path)
        {
            string aside = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty,
                $"{Path.GetFileNameWithoutExtension(path)}.broken-{DateTimeOffset.Now:yyyyMMdd-HHmmss}{Path.GetExtension(path)}");

            try
            {
                storage.Move(path, aside);
                Logger.Log($"{path} could not be read and was kept as {aside}", level: LogLevel.Important);
                return true;
            }
            catch (Exception e)
            {
                Logger.Error(e, $"{path} could not be read, and could not be kept aside either; it will not be written over");
                return !storage.Exists(path);
            }
        }
    }
}
