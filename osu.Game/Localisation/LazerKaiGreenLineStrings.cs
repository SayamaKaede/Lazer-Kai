// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiGreenLineStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiGreenLine";

        /// <summary>
        /// "Green lines"
        /// </summary>
        public static LocalisableString Title => new TranslatableString(getKey(@"title"), @"Green lines");

        /// <summary>
        /// "Green line"
        /// </summary>
        public static LocalisableString GreenLine => new TranslatableString(getKey(@"green_line"), @"Green line");

        /// <summary>
        /// "Red line"
        /// </summary>
        public static LocalisableString RedLine => new TranslatableString(getKey(@"red_line"), @"Red line");

        /// <summary>
        /// "Add"
        /// </summary>
        public static LocalisableString Add => new TranslatableString(getKey(@"add"), @"Add");

        /// <summary>
        /// "Remove"
        /// </summary>
        public static LocalisableString Remove => new TranslatableString(getKey(@"remove"), @"Remove");

        /// <summary>
        /// "Cancel"
        /// </summary>
        public static LocalisableString Cancel => new TranslatableString(getKey(@"cancel"), @"Cancel");

        /// <summary>
        /// "Confirm"
        /// </summary>
        public static LocalisableString Confirm => new TranslatableString(getKey(@"confirm"), @"Confirm");

        /// <summary>
        /// "Use current time"
        /// </summary>
        public static LocalisableString UseCurrentTime => new TranslatableString(getKey(@"use_current_time"), @"Use current time");

        /// <summary>
        /// "Paste the selected lines from the current time"
        /// </summary>
        public static LocalisableString PasteAtCurrentTime => new TranslatableString(getKey(@"paste_at_current_time"), @"Paste the selected lines from the current time");

        /// <summary>
        /// "Time"
        /// </summary>
        public static LocalisableString Time => new TranslatableString(getKey(@"time"), @"Time");

        /// <summary>
        /// "Slider velocity"
        /// </summary>
        public static LocalisableString SliderVelocity => new TranslatableString(getKey(@"slider_velocity"), @"Slider velocity");

        /// <summary>
        /// "Sample set"
        /// </summary>
        public static LocalisableString SampleSet => new TranslatableString(getKey(@"sample_set"), @"Sample set");

        /// <summary>
        /// "Custom index"
        /// </summary>
        public static LocalisableString CustomIndex => new TranslatableString(getKey(@"custom_index"), @"Custom index");

        /// <summary>
        /// "Volume"
        /// </summary>
        public static LocalisableString Volume => new TranslatableString(getKey(@"volume"), @"Volume");

        /// <summary>
        /// "Green lines can be edited on osu! beatmaps in stable's format (v14 and earlier), with "Improve compatibility with stable" on."
        /// </summary>
        public static LocalisableString NotEditable => new TranslatableString(getKey(@"not_editable"),
            @"Green lines can be edited on osu! beatmaps in stable's format (v14 and earlier), with ""Improve compatibility with stable"" on.");

        /// <summary>
        /// "Use the green line's velocity ({0}x)"
        /// </summary>
        public static LocalisableString UseGreenLineVelocity(string velocity) => new TranslatableString(getKey(@"use_green_line_velocity"), @"Use the green line's velocity ({0}x)", velocity);

        /// <summary>
        /// "Using the green line's velocity"
        /// </summary>
        public static LocalisableString UsingGreenLineVelocity => new TranslatableString(getKey(@"using_green_line_velocity"), @"Using the green line's velocity");

        /// <summary>
        /// "Remove unneeded green lines"
        /// </summary>
        public static LocalisableString RemoveUnneeded => new TranslatableString(getKey(@"remove_unneeded"), @"Remove unneeded green lines");

        /// <summary>
        /// "Remove the unneeded green lines?"
        /// </summary>
        public static LocalisableString RemoveUnneededHeader => new TranslatableString(getKey(@"remove_unneeded_header"), @"Remove the unneeded green lines?");

        /// <summary>
        /// "Green lines that change nothing are removed: ones saying what the line before them does, and ones saying what another line at the same time does. Lines at the same time that say different things stay. It can be undone."
        /// </summary>
        public static LocalisableString RemoveUnneededBody => new TranslatableString(getKey(@"remove_unneeded_body"),
            @"Green lines that change nothing are removed: ones saying what the line before them does, and ones saying what another line at the same time does. Lines at the same time that say different things stay. It can be undone.");

        /// <summary>
        /// "Removed {0} unneeded green lines."
        /// </summary>
        public static LocalisableString Removed(int count) => new TranslatableString(getKey(@"removed"), @"Removed {0} unneeded green lines.", count);

        /// <summary>
        /// "There were no unneeded green lines."
        /// </summary>
        public static LocalisableString NoneRemoved => new TranslatableString(getKey(@"none_removed"), @"There were no unneeded green lines.");

        /// <summary>
        /// "All"
        /// </summary>
        public static LocalisableString FilterAll => new TranslatableString(getKey(@"filter_all"), @"All");

        /// <summary>
        /// "Red lines"
        /// </summary>
        public static LocalisableString FilterRedLines => new TranslatableString(getKey(@"filter_red_lines"), @"Red lines");

        /// <summary>
        /// "Green lines"
        /// </summary>
        public static LocalisableString FilterGreenLines => new TranslatableString(getKey(@"filter_green_lines"), @"Green lines");

        /// <summary>
        /// "Adjust the offset of the whole beatmap..."
        /// </summary>
        public static LocalisableString WholeOffset => new TranslatableString(getKey(@"whole_offset"), @"Adjust the offset of the whole beatmap...");

        /// <summary>
        /// "Adjust the offset of the whole beatmap"
        /// </summary>
        public static LocalisableString WholeOffsetHeader => new TranslatableString(getKey(@"whole_offset_header"), @"Adjust the offset of the whole beatmap");

        /// <summary>
        /// "The red lines, green lines, objects, breaks, bookmarks and the preview point are all moved by the same amount. Objects and green lines less than 2 ms off a tick are then put on it. It can be undone."
        /// </summary>
        public static LocalisableString WholeOffsetBody => new TranslatableString(getKey(@"whole_offset_body"),
            @"The red lines, green lines, objects, breaks, bookmarks and the preview point are all moved by the same amount. Objects and green lines less than 2 ms off a tick are then put on it. It can be undone.");

        /// <summary>
        /// "Milliseconds to move by"
        /// </summary>
        public static LocalisableString WholeOffsetAmount => new TranslatableString(getKey(@"whole_offset_amount"), @"Milliseconds to move by");

        /// <summary>
        /// "A negative number moves everything earlier. With 0, nothing is moved and only what is off a tick is put on it."
        /// </summary>
        public static LocalisableString WholeOffsetAmountHint => new TranslatableString(getKey(@"whole_offset_amount_hint"),
            @"A negative number moves everything earlier. With 0, nothing is moved and only what is off a tick is put on it.");

        /// <summary>
        /// "Apply"
        /// </summary>
        public static LocalisableString WholeOffsetApply => new TranslatableString(getKey(@"whole_offset_apply"), @"Apply");

        /// <summary>
        /// "Moved everything by {0} ms. Put on a tick: {1} objects, {2} green lines."
        /// </summary>
        public static LocalisableString WholeOffsetMoved(string amount, int objects, int lines) =>
            new TranslatableString(getKey(@"whole_offset_moved"), @"Moved everything by {0} ms. Put on a tick: {1} objects, {2} green lines.", amount, objects, lines);

        /// <summary>
        /// "Confirm or cancel the changes to the green lines first."
        /// </summary>
        public static LocalisableString WholeOffsetPending => new TranslatableString(getKey(@"whole_offset_pending"), @"Confirm or cancel the changes to the green lines first.");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
