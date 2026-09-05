// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Screens.Edit.Collab;

namespace osu.Game.Screens.Edit.Components.Timelines.Summary.Parts
{
    /// <summary>
    /// Says whose stretch the playhead is in, for as long as it is asked to.
    /// </summary>
    /// <remarks>
    /// Separate from the part next door, which lights whatever the collaboration panel is pointing at while it is
    /// open. This one answers the song rather than the pointer, and keeps answering after the panel is closed --
    /// what it is for is a recording of the beatmap being played through, with the split readable as it goes.
    /// </remarks>
    public partial class CollabPlayheadPart : TimelinePart
    {
        private const float lit_alpha = 0.35f;

        [Resolved]
        private CollabHighlight highlight { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        private readonly IBindable<bool> following = new BindableBool();
        private readonly IBindable<CollabBand[]> bands = new Bindable<CollabBand[]>();

        private Box band = null!;

        /// <summary>
        /// The stretch currently drawn, so a playhead moving along inside one does not redraw it every frame.
        /// </summary>
        private CollabBand? drawn;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Add(band = new Box
            {
                RelativePositionAxes = Axes.X,
                RelativeSizeAxes = Axes.Both,
                Blending = BlendingParameters.Additive,
                Alpha = 0,
            });

            following.BindTo(highlight.FollowPlayhead);
            bands.BindTo(highlight.Bands);

            // a stretch can be handed to somebody else without the playhead moving, which changes the colour
            // without changing where it is drawn.
            bands.BindValueChanged(_ => redraw());
            following.BindValueChanged(_ => redraw());
        }

        protected override void Update()
        {
            base.Update();

            var under = bandUnderThePlayhead();

            if (isTheSameStretch(under, drawn))
                return;

            drawn = under;
            draw();
        }

        private CollabBand? bandUnderThePlayhead()
        {
            if (!following.Value)
                return null;

            foreach (var candidate in bands.Value)
            {
                if (candidate.Contains(editorClock.CurrentTime))
                    return candidate;
            }

            return null;
        }

        private static bool isTheSameStretch(CollabBand? a, CollabBand? b)
        {
            if (a.HasValue != b.HasValue)
                return false;

            return !a.HasValue || (a.Value.Start == b!.Value.Start && a.Value.End == b.Value.End);
        }

        private void redraw()
        {
            drawn = bandUnderThePlayhead();
            draw();
        }

        private void draw()
        {
            if (drawn == null)
            {
                band.FadeOut(200, Easing.OutQuint);
                return;
            }

            band.X = (float)drawn.Value.Start;
            band.Width = (float)(drawn.Value.End - drawn.Value.Start);
            band.Colour = drawn.Value.Colour;
            band.FadeTo(lit_alpha, 100, Easing.OutQuint);
        }
    }
}
