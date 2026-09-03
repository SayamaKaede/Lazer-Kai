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

        /// <summary>
        /// Held onto rather than left to the loading method. Nothing else refers to one of these,
        /// and the settings are bound weakly, so a collected one stops hearing about changes and the
        /// outline only catches up on the next visit to the editor.
        /// </summary>
        private EditorSelectionAppearance appearance = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OsuConfigManager config)
        {
            appearance = new EditorSelectionAppearance(config);

            void updateAppearance()
            {
                Colour = appearance.GetObjectColour(colours.Yellow);
                Alpha = appearance.ObjectOpacity;
                ring.BorderThickness = appearance.GetObjectThickness(default_ring_thickness);
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
