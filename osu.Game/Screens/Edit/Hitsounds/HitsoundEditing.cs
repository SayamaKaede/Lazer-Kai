// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Graphics.UserInterface;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects.Legacy;
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
        /// Whether the points' normal sounds come from <paramref name="bankName"/> of their own, or for auto, from their
        /// green line, as a sampleset of 0 has it in stable (2026-10-10).
        /// </summary>
        public static TernaryState BankState(IEnumerable<HitPoint> points, string bankName)
        {
            var normals = points.SelectMany(p => p.Samples).Where(s => s.Name == HitSampleInfo.HIT_NORMAL);

            return bankName == HitObjectComposer.HIT_BANK_AUTO
                ? normals.GetTernaryState(FollowsLine)
                : normals.GetTernaryState(s => !FollowsLine(s) && s.Bank == bankName);
        }

        /// <summary>
        /// Whether a normal sound's bank is its green line's.
        /// </summary>
        public static bool FollowsLine(HitSampleInfo sample) => sample is ConvertHitObjectParser.LegacyHitSampleInfo { FollowsLine: true };

        /// <summary>
        /// <paramref name="sample"/> following its green line's bank, or keeping its own.
        /// </summary>
        public static HitSampleInfo FollowingLine(HitSampleInfo sample, bool followsLine)
        {
            if (sample is ConvertHitObjectParser.LegacyHitSampleInfo legacy)
                return legacy.FollowsLine == followsLine ? legacy : legacy.WithFollowingLine(followsLine);

            if (!followsLine)
                return sample;

            int customSampleBank = int.TryParse(sample.Suffix, out int index) ? index : sample.UseBeatmapSamples ? 1 : 0;
            return new ConvertHitObjectParser.LegacyHitSampleInfo(sample.Name, sample.Bank, sample.Volume, sample.EditorAutoBank, customSampleBank) { FollowsLine = true };
        }

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
        /// Puts the points' normal sounds, and the additions that follow them, on <paramref name="bankName"/>, or for auto,
        /// on their green line's, to follow it from then on.
        /// </summary>
        /// <param name="points">The points to change.</param>
        /// <param name="bankName">The bank, or auto.</param>
        /// <param name="lineBank">The bank of the green line a point reads, or null where there are no lines: auto then
        /// keeps the bank there is.</param>
        /// <returns>The points that changed.</returns>
        public static IReadOnlyList<HitPoint> SetBank(IReadOnlyList<HitPoint> points, string bankName, Func<HitPoint, string?>? lineBank = null)
        {
            bool auto = bankName == HitObjectComposer.HIT_BANK_AUTO;

            return change(points, (point, samples) =>
            {
                string bank = auto ? lineBank?.Invoke(point) ?? normalBankOf(samples) : bankName;

                return samples.Select(s => s.Name == HitSampleInfo.HIT_NORMAL
                                  ? FollowingLine(s.With(newBank: bank), auto)
                                  : s.EditorAutoBank
                                      ? s.With(newBank: bank)
                                      : s)
                              .ToList();
            });
        }

        private static string normalBankOf(IEnumerable<HitSampleInfo> samples) => samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL)?.Bank ?? HitSampleInfo.BANK_NORMAL;

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
        /// What a point says in the buttons' terms: its sampleset, its additions' bank and its additions. Of several
        /// points, the first's banks and every addition any of them has, as layered points are heard together.
        /// </summary>
        public static PointHitsound HitsoundOf(IEnumerable<HitPoint> points)
        {
            var samples = points.SelectMany(p => p.Samples).ToArray();

            var normal = samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);
            string bank = normal == null ? HitSampleInfo.BANK_NORMAL : FollowsLine(normal) ? HitObjectComposer.HIT_BANK_AUTO : normal.Bank;
            var additions = samples.Where(s => HitSampleInfo.ALL_ADDITIONS.Contains(s.Name)).ToArray();
            string? additionBank = additions.Length == 0 ? null : additions[0].EditorAutoBank ? HitObjectComposer.HIT_BANK_AUTO : additions[0].Bank;

            return new PointHitsound(bank, additionBank, additions.Select(s => s.Name).Distinct().ToArray());
        }

        /// <summary>
        /// Gives the points what <paramref name="hitsound"/> says: its sampleset, and its additions on its additions' bank
        /// and no others. Each keeps its own volume and custom index, which in stable are the green line's.
        /// </summary>
        /// <param name="points">The points to change.</param>
        /// <param name="hitsound">What they are to say.</param>
        /// <param name="lineBank">The bank of the green line a point reads, for an auto sampleset: see <see cref="SetBank"/>.</param>
        /// <returns>The points that changed.</returns>
        public static IReadOnlyList<HitPoint> SetHitsound(IReadOnlyList<HitPoint> points, PointHitsound hitsound, Func<HitPoint, string?>? lineBank = null) =>
            change(points, (point, samples) => WithHitsound(samples, hitsound, lineBank?.Invoke(point)));

        /// <summary>
        /// <paramref name="samples"/> as <paramref name="hitsound"/> says, each sound keeping its volume and custom index:
        /// an addition put on takes them from the normal sound, as one put on with a button does.
        /// </summary>
        /// <param name="samples">The sounds as they are.</param>
        /// <param name="hitsound">What they are to say.</param>
        /// <param name="lineBank">The bank of their green line, which an auto sampleset follows; null to keep the bank there is.</param>
        public static IList<HitSampleInfo> WithHitsound(IList<HitSampleInfo> samples, PointHitsound hitsound, string? lineBank = null)
        {
            var normal = samples.FirstOrDefault(s => s.Name == HitSampleInfo.HIT_NORMAL);

            bool followsLine = hitsound.Bank == HitObjectComposer.HIT_BANK_AUTO;
            string bank = followsLine ? lineBank ?? normal?.Bank ?? HitSampleInfo.BANK_NORMAL : hitsound.Bank;

            var newNormal = FollowingLine(normal?.With(newBank: bank) ?? new HitSampleInfo(HitSampleInfo.HIT_NORMAL, bank, editorAutoBank: false), followsLine);

            bool auto = hitsound.AdditionBank == null || hitsound.AdditionBank == HitObjectComposer.HIT_BANK_AUTO;
            string additionBank = auto ? newNormal.Bank : hitsound.AdditionBank!;

            // anything that is neither, such as a sample from a file, stays where it was.
            var result = samples.Where(s => !HitSampleInfo.ALL_ADDITIONS.Contains(s.Name))
                                .Select(s => s.Name == HitSampleInfo.HIT_NORMAL ? newNormal : s)
                                .ToList();

            if (normal == null)
                result.Insert(0, newNormal);

            foreach (string name in hitsound.Additions)
            {
                var from = samples.FirstOrDefault(s => s.Name == name) ?? newNormal;
                result.Add(from.With(newName: name, newBank: additionBank, newEditorAutoBank: auto));
            }

            return result;
        }

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

        private static IReadOnlyList<HitPoint> change(IReadOnlyList<HitPoint> points, Func<IList<HitSampleInfo>, IList<HitSampleInfo>> changeSamples) =>
            change(points, (_, samples) => changeSamples(samples));

        private static IReadOnlyList<HitPoint> change(IReadOnlyList<HitPoint> points, Func<HitPoint, IList<HitSampleInfo>, IList<HitSampleInfo>> changeSamples)
        {
            var changed = new List<HitPoint>();

            foreach (var point in points)
            {
                var samples = point.Samples;
                var newSamples = changeSamples(point, samples);

                // a sample's own equality leaves out whether its bank follows the sampleset, and its volume: additions
                // given the bank they were following were taken for unchanged (2026-10-08).
                if (newSamples.Count == samples.Count
                    && newSamples.Zip(samples).All(p => p.First.Equals(p.Second) && p.First.EditorAutoBank == p.Second.EditorAutoBank && p.First.Volume == p.Second.Volume))
                    continue;

                point.SetSamples(newSamples);
                changed.Add(point);
            }

            return changed;
        }
    }

    /// <summary>
    /// A point's sounds in the hitsound buttons' terms, to be given to other points.
    /// </summary>
    /// <param name="Bank">The sampleset, auto for the green line's.</param>
    /// <param name="AdditionBank">The additions' bank, auto for one that follows the sampleset, or null without additions.</param>
    /// <param name="Additions">Which of whistle, finish and clap there are.</param>
    public readonly record struct PointHitsound(string Bank, string? AdditionBank, IReadOnlyList<string> Additions);

    /// <summary>
    /// One of the hitsounds copied on the hitsound screen.
    /// </summary>
    /// <param name="Offset">How long after the first of those copied it is heard.</param>
    /// <param name="Hitsound">What it says.</param>
    public readonly record struct CopiedHitsound(double Offset, PointHitsound Hitsound);
}
