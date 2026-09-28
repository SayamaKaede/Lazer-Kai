// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Beatmaps;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// The mods set on a difficulty, on its panel in song select.
    /// </summary>
    public partial class DifficultyModIcons : FillFlowContainer<ModIcon>
    {
        [Resolved]
        private DifficultyModStore? store { get; set; }

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        private BeatmapInfo? beatmap;

        public BeatmapInfo? Beatmap
        {
            get => beatmap;
            set
            {
                beatmap = value;

                if (IsLoaded)
                    updateIcons();
            }
        }

        public DifficultyModIcons()
        {
            AutoSizeAxes = Axes.Both;
            Direction = FillDirection.Horizontal;
            Spacing = new Vector2(-10, 0);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (store != null)
                store.Changed += onChanged;

            ruleset.BindValueChanged(_ => updateIcons(), true);
        }

        private void onChanged() => Schedule(updateIcons);

        private void updateIcons()
        {
            var mods = beatmap == null ? null : store?.Get(beatmap, ruleset.Value.CreateInstance());

            ChildrenEnumerable = (mods ?? Array.Empty<Mod>()).AsOrdered().Select(mod => new ModIcon(mod)
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Scale = new Vector2(0.3f),
                // trim mod icon height down to its true height for alignment purposes.
                Height = ModIcon.MOD_ICON_SIZE.Y * 3 / 4f,
            });
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (store != null)
                store.Changed -= onChanged;
        }
    }
}
