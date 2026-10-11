// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics.Containers;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Mania.UI;
using osu.Game.Screens.Edit.Compose.Components;

namespace osu.Game.Rulesets.Mania.Edit
{
    public partial class ManiaBeatSnapGrid : BeatSnapGrid
    {
        private ManiaPlayfield? playfield;

        private readonly BindableFloat thickness = new BindableFloat();
        private readonly BindableFloat opacity = new BindableFloat();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorManiaTickThickness, thickness);
            config.BindWith(OsuSetting.EditorManiaTickOpacity, opacity);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            thickness.BindValueChanged(t => LineThickness = t.NewValue, true);
            opacity.BindValueChanged(o => LineOpacity = o.NewValue, true);
        }

        /// <remarks>
        /// Across each stage, where its bar lines are drawn: under the notes, above and below the judgement line. The
        /// lines were one a lane, in each lane's own underlay, which is as many times the lines as there are lanes; drawn
        /// all the way along at all times, that is eighteen times too many of them (2026-10-12).
        /// </remarks>
        protected override IEnumerable<Container> GetTargetContainers(HitObjectComposer composer)
        {
            playfield = (ManiaPlayfield)composer.Playfield;

            return playfield.Stages.Select(stage => (Container)stage.HitObjectContainer.Parent!);
        }

        /// <summary>
        /// The ticks are drawn all the way along the lanes at all times, as in stable's editor, rather than near what is
        /// selected or the cursor only.
        /// </summary>
        protected override double? AlwaysShownTimeSpan => playfield?.Stages.FirstOrDefault()?.ScrollingInfo.TimeRange.Value;
    }
}
