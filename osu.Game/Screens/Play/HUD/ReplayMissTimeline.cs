// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Play.PlayerSettings;
using osuTK.Graphics;

namespace osu.Game.Screens.Play.HUD
{
    /// <summary>
    /// What a replay missed, laid out along the bottom over the length of the beatmap, to go to each by clicking it.
    /// </summary>
    public partial class ReplayMissTimeline : CompositeDrawable
    {
        /// <summary>
        /// Goes to a miss that was clicked.
        /// </summary>
        public Action<ReplayMiss>? GoTo;

        private readonly ReplayJudgementSimulation simulation;
        private readonly double startTime;
        private readonly double endTime;

        private readonly Bindable<bool> shown = new Bindable<bool>();

        private OsuSpriteText status = null!;
        private Container<Marker> markers = null!;
        private Box playhead = null!;
        private bool marked;

        [Resolved]
        private IGameplayClock gameplayClock { get; set; } = null!;

        public ReplayMissTimeline(ReplayJudgementSimulation simulation, IBeatmap beatmap)
        {
            this.simulation = simulation;

            startTime = beatmap.HitObjects.Count == 0 ? 0 : beatmap.HitObjects.Min(h => h.StartTime);
            endTime = beatmap.HitObjects.Count == 0 ? 0 : beatmap.HitObjects.Max(h => h.GetEndTime());

            Anchor = Anchor.BottomCentre;
            Origin = Anchor.BottomCentre;
            RelativeSizeAxes = Axes.X;
            // settled by trying them in the game.
            Width = 0.75f;
            Height = 55;
            Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            InternalChildren = new Drawable[]
            {
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    CornerRadius = 4,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.5f,
                    },
                },
                status = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.GetFont(size: 14),
                    Text = LazerKaiReplayStrings.Judging,
                },
                markers = new Container<Marker>
                {
                    RelativeSizeAxes = Axes.Both,
                },
                playhead = new Box
                {
                    RelativePositionAxes = Axes.X,
                    RelativeSizeAxes = Axes.Y,
                    Origin = Anchor.TopCentre,
                    Width = 2,
                    Colour = Color4.White,
                },
            };

            config.BindWith(OsuSetting.ReplayShowMissTimeline, shown);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            shown.BindValueChanged(s => this.FadeTo(s.NewValue ? 1 : 0, 200, Easing.OutQuint), true);
            FinishTransforms();
        }

        // only updated while shown: the markers are put in the first time it is, if the replay has been judged by then.
        protected override void Update()
        {
            base.Update();

            playhead.X = positionOf(gameplayClock.CurrentTime);

            if (marked || simulation.Misses is not IReadOnlyList<ReplayMiss> misses)
                return;

            marked = true;

            if (misses.Count == 0)
                status.Text = LazerKaiReplayStrings.NoMisses;
            else
                status.Hide();

            markers.ChildrenEnumerable = misses.Select(m => new Marker(m)
            {
                X = positionOf(m.Time),
                Action = () => GoTo?.Invoke(m),
            });
        }

        private float positionOf(double time) => endTime > startTime ? (float)Math.Clamp((time - startTime) / (endTime - startTime), 0, 1) : 0;

        public partial class Marker : OsuClickableContainer
        {
            public readonly ReplayMiss Miss;

            private Box line = null!;

            public Marker(ReplayMiss miss)
            {
                Miss = miss;

                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;
                Origin = Anchor.TopCentre;
                // wider than it is drawn, to be easier to point at.
                Width = 9;

                // misses in front of breaks, in front of 50s, in front of 100s.
                Depth = (float)miss.Kind;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                bool counted = Miss.Kind == ReplayMissKind.Miss || Miss.Kind == ReplayMissKind.Break;

                Child = line = new Box
                {
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    RelativeSizeAxes = Axes.Y,
                    // a combo lost stands full height; a 100 or a 50 half.
                    Height = counted ? 1 : 0.5f,
                    Width = 3,
                    Colour = ReplayMissList.KindColour(Miss.Kind, colours),
                };

                TooltipText = LocalisableString.Interpolate($@"{Miss.Time.ToEditorFormattedString()}  {ReplayMissList.KindText(Miss.Kind)}  {ReplayMissList.OffsetText(Miss)}");
            }

            protected override bool OnHover(HoverEvent e)
            {
                line.ResizeWidthTo(5, 100);
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                line.ResizeWidthTo(3, 100);
                base.OnHoverLost(e);
            }
        }
    }
}
