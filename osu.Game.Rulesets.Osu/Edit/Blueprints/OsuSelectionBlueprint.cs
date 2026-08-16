// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Game.Configuration;
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
        /// How long an object stays selectable after its time has passed.
        /// </summary>
        /// <remarks>
        /// Objects fade out gradually once passed, leaving them barely visible yet still selectable for a long
        /// stretch, which causes accidental selections. Cutting selection off by time rather than by how faded the
        /// object looks keeps this predictable: the drawable's own alpha is not a reliable signal in the editor,
        /// where objects are kept visible for editing regardless of where the clock sits.
        /// It also makes circles and sliders behave alike, which they otherwise do not because their fade-outs differ.
        /// </remarks>
        private readonly Bindable<double> selectableAfterEndTime = new Bindable<double>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorSelectableAfterEndTime, selectableAfterEndTime);
        }

        public override bool IsSelectable =>
            // Bypass fade out extension from hit markers for selection purposes.
            // This is to match stable, where even when the afterimage hit markers are still visible, objects are not selectable.
            base.ShouldBeAlive
            // The cut-off deliberately only applies on the way out. Objects which have not appeared yet are also
            // invisible, but the editor routinely shows and manipulates upcoming objects.
            && editorClock.CurrentTime <= Item.GetEndTime() + selectableAfterEndTime.Value;

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }
    }
}
