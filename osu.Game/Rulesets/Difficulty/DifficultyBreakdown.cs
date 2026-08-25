// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
        /// Adds up what each skill the ruleset measures with made of every object — for osu!, aim and speed and
        /// reading.
        /// </summary>
        /// <remarks>
        /// Skills are not on a shared scale and a beatmap's rating is not their sum, so this is only meaningful as a
        /// shape: it shows where the work in a beatmap is, not how hard any part is in star rating terms.
        /// </remarks>
        public double[] GetCombinedDifficulties()
        {
            double[] combined = new double[Times.Length];
            var counted = new HashSet<Type>();

            foreach (var skill in skills)
            {
                // a ruleset may build a skill a second time only to take a ratio against it: osu! builds aim again
                // with sliders left out to arrive at its slider factor, taiko builds stamina again for single
                // colours. Those copies are not difficulty of their own, and adding them in counts one skill twice,
                // which tilts the whole curve towards whatever that skill measures.
                if (!counted.Add(skill.GetType()))
                    continue;

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
