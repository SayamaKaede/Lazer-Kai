// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// One stretch of a beatmap between two bookmarks, which a collaborator can be given.
    /// </summary>
    public readonly struct CollabBlock
    {
        /// <summary>
        /// The bookmark this block opens on.
        /// </summary>
        /// <remarks>
        /// What an assignment is remembered against. A bookmark's own time is used rather than the block's position in
        /// the list so that adding a bookmark elsewhere leaves the rest of the assignments alone, and rather than the
        /// block's start so that nudging the first object does not lose the first block's.
        /// </remarks>
        public readonly int Key;

        /// <summary>The moment the block begins, which for the first is where the beatmap begins.</summary>
        public readonly double Start;

        /// <summary>The moment the block ends, which for the last is where the beatmap ends.</summary>
        public readonly double End;

        /// <summary>How long of this block is actually played, counted the way the whole beatmap's is.</summary>
        public readonly double DrainTime;

        /// <summary>
        /// How long the block is from bookmark to bookmark, rests and all: what a collaboration is agreed in before any
        /// of it is mapped, when <see cref="DrainTime"/> is still nothing.
        /// </summary>
        public double Length => End - Start;

        /// <summary>How many objects begin inside it.</summary>
        public readonly int ObjectCount;

        /// <summary>
        /// What the stretch before the first bookmark was remembered against while there was one (until 2026-10-18).
        /// </summary>
        /// <remarks>
        /// No block opens on it any more, but a project saved before then can still hold it, until
        /// <see cref="CollabProject.DropAssignmentsOutsideOf"/> takes it out.
        /// </remarks>
        public const int HEAD = int.MinValue;

        public CollabBlock(int key, double start, double end, double drainTime, int objectCount)
        {
            Key = key;
            Start = start;
            End = end;
            DrainTime = drainTime;
            ObjectCount = objectCount;
        }
    }
}
