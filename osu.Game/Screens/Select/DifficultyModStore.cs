// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Beatmaps;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// Mods set on a difficulty from song select, put on whenever that difficulty is selected: a tournament pool fixes
    /// the mods each of its maps is played with, and putting them on by hand for every map is what this saves.
    /// </summary>
    public class DifficultyModStore
    {
        public const string FILENAME = "difficulty-mods.json";

        /// <summary>
        /// Invoked when mods are set on a difficulty or taken off it.
        /// </summary>
        public event Action? Changed;

        private readonly Storage storage;
        private readonly Dictionary<string, Entry> entries;

        public DifficultyModStore(Storage storage)
        {
            this.storage = storage;
            entries = load();
        }

        /// <returns>The mods set on <paramref name="beatmap"/>, or null if none are set for <paramref name="ruleset"/>.</returns>
        public Mod[]? Get(BeatmapInfo beatmap, Ruleset ruleset)
        {
            if (!entries.TryGetValue(keyOf(beatmap), out var entry) || entry.Ruleset != ruleset.ShortName)
                return null;

            return entry.Mods.Select(m => m.ToMod(ruleset)).ToArray();
        }

        public void Set(BeatmapInfo beatmap, Ruleset ruleset, IEnumerable<Mod> mods)
        {
            entries[keyOf(beatmap)] = new Entry(ruleset.ShortName, mods.Select(m => new APIMod(m)).ToArray());
            write();
            Changed?.Invoke();
        }

        public void Clear(BeatmapInfo beatmap)
        {
            if (!entries.Remove(keyOf(beatmap)))
                return;

            write();
            Changed?.Invoke();
        }

        // an update from the website imports the set anew, and every difficulty in it gets a new ID; the online one stays.
        private static string keyOf(BeatmapInfo beatmap) => beatmap.OnlineID > 0 ? $"online:{beatmap.OnlineID}" : beatmap.ID.ToString();

        private Dictionary<string, Entry> load()
        {
            if (!storage.Exists(FILENAME))
                return new Dictionary<string, Entry>();

            try
            {
                using (var stream = storage.GetStream(FILENAME))
                using (var reader = new StreamReader(stream))
                    return JsonConvert.DeserializeObject<Dictionary<string, Entry>>(reader.ReadToEnd()) ?? new Dictionary<string, Entry>();
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read the mods set on difficulties");
                return new Dictionary<string, Entry>();
            }
        }

        private void write()
        {
            try
            {
                using (var stream = storage.CreateFileSafely(FILENAME))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(entries, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the mods set on difficulties");
            }
        }

        private record Entry(string Ruleset, APIMod[] Mods);
    }
}
