// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Audio;
using osu.Game.Localisation;
using osu.Game.Rulesets.Edit;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// The addition bank a fresh selection starts on.
    /// </summary>
    /// <remarks>
    /// An addition bank can only be held by an addition sample, so it cannot be set on an object until a whistle,
    /// finish or clap is on it. What the editor does instead is carry a pre-selected bank and hand it to the addition
    /// as it is added — this is which bank that starts as.
    /// </remarks>
    public enum EditorAdditionBank
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AdditionBankAuto))]
        Auto,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AdditionBankNormal))]
        Normal,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AdditionBankSoft))]
        Soft,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.AdditionBankDrum))]
        Drum,
    }

    public static class EditorAdditionBankExtensions
    {
        /// <summary>
        /// The name the editor keeps this bank under.
        /// </summary>
        public static string ToBankName(this EditorAdditionBank bank)
        {
            switch (bank)
            {
                case EditorAdditionBank.Normal:
                    return HitSampleInfo.BANK_NORMAL;

                case EditorAdditionBank.Soft:
                    return HitSampleInfo.BANK_SOFT;

                case EditorAdditionBank.Drum:
                    return HitSampleInfo.BANK_DRUM;

                default:
                    return HitObjectComposer.HIT_BANK_AUTO;
            }
        }

        public static LocalisableString GetDescription(this EditorAdditionBank bank) => bank switch
        {
            EditorAdditionBank.Normal => EditorStrings.AdditionBankNormal,
            EditorAdditionBank.Soft => EditorStrings.AdditionBankSoft,
            EditorAdditionBank.Drum => EditorStrings.AdditionBankDrum,
            _ => EditorStrings.AdditionBankAuto,
        };
    }
}
