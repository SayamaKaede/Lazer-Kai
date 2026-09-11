// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Rulesets;
using osu.Game.Skinning;
using Realms;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// A skin for each of the four rulesets, put on whenever that ruleset is selected. See <see cref="RulesetSkinSwitcher"/>.
    /// </summary>
    public partial class RulesetSkinSettings : LazerKaiSubsection
    {
        protected override LocalisableString Header => LazerKaiSettingsStrings.RulesetSkinHeader;

        private readonly List<RulesetSkinDropdown> dropdowns = new List<RulesetSkinDropdown>();

        [Resolved]
        private SkinManager skins { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private IDisposable? realmSubscription;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, RulesetStore rulesets)
        {
            foreach (var (onlineId, setting) in RulesetSkinSwitcher.SETTINGS)
            {
                var dropdown = new RulesetSkinDropdown
                {
                    AlwaysShowSearchBar = true,
                    AllowNonContiguousMatching = true,
                    Caption = rulesets.GetRuleset(onlineId)!.Name,
                    HintText = LazerKaiSettingsStrings.RulesetSkinDescription,
                    Current = config.GetBindable<string>(setting),
                };

                dropdowns.Add(dropdown);
                Add(new SettingsItemV2(dropdown));
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            realmSubscription = realm.RegisterForNotifications(_ => realm.Realm.All<SkinInfo>().Where(s => !s.DeletePending), skinsChanged);
        }

        private void skinsChanged(IRealmCollection<SkinInfo> sender, ChangeSet? changes)
        {
            // realm recycling, the same as the skin section reads it: the protected skins are otherwise always there.
            if (!sender.Any())
                return;

            // the random entry is a choice made at the moment of choosing, which a ruleset's skin cannot be.
            var names = skins.GetAllUsableSkins()
                             .Where(s => s.ID != SkinInfo.RANDOM_SKIN)
                             .ToDictionary(s => s.ID.ToString(), s => s.PerformRead(info => info.ToString()));

            var items = names.Keys.Prepend(string.Empty).ToList();

            Schedule(() =>
            {
                foreach (var dropdown in dropdowns)
                {
                    // names first: the items are drawn with them the moment they arrive.
                    dropdown.Names = names;
                    dropdown.Items = items;
                }
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            realmSubscription?.Dispose();
        }

        private partial class RulesetSkinDropdown : FormDropdown<string>
        {
            public IReadOnlyDictionary<string, string> Names { get; set; } = new Dictionary<string, string>();

            // a skin deleted since it was chosen reads as none, since none is what the ruleset then gets.
            protected override LocalisableString GenerateItemText(string item)
                => Names.TryGetValue(item, out string? name) ? name : LazerKaiSettingsStrings.RulesetSkinNotSet;
        }
    }
}
