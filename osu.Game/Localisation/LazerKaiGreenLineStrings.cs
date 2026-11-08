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
        /// "A green line cannot be added before the first red line"
        /// </summary>
        public static LocalisableString NothingToCarryOnFrom => new TranslatableString(getKey(@"nothing_to_carry_on_from"), @"A green line cannot be added before the first red line");

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
        /// "Remove green lines this difficulty does not use"
        /// </summary>
        public static LocalisableString RemoveUnusedHere => new TranslatableString(getKey(@"remove_unused_here"), @"Remove green lines this difficulty does not use");

        /// <summary>
        /// "Remove the green lines this difficulty does not use?"
        /// </summary>
        public static LocalisableString RemoveUnusedHereHeader => new TranslatableString(getKey(@"remove_unused_here_header"), @"Remove the green lines this difficulty does not use?");

        /// <summary>
        /// "Green lines that change nothing this difficulty plays are removed, including ones kept for the hitsounds of other difficulties: a volume or sampleset no note sounds before the next line, and a velocity no slider starts under. Under a slider or spinner, a change of volume stays. A line that stays takes the velocity of the line before it when no slider starts under it. It can be undone."
        /// </summary>
        public static LocalisableString RemoveUnusedHereBody => new TranslatableString(getKey(@"remove_unused_here_body"),
            @"Green lines that change nothing this difficulty plays are removed, including ones kept for the hitsounds of other difficulties: a volume or sampleset no note sounds before the next line, and a velocity no slider starts under. Under a slider or spinner, a change of volume stays. A line that stays takes the velocity of the line before it when no slider starts under it. It can be undone.");

        /// <summary>
        /// "Removed {0} green lines and set the velocity of {1} back to the line before."
        /// </summary>
        public static LocalisableString RemovedUnusedHere(int removed, int setBack) => new TranslatableString(getKey(@"removed_unused_here"),
            @"Removed {0} green lines and set the velocity of {1} back to the line before.", removed, setBack);

        /// <summary>
        /// "There were no green lines this difficulty does not use."
        /// </summary>
        public static LocalisableString NoneUnusedHere => new TranslatableString(getKey(@"none_unused_here"), @"There were no green lines this difficulty does not use.");

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

        /// <summary>
        /// "Copy timing..."
        /// </summary>
        public static LocalisableString CopyRedLines => new TranslatableString(getKey(@"copy_red_lines"), @"Copy timing...");

        /// <summary>
        /// "Copy timing"
        /// </summary>
        public static LocalisableString CopyRedLinesHeader => new TranslatableString(getKey(@"copy_red_lines_header"), @"Copy timing");

        /// <summary>
        /// "Puts the timing of one difficulty in the place of another's: where its red lines are, the BPM and the time signature."
        /// </summary>
        public static LocalisableString CopyRedLinesBody => new TranslatableString(getKey(@"copy_red_lines_body"),
            @"Puts the timing of one difficulty in the place of another's: where its red lines are, the BPM and the time signature.");

        /// <summary>
        /// "Direction"
        /// </summary>
        public static LocalisableString CopyRedLinesDirection => new TranslatableString(getKey(@"copy_red_lines_direction"), @"Direction");

        /// <summary>
        /// "Another difficulty → this difficulty"
        /// </summary>
        public static LocalisableString CopyRedLinesFromAnother => new TranslatableString(getKey(@"copy_red_lines_from_another"), @"Another difficulty → this difficulty");

        /// <summary>
        /// "This difficulty → other difficulties"
        /// </summary>
        public static LocalisableString CopyRedLinesToOthers => new TranslatableString(getKey(@"copy_red_lines_to_others"), @"This difficulty → other difficulties");

        /// <summary>
        /// "Copy from"
        /// </summary>
        public static LocalisableString CopyRedLinesSource => new TranslatableString(getKey(@"copy_red_lines_source"), @"Copy from");

        /// <summary>
        /// "Copy to"
        /// </summary>
        public static LocalisableString CopyRedLinesTargets => new TranslatableString(getKey(@"copy_red_lines_targets"), @"Copy to");

        /// <summary>
        /// "The difficulties ticked are written to at once. This cannot be undone."
        /// </summary>
        public static LocalisableString CopyRedLinesToOthersWarning => new TranslatableString(getKey(@"copy_red_lines_to_others_warning"),
            @"The difficulties ticked are written to at once. This cannot be undone.");

        /// <summary>
        /// "Move the objects with the timing"
        /// </summary>
        public static LocalisableString CopyRedLinesKeepBeats => new TranslatableString(getKey(@"copy_red_lines_keep_beats"), @"Move the objects with the timing");

        /// <summary>
        /// "Objects, green lines, bookmarks and breaks go to the same tick of the same bar under the new timing: as far on as the offset is, and further apart where the BPM is slower. Off, only what is within the milliseconds below of a tick is moved."
        /// </summary>
        public static LocalisableString CopyRedLinesKeepBeatsHint => new TranslatableString(getKey(@"copy_red_lines_keep_beats_hint"),
            @"Objects, green lines, bookmarks and breaks go to the same tick of the same bar under the new timing: as far on as the offset is, and further apart where the BPM is slower. Off, only what is within the milliseconds below of a tick is moved.");

        /// <summary>
        /// "Snap what is within (ms)"
        /// </summary>
        public static LocalisableString CopyRedLinesWithin => new TranslatableString(getKey(@"copy_red_lines_within"), @"Snap what is within (ms)");

        /// <summary>
        /// "Once the timing is replaced, objects, green lines and bookmarks within this many milliseconds of a tick are put on it. Nothing is snapped for 0."
        /// </summary>
        public static LocalisableString CopyRedLinesWithinHint => new TranslatableString(getKey(@"copy_red_lines_within_hint"), @"Once the timing is replaced, objects, green lines and bookmarks within this many milliseconds of a tick are put on it. Nothing is snapped for 0.");

        /// <summary>
        /// "Copy"
        /// </summary>
        public static LocalisableString CopyRedLinesApply => new TranslatableString(getKey(@"copy_red_lines_apply"), @"Copy");

        /// <summary>
        /// "The difficulty to copy from has no red lines."
        /// </summary>
        public static LocalisableString CopyRedLinesNone => new TranslatableString(getKey(@"copy_red_lines_none"), @"The difficulty to copy from has no red lines.");

        /// <summary>
        /// "Copied the timing ({0} red lines). Put on a tick: {1} objects, {2} green lines. Objects off every tick: {3}"
        /// </summary>
        public static LocalisableString CopyRedLinesDone(int redLines, int objects, int lines, int offTicks) => new TranslatableString(getKey(@"copy_red_lines_done"),
            @"Copied the timing ({0} red lines). Put on a tick: {1} objects, {2} green lines. Objects off every tick: {3}", redLines, objects, lines, offTicks);

        /// <summary>
        /// "Copied the timing to {0} difficulties. Put on a tick: {1} objects, {2} green lines."
        /// </summary>
        public static LocalisableString CopyRedLinesDoneToOthers(int difficulties, int objects, int lines) => new TranslatableString(getKey(@"copy_red_lines_done_to_others"),
            @"Copied the timing to {0} difficulties. Put on a tick: {1} objects, {2} green lines.", difficulties, objects, lines);

        /// <summary>
        /// "Objects off every tick remain in: {0}"
        /// </summary>
        public static LocalisableString CopyRedLinesOffTicksIn(string difficulties) => new TranslatableString(getKey(@"copy_red_lines_off_ticks_in"),
            @"Objects off every tick remain in: {0}", difficulties);

        /// <summary>
        /// "Not copied to these, which could not be kept in stable's format: {0}"
        /// </summary>
        public static LocalisableString CopyRedLinesNotKept(string difficulties) => new TranslatableString(getKey(@"copy_red_lines_not_kept"),
            @"Not copied to these, which could not be kept in stable's format: {0}", difficulties);

        /// <summary>
        /// "Could not copy to: {0}"
        /// </summary>
        public static LocalisableString CopyRedLinesFailed(string difficulties) => new TranslatableString(getKey(@"copy_red_lines_failed"), @"Could not copy to: {0}", difficulties);

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
