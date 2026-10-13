// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Extensions;
using osu.Framework.IO.Stores;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Models;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// The files of a beatmap set, read by their names in it.
    /// </summary>
    /// <remarks>
    /// The game keeps the files of every beatmap set by what is in them, and a set is the names it has them under.
    /// Nothing has to be written out to a folder for the files of one set to be read while another is being edited,
    /// which opening a set's folder does and can do for one set at a time.
    /// </remarks>
    public class BeatmapSetFiles : IResourceStore<byte[]>
    {
        private readonly BeatmapSetInfo set;
        private readonly WorkingBeatmap working;

        /// <param name="beatmaps">The beatmaps of the game.</param>
        /// <param name="set">The set, which has a difficulty at least.</param>
        public BeatmapSetFiles(BeatmapManager beatmaps, BeatmapSetInfo set)
        {
            this.set = set;

            // any difficulty of the set reads the files the game keeps.
            working = beatmaps.GetWorkingBeatmap(set.Beatmaps.First());
        }

        /// <summary>
        /// The beatmap set of <paramref name="id"/> with its difficulties and its files, apart from the game's own.
        /// </summary>
        /// <remarks>
        /// A set is otherwise handed around without its files, which most of the game has no use for.
        /// </remarks>
        public static BeatmapSetInfo? Read(RealmAccess realm, Guid id) => realm.Run(r =>
        {
            // with what has only just been written: a set brought into the game a moment ago is not found otherwise.
            var set = r.FindWithRefresh<BeatmapSetInfo>(id);

            if (set == null)
                return null;

            var read = set.Detach();

            read.Files.AddRange(set.Files.Detach());
            return read;
        });

        public Stream? GetStream(string name) => set.GetFile(name) is RealmNamedFileUsage file ? working.GetStream(file.File.GetStoragePath()) : null;

        public byte[] Get(string name)
        {
            using (var stream = GetStream(name))
                return stream?.ReadAllRemainingBytesToArray()!;
        }

        public Task<byte[]> GetAsync(string name, CancellationToken cancellationToken = default) => Task.FromResult(Get(name));

        public IEnumerable<string> GetAvailableResources() => set.Files.Select(f => f.Filename);

        public void Dispose()
        {
        }
    }
}
