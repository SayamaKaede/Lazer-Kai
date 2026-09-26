// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Rulesets.Scoring;
using osuTK.Graphics;

namespace osu.Game.Screens.Play.PlayerSettings
{
    /// <summary>
    /// The objects a replay missed, broke the combo on, or hit for a 100 or a 50, to go to each by clicking it.
    /// </summary>
    public partial class ReplayMissList : PlayerSettingsGroup
    {
        /// <summary>
        /// Goes to a miss that was clicked.
        /// </summary>
        public Action<ReplayMiss>? GoTo;

        private readonly ReplayJudgementSimulation simulation;

        private OsuTextFlowContainer status = null!;
        private FillFlowContainer<Row> rows = null!;
        private bool listed;

        public ReplayMissList(ReplayJudgementSimulation simulation)
            : base(LazerKaiReplayStrings.Misses)
        {
            this.simulation = simulation;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Children = new Drawable[]
            {
                status = new OsuTextFlowContainer(s => s.Font = OsuFont.GetFont(size: 14))
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Text = LazerKaiReplayStrings.Judging,
                },
                rows = new FillFlowContainer<Row>
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                },
            };
        }

        // only updated while the settings are out: the rows are made the first time they are, if the replay has been
        // judged by then.
        protected override void Update()
        {
            base.Update();

            if (listed || simulation.Misses is not IReadOnlyList<ReplayMiss> misses)
                return;

            listed = true;

            if (misses.Count == 0)
                status.Text = LazerKaiReplayStrings.NoMisses;
            else
                status.Hide();

            rows.ChildrenEnumerable = misses.Select(m => new Row(m) { Action = () => GoTo?.Invoke(m) });
        }

        public partial class Row : OsuClickableContainer
        {
            public readonly ReplayMiss Miss;

            private Box hover = null!;

            public Row(ReplayMiss miss)
            {
                Miss = miss;

                RelativeSizeAxes = Axes.X;
                Height = 20;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                var font = OsuFont.GetFont(size: 14, fixedWidth: true);

                Children = new Drawable[]
                {
                    hover = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.White,
                        Alpha = 0,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 4,
                        Font = font,
                        Text = Miss.Time.ToEditorFormattedString(),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        X = 90,
                        Font = OsuFont.GetFont(size: 14, weight: FontWeight.SemiBold),
                        Text = KindText(Miss.Kind),
                        Colour = KindColour(Miss.Kind, colours),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        X = -4,
                        Font = font,
                        Text = OffsetText(Miss),
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.FadeTo(0.1f, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.FadeOut(100);
                base.OnHoverLost(e);
            }
        }

        public static LocalisableString KindText(ReplayMissKind kind)
        {
            switch (kind)
            {
                case ReplayMissKind.Miss:
                    return LazerKaiReplayStrings.Miss;

                case ReplayMissKind.Break:
                    return LazerKaiReplayStrings.SliderBreak;

                case ReplayMissKind.Meh:
                    return @"50";

                default:
                    return @"100";
            }
        }

        public static Color4 KindColour(ReplayMissKind kind, OsuColour colours)
        {
            switch (kind)
            {
                case ReplayMissKind.Miss:
                    return colours.ForHitResult(HitResult.Miss);

                case ReplayMissKind.Break:
                    return colours.Pink;

                case ReplayMissKind.Meh:
                    return colours.ForHitResult(HitResult.Meh);

                default:
                    return colours.ForHitResult(HitResult.Ok);
            }
        }

        /// <summary>
        /// How early (below zero, as the hit error meter has it) or late it was hit, if it was hit at a time that can be.
        /// </summary>
        public static string OffsetText(ReplayMiss miss) => miss.Offset is double offset ? $@"{offset:+0;-0;0}ms" : string.Empty;
    }
}
