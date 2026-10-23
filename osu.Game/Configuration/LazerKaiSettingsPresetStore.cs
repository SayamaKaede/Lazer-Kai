// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.Extensions;
using osu.Game.IO;

namespace osu.Game.Configuration
{
    /// <summary>
    /// Where the presets a mapper saves are kept: a file each, named after the preset, so that one can be handed to
    /// somebody else by the file alone.
    /// </summary>
    public class LazerKaiSettingsPresetStore
    {
        public const string DIRECTORY = "settings-presets";

        /// <summary>
        /// The settings as they were before a preset was last applied. Named so that no preset a mapper saves can be it:
        /// a name has its leading dots taken off.
        /// </summary>
        private const string before_applying = ".before-applying";

        private readonly Storage storage;

        /// <summary>
        /// Files that could not be read and are still where they were, which a save would write over.
        /// </summary>
        private readonly HashSet<string> stuck = new HashSet<string>();

        public LazerKaiSettingsPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        /// <summary>
        /// The presets a mapper saved, by name.
        /// </summary>
        public IEnumerable<string> Names()
        {
            if (!storage.ExistsDirectory(DIRECTORY))
                return Enumerable.Empty<string>();

            return storage.GetFiles(DIRECTORY, "*.json")
                          .Select(Path.GetFileNameWithoutExtension)
                          .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith('.'))
                          .Select(n => n!)
                          .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase);
        }

        /// <summary>
        /// A name as a preset can be saved under, or an empty one if nothing of it can be.
        /// </summary>
        public static string NameFor(string name) => name.Trim().GetValidFilename().TrimStart('.').Trim();

        public LazerKaiSettingsPreset? Load(string name) => read(pathFor(name));

        public void Save(string name, LazerKaiSettingsPreset preset) => write(pathFor(name), preset);

        public void Delete(string name)
        {
            string path = pathFor(name);

            if (storage.Exists(path))
                storage.Delete(path);
        }

        public LazerKaiSettingsPreset? LoadBeforeApplying() => read(pathFor(before_applying));

        public void SaveBeforeApplying(LazerKaiSettingsPreset preset) => write(pathFor(before_applying), preset);

        private static string pathFor(string name) => Path.Combine(DIRECTORY, $"{name}.json");

        private LazerKaiSettingsPreset? read(string path)
        {
            if (!storage.Exists(path))
                return null;

            try
            {
                using (var stream = storage.GetStream(path))
                using (var reader = new StreamReader(stream))
                {
                    var values = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader.ReadToEnd());

                    if (values == null)
                        return null;

                    // a setting this version does not know, from a newer one or one since removed, is passed over.
                    return new LazerKaiSettingsPreset(values.Where(v => Enum.TryParse<OsuSetting>(v.Key, out _))
                                                            .ToDictionary(v => Enum.Parse<OsuSetting>(v.Key), v => v.Value));
                }
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not read a settings preset");

                if (!UnreadableFile.SetAside(storage, path))
                    stuck.Add(path);

                return null;
            }
        }

        private void write(string path, LazerKaiSettingsPreset preset)
        {
            try
            {
                if (stuck.Contains(path))
                    return;

                using (var stream = storage.CreateFileSafely(path))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(preset.Values.ToDictionary(v => v.Key.ToString(), v => v.Value), Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write a settings preset");
            }
        }
    }
}
