// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Configuration;
using osu.Game.Skinning;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// A <see cref="SkinProvidingContainer"/> that fires <see cref="ISkinSource.SourceChanged"/> when users have made a change to the beatmap skin
    /// of the map being edited.
    /// </summary>
    public partial class EditorSkinProvidingContainer : RulesetSkinProvidingContainer
    {
        private readonly EditorBeatmapSkin? beatmapSkin;

        public EditorSkinProvidingContainer(EditorBeatmap editorBeatmap)
            : base(editorBeatmap.PlayableBeatmap.BeatmapInfo.Ruleset.CreateInstance(), editorBeatmap.PlayableBeatmap, editorBeatmap.BeatmapSkin?.Skin)
        {
            beatmapSkin = editorBeatmap.BeatmapSkin;
        }

        /// <summary>
        /// The editor forces the beatmap's own colours and hitsounds on, where gameplay lets them be turned off.
        /// These put the same two switches on the editor, on their own settings so that turning them off while
        /// mapping does not also change how the beatmap plays.
        /// </summary>
        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorBeatmapColours, BeatmapColours);
            config.BindWith(OsuSetting.EditorBeatmapHitsounds, BeatmapHitsounds);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (beatmapSkin != null)
                beatmapSkin.BeatmapSkinChanged += triggerSourceChanged;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (beatmapSkin != null)
                beatmapSkin.BeatmapSkinChanged -= triggerSourceChanged;
        }

        private void triggerSourceChanged() => Schedule(TriggerSourceChanged);
    }
}
