// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Timing;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// The stream a shape is about to become, shown while the shape is still being drawn.
    /// </summary>
    /// <remarks>
    /// Drawn objects of its own rather than objects put in the beatmap. Adding to the beatmap ends the placement
    /// that is doing the adding -- <see cref="Screens.Edit.Compose.Components.ComposeBlueprintContainer"/> refreshes
    /// the placement whenever an object arrives -- so a preview built that way takes the editor down with it.
    ///
    /// Each circle is given a clock stopped at its own start time, which is the moment it would be hit: fully drawn,
    /// numbered, in its combo colour, and not moving. That is the state a stream is read in, and it means the whole
    /// run is visible at once however long it is.
    /// </remarks>
    public partial class StreamPlacementPreview : CompositeDrawable
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        private Container content = null!;

        private readonly List<DrawableHitCircle> circles = new List<DrawableHitCircle>();

        /// <summary>How many circles are being shown.</summary>
        public int Count => circles.Count;

        public StreamPlacementPreview()
        {
            RelativeSizeAxes = Axes.Both;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChild = content = new Container { RelativeSizeAxes = Axes.Both };
        }

        /// <summary>
        /// Brings what is shown into line with <paramref name="plan"/>.
        /// </summary>
        /// <remarks>
        /// The circles are rebuilt only when how many of them there are changes. A drag moves them every frame, and
        /// a drawable object cannot be moved by writing to the one it was built from once its clock has stopped.
        /// </remarks>
        public void Set(IReadOnlyList<StreamConversion.Note> plan)
        {
            if (plan.Count != circles.Count)
                rebuild(plan.Count);

            for (int i = 0; i < circles.Count; i++)
                circles[i].Position = plan[i].Position;
        }

        public void Clear()
        {
            content.Clear();
            circles.Clear();
        }

        private void rebuild(int count)
        {
            Clear();

            for (int i = 0; i < count; i++)
            {
                var circle = new HitCircle { IndexInCurrentCombo = i };

                circle.ApplyDefaults(editorBeatmap.ControlPointInfo, editorBeatmap.Difficulty);

                circles.Add(new DrawableHitCircle(circle)
                {
                    // stopped at the moment it would be hit, so it is drawn the way it is read rather than part
                    // way through approaching.
                    Clock = new FramedClock(new ManualClock { CurrentTime = circle.StartTime }),
                    ProcessCustomClock = false,
                    AlwaysPresent = true,
                });
            }

            content.AddRange(circles.Cast<Drawable>());
        }
    }
}
