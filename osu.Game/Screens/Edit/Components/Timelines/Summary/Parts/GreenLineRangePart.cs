// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Lights the stretch of the song the green line panel's selected line is in effect for: from it to the next line.
    /// </summary>
    /// <remarks>
    /// Added rather than drawn over, as the collaboration panel's stretches are, so that what is underneath stays
    /// readable: what the stretch is for is seeing which objects the line reaches.
    /// </remarks>
    public partial class GreenLineRangePart : TimelinePart
    {
        private const float lit_alpha = 0.35f;

        [Resolved]
        private SavedTimingLines? savedTimingLines { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        private readonly Bindable<SavedTimingLine?> selected = new Bindable<SavedTimingLine?>();
        private readonly BindableList<SavedTimingLine> lines = new BindableList<SavedTimingLine>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (savedTimingLines == null)
                return;

            selected.BindTo(savedTimingLines.Selected);
            lines.BindTo(savedTimingLines.Lines);

            selected.BindValueChanged(_ => light());
            lines.BindCollectionChanged((_, _) => light(), true);
        }

        private void light()
        {
            // faded out rather than dropped, so that moving from line to line does not flicker the strip.
            foreach (var band in Children.ToArray())
                band.FadeOut(200, Easing.OutQuint).Expire();

            if (selected.Value is not SavedTimingLine line)
                return;

            double end = lines.Where(l => l.Time > line.Time).Select(l => l.Time).DefaultIfEmpty(Content.RelativeChildSize.X).First();

            Add(new Band(line.Time, end) { Colour = line.Uninherited ? colours.Red1 : colours.Lime1 });
        }

        public partial class Band : Box
        {
            public Band(double start, double end)
            {
                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Both;
                Blending = BlendingParameters.Additive;
                Alpha = 0;

                X = (float)start;
                Width = (float)(end - start);
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                this.FadeTo(lit_alpha, 100, Easing.OutQuint);
            }
        }
    }
}
