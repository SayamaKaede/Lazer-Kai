// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Screens.Edit.Setup
{
    /// <summary>
    /// Keeps the combo colour presets on disk, beside the game's other working files.
    /// </summary>
    /// <remarks>
    /// One file for all of them: a palette is picked for a song, not kept by one, so the same presets are offered
    /// whichever beatmap is open.
    /// </remarks>
    public class ComboColourPresetStore
    {
        public const string FILENAME = "combo-colour-presets.json";

        private readonly Storage storage;

        public ComboColourPresetStore(Storage storage)
        {
            this.storage = storage;
        }

        public ComboColourPresetList Load()
        {
            if (!storage.Exists(FILENAME))
                return new ComboColourPresetList();

            try
            {
                using (var stream = storage.GetStream(FILENAME))
                {
                    if (stream == null)
                        return new ComboColourPresetList();

                    using (var reader = new StreamReader(stream))
                        return JsonConvert.DeserializeObject<ComboColourPresetList>(reader.ReadToEnd()) ?? new ComboColourPresetList();
                }
            }
            catch (Exception e)
            {
                // a list that cannot be read is not worth stopping the editor for; carry on without it.
                Logger.Error(e, "Could not read the combo colour presets");
                return new ComboColourPresetList();
            }
        }

        public void Save(ComboColourPresetList list)
        {
            try
            {
                using (var stream = storage.CreateFileSafely(FILENAME))
                using (var writer = new StreamWriter(stream))
                    writer.Write(JsonConvert.SerializeObject(list, Formatting.Indented));
            }
            catch (Exception e)
            {
                Logger.Error(e, "Could not write the combo colour presets");
            }
        }
    }
}
