// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// Decides how the difficulty graph winds down across the stretches where nothing was measured.
    /// </summary>
    /// <remarks>
    /// A skill measures an object where it starts, so a long slider or one with reverses leaves everything between
    /// its ends with no value at all. Drawn as nothing, those stretches read as breaks — places with no beatmap in
    /// them — when they are the opposite. Winding down from what came before says something is still going on
    /// without inventing a height for it.
    /// </remarks>
    public static class DifficultyGraphFade
    {
        /// <summary>
        /// How many columns a fade takes to fall to a tenth of what it started at.
        /// </summary>
        /// <remarks>
        /// Counted in columns rather than in seconds, because the graph is drawn with the same number of columns
        /// whatever the beatmap. A fade of a fixed number of seconds would trail across half a short beatmap and be
        /// invisible on a long one, which is the very thing the column count was made not to do.
        /// </remarks>
        public const int FADE_COLUMNS = 6;

        /// <summary>
        /// Where each column takes its value from, and how much of it is left by the time it gets there.
        /// </summary>
        /// <remarks>
        /// A column with something measured in it is itself at full height. One with nothing takes the last measured
        /// column's value, faded. <see cref="Fade.Source"/> is -1 where there is nothing to take — before the first
        /// object, and through a break.
        /// </remarks>
        /// <param name="measured">Whether each column has an object measured in it.</param>
        /// <param name="inBreak">Whether each column is inside one of the beatmap's breaks.</param>
        public static Fade[] Across(bool[] measured, bool[] inBreak)
        {
            var fades = new Fade[measured.Length];
            int last = -1;

            for (int i = 0; i < measured.Length; i++)
            {
                if (inBreak[i])
                {
                    // the beatmap starts again on the other side rather than picking the fade back up.
                    last = -1;
                    fades[i] = new Fade(-1, 0);
                    continue;
                }

                if (measured[i])
                {
                    last = i;
                    fades[i] = new Fade(i, 1);
                    continue;
                }

                fades[i] = last < 0
                    ? new Fade(-1, 0)
                    : new Fade(last, Math.Pow(10, -(double)(i - last) / FADE_COLUMNS));
            }

            return fades;
        }

        public readonly struct Fade
        {
            /// <summary>
            /// The column whose value this one is drawn from, or -1 for nothing at all.
            /// </summary>
            public readonly int Source;

            /// <summary>
            /// How much of that column's value is left here.
            /// </summary>
            public readonly double Factor;

            public Fade(int source, double factor)
            {
                Source = source;
                Factor = factor;
            }
        }
    }
}
