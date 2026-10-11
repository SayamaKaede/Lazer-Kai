// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mania.Beatmaps;
using osu.Game.Rulesets.Mania.UI;
using System.Collections.Generic;

namespace osu.Game.Rulesets.Mania.Edit
{
    public partial class ManiaEditorPlayfield : ManiaPlayfield
    {
        public ManiaEditorPlayfield(List<StageDefinition> stages)
            : base(stages)
        {
        }

        protected override Stage CreateStage(int firstColumnIndex, StageDefinition stageDefinition, ref ManiaAction columnAction)
            => new EditorStage(firstColumnIndex, stageDefinition, ref columnAction);

        /// <summary>
        /// How far apart the stages are put, in pixels.
        /// </summary>
        private readonly BindableFloat stageSpacing = new BindableFloat();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorManiaStageSpacing, stageSpacing);
        }

        protected override void UpdateAfterChildren()
        {
            base.UpdateAfterChildren();

            bringStagesTogether();
        }

        /// <summary>
        /// Puts the stages side by side in the middle, <see cref="stageSpacing"/> apart.
        /// </summary>
        /// <remarks>
        /// The playfield gives each stage an equal share of its width and puts the stage in the middle of it. Two stages
        /// are then as far apart as the playfield is wider than they are, which with most skins is a wide gap to work
        /// across: lanes that belong side by side, as the lanes of a difficulty made to hitsound in do (2026-10-12).
        /// </remarks>
        private void bringStagesTogether()
        {
            if (Stages.Count < 2)
                return;

            float total = stageSpacing.Value * (Stages.Count - 1);

            foreach (var stage in Stages)
                total += stage.DrawWidth;

            float share = DrawWidth / Stages.Count;
            float left = (DrawWidth - total) / 2;

            for (int i = 0; i < Stages.Count; i++)
            {
                var stage = Stages[i];

                // where the middle of its share is, which is where it is with no offset.
                float middle = share * (i + 0.5f);
                float x = left + stage.DrawWidth / 2 - middle;

                if (stage.X != x)
                    stage.X = x;

                left += stage.DrawWidth + stageSpacing.Value;
            }
        }
    }
}
