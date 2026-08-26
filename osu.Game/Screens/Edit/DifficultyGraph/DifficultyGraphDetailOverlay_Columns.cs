// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    public partial class DifficultyGraphDetailOverlay
    {
        /// <summary>
        /// One column of the graph: the hardest object landing in it, taken apart by skill.
        /// </summary>
        /// <remarks>
        /// <see cref="Skills"/> is in the order the ruleset builds its skills, and comes to <see cref="Total"/>.
        /// </remarks>
        public readonly struct SkillColumn
        {
            public readonly double Total;
            public readonly double[] Skills;

            /// <summary>
            /// Whether anything was actually measured in this column, as opposed to it winding down from what was.
            /// </summary>
            public readonly bool Measured;

            public SkillColumn(double total, double[] skills, bool measured = false)
            {
                Total = total;
                Skills = skills;
                Measured = measured;
            }
        }

        /// <summary>
        /// Cuts a beatmap's per-skill difficulties into columns of the graph.
        /// </summary>
        /// <remarks>
        /// A column holds the hardest object in it rather than a sum of them, which is what the summary timeline
        /// draws too — so the top of the stack drawn here is the curve drawn there, and the two cannot disagree.
        /// </remarks>
        /// <param name="times">When each object is, in beatmap time.</param>
        /// <param name="perSkill">What each skill made of each object, one array per skill.</param>
        /// <param name="trackLength">How long the song is.</param>
        /// <param name="columns">How many columns to cut it into.</param>
        /// <param name="breaks">
        /// When each of the beatmap's breaks runs from and to. A break is drawn as the empty stretch it is; anything
        /// else with nothing measured in it winds down from what came before.
        /// </param>
        public static SkillColumn[] ColumnsFor(double[] times, IReadOnlyList<double[]> perSkill, double trackLength, int columns,
                                               IEnumerable<(double Start, double End)>? breaks = null)
        {
            if (trackLength <= 0 || columns <= 0)
                return Array.Empty<SkillColumn>();

            var built = new SkillColumn[columns];
            bool[] inBreak = new bool[columns];

            foreach ((double start, double end) in breaks ?? Array.Empty<(double, double)>())
            {
                int from = (int)(start / trackLength * columns);
                int to = (int)(end / trackLength * columns);

                for (int i = Math.Max(from, 0); i <= Math.Min(to, columns - 1); i++)
                    inBreak[i] = true;
            }

            for (int i = 0; i < times.Length; i++)
            {
                int index = (int)(times[i] / trackLength * columns);

                if (index < 0 || index >= columns)
                    continue;

                // a skill may bail out early on an object it has nothing to say about, so it can report fewer values
                // than there were objects. It made nothing of the rest rather than something unknown.
                double[] skills = perSkill.Select(s => i < s.Length ? s[i] : 0).ToArray();
                double total = skills.Sum();

                if (total > built[index].Total)
                    built[index] = new SkillColumn(total, skills, true);
            }

            var fades = DifficultyGraphFade.Across(built.Select(c => c.Measured).ToArray(), inBreak);

            for (int i = 0; i < columns; i++)
            {
                if (built[i].Measured)
                    continue;

                var fade = fades[i];

                if (fade.Source < 0)
                {
                    built[i] = new SkillColumn(0, new double[perSkill.Count]);
                    continue;
                }

                // the whole stack fades together, so what the difficulty was made of stays readable as it winds down.
                double[] faded = built[fade.Source].Skills.Select(v => v * fade.Factor).ToArray();
                built[i] = new SkillColumn(faded.Sum(), faded);
            }

            return built;
        }
    }
}
