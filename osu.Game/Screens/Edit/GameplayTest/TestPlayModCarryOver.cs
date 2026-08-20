// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Screens.Edit.GameplayTest
{
    /// <summary>
    /// Carries a mod selection made inside the editor back out to song select.
    /// </summary>
    /// <remarks>
    /// A screen is handed the game's mod selection as a lease, which reverts the moment the screen is left. That is
    /// deliberate: testing a beatmap with mods is not supposed to disturb what the player picked to play with. When the
    /// two selections are meant to be one and the same, the revert has to be undone once it has happened, and the
    /// editor is long gone by then — so the handover waits here instead.
    /// </remarks>
    public partial class TestPlayModCarryOver : Component
    {
        [Resolved]
        private Bindable<IReadOnlyList<Mod>> selectedMods { get; set; } = null!;

        private IReadOnlyList<Mod>? pending;

        /// <summary>
        /// Hands a selection over, to be applied as soon as no screen is holding the lease on it.
        /// </summary>
        public void HandOver(IReadOnlyList<Mod> mods) => pending = mods;

        protected override void Update()
        {
            base.Update();

            // watched for rather than hooked onto the lease ending, because that both frees the selection and reverts
            // it, and this has to land after the revert whichever order those happen in.
            if (pending == null || selectedMods.Disabled)
                return;

            selectedMods.Value = pending;
            pending = null;
        }
    }
}
