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
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Sliders.Components
{
    public partial class SliderBodyPiece : BlueprintPiece<Slider>
    {
        /// <summary>
        /// Matches <see cref="DrawableSliderPath.BorderSize"/>'s own default.
        /// </summary>
        private const float default_border_size = 1;

        private readonly ManualSliderBody body;

        /// <summary>
        /// Offset in absolute (local) coordinates from the start of the curve.
        /// </summary>
        public Vector2 PathStartLocation => body.PathOffset;

        /// <summary>
        /// Offset in absolute (local) coordinates from the end of the curve.
        /// </summary>
        public Vector2 PathEndLocation => body.PathEndOffset;

        public SliderBodyPiece()
        {
            AutoSizeAxes = Axes.Both;

            // SliderSelectionBlueprint relies on calling ReceivePositionalInputAt on this drawable to determine whether selection should occur.
            // Without AlwaysPresent, a movement in a parent container (ie. the editor composer area resizing) could cause incorrect input handling.
            AlwaysPresent = true;

            InternalChild = body = new ManualSliderBody
            {
                AccentColour = Color4.Transparent
            };
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OsuConfigManager config)
        {
            var appearance = new EditorSelectionAppearance(config);

            void updateAppearance()
            {
                body.BorderColour = appearance.GetColour(colours.Yellow);
                body.BorderSize = appearance.GetThickness(default_border_size);
                Alpha = appearance.Opacity;
            }

            appearance.Changed += updateAppearance;
            updateAppearance();
        }

        private int? lastVersion;

        public override void UpdateFrom(Slider hitObject)
        {
            base.UpdateFrom(hitObject);

            body.PathRadius = hitObject.Scale * OsuHitObject.OBJECT_RADIUS;

            if (lastVersion != hitObject.Path.Version.Value)
            {
                lastVersion = hitObject.Path.Version.Value;
                body.SetVertices(hitObject.Path.CalculatedPath);
            }

            OriginPosition = body.PathOffset;
        }

        public void RecyclePath() => body.RecyclePath();

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => body.ReceivePositionalInputAt(screenSpacePos);
    }
}
