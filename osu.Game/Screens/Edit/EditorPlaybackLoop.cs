// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Sends playback back to the start when it runs off the end of the track, instead of letting it stop there.
    /// </summary>
    /// <remarks>
    /// Useful when listening to a section repeatedly, and for short beatmaps where the end comes round often.
    /// </remarks>
    public partial class EditorPlaybackLoop : Component
    {
        /// <summary>
        /// How close to the end counts as having reached it.
        /// </summary>
        /// <remarks>
        /// The clock stops a little short of the track's stated length, so waiting for an exact match would never
        /// trigger.
        /// </remarks>
        private const double end_tolerance = 100;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorLoopPlayback, enabled);
        }

        protected override void Update()
        {
            base.Update();

            if (!enabled.Value || !editorClock.IsRunning)
                return;

            if (editorClock.CurrentTime < editorClock.TrackLength - end_tolerance)
                return;

            editorClock.Seek(0);
            editorClock.Start();
        }
    }
}
