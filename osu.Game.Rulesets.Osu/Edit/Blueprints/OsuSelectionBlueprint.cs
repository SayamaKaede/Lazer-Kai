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
        /// Whether an object stops being selectable once it has finished fading from view.
        /// </summary>
        private readonly Bindable<bool> ignoreFadedObjects = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorIgnoreFadedObjects, ignoreFadedObjects);
        }

        public override bool IsSelectable =>
            // Bypass fade out extension from hit markers for selection purposes.
            // This is to match stable, where even when the afterimage hit markers are still visible, objects are not selectable.
            base.ShouldBeAlive
            // Blueprints outlive the objects they belong to, so an object gone from the screen still gets dragged
            // into selections aimed at what is there now.
            //
            // Whether the object is still drawn settles it. Its lifetime already accounts for the fade, for hit
            // animations being on or off, and for the markers left behind, so this holds at any approach rate or
            // tempo — where a duration in milliseconds means something different for every beatmap, and the
            // drawable's opacity turned out to report only 0 or 1 with nothing in between.
            //
            // It applies only on the way out. Objects which have not appeared yet are not drawn either, but the
            // editor routinely shows and manipulates upcoming objects.
            && (!ignoreFadedObjects.Value
                || editorClock.CurrentTime <= Item.GetEndTime()
                || DrawableObject?.IsAlive == true);

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }
    }
}
