// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Screens.Edit;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints.Spinners.Components
{
    public partial class SpinnerPiece : BlueprintPiece<Spinner>
    {
        private readonly Circle circle;
        private readonly Circle ring;

        public SpinnerPiece()
        {
            Origin = Anchor.Centre;

            RelativeSizeAxes = Axes.Both;
            FillMode = FillMode.Fit;
            Size = new Vector2(1);

            InternalChildren = new Drawable[]
            {
                circle = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0.5f,
                },
                ring = new Circle
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(OsuHitObject.OBJECT_RADIUS),
                },
            };
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
                // no thickness here: a spinner's outline is a filled disc and a solid ring, with no border to widen.
                Colour = appearance.GetObjectColour(colours.Yellow);
                Alpha = appearance.ObjectOpacity;
            }

            appearance.Changed += updateAppearance;
            updateAppearance();
        }

        public override void UpdateFrom(Spinner hitObject)
        {
            base.UpdateFrom(hitObject);

            ring.Scale = new Vector2(hitObject.Scale);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => circle.ReceivePositionalInputAt(screenSpacePos);
    }
}
