// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Screens.Select.Filter;

namespace osu.Game.Screens.Select
{
    /// <summary>
    /// How long ago a difficulty was last saved in the editor, on a panel listed by <see cref="SortMode.DateUpdated"/> (2026-10-19).
    /// Hidden under any other sort, and for a difficulty never saved here.
    /// </summary>
    public partial class PanelSaveTime : DrawableDate
    {
        private readonly Bindable<SortMode> sort = new Bindable<SortMode>();

        private BeatmapInfo? saved;

        public PanelSaveTime()
            // not the default date: that is the earliest there is, and moving it into a time zone behind UTC throws.
            : base(DateTimeOffset.Now, italic: false)
        {
            Font = OsuFont.Style.Caption1.With(weight: FontWeight.SemiBold);
        }

        /// <summary>
        /// The difficulty to say was saved, or null for none.
        /// </summary>
        public BeatmapInfo? Saved
        {
            set
            {
                saved = value;

                if (value?.LastLocalUpdate != null)
                    Date = value.LastLocalUpdate.Value;

                updateVisibility();
            }
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.SongSelectSortingMode, sort);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            sort.BindValueChanged(_ => updateVisibility(), true);
        }

        private void updateVisibility() => Alpha = sort.Value == SortMode.DateUpdated && saved?.LastLocalUpdate != null ? 1 : 0;
    }
}
