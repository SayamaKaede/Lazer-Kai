// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// Marks the times nightcore plays its cymbal at.
    /// </summary>
    /// <remarks>
    /// Mapping to nightcore means writing around a rhythm the beatmap does not contain, and the cymbal is the loudest
    /// part of it. The mod's own rule is followed (<see cref="osu.Game.Rulesets.Mods.ModNightcore{TObject}"/>): counting
    /// from a timing point, the cymbal falls every four bars, and counting starts again at the next timing point. A
    /// timing point that omits its first bar line has no cymbal on it, the same way it has none in gameplay.
    /// </remarks>
    public partial class TimelineNightcoreCymbalDisplay : TimelinePart<TimelineNightcoreCymbalDisplay.CymbalPiece>
    {
        /// <summary>How many bars apart the cymbal falls, which is the mod's own segment length.</summary>
        private const int bars_per_cymbal = 4;

        [Resolved]
        private Timeline timeline { get; set; } = null!;

        /// <summary>
        /// Whether this timeline marks the cymbals at all. Only the timing screen does: the compose screen has the
        /// objects on its timeline, and the marks would sit over them.
        /// </summary>
        public bool Enabled { get; set; }

        private readonly Bindable<bool> visible = new Bindable<bool>();

        private readonly Cached cymbalCache = new Cached();

        private ControlPointInfo controlPointInfo = null!;

        private (double min, double max) visibleRange = (double.MinValue, double.MaxValue);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorTimelineShowNightcoreCymbals, visible);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            visible.BindValueChanged(v =>
            {
                Alpha = Enabled && v.NewValue ? 1 : 0;
                cymbalCache.Invalidate();
            }, true);
        }

        protected override void LoadBeatmap(EditorBeatmap beatmap)
        {
            base.LoadBeatmap(beatmap);

            beatmap.ControlPointInfo.ControlPointsChanged += () => cymbalCache.Invalidate();
            controlPointInfo = beatmap.ControlPointInfo;
        }

        protected override void Update()
        {
            base.Update();

            if (!Enabled || !visible.Value || DrawWidth <= 0)
                return;

            (double, double) newRange = (
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopLeft).X - CymbalPiece.WIDTH) / DrawWidth * Content.RelativeChildSize.X,
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopRight).X + CymbalPiece.WIDTH) / DrawWidth * Content.RelativeChildSize.X);

            if (visibleRange != newRange)
            {
                visibleRange = newRange;
                cymbalCache.Invalidate();
            }

            if (!cymbalCache.IsValid)
            {
                recreatePieces();
                cymbalCache.Validate();
            }
        }

        private void recreatePieces()
        {
            Clear();

            if (!Enabled || !visible.Value)
                return;

            foreach (double time in CymbalTimes(controlPointInfo, visibleRange.min, visibleRange.max))
                Add(new CymbalPiece { X = (float)time });
        }

        /// <summary>
        /// The times nightcore plays its cymbal at, within the given range.
        /// </summary>
        public static IEnumerable<double> CymbalTimes(ControlPointInfo controlPointInfo, double start, double end)
        {
            var timingPoints = controlPointInfo.TimingPoints;

            for (int i = 0; i < timingPoints.Count; i++)
            {
                var timingPoint = timingPoints[i];

                // a timing point only counts up to the next one, which starts the count again.
                double until = i + 1 < timingPoints.Count ? Math.Min(end, timingPoints[i + 1].Time) : end;
                double segment = timingPoint.BeatLength * timingPoint.TimeSignature.Numerator * bars_per_cymbal;

                if (segment <= 0)
                    continue;

                // the first cymbal of a timing point is the timing point itself, unless it omits its first bar line.
                int first = timingPoint.OmitFirstBarLine ? 1 : 0;

                if (start > timingPoint.Time)
                    first = Math.Max(first, (int)Math.Ceiling((start - timingPoint.Time) / segment));

                for (int cymbal = first; ; cymbal++)
                {
                    double time = timingPoint.Time + cymbal * segment;

                    if (time > until)
                        break;

                    yield return time;
                }
            }
        }

        public partial class CymbalPiece : CompositeDrawable
        {
            /// <summary>Most of the timeline's height: the timing screen shows no objects for it to sit clear of.</summary>
            public const float WIDTH = 56;

            public CymbalPiece()
            {
                RelativePositionAxes = Axes.X;
                Size = new osuTK.Vector2(WIDTH);

                Anchor = Anchor.CentreLeft;
                Origin = Anchor.Centre;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours, OverlayColourProvider colourProvider)
            {
                InternalChildren = new Drawable[]
                {
                    // the ticks run right through the mark, so it is given a ground of its own to be read against.
                    new Circle
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background4,
                        Alpha = 0.8f,
                    },
                    new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Size = new osuTK.Vector2(0.72f),
                        Icon = OsuIcon.EditorFinish,
                        // red, the way a timing point is red: this is a rhythm the timing points decide.
                        Colour = colours.Red1,
                    },
                };
            }
        }
    }
}
