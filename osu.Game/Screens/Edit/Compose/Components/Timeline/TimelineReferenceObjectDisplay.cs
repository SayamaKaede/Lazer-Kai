// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// A row of the difficulty being referenced, drawn under the one being edited so their rhythms can be read against
    /// each other.
    /// </summary>
    /// <remarks>
    /// Bars rather than the real blueprints: on the timeline an object already is a bar with rounded ends, and the
    /// blueprints carry selection, dragging and sample handling that none of this is allowed to do.
    /// </remarks>
    public partial class TimelineReferenceObjectDisplay : TimelinePart
    {
        [Resolved]
        private Timeline timeline { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private EditorReferenceDifficulty? reference { get; set; }

        private readonly Bindable<EditorSelectionColour> colour = new Bindable<EditorSelectionColour>();
        private readonly Bindable<float> opacity = new Bindable<float>(1);

        private readonly Cached objectCache = new Cached();

        private (float min, float max) visibleRange = (float.MinValue, float.MaxValue);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            RelativeSizeAxes = Axes.Both;
            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
            RelativePositionAxes = Axes.Y;

            // the lower of the two rows, sat so the gaps above it, below it and between the two are the same.
            Height = 0.5f;
            Y = 0.25f;

            config.BindWith(OsuSetting.EditorReferenceDifficultyColour, colour);
            config.BindWith(OsuSetting.EditorReferenceDifficultyOpacity, opacity);

            colour.BindValueChanged(c => Colour = c.NewValue.ToColour() ?? Color4.White, true);
            opacity.BindValueChanged(o => Alpha = o.NewValue, true);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            reference?.Beatmap.BindValueChanged(_ => objectCache.Invalidate(), true);
        }

        protected override void Update()
        {
            base.Update();

            if (DrawWidth <= 0)
                return;

            (float, float) newRange = (
                ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopLeft).X / DrawWidth * Content.RelativeChildSize.X,
                ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopRight).X / DrawWidth * Content.RelativeChildSize.X);

            if (visibleRange != newRange)
            {
                visibleRange = newRange;
                objectCache.Invalidate();
            }

            if (!objectCache.IsValid)
            {
                recreateObjects();
                objectCache.Validate();
            }
        }

        private void recreateObjects()
        {
            Clear();

            var beatmap = reference?.Beatmap.Value;

            if (beatmap == null)
                return;

            // only what is on screen: a long difficulty is thousands of objects, and the rest of the timeline works
            // the same way.
            foreach (var hitObject in beatmap.HitObjects.Where(isVisible))
                Add(new ReferenceObject(hitObject));
        }

        private bool isVisible(HitObject hitObject) => hitObject.GetEndTime() >= visibleRange.min && hitObject.StartTime <= visibleRange.max;

        private partial class ReferenceObject : CompositeDrawable
        {
            public ReferenceObject(HitObject hitObject)
            {
                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Both;

                Anchor = Anchor.CentreLeft;
                Origin = Anchor.CentreLeft;

                X = (float)hitObject.StartTime;
                Width = (float)(hitObject.GetEndTime() - hitObject.StartTime);
                Height = 0.6f;

                InternalChild = new Circle { RelativeSizeAxes = Axes.Both };
            }

            protected override void Update()
            {
                base.Update();

                // the end caps hang past the object's own bounds, the way the real ones do, so an object with no
                // length is still a circle rather than a sliver. Width here is measured in milliseconds, so the
                // rounding cannot come from it.
                Padding = new MarginPadding { Horizontal = -DrawHeight / 2 };
            }
        }
    }
}
