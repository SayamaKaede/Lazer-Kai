// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Rulesets;

namespace osu.Game.Skinning
{
    /// <summary>
    /// Puts on the skin chosen for the selected ruleset, and takes it off again for a ruleset with none.
    /// </summary>
    /// <remarks>
    /// Somebody mapping in more than one mode wants each drawn the way they play it, and changing skin by hand at every
    /// switch is the chore this saves.
    ///
    /// The skin setting keeps what it had: it is what a ruleset with no skin of its own gets, so a ruleset's skin must
    /// not be written over it. The game writes every change of skin back into that setting, and asks
    /// <see cref="Applying"/> first to leave these changes out.
    ///
    /// A skin picked while a ruleset's own is on — in the skin section, with the skin shortcuts, on any screen — replaces
    /// that ruleset's skin rather than the skin setting (<see cref="TryRecordForRuleset"/>). Written into the skin setting
    /// instead, it went on for the moment and then stayed there, with the ruleset's own still set and doing nothing,
    /// until that was cleared and set again (reported 2026-09-11). A ruleset with none of its own still takes it into
    /// the skin setting.
    /// </remarks>
    public partial class RulesetSkinSwitcher : Component
    {
        /// <summary>
        /// The setting that holds each ruleset's skin, by the ruleset's online ID.
        /// </summary>
        public static readonly IReadOnlyDictionary<int, OsuSetting> SETTINGS = new Dictionary<int, OsuSetting>
        {
            [0] = OsuSetting.RulesetSkinOsu,
            [1] = OsuSetting.RulesetSkinTaiko,
            [2] = OsuSetting.RulesetSkinCatch,
            [3] = OsuSetting.RulesetSkinMania,
        };

        /// <summary>
        /// Whether the skin is being changed by this, rather than chosen by the user.
        /// </summary>
        public bool Applying { get; private set; }

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private readonly Bindable<string> skinSetting = new Bindable<string>();
        private readonly Dictionary<int, Bindable<string>> rulesetSkins = new Dictionary<int, Bindable<string>>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.Skin, skinSetting);

            foreach (var (onlineId, setting) in SETTINGS)
                rulesetSkins[onlineId] = config.GetBindable<string>(setting);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ruleset.BindValueChanged(_ => apply());

            foreach (var setting in rulesetSkins.Values)
                setting.BindValueChanged(_ => apply());

            apply();
        }

        /// <summary>
        /// Takes a skin the user picked as the selected ruleset's own, if that ruleset has one.
        /// </summary>
        /// <returns>Whether it was taken. If not, it belongs to the skin setting.</returns>
        public bool TryRecordForRuleset(string skinId)
        {
            // the game's first skin is put on from the setting before this has loaded, and belongs to the setting anyway.
            if (!IsLoaded)
                return false;

            if (chosenForRuleset() == null)
                return false;

            // the random entry is only a way to a real skin: picking it is followed at once by the skin it lands on,
            // which is what gets recorded. Recorded itself, it would read as a skin that is gone.
            if (skinId == SkinInfo.RANDOM_SKIN.ToString())
                return true;

            rulesetSkins[ruleset.Value.OnlineID].Value = skinId;
            return true;
        }

        private void apply()
        {
            string target = chosenForRuleset() ?? skinSetting.Value;

            if (skins.CurrentSkinInfo.Value.ID.ToString() == target)
                return;

            Applying = true;

            try
            {
                skins.SetSkinFromConfiguration(target);
            }
            finally
            {
                Applying = false;
            }
        }

        /// <summary>
        /// The skin chosen for the selected ruleset, or <c>null</c> if it has none that can be put on.
        /// </summary>
        private string? chosenForRuleset()
        {
            if (!rulesetSkins.TryGetValue(ruleset.Value.OnlineID, out var setting))
                return null;

            // one deleted since it was chosen is as good as none. Handed on regardless, the skin manager would put
            // triangles on in its place — a skin nobody chose, where the skin setting's is what "none" promises.
            if (!Guid.TryParse(setting.Value, out var id) || id == SkinInfo.RANDOM_SKIN || skins.GetAllUsableSkins().All(s => s.ID != id))
                return null;

            return setting.Value;
        }
    }
}
