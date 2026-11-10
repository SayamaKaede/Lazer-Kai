// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiSnapshotStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiSnapshot";

        /// <summary>
        /// "Snapshots"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Snapshots");

        /// <summary>
        /// "This difficulty"
        /// </summary>
        public static LocalisableString ThisDifficulty => new TranslatableString(getKey(@"this_difficulty"), @"This difficulty");

        /// <summary>
        /// "Whole set"
        /// </summary>
        public static LocalisableString WholeSet => new TranslatableString(getKey(@"whole_set"), @"Whole set");

        /// <summary>
        /// "No snapshots yet. What changes is recorded day by day"
        /// </summary>
        public static LocalisableString NoSnapshots => new TranslatableString(getKey(@"no_snapshots"), @"No snapshots yet. What changes is recorded day by day");

        /// <summary>
        /// "Snapshots are not recorded for this beatmap. They can be turned on below"
        /// </summary>
        public static LocalisableString NotRecording => new TranslatableString(getKey(@"not_recording"), @"Snapshots are not recorded for this beatmap. They can be turned on below");

        /// <summary>
        /// "Record snapshots of this beatmap"
        /// </summary>
        public static LocalisableString Record => new TranslatableString(getKey(@"record"), @"Record snapshots of this beatmap");

        /// <summary>
        /// "Days to keep (empty for no limit)"
        /// </summary>
        public static LocalisableString KeepDays => new TranslatableString(getKey(@"keep_days"), @"Days to keep (empty for no limit)");

        /// <summary>
        /// "No changes"
        /// </summary>
        public static LocalisableString NoChanges => new TranslatableString(getKey(@"no_changes"), @"No changes");

        /// <summary>
        /// "Nothing recorded on this day"
        /// </summary>
        public static LocalisableString NotSaved => new TranslatableString(getKey(@"not_saved"), @"Nothing recorded on this day");

        /// <summary>
        /// "The first record. There is nothing earlier to compare it with"
        /// </summary>
        public static LocalisableString FirstRecord => new TranslatableString(getKey(@"first_record"), @"The first record. There is nothing earlier to compare it with");

        /// <summary>
        /// "{0} more"
        /// </summary>
        public static LocalisableString More(int count) => new TranslatableString(getKey(@"more"), @"{0} more", count);

        /// <summary>
        /// "Added: {0}"
        /// </summary>
        public static LocalisableString Added(LocalisableString what) => new TranslatableString(getKey(@"added"), @"Added: {0}", what);

        /// <summary>
        /// "Removed: {0}"
        /// </summary>
        public static LocalisableString Removed(LocalisableString what) => new TranslatableString(getKey(@"removed"), @"Removed: {0}", what);

        /// <summary>
        /// "Changed: {0}"
        /// </summary>
        public static LocalisableString Changed(LocalisableString what) => new TranslatableString(getKey(@"changed"), @"Changed: {0}", what);

        /// <summary>
        /// "Changed: {0} ({1})"
        /// </summary>
        public static LocalisableString ChangedIn(LocalisableString what, LocalisableString aspects) => new TranslatableString(getKey(@"changed_in"), @"Changed: {0} ({1})", what, aspects);

        /// <summary>
        /// ", "
        /// </summary>
        public static LocalisableString Separator => new TranslatableString(getKey(@"separator"), @", ");

        /// <summary>
        /// "file {0}"
        /// </summary>
        public static LocalisableString File(string name) => new TranslatableString(getKey(@"file"), @"file {0}", name);

        /// <summary>
        /// "event line {0}"
        /// </summary>
        public static LocalisableString Event(string line) => new TranslatableString(getKey(@"event"), @"event line {0}", line);

        /// <summary>
        /// "circle"
        /// </summary>
        public static LocalisableString Circle => new TranslatableString(getKey(@"circle"), @"circle");

        /// <summary>
        /// "slider"
        /// </summary>
        public static LocalisableString Slider => new TranslatableString(getKey(@"slider"), @"slider");

        /// <summary>
        /// "spinner"
        /// </summary>
        public static LocalisableString Spinner => new TranslatableString(getKey(@"spinner"), @"spinner");

        /// <summary>
        /// "hold note"
        /// </summary>
        public static LocalisableString HoldNote => new TranslatableString(getKey(@"hold_note"), @"hold note");

        /// <summary>
        /// "red line"
        /// </summary>
        public static LocalisableString RedLine => new TranslatableString(getKey(@"red_line"), @"red line");

        /// <summary>
        /// "green line"
        /// </summary>
        public static LocalisableString GreenLine => new TranslatableString(getKey(@"green_line"), @"green line");

        /// <summary>
        /// "bookmark"
        /// </summary>
        public static LocalisableString Bookmark => new TranslatableString(getKey(@"bookmark"), @"bookmark");

        /// <summary>
        /// "break"
        /// </summary>
        public static LocalisableString Break => new TranslatableString(getKey(@"break"), @"break");

        /// <summary>
        /// "position"
        /// </summary>
        public static LocalisableString Position => new TranslatableString(getKey(@"position"), @"position");

        /// <summary>
        /// "New Combo"
        /// </summary>
        public static LocalisableString NewCombo => new TranslatableString(getKey(@"new_combo"), @"New Combo");

        /// <summary>
        /// "hitsound"
        /// </summary>
        public static LocalisableString Hitsound => new TranslatableString(getKey(@"hitsound"), @"hitsound");

        /// <summary>
        /// "anchors"
        /// </summary>
        public static LocalisableString Shape => new TranslatableString(getKey(@"shape"), @"anchors");

        /// <summary>
        /// "repeats"
        /// </summary>
        public static LocalisableString Repeats => new TranslatableString(getKey(@"repeats"), @"repeats");

        /// <summary>
        /// "length"
        /// </summary>
        public static LocalisableString Length => new TranslatableString(getKey(@"length"), @"length");

        /// <summary>
        /// "BPM"
        /// </summary>
        public static LocalisableString Bpm => new TranslatableString(getKey(@"bpm"), @"BPM");

        /// <summary>
        /// "time signature"
        /// </summary>
        public static LocalisableString Meter => new TranslatableString(getKey(@"meter"), @"time signature");

        /// <summary>
        /// "SV"
        /// </summary>
        public static LocalisableString SliderVelocity => new TranslatableString(getKey(@"slider_velocity"), @"SV");

        /// <summary>
        /// "volume"
        /// </summary>
        public static LocalisableString Volume => new TranslatableString(getKey(@"volume"), @"volume");

        /// <summary>
        /// "Kiai"
        /// </summary>
        public static LocalisableString Kiai => new TranslatableString(getKey(@"kiai"), @"Kiai");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
