// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Skinning.Default;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components
{
    public partial class HitCirclePiece : BlueprintPiece<HitCircle>
    {
        private const float default_ring_thickness = 9;

        private readonly RingPiece ring;

        public HitCirclePiece()
        {
            Origin = Anchor.Centre;

            Size = OsuHitObject.OBJECT_DIMENSIONS;

            CornerRadius = Size.X / 2;
            CornerExponent = 2;

            InternalChild = ring = new RingPiece(default_ring_thickness);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OsuConfigManager config)
        {
            var appearance = new EditorSelectionAppearance(config);

            void updateAppearance()
            {
                Colour = appearance.GetColour(colours.Yellow);
                Alpha = appearance.Opacity;
                ring.BorderThickness = appearance.GetThickness(default_ring_thickness);
            }

            appearance.Changed += updateAppearance;
            updateAppearance();
        }

        public override void UpdateFrom(HitCircle hitObject)
        {
            base.UpdateFrom(hitObject);

            Scale = new Vector2(hitObject.Scale);
        }
    }
}
