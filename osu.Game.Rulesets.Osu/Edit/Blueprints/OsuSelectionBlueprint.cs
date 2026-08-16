// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints
{
    public abstract partial class OsuSelectionBlueprint<T> : HitObjectSelectionBlueprint<T>
        where T : OsuHitObject
    {
        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        protected new DrawableOsuHitObject DrawableObject => (DrawableOsuHitObject)base.DrawableObject;

        protected override bool AlwaysShowWhenSelected => true;

        protected override bool ShouldBeAlive => base.ShouldBeAlive
                                                 || (DrawableObject is not DrawableSpinner && ShowHitMarkers.Value && editorClock.CurrentTime >= Item.StartTime
                                                     && editorClock.CurrentTime - Item.GetEndTime() < HitCircleOverlapMarker.FADE_OUT_EXTENSION);

        /// <summary>
        /// How visible an object must still be to remain selectable.
        /// </summary>
        /// <remarks>
        /// Objects fade out gradually once their time has passed (see <see cref="DrawableHitCircle.SuppressHitAnimations"/>,
        /// which fades a circle out over 700ms), leaving them barely visible yet still selectable for a long stretch.
        /// That leads to a lot of accidental selections, so selection is cut off while they are still legible.
        /// Applying the same threshold to every object type also makes circles and sliders behave consistently,
        /// which they otherwise do not because their fade-outs differ.
        /// </remarks>
        private const float selectable_alpha_threshold = 0.5f;

        public override bool IsSelectable =>
            // Bypass fade out extension from hit markers for selection purposes.
            // This is to match stable, where even when the afterimage hit markers are still visible, objects are not selectable.
            base.ShouldBeAlive
            // The threshold deliberately only applies on the way out. Objects which have not appeared yet are also
            // faint or invisible, but the editor routinely shows and manipulates upcoming objects.
            && (editorClock.CurrentTime <= Item.GetEndTime() || DrawableObject.Alpha >= selectable_alpha_threshold);

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }
    }
}
