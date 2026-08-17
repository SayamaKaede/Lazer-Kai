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
            // Objects stay selectable long after they have gone from the screen, which drags them into drag
            // selections and clicks aimed at what is there now.
            //
            // The cut-off is the editor's own fade-out length rather than a duration of its own: that is how long an
            // object remains on screen after its time, whatever the approach rate or tempo, and it is the same window
            // the hit markers use. Enabling hit animations makes an object disappear sooner than this, so the cut-off
            // errs on the side of an object still being visible rather than cutting one off early.
            //
            // It applies only on the way out. Objects which have not appeared yet are equally invisible, but the
            // editor routinely shows and manipulates upcoming objects.
            && (!ignoreFadedObjects.Value || editorClock.CurrentTime <= Item.GetEndTime() + HitCircleOverlapMarker.FADE_OUT_EXTENSION);

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }
    }
}
