// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using osu.Framework.Platform;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// The sounds kept beside the game to hitsound any beatmap with: a folder of the mapper's own, which the lanes of a
    /// difficulty made to hitsound in are given sounds from.
    /// </summary>
    /// <remarks>
    /// A sound a lane is given is copied into the beatmap set, so a beatmap does not depend on the folder once its
    /// sounds are picked.
    /// </remarks>
    public class HitsoundLibrary
    {
        public const string DIRECTORY = "hitsounds";

        private static readonly string[] extensions = { ".wav", ".ogg", ".mp3" };

        private readonly Storage storage;

        public HitsoundLibrary(Storage storage)
        {
            // made as it is asked for, so the folder is there to put sounds in from the first time it is opened.
            this.storage = storage.GetStorageForDirectory(DIRECTORY);
        }

        /// <summary>
        /// Every sound in the folder and the folders in it, by its path from the folder with forward slashes, in order.
        /// </summary>
        public IReadOnlyList<string> Files()
        {
            var found = new List<string>();
            collect(string.Empty, found);
            return found.OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        private void collect(string directory, List<string> found)
        {
            foreach (string file in storage.GetFiles(directory))
            {
                if (extensions.Contains(Path.GetExtension(file), StringComparer.OrdinalIgnoreCase))
                    found.Add(file.Replace('\\', '/'));
            }

            foreach (string child in storage.GetDirectories(directory))
                collect(child, found);
        }

        /// <summary>
        /// The sound at <paramref name="file"/>, as <see cref="Files"/> names it; null if it is no longer there.
        /// </summary>
        public Stream? Open(string file) => storage.Exists(file) ? storage.GetStream(file) : null;

        /// <summary>
        /// Shows the folder in the system's file browser, to put sounds in.
        /// </summary>
        public void PresentExternally() => storage.PresentExternally();
    }
}
