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

        /// <summary>
        /// Below this, an object has faded far enough to count as gone.
        /// </summary>
        /// <remarks>
        /// Not zero: the last of the fade is spent at an opacity nothing can be picked out at, and an object that
        /// cannot be seen should not be caught by a drag across empty space.
        /// </remarks>
        private const float faded_alpha = 0.06f;

        public override bool IsSelectable =>
            // Bypass fade out extension from hit markers for selection purposes.
            // This is to match stable, where even when the afterimage hit markers are still visible, objects are not selectable.
            base.ShouldBeAlive
            // Objects stay selectable after they have gone from the screen, which drags them into selections aimed
            // at what is there now.
            //
            // Read from how visible the object actually is rather than from a duration. The editor fades an object
            // out over its own window (see DrawableHitCircle.SuppressHitAnimations), turning hit animations back on
            // makes it go sooner, and hit markers change what is left behind — the drawable's own opacity already
            // accounts for all three, where any duration would have to be kept in step with them by hand.
            //
            // It applies only on the way out. Objects which have not appeared yet are equally transparent, but the
            // editor routinely shows and manipulates upcoming objects.
            && (!ignoreFadedObjects.Value
                || editorClock.CurrentTime <= Item.GetEndTime()
                || DrawableObject.Alpha > faded_alpha);

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }
    }
}
