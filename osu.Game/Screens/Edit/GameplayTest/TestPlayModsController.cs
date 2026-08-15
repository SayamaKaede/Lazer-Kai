// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using Humanizer;
using Newtonsoft.Json;
using osu.Framework.Bindables;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Edit.Components.Menus;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.GameplayTest
{
    /// <summary>
    /// Holds the set of mods to apply when entering gameplay test from the editor, and exposes a menu for toggling them.
    /// The selection is persisted per ruleset so it survives across editor sessions.
    /// </summary>
    public class TestPlayModsController
    {
        /// <summary>
        /// Mods menu item (with submenus containing all mods applicable to the current ruleset).
        /// Should be added to the <see cref="Editor"/>'s global menu.
        /// </summary>
        public EditorMenuItem Menu { get; }

        /// <summary>
        /// The mods to apply when entering gameplay test. Writes are persisted and reflected in <see cref="Menu"/>.
        /// </summary>
        public Bindable<IReadOnlyList<Mod>> Mods => mods;

        private readonly Bindable<IReadOnlyList<Mod>> mods = new Bindable<IReadOnlyList<Mod>>(Array.Empty<Mod>());

        private readonly Bindable<string> persistedMods;
        private readonly string rulesetShortName;

        private readonly Dictionary<Type, ToggleMenuItem> toggles = new Dictionary<Type, ToggleMenuItem>();

        /// <summary>
        /// Guards against <see cref="setModState"/> being re-entered while toggle states are being synchronised with <see cref="mods"/>.
        /// </summary>
        private bool synchronisingToggles;

        public TestPlayModsController(OsuConfigManager config, RulesetInfo rulesetInfo)
        {
            rulesetShortName = rulesetInfo.ShortName;
            persistedMods = config.GetBindable<string>(OsuSetting.EditorTestPlayMods);

            var ruleset = rulesetInfo.CreateInstance();

            var typeMenuItems = new List<MenuItem>();

            foreach (var group in ruleset.CreateAllMods()
                                         .Where(m => m.HasImplementation && m.UserPlayable)
                                         .GroupBy(m => m.Type))
            {
                var modMenuItems = new List<MenuItem>();

                foreach (var mod in group)
                {
                    var toggle = new ToggleMenuItem($"{mod.Name} ({mod.Acronym})");

                    // capturing `mod` here is safe: it is only ever used as a template to clone from.
                    toggle.State.BindValueChanged(state => setModState(mod, state.NewValue));

                    toggles[mod.GetType()] = toggle;
                    modMenuItems.Add(toggle);
                }

                typeMenuItems.Add(new MenuItem(group.Key.Humanize(LetterCasing.Title)) { Items = modMenuItems });
            }

            Menu = new EditorMenuItem(EditorStrings.TestPlayMods) { Items = typeMenuItems };

            mods.Value = readPersistedMods(ruleset);
            mods.BindValueChanged(_ =>
            {
                synchroniseToggles();
                writePersistedMods();
            }, true);
        }

        /// <summary>
        /// Clears the current selection.
        /// </summary>
        public void DeselectAll() => mods.Value = Array.Empty<Mod>();

        private void setModState(Mod mod, bool enabled)
        {
            if (synchronisingToggles)
                return;

            var newSelection = mods.Value.Where(m => m.GetType() != mod.GetType()).ToList();

            if (enabled)
            {
                // deselect anything the newly enabled mod cannot be played alongside, mirroring how mod selection behaves elsewhere.
                newSelection.RemoveAll(m => !ModUtils.CheckCompatibleSet(new[] { m, mod }));
                newSelection.Add(mod.DeepClone());
            }

            mods.Value = newSelection;
        }

        private void synchroniseToggles()
        {
            synchronisingToggles = true;

            foreach ((var type, var toggle) in toggles)
                toggle.State.Value = mods.Value.Any(m => m.GetType() == type);

            synchronisingToggles = false;
        }

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
