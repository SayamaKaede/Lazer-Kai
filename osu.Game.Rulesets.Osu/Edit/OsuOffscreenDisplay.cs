// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Outlines the area objects must stay inside to remain visible during gameplay, and marks the objects which
    /// currently break out of it.
    /// </summary>
    /// <remarks>
    /// The bounds and the offscreen test are shared with <see cref="CheckOffscreenObjects"/>, so what is drawn here
    /// always agrees with what beatmap verification reports.
    /// </remarks>
    public partial class OsuOffscreenDisplay : CompositeDrawable
    {
        /// <summary>
        /// How far outside an object's own radius the warning ring is drawn.
        /// </summary>
        private const float ring_padding = 6;

        private const float ring_thickness = 3;

        /// <summary>
        /// Matches the step <see cref="CheckOffscreenObjects"/> walks slider paths with.
        /// </summary>
        private const int path_step_size = 5;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private Container<Drawable> markers = null!;
        private Container playAreaBorder = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, OsuColour colours)
        {
            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                playAreaBorder = new Container
                {
                    Masking = true,
                    BorderThickness = 2,
                    // matches the composer's grid rather than the warning colour: this is a reference line, not a problem.
                    BorderColour = Color4.White,
                    Alpha = 0.35f,
                    // positioned in playfield coordinates, which is what the bounds are expressed in.
                    Position = new Vector2(CheckOffscreenObjects.MIN_X, CheckOffscreenObjects.MIN_Y),
                    Size = new Vector2(CheckOffscreenObjects.MAX_X - CheckOffscreenObjects.MIN_X,
                        CheckOffscreenObjects.MAX_Y - CheckOffscreenObjects.MIN_Y),
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        AlwaysPresent = true,
                        Alpha = 0,
                    }
                },
                markers = new Container { RelativeSizeAxes = Axes.Both },
            };

            config.BindWith(OsuSetting.EditorShowOffscreenObjects, enabled);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            editorBeatmap.BeatmapReprocessed += refresh;
            enabled.BindValueChanged(_ => refresh(), true);
        }

        private void refresh() => Scheduler.AddOnce(() =>
        {
            markers.Clear();
            playAreaBorder.Alpha = enabled.Value ? 0.35f : 0;

            if (!enabled.Value)
                return;

            foreach (var hitObject in editorBeatmap.HitObjects.OfType<OsuHitObject>())
            {
                switch (hitObject)
                {
                    case Slider slider:
                        // the whole slider is outlined, not just the end which happens to be out. Marking one end
                        // alone reads as if only that end were the problem.
                        if (isOffscreen(slider))
                            markers.Add(new OffscreenSliderOutline(slider));

                        break;

                    case HitCircle circle:
                        if (CheckOffscreenObjects.IsOffscreen(circle.StackedPosition, circle.Radius))
                        {
                            markers.Add(new OffscreenMarker((float)circle.Radius + ring_padding)
                            {
                                Position = circle.StackedPosition,
                            });
                        }

                        break;
                }
            }
        });

        /// <summary>
        /// Whether any point along the slider's path leaves the visible area.
        /// </summary>
        private static bool isOffscreen(Slider slider)
        {
            for (int i = 0; i < slider.Distance; i += path_step_size)
            {
                if (CheckOffscreenObjects.IsOffscreen(slider.StackedPositionAt(i / slider.Distance), slider.Radius))
                    return true;
            }

            return CheckOffscreenObjects.IsOffscreen(slider.StackedEndPosition, slider.Radius);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap != null)
                editorBeatmap.BeatmapReprocessed -= refresh;
        }

        /// <summary>
        /// Traces a slider's path so the whole object reads as offscreen, matching how selection outlines it.
        /// </summary>
        private partial class OffscreenSliderOutline : CompositeDrawable
        {
            public OffscreenSliderOutline(Slider slider)
            {
                var body = new ManualSliderBody
                {
                    AccentColour = Color4.Transparent,
                    BorderColour = Color4.Red,
                    BorderSize = ring_thickness,
                    PathRadius = slider.Scale * OsuHitObject.OBJECT_RADIUS,
                };

                body.SetVertices(slider.Path.CalculatedPath);

                InternalChild = body;

                Position = slider.StackedPosition;
                OriginPosition = body.PathOffset;
            }
        }

        private partial class OffscreenMarker : CircularContainer
        {
            public OffscreenMarker(float radius)
            {
                Origin = Anchor.Centre;
                Size = new Vector2(radius * 2);

                Masking = true;
                BorderThickness = ring_thickness;
                BorderColour = Color4.Red;

                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    AlwaysPresent = true,
                    Alpha = 0,
                };
            }
        }
    }
}
