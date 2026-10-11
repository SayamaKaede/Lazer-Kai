// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Where a key held to seek back looks back from while the editor is playing: where its last repeat went to, rather
    /// than the playhead.
    /// </summary>
    /// <remarks>
    /// A seek back goes to the last of something before the playhead: a white tick, an object, a point a sound is
    /// played at. A key held repeats every 70ms, and the editor plays on between two repeats. Going by the playhead, a
    /// repeat finds what the one before it went to, which has just gone by again, and the key held goes back to one place
    /// over and over for as long as it is held (2026-10-11).
    /// </remarks>
    public class HeldSeekBack
    {
        private double? wentTo;

        /// <summary>
        /// The time a seek back is to look back from.
        /// </summary>
        /// <param name="playhead">Where the editor is.</param>
        /// <param name="playing">Whether the editor is playing. Stopped, the playhead is where the last seek left it.</param>
        /// <param name="repeat">Whether this is a repeat of a key held, rather than the key going down.</param>
        public double From(double playhead, bool playing, bool repeat) =>
            playing && repeat && wentTo is double time ? Math.Min(time, playhead) : playhead;

        /// <summary>
        /// Keeps where a seek back went to, for the next repeat to go on from.
        /// </summary>
        public void WentTo(double time) => wentTo = time;

        /// <summary>
        /// Forgets it: the editor has been taken somewhere else.
        /// </summary>
        public void Reset() => wentTo = null;
    }
}
