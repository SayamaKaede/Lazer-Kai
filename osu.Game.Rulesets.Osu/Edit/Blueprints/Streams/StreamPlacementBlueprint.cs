// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders;
using osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Streams
{
    /// <summary>
    /// Draws the shape a stream runs along, exactly the way a slider is drawn, and hands it to the panel that
    /// turns a shape into circles.
    /// </summary>
    /// <remarks>
    /// The slider itself never reaches the beatmap. It is the shape that is wanted, and putting it in first would
    /// make placing a stream two entries in the undo history instead of one.
    ///
    /// The circles are shown while the shape is still being drawn, as drawables this blueprint owns. They cannot be
    /// beatmap objects: the compose container ends the running placement whenever an object is added, so a preview
    /// built that way tears down the very placement that is drawing it.
    /// </remarks>
    public partial class StreamPlacementBlueprint : SliderPlacementBlueprint
    {
        [Resolved(canBeNull: true)]
        private StreamConversionPanel? panel { get; set; }

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private BindableBeatDivisor beatDivisor { get; set; } = null!;

        private StreamPlacementPreview preview = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AddInternal(preview = new StreamPlacementPreview());
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // the shape is not what is being placed. Its body and its two ends are the slider tool's way of showing
            // what a path will become, and here it becomes circles -- which are drawn instead. What stays is the
            // line between the anchors, which is how the path is read while it is being drawn.
            foreach (var piece in InternalChildren.Where(d => d is SliderBodyPiece || d is HitCirclePiece))
                piece.Alpha = 0;
        }

        protected override void Update()
        {
            base.Update();

            if (PlacementActive != PlacementState.Active)
            {
                preview.Clear();
                return;
            }

            double beatLength = editorBeatmap.ControlPointInfo.TimingPointAt(HitObject.StartTime).BeatLength;

            preview.Set(StreamConversion.Plan(HitObject, beatLength / beatDivisor.Value));
        }

        public override void EndPlacement(bool commit)
        {
            preview.Clear();

            if (commit && IsValidForPlacement && panel != null)
            {
                var shape = HitObject;

                // taken out of the placement's hands rather than committed: the panel adds the circles itself, and
                // it is the one that knows whether the shape is being kept.
                base.EndPlacement(false);

                // straight away. Putting this off to the next frame was tried twice and reached nothing either
                // time: a finished placement is thrown away in the same frame, and the panel is hidden until it is
                // opened -- a drawable that is not present does not run what it was asked to do later.
                //
                // Opening does put the shape in the beatmap, which ends this placement a second time from inside
                // this call. That is survivable: the second time round there is nothing left to commit, and being
                // expired only takes effect once the frame is over.
                panel.Open(shape, fromTheBeatmap: false);
                return;
            }

            base.EndPlacement(commit);
        }
    }
}
