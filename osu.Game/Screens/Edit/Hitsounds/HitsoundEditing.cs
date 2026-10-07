// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// What the hitsound buttons read from and change on a set of <see cref="HitPoint"/>s.
    /// </summary>
    /// <remarks>
    /// The rules are the compose screen's (<c>HitObjectComposer.SelectionState</c>), which works on whole objects; these
    /// work on single nodes as well, so that the end of a slider can be given a sound of its own.
    /// </remarks>
    public static class HitsoundEditing
    {
        public static TernaryState AdditionState(IEnumerable<HitPoint> points, string sampleName) =>
            points.GetTernaryState(p => p.Samples.Any(s => s.Name == sampleName));

        /// <summary>
        /// Whether the points' normal sounds come from <paramref name="bankName"/>. Never for auto, which only exists
        /// while objects are placed.
        /// </summary>
        public static TernaryState BankState(IEnumerable<HitPoint> points, string bankName) =>
            bankName == HitObjectComposer.HIT_BANK_AUTO
                ? TernaryState.False
                : points.SelectMany(p => p.Samples).Where(s => s.Name == HitSampleInfo.HIT_NORMAL).GetTernaryState(s => s.Bank == bankName);

        /// <summary>
        /// Whether the points' additions come from <paramref name="bankName"/>, or null if none of them has an addition
        /// to say.
        /// </summary>
        public static TernaryState? AdditionBankState(IEnumerable<HitPoint> points, string bankName)
        {
            var additions = points.SelectMany(p => p.Samples).Where(s => s.Name != HitSampleInfo.HIT_NORMAL).ToArray();

            if (additions.Length == 0)
                return null;

            return bankName == HitObjectComposer.HIT_BANK_AUTO
                ? additions.GetTernaryState(s => s.EditorAutoBank)
                : additions.GetTernaryState(s => s.Bank == bankName && !s.EditorAutoBank);
        }

        /// <summary>
        /// Puts <paramref name="sampleName"/> on every point that does not have it, or takes it off every point.
        /// </summary>
        /// <param name="points">The points to change.</param>
        /// <param name="sampleName">The addition.</param>
        /// <param name="on">Whether the points are to have it.</param>
        /// <param name="additionBank">The bank an addition goes on with where a point has no other addition to follow.</param>
        /// <returns>The points that changed.</returns>
        public static IReadOnlyList<HitPoint> SetAddition(IReadOnlyList<HitPoint> points, string sampleName, bool on, string additionBank)
        {
            if (!on)
                return change(points, samples => samples.Where(s => s.Name != sampleName).ToList());

            return change(points, samples => samples.Any(s => s.Name == sampleName) ? samples : samples.Append(createAddition(samples, sampleName, additionBank)).ToList());
        }

        /// <summary>
        /// Puts the points' normal sounds, and the additions that follow them, on <paramref name="bankName"/>.
        /// </summary>
        /// <returns>The points that changed.</returns>
        public static IReadOnlyList<HitPoint> SetBank(IReadOnlyList<HitPoint> points, string bankName)
        {
            if (bankName == HitObjectComposer.HIT_BANK_AUTO)
                return new List<HitPoint>();

            return change(points, samples => samples.Select(s => s.Name == HitSampleInfo.HIT_NORMAL || s.EditorAutoBank ? s.With(newBank: bankName) : s).ToList());
        }

        /// <summary>
        /// Puts the points' additions on <paramref name="bankName"/>, or on whatever bank their normal sounds are on, for
        /// auto.
        /// </summary>
        /// <returns>The points that changed.</returns>
        public static IReadOnlyList<HitPoint> SetAdditionBank(IReadOnlyList<HitPoint> points, string bankName) =>
            change(points, samples =>
            {
                string normalBank = samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL)?.Bank ?? HitSampleInfo.BANK_SOFT;

                return samples.Select(s => s.Name == HitSampleInfo.HIT_NORMAL
                                  ? s
                                  : bankName == HitObjectComposer.HIT_BANK_AUTO
                                      ? s.With(newBank: normalBank, newEditorAutoBank: true)
                                      : s.With(newBank: bankName, newEditorAutoBank: false))
                              .ToList();
            });

        /// <summary>
        /// A new addition for a point with <paramref name="samples"/>: on the bank of an addition it already has, or else
        /// on <paramref name="additionBank"/>, as loud as its normal sound and with the same custom index.
        /// </summary>
        private static HitSampleInfo createAddition(IList<HitSampleInfo> samples, string sampleName, string additionBank)
        {
            var existing = samples.FirstOrDefault(s => s.Name != HitSampleInfo.HIT_NORMAL);

            if (existing != null)
                return existing.With(newName: sampleName);

            var normal = samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            bool auto = additionBank == HitObjectComposer.HIT_BANK_AUTO;

            if (normal == null)
                return new HitSampleInfo(sampleName, auto ? HitSampleInfo.BANK_SOFT : additionBank, editorAutoBank: auto);

            return auto
                ? normal.With(newName: sampleName, newEditorAutoBank: true)
                : normal.With(newName: sampleName, newBank: additionBank, newEditorAutoBank: false);
        }

        private static IReadOnlyList<HitPoint> change(IReadOnlyList<HitPoint> points, System.Func<IList<HitSampleInfo>, IList<HitSampleInfo>> changeSamples)
        {
            var changed = new List<HitPoint>();

            foreach (var point in points)
            {
                var samples = point.Samples;
                var newSamples = changeSamples(samples);

                if (newSamples.SequenceEqual(samples))
                    continue;

                point.SetSamples(newSamples);
                changed.Add(point);
            }

            return changed;
        }
    }
}
