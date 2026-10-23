// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Game.IO;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Keeps the presets of lane sounds on disk, beside the game's other working files.
    /// </summary>
    /// <remarks>
    /// One file for all of them, as the combo colour presets have: the sounds a mapper hitsounds with are the same from
    /// one beatmap to the next, which is what a preset is for.
    /// </remarks>
    public class LaneSoundPresetStore
    {
        public const string FILENAME = "hitsound-lane-presets.json";

        private readonly Storage storage;

        /// <summary>
        /// Whether the file could not be read and is still where it was, so that a save would write over it.
        /// </summary>
        private bool stuck;

        public LaneSoundPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        public LaneSoundPresetList Load()
        {
            if (!storage.Exists(FILENAME))
                return new LaneSoundPresetList();

            try
            {
                using (var stream = storage.GetStream(FILENAME))
                {
                    if (stream == null)
                        return new LaneSoundPresetList();

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<LaneSoundPresetList>(reader.ReadToEnd()) ?? new LaneSoundPresetList();
                }
            }
            catch (Exception e)
            {
                // a list that cannot be read is not worth stopping the editor for; carry on without it.
                Logger.Error(e, "Could not read the presets of lane sounds");
                stuck = !UnreadableFile.SetAside(storage, FILENAME);
                return new LaneSoundPresetList();
            }
        }

        public void Save(LaneSoundPresetList list)
        {
            try
            {
                if (stuck)
                    return;

                using (var stream = storage.CreateFileSafely(FILENAME))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the presets of lane sounds");
            }
        }
    }
}
