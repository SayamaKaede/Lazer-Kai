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
    /// <see cref="Applying"/> first to leave these changes out. A skin picked by hand while a ruleset's own is on is
    /// still written back, so it becomes the skin for the rulesets without one.
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
            if (!Guid.TryParse(setting.Value, out var id) || skins.GetAllUsableSkins().All(s => s.ID != id))
                return null;

            return setting.Value;
        }
    }
}
