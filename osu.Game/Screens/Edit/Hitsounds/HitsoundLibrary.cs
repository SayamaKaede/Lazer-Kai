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
    /// A lane sounds as the file here itself, and nothing is put in the beatmap for it: a sound taken out of the folder
    /// or given another name is a lane that no longer sounds, and one put in place of another under its name is what
    /// every lane given that name sounds as. Until 2026-10-13 the sound was copied into the beatmap set as a lane was
    /// given it, under a folder of its own, which was not wanted there.
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
        public Stream? Open(string file) => Exists(file) ? storage.GetStream(file) : null;

        /// <summary>
        /// Whether the folder has the sound at <paramref name="file"/>, as <see cref="Files"/> names it.
        /// </summary>
        public bool Exists(string file) => file.Length > 0 && storage.Exists(file);

        /// <summary>
        /// What is in the sound at <paramref name="file"/>, as <see cref="Files"/> names it; null if it is no longer
        /// there.
        /// </summary>
        public byte[]? Read(string file)
        {
            using (var stream = Open(file))
            {
                if (stream == null)
                    return null;

                using (var memory = new MemoryStream())
                {
                    stream.CopyTo(memory);
                    return memory.ToArray();
                }
            }
        }

        /// <summary>
        /// Shows the folder in the system's file browser, to put sounds in.
        /// </summary>
        public void PresentExternally() => storage.PresentExternally();
    }
}
