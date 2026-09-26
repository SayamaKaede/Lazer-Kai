// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// A search typed into song select, kept under a name to be put back later.
    /// </summary>
    public record SearchPreset(string Name, string Query);

    /// <summary>
    /// Where the search presets are kept: one file, in the order they were saved.
    /// </summary>
    public class SearchPresetStore
    {
        public const string FILENAME = "search-presets.json";

        private readonly Storage storage;

        public SearchPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        public IReadOnlyList<SearchPreset> Load()
        {
            if (!storage.Exists(FILENAME))
                return Array.Empty<SearchPreset>();

            try
            {
                using (var stream = storage.GetStream(FILENAME))
                using (var reader = new StreamReader(stream))
                {
                    return JsonConvert.DeserializeObject<List<SearchPreset>>(reader.ReadToEnd())?
                                      .Where(p => !string.IsNullOrWhiteSpace(p.Name) && !string.IsNullOrWhiteSpace(p.Query))
                                      .ToArray()
                           ?? Array.Empty<SearchPreset>();
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read the search presets");
                return Array.Empty<SearchPreset>();
            }
        }

        /// <summary>
        /// Keeps <paramref name="query"/> under <paramref name="name"/>, or under the query itself if no name is given.
        /// A preset already under that name is replaced where it stands.
        /// </summary>
        /// <returns>The presets as they are now.</returns>
        public IReadOnlyList<SearchPreset> Add(string name, string query)
        {
            query = query.Trim();
            name = name.Trim();

            if (name.Length == 0)
                name = query;

            var presets = Load().ToList();
            var preset = new SearchPreset(name, query);
            int existing = presets.FindIndex(p => p.Name == name);

            if (existing >= 0)
                presets[existing] = preset;
            else
                presets.Add(preset);

            write(presets);
            return presets;
        }

        /// <returns>The presets as they are now.</returns>
        public IReadOnlyList<SearchPreset> Remove(string name)
        {
            var presets = Load().Where(p => p.Name != name).ToArray();
            write(presets);
            return presets;
        }

        private void write(IEnumerable<SearchPreset> presets)
        {
            try
            {
                using (var stream = storage.CreateFileSafely(FILENAME))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(presets, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the search presets");
            }
        }
    }
}
