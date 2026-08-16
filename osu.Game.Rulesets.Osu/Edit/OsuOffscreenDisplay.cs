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
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Checks;
using osu.Game.Rulesets.Osu.Objects;
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
                    BorderColour = colours.Red1,
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
                foreach (var (position, radius) in offscreenPointsOf(hitObject))
                {
                    markers.Add(new OffscreenMarker(radius + ring_padding)
                    {
                        Position = position,
                    });
                }
            }
        });

        /// <summary>
        /// The positions at which an object breaks out of the visible area. Sliders are reported at both ends, since
        /// either can be the part which goes out.
        /// </summary>
        private IEnumerable<(Vector2 position, float radius)> offscreenPointsOf(OsuHitObject hitObject)
        {
            switch (hitObject)
            {
                case Slider slider:
                    if (CheckOffscreenObjects.IsOffscreen(slider.StackedPosition, slider.Radius))
                        yield return (slider.StackedPosition, (float)slider.Radius);

                    if (CheckOffscreenObjects.IsOffscreen(slider.StackedEndPosition, slider.Radius))
                        yield return (slider.StackedEndPosition, (float)slider.Radius);

                    break;

                case HitCircle circle:
                    if (CheckOffscreenObjects.IsOffscreen(circle.StackedPosition, circle.Radius))
                        yield return (circle.StackedPosition, (float)circle.Radius);

                    break;
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap != null)
                editorBeatmap.BeatmapReprocessed -= refresh;
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
