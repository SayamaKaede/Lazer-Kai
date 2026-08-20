// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Plays the song at the speed the test play mods will run it at, while those mods are being previewed.
    /// </summary>
    /// <remarks>
    /// Rate changing mods do not move hit objects, so nothing on the timeline shifts under the mapper: only the music
    /// and the scrolling it drives change pace, which is all those mods do to a beatmap in the first place.
    /// <para>
    /// Timing mode is left alone. The editor plays at a true rate there on purpose — judging timing by ear against a
    /// resampled track is inaccurate, which is the same reason the speed control is taken away in that mode.
    /// </para>
    /// </remarks>
    public partial class EditorModRatePreview : Component
    {
        /// <summary>
        /// Whether the playback speed is currently the mods' to decide rather than the mapper's.
        /// </summary>
        public IBindable<bool> RateDrivenByMods => rateDrivenByMods;

        private readonly BindableBool rateDrivenByMods = new BindableBool();

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private Editor editor { get; set; } = null!;

        private Bindable<bool> applyModsToComposer = null!;

        /// <summary>
        /// What the mods were applied to, kept so it can be taken off the clock again.
        /// </summary>
        private AudioAdjustments? applied;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            applyModsToComposer = config.GetBindable<bool>(OsuSetting.EditorApplyTestPlayModsToComposer);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            applyModsToComposer.BindValueChanged(_ => updateAdjustments());
            editor.Mode.BindValueChanged(_ => updateAdjustments());

            // the mods themselves are handed over live rather than cloned, so dragging a mod's speed setting moves the
            // song along with it. Only the selection changing calls for the adjustments to be built again.
            editor.TestPlayMods.Mods.BindValueChanged(_ => updateAdjustments(), true);
        }

        private void updateAdjustments()
        {
            if (applied != null)
            {
                editorClock.AudioAdjustments.UnbindAdjustments(applied);
                applied = null;
            }

            var rateMods = previewableRateMods();

            if (rateMods.Length > 0)
            {
                applied = new AudioAdjustments();

                foreach (var mod in rateMods)
                    mod.ApplyToTrack(applied);

                editorClock.AudioAdjustments.BindAdjustments(applied);
            }

            rateDrivenByMods.Value = applied != null;
        }

        private IApplicableToRate[] previewableRateMods()
        {
            if (!applyModsToComposer.Value || editor.Mode.Value == EditorScreenMode.Timing)
                return Array.Empty<IApplicableToRate>();

            // the same exclusion the composer makes: a mod driving the playfield every frame is written for a gameplay
            // screen, and reaches for parts of one the editor has no reason to show.
            return editor.TestPlayMods.Mods.Value.OfType<IApplicableToRate>()
                         .Where(m => m is not IUpdatableByPlayfield)
                         .ToArray();
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (applied != null && editorClock.IsNotNull())
                editorClock.AudioAdjustments.UnbindAdjustments(applied);
        }
    }
}
