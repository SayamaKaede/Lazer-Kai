// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Game.Rulesets.Difficulty.Skills;

namespace osu.Game.Rulesets.Difficulty
{
    /// <summary>
    /// What each skill made of every object in a beatmap, kept alongside the times those objects sit at.
    /// </summary>
    /// <remarks>
    /// A star rating says how hard a beatmap is; this says where that difficulty is.
    /// </remarks>
    public class DifficultyBreakdown
    {
        /// <summary>
        /// Times of the objects that were processed, in beatmap time.
        /// </summary>
        public readonly double[] Times;

        private readonly Skill[] skills;

        public DifficultyBreakdown(double[] times, Skill[] skills)
        {
            Times = times;
            this.skills = skills;
        }

        /// <summary>
        /// Adds up what every skill made of each object.
        /// </summary>
        /// <remarks>
        /// Skills are not on a shared scale and a beatmap's rating is not their sum, so this is only meaningful as a
        /// shape: it shows where the work in a beatmap is, not how hard any part is in star rating terms.
        /// </remarks>
        public double[] GetCombinedDifficulties()
        {
            double[] combined = new double[Times.Length];

            foreach (var skill in skills)
            {
                var difficulties = skill.GetObjectDifficulties();

                // a skill may bail out early on an object it has nothing to say about, so it can report fewer values
                // than there were objects.
                for (int i = 0; i < Math.Min(difficulties.Count, combined.Length); i++)
                    combined[i] += difficulties[i];
            }

            return combined;
        }

        /// <summary>
        /// The individual skills, named as their implementing type.
        /// </summary>
        public (string Name, double[] Difficulties)[] GetPerSkillDifficulties() =>
            skills.Select(s => (s.GetType().Name, s.GetObjectDifficulties().ToArray())).ToArray();
    }
}
