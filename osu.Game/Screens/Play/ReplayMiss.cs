// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Screens.Play
{
    /// <summary>
    /// An object a replay did not hit perfectly, worth going back to.
    /// </summary>
    /// <param name="HitObject">The object, as it is in the beatmap.</param>
    /// <param name="Time">Where it went wrong: the object itself, or for a break, the part of it that broke the combo.</param>
    /// <param name="Kind">How it went wrong.</param>
    /// <param name="Offset">How early (below zero) or late it was hit, where it was hit at a time that can be early or late.</param>
    public record ReplayMiss(HitObject HitObject, double Time, ReplayMissKind Kind, double? Offset)
    {
        /// <summary>
        /// One for each object that was missed, broke the combo, or was hit for less than the most it gives, in the
        /// order of the beatmap.
        /// </summary>
        /// <param name="beatmap">The beatmap that was played.</param>
        /// <param name="results">What was judged, including what was judged for the parts of each object.</param>
        public static IReadOnlyList<ReplayMiss> From(IBeatmap beatmap, IEnumerable<JudgementResult> results)
        {
            var resultsByObject = results.ToLookup(r => r.HitObject);
            var misses = new List<ReplayMiss>();

            foreach (var hitObject in beatmap.HitObjects)
            {
                var own = resultsByObject[hitObject].LastOrDefault();

                if (own == null)
                    continue;

                var parts = nestedOf(hitObject).SelectMany(n => resultsByObject[n]).ToArray();

                // the one judged at a time that can be early or late: a circle, or the head of a slider.
                var timed = new[] { own }.Concat(parts).FirstOrDefault(r => r.HitObject.HitWindows != HitWindows.Empty);
                double? offset = timed?.IsHit == true ? timed.TimeOffset : null;

                // what the object is shown to have got: its own, or where it gets nothing of its own that counts (a
                // slider, unless sliders are played the classic way), that of its head.
                var shown = own.Type.IsBasic() ? own : timed ?? own;

                if (!shown.IsHit)
                {
                    misses.Add(new ReplayMiss(hitObject, hitObject.StartTime, ReplayMissKind.Miss, null));
                    continue;
                }

                var broken = parts.Where(r => r.Type.BreaksCombo()).MinBy(r => r.HitObject.StartTime);

                if (broken != null)
                {
                    misses.Add(new ReplayMiss(hitObject, broken.HitObject.StartTime, ReplayMissKind.Break, offset));
                    continue;
                }

                switch (shown.Type)
                {
                    case HitResult.Meh:
                        misses.Add(new ReplayMiss(hitObject, hitObject.StartTime, ReplayMissKind.Meh, offset));
                        break;

                    case HitResult.Ok:
                        misses.Add(new ReplayMiss(hitObject, hitObject.StartTime, ReplayMissKind.Ok, offset));
                        break;
                }
            }

            return misses;
        }

        private static IEnumerable<HitObject> nestedOf(HitObject hitObject) =>
            hitObject.NestedHitObjects.SelectMany(n => nestedOf(n).Prepend(n));
    }

    public enum ReplayMissKind
    {
        /// <summary>
        /// The object was missed.
        /// </summary>
        Miss,

        /// <summary>
        /// The object was hit, but a part of it broke the combo, like a slider's tick.
        /// </summary>
        Break,

        /// <summary>
        /// A 50.
        /// </summary>
        Meh,

        /// <summary>
        /// A 100.
        /// </summary>
        Ok,
    }
}
