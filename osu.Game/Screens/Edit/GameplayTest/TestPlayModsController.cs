// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.GameplayTest
{
    /// <summary>
    /// Holds the set of mods to apply when entering gameplay test from the editor.
    /// The selection is persisted per ruleset so it survives across editor sessions.
    /// </summary>
    /// <remarks>
    /// Choosing the mods is left to <see cref="EditorModSelectOverlay"/>, which writes into <see cref="Mods"/>.
    /// Because the store is a list of <see cref="APIMod"/>, each mod's settings are carried along with it.
    /// </remarks>
    public class TestPlayModsController
    {
        /// <summary>
        /// The mods to apply when entering gameplay test. Writes are persisted.
        /// </summary>
        public Bindable<IReadOnlyList<Mod>> Mods => mods;

        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        private readonly Bindable<string> persistedMods;
        private readonly string rulesetShortName;

        public TestPlayModsController(OsuConfigManager config, RulesetInfo rulesetInfo)
        {
            rulesetShortName = rulesetInfo.ShortName;
            persistedMods = config.GetBindable<string>(OsuSetting.EditorTestPlayMods);

            mods.Value = readPersistedMods(rulesetInfo.CreateInstance());
            mods.BindValueChanged(_ => writePersistedMods(), true);
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void DeselectAll() => mods.Value = Array.Empty<Mod>();

        private IReadOnlyList<Mod> readPersistedMods(Ruleset ruleset)
        {
            if (!readAllPersistedMods().TryGetValue(rulesetShortName, out var apiMods))
                return Array.Empty<Mod>();

            var result = apiMods.Select(m => m.ToMod(ruleset)).Where(m => m is not UnknownMod).ToList();

            // guard against a selection which was made invalid by a mod compatibility change since it was saved.
            return ModUtils.CheckCompatibleSet(result) ? result : Array.Empty<Mod>();
        }

        private void writePersistedMods()
        {
            var all = readAllPersistedMods();

            all[rulesetShortName] = mods.Value.Select(m => new APIMod(m)).ToList();

            persistedMods.Value = JsonConvert.SerializeObject(all);
        }

        private Dictionary<string, List<APIMod>> readAllPersistedMods()
        {
            if (string.IsNullOrEmpty(persistedMods.Value))
                return new Dictionary<string, List<APIMod>>();

            try
            {
                return JsonConvert.DeserializeObject<Dictionary<string, List<APIMod>>>(persistedMods.Value) ?? new Dictionary<string, List<APIMod>>();
            }
            catch (Exception e)
            {
                Logger.Log($"Failed to read editor test play mods ({e.Message}); resetting to empty.");
                return new Dictionary<string, List<APIMod>>();
            }
        }
    }
}
