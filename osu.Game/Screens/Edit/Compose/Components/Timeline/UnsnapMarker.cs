// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osuTK;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// A mark over a moment in the timeline that does not sit on the beat.
    /// </summary>
    /// <remarks>
    /// Unsnapped objects are already reported by the verifier, but only once the whole beatmap is checked — by
    /// which point the mistake is somewhere in an hour of work rather than under the cursor. This says so while
    /// the object is still being placed.
    ///
    /// What counts as unsnapped is the ranking criteria's line rather than the verifier's: anything within
    /// <see cref="TOLERATED_MS"/> of the nearest snap is allowed, so marking it says a map is wrong when it is
    /// not. The verifier is deliberately left alone at its own thresholds — it is what other people run, and a
    /// fork that quietly answers differently from everyone else's check is worse than one that marks less.
    /// The two disagreeing below three milliseconds is the point, not an oversight.
    ///
    /// One mark, one colour. There is no longer a tier to draw: below the line is allowed and above it is not.
    ///
    /// It takes no input. A mark that could be clicked would be a mark sitting between the mapper and the object
    /// it is about.
    /// </remarks>
    public partial class UnsnapMarker : SpriteIcon
    {
        /// <summary>
        /// How far off the beat an object is allowed to be before it is marked, in milliseconds.
        /// </summary>
        /// <remarks>
        /// The ranking criteria allows an object to sit up to two milliseconds off, so two is not a mistake and
        /// is not marked. Held as more-than rather than three-or-more: an object two and a half milliseconds out
        /// breaks the rule, and rounding it into the clear would be marking by the displayed number rather than
        /// by the one that counts.
        /// </remarks>
        public const double TOLERATED_MS = 2;

        private readonly Bindable<bool> enabled = new Bindable<bool>();

        private double markedTime;

        /// <summary>
        /// How far the moment being marked is from the nearest snap, in milliseconds.
        /// </summary>
        public double Unsnap { get; private set; }

        [Resolved]
        private EditorBeatmap? editorBeatmap { get; set; }

        [Resolved]
        private OsuColour colours { get; set; } = null!;

        public UnsnapMarker()
        {
            Icon = FontAwesome.Solid.CaretDown;
            Size = new Vector2(10);
            Alpha = 0;
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => false;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorShowUnsnapMarkers, enabled);
            enabled.BindValueChanged(_ => Refresh());
        }

        /// <summary>
        /// Points the mark at a moment, and shows it if that moment is off the beat.
        /// </summary>
        public void SetTime(double time)
        {
            markedTime = time;
            Refresh();
        }

        /// <summary>
        /// Reads the moment again, for when the timing behind it moved rather than the object.
        /// </summary>
        public void Refresh()
        {
            if (!enabled.Value || editorBeatmap == null)
            {
                Alpha = 0;
                return;
            }

            Unsnap = Math.Abs(markedTime - editorBeatmap.ControlPointInfo.GetClosestSnappedTime(markedTime));

            if (Unsnap <= TOLERATED_MS)
            {
                Alpha = 0;
                return;
            }

            Alpha = 1;
            Colour = colours.Red1;
        }
    }
}
