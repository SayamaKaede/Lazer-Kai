// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Logging;
using osu.Game.Configuration;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Edit.Blueprints.HitCircles.Components;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Edit;

namespace osu.Game.Rulesets.Osu.Edit.Blueprints
{
    public abstract partial class OsuSelectionBlueprint<T> : HitObjectSelectionBlueprint<T>
        where T : OsuHitObject
    {
        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private Playfield? playfield { get; set; }

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
                || stillDrawn);

        /// <summary>
        /// Whether the playfield is still drawing this object.
        /// </summary>
        /// <remarks>
        /// Asked of the playfield rather than of <see cref="DrawableObject"/>, which reports the same values forever
        /// — always drawn, always fully opaque — and so cannot say when an object left the screen.
        ///
        /// The playfield decides that per object, which covers what a rule written here would have to keep track of
        /// by hand: circles measured from their own time and sliders from their tail, plus the approach rate, the
        /// fade, hit animations and the markers left behind.
        ///
        /// Permissive when there is no playfield to ask, so nothing becomes unselectable by accident.
        /// </remarks>
        private bool stillDrawn =>
            playfield?.HitObjectContainer.AliveObjects.Any(d => ReferenceEquals(d.HitObject, Item)) != false;

        protected OsuSelectionBlueprint(T hitObject)
            : base(hitObject)
        {
        }

        private double lastLogTime = double.NegativeInfinity;

        // temporary: reporting from the class that actually decides, rather than from the playfield alongside it.
        protected override void Update()
        {
            base.Update();

            if (Time.Current - lastLogTime < 500)
                return;

            double since = editorClock.CurrentTime - Item.GetEndTime();

            if (since < 0 || since > 4000)
                return;

            lastLogTime = Time.Current;

            Logger.Log($"[diag-bp] t={Item.StartTime:0} since={since:0} drawable={DrawableObject?.GetType().Name ?? "null"} "
                       + $"bpAlive={DrawableObject?.IsAlive} bpAlpha={DrawableObject?.Alpha:0.000} "
                       + $"shouldBeAlive={ShouldBeAlive} selectable={IsSelectable} thisAlive={IsAlive}",
                LoggingTarget.Runtime, LogLevel.Important);
        }
    }
}
