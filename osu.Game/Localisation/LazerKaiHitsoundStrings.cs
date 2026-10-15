// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiHitsoundStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiHitsound";

        /// <summary>
        /// "Hitsounds"
        /// </summary>
        public static LocalisableString Additions => new TranslatableString(getKey(@"additions"), @"Hitsounds");

        /// <summary>
        /// "Sampleset"
        /// </summary>
        public static LocalisableString Sampleset => new TranslatableString(getKey(@"sampleset"), @"Sampleset");

        /// <summary>
        /// "Additions"
        /// </summary>
        public static LocalisableString AdditionBank => new TranslatableString(getKey(@"addition_bank"), @"Additions");

        /// <summary>
        /// "{0} selected objects"
        /// </summary>
        public static LocalisableString Selected(int count) => new TranslatableString(getKey(@"selected"), @"{0} selected objects", count);

        /// <summary>
        /// "circle"
        /// </summary>
        public static LocalisableString Circle => new TranslatableString(getKey(@"circle"), @"circle");

        /// <summary>
        /// "slider head"
        /// </summary>
        public static LocalisableString SliderHead => new TranslatableString(getKey(@"slider_head"), @"slider head");

        /// <summary>
        /// "slider reverse"
        /// </summary>
        public static LocalisableString SliderReverse => new TranslatableString(getKey(@"slider_reverse"), @"slider reverse");

        /// <summary>
        /// "slider tail"
        /// </summary>
        public static LocalisableString SliderTail => new TranslatableString(getKey(@"slider_tail"), @"slider tail");

        /// <summary>
        /// "spinner end"
        /// </summary>
        public static LocalisableString SpinnerEnd => new TranslatableString(getKey(@"spinner_end"), @"spinner end");

        /// <summary>
        /// "Copy of the objects"
        /// </summary>
        public static LocalisableString CopyIntoNewDifficulty => new TranslatableString(getKey(@"copy_into_new_difficulty"), @"Copy of the objects");

        /// <summary>
        /// "Circles where sounds are played"
        /// </summary>
        public static LocalisableString MakeHitsoundDifficulty => new TranslatableString(getKey(@"make_hitsound_difficulty"), @"Circles where sounds are played");

        /// <summary>
        /// "For an osu! beatmap in v14 format, while improved stable compatibility is on"
        /// </summary>
        public static LocalisableString NewDifficultyUnavailable => new TranslatableString(getKey(@"new_difficulty_unavailable"), @"For an osu! beatmap in v14 format, while improved stable compatibility is on");

        /// <summary>
        /// "Not every object could be read, so no difficulty was made"
        /// </summary>
        public static LocalisableString CouldNotReadObjects => new TranslatableString(getKey(@"could_not_read_objects"), @"Not every object could be read, so no difficulty was made");

        /// <summary>
        /// "(selected)"
        /// </summary>
        public static LocalisableString SelectedSuffix => new TranslatableString(getKey(@"selected_suffix"), @"(selected)");

        /// <summary>
        /// "slider body"
        /// </summary>
        public static LocalisableString SliderBody => new TranslatableString(getKey(@"slider_body"), @"slider body");

        /// <summary>
        /// "Selection outline colour"
        /// </summary>
        public static LocalisableString OutlineColour => new TranslatableString(getKey(@"outline_colour"), @"Selection outline colour");

        /// <summary>
        /// "Selection outline thickness"
        /// </summary>
        public static LocalisableString OutlineThickness => new TranslatableString(getKey(@"outline_thickness"), @"Selection outline thickness");

        /// <summary>
        /// "Selection outline opacity"
        /// </summary>
        public static LocalisableString OutlineOpacity => new TranslatableString(getKey(@"outline_opacity"), @"Selection outline opacity");

        /// <summary>
        /// "New difficulty"
        /// </summary>
        public static LocalisableString NewDifficultyHeader => new TranslatableString(getKey(@"new_difficulty_header"), @"New difficulty");

        /// <summary>
        /// "Settings"
        /// </summary>
        public static LocalisableString DisplayHeader => new TranslatableString(getKey(@"display_header"), @"Settings");

        /// <summary>
        /// "Seek on by the previous gap after placing"
        /// </summary>
        public static LocalisableString AdvanceByPreviousSpacing => new TranslatableString(getKey(@"advance_by_previous_spacing"), @"Seek on by the previous gap after placing");

        /// <summary>
        /// "Paste hitsounds only"
        /// </summary>
        public static LocalisableString PasteHitsoundsOnly => new TranslatableString(getKey(@"paste_hitsounds_only"), @"Paste hitsounds only");

        /// <summary>
        /// "Opacity of the middle"
        /// </summary>
        public static LocalisableString PanelOpacity => new TranslatableString(getKey(@"panel_opacity"), @"Opacity of the middle");

        /// <summary>
        /// "Sound on a change"
        /// </summary>
        public static LocalisableString ChangeSound => new TranslatableString(getKey(@"change_sound"), @"Sound on a change");

        /// <summary>
        /// "Menu sound"
        /// </summary>
        public static LocalisableString ChangeSoundMenu => new TranslatableString(getKey(@"change_sound_menu"), @"Menu sound");

        /// <summary>
        /// "The hitsound"
        /// </summary>
        public static LocalisableString ChangeSoundHitsound => new TranslatableString(getKey(@"change_sound_hitsound"), @"The hitsound");

        /// <summary>
        /// "Copy the previous hitsounds"
        /// </summary>
        public static LocalisableString SameAsPrevious => new TranslatableString(getKey(@"same_as_previous"), @"Copy the previous hitsounds");

        /// <summary>
        /// "Copy hitsounds"
        /// </summary>
        public static LocalisableString CopyHitsounds => new TranslatableString(getKey(@"copy_hitsounds"), @"Copy hitsounds");

        /// <summary>
        /// "Paste hitsounds"
        /// </summary>
        public static LocalisableString PasteHitsounds => new TranslatableString(getKey(@"paste_hitsounds"), @"Paste hitsounds");

        /// <summary>
        /// "Copy hitsounds from another difficulty"
        /// </summary>
        public static LocalisableString CopyFromHeader => new TranslatableString(getKey(@"copy_from_header"), @"Copy hitsounds from another difficulty");

        /// <summary>
        /// "Copy from"
        /// </summary>
        public static LocalisableString CopyFromSource => new TranslatableString(getKey(@"copy_from_source"), @"Copy from");

        /// <summary>
        /// "Copy onto this difficulty"
        /// </summary>
        public static LocalisableString CopyFromButton => new TranslatableString(getKey(@"copy_from_button"), @"Copy onto this difficulty");

        /// <summary>
        /// "For an osu! beatmap in v14 format, while improved stable compatibility is on, with another osu! difficulty in the set"
        /// </summary>
        public static LocalisableString CopyFromUnavailable => new TranslatableString(getKey(@"copy_from_unavailable"),
            @"For an osu! beatmap in v14 format, while improved stable compatibility is on, with another osu! difficulty in the set");

        /// <summary>
        /// "Could not read the file of the difficulty to copy from"
        /// </summary>
        public static LocalisableString CouldNotReadDifficulty => new TranslatableString(getKey(@"could_not_read_difficulty"), @"Could not read the file of the difficulty to copy from");

        /// <summary>
        /// "Green lines that may be unsnapped"
        /// </summary>
        public static LocalisableString NearLinesHeader => new TranslatableString(getKey(@"near_lines_header"), @"Green lines that may be unsnapped");

        /// <summary>
        /// "Also show unsnaps that change nothing"
        /// </summary>
        public static LocalisableString ShowHarmlessUnsnaps => new TranslatableString(getKey(@"show_harmless_unsnaps"), @"Also show unsnaps that change nothing");

        /// <summary>
        /// "Default"
        /// </summary>
        public static LocalisableString DefaultIndex => new TranslatableString(getKey(@"default_index"), @"Default");

        /// <summary>
        /// "Keep"
        /// </summary>
        public static LocalisableString Keep => new TranslatableString(getKey(@"keep"), @"Keep");

        /// <summary>
        /// "Kept after placing"
        /// </summary>
        public static LocalisableString KeepTooltip => new TranslatableString(getKey(@"keep_tooltip"), @"Kept after placing");

        /// <summary>
        /// "None"
        /// </summary>
        public static LocalisableString NoNearLines => new TranslatableString(getKey(@"no_near_lines"), @"None");

        /// <summary>
        /// "{1} ms after the {0} (applies)"
        /// </summary>
        public static LocalisableString LineAfterTaken(LocalisableString kind, int distance) =>
            new TranslatableString(getKey(@"line_after_taken"), @"{1} ms after the {0} (applies)", kind, distance);

        /// <summary>
        /// "{1} ms after the {0} (does not apply)"
        /// </summary>
        public static LocalisableString LineAfterNotTaken(LocalisableString kind, int distance) =>
            new TranslatableString(getKey(@"line_after_not_taken"), @"{1} ms after the {0} (does not apply)", kind, distance);

        /// <summary>
        /// "{1} ms before the {0} (applies)"
        /// </summary>
        public static LocalisableString LineBeforeTaken(LocalisableString kind, int distance) =>
            new TranslatableString(getKey(@"line_before_taken"), @"{1} ms before the {0} (applies)", kind, distance);

        /// <summary>
        /// "{1} ms before the {0} (does not apply)"
        /// </summary>
        public static LocalisableString LineBeforeNotTaken(LocalisableString kind, int distance) =>
            new TranslatableString(getKey(@"line_before_not_taken"), @"{1} ms before the {0} (does not apply)", kind, distance);

        /// <summary>
        /// "Green line at {0}"
        /// </summary>
        public static LocalisableString LineAt(string time) => new TranslatableString(getKey(@"line_at"), @"Green line at {0}", time);

        /// <summary>
        /// "Green line at {0} (new)"
        /// </summary>
        public static LocalisableString LineToAdd(string time) => new TranslatableString(getKey(@"line_to_add"), @"Green line at {0} (new)", time);

        /// <summary>
        /// "Green lines of the selection"
        /// </summary>
        public static LocalisableString LinesOfSelection => new TranslatableString(getKey(@"lines_of_selection"), @"Green lines of the selection");

        /// <summary>
        /// "Place a circle"
        /// </summary>
        public static LocalisableString PlaceCircle => new TranslatableString(getKey(@"place_circle"), @"Place a circle");

        /// <summary>
        /// "Place with the previous"
        /// </summary>
        public static LocalisableString PlaceCircleWithPrevious => new TranslatableString(getKey(@"place_circle_with_previous"), @"Place with the previous");

        /// <summary>
        /// "Copied hitsounds ({0}, {1} points)"
        /// </summary>
        public static LocalisableString StatusCopied(string times, int count) =>
            new TranslatableString(getKey(@"status_copied"), @"Copied hitsounds ({0}, {1} points)", times, count);

        /// <summary>
        /// "Copied hitsounds (nothing sounds there)"
        /// </summary>
        public static LocalisableString StatusNothingToCopy => new TranslatableString(getKey(@"status_nothing_to_copy"), @"Copied hitsounds (nothing sounds there)");

        /// <summary>
        /// "Pasted hitsounds ({0}, {1} points)"
        /// </summary>
        public static LocalisableString StatusPasted(string times, int count) =>
            new TranslatableString(getKey(@"status_pasted"), @"Pasted hitsounds ({0}, {1} points)", times, count);

        /// <summary>
        /// "Pasted hitsounds (nothing sounds there)"
        /// </summary>
        public static LocalisableString StatusNothingToPasteOnto => new TranslatableString(getKey(@"status_nothing_to_paste_onto"), @"Pasted hitsounds (nothing sounds there)");

        /// <summary>
        /// "Pasted hitsounds (none copied)"
        /// </summary>
        public static LocalisableString StatusNothingCopied => new TranslatableString(getKey(@"status_nothing_copied"), @"Pasted hitsounds (none copied)");

        /// <summary>
        /// "Copied the previous hitsounds ({0} → {1}, {2} points)"
        /// </summary>
        public static LocalisableString StatusCopiedPrevious(string from, string times, int count) =>
            new TranslatableString(getKey(@"status_copied_previous"), @"Copied the previous hitsounds ({0} → {1}, {2} points)", from, times, count);

        /// <summary>
        /// "Copied the previous hitsounds (nothing sounds there)"
        /// </summary>
        public static LocalisableString StatusNothingToCopyOnto => new TranslatableString(getKey(@"status_nothing_to_copy_onto"), @"Copied the previous hitsounds (nothing sounds there)");

        /// <summary>
        /// "Copied the previous hitsounds (nothing before)"
        /// </summary>
        public static LocalisableString StatusNothingBefore => new TranslatableString(getKey(@"status_nothing_before"), @"Copied the previous hitsounds (nothing before)");

        /// <summary>
        /// "Empty osu!mania difficulty to hitsound in"
        /// </summary>
        public static LocalisableString MakeManiaHitsoundDifficulty => new TranslatableString(getKey(@"make_mania_hitsound_difficulty"), @"Empty osu!mania difficulty to hitsound in");

        /// <summary>
        /// "For an osu! beatmap"
        /// </summary>
        public static LocalisableString ManiaDifficultyUnavailable => new TranslatableString(getKey(@"mania_difficulty_unavailable"), @"For an osu! beatmap");

        /// <summary>
        /// "Lane sounds"
        /// </summary>
        public static LocalisableString LaneSoundsHeader => new TranslatableString(getKey(@"lane_sounds_header"), @"Lane sounds");

        /// <summary>
        /// "A note sounds as the lane it is placed in."
        /// </summary>
        public static LocalisableString LaneSoundsHint => new TranslatableString(getKey(@"lane_sounds_hint"), @"A note sounds as the lane it is placed in.");

        /// <summary>
        /// "Open the hitsound folder"
        /// </summary>
        public static LocalisableString OpenSoundFolder => new TranslatableString(getKey(@"open_sound_folder"), @"Open the hitsound folder");

        /// <summary>
        /// "Reload the hitsound folder"
        /// </summary>
        public static LocalisableString ReloadSoundFolder => new TranslatableString(getKey(@"reload_sound_folder"), @"Reload the hitsound folder");

        /// <summary>
        /// "The hitsound folder is empty. Put wav, ogg or mp3 files in it."
        /// </summary>
        public static LocalisableString SoundFolderEmpty => new TranslatableString(getKey(@"sound_folder_empty"), @"The hitsound folder is empty. Put wav, ogg or mp3 files in it.");

        /// <summary>
        /// "Lane {0}"
        /// </summary>
        public static LocalisableString Lane(int number) => new TranslatableString(getKey(@"lane"), @"Lane {0}", number);

        /// <summary>
        /// "Click to select every note in this lane"
        /// </summary>
        public static LocalisableString SelectLaneNotes => new TranslatableString(getKey(@"select_lane_notes"), @"Click to select every note in this lane");

        /// <summary>
        /// "Lanes {0} are mixed into one file where they sound together"
        /// </summary>
        public static LocalisableString LanesMixed(string lanes) => new TranslatableString(getKey(@"lanes_mixed"), @"Lanes {0} are mixed into one file where they sound together", lanes);

        /// <summary>
        /// "Sample set"
        /// </summary>
        public static LocalisableString LaneBank => new TranslatableString(getKey(@"lane_bank"), @"Sample set");

        /// <summary>
        /// "Hitsound"
        /// </summary>
        public static LocalisableString LaneKind => new TranslatableString(getKey(@"lane_kind"), @"Hitsound");

        /// <summary>
        /// "Play"
        /// </summary>
        public static LocalisableString PlayLaneSound => new TranslatableString(getKey(@"play_lane_sound"), @"Play");

        /// <summary>
        /// "Turn into a hitsound difficulty"
        /// </summary>
        public static LocalisableString ConvertToHitsoundDifficulty => new TranslatableString(getKey(@"convert_to_hitsound_difficulty"), @"Turn into a hitsound difficulty");

        /// <summary>
        /// "With the volumes set here"
        /// </summary>
        public static LocalisableString ConvertVolumes => new TranslatableString(getKey(@"convert_volumes"), @"With the volumes set here");

        /// <summary>
        /// "Without them, the volumes of the hitsound difficulty are kept as they are there."
        /// </summary>
        public static LocalisableString ConvertVolumesHint => new TranslatableString(getKey(@"convert_volumes_hint"), @"Without them, the volumes of the hitsound difficulty are kept as they are there.");

        /// <summary>
        /// "Mute slider bodies"
        /// </summary>
        public static LocalisableString MuteSliderBodies => new TranslatableString(getKey(@"mute_slider_bodies"), @"Mute slider bodies");

        /// <summary>
        /// "Writes a silent sliderslide for every sample set and custom index the lines of the hitsound difficulty say. One of that name already in the beatmap is left as it is."
        /// </summary>
        public static LocalisableString MuteSliderBodiesHint => new TranslatableString(getKey(@"mute_slider_bodies_hint"),
            @"Writes a silent sliderslide for every sample set and custom index the lines of the hitsound difficulty say. One of that name already in the beatmap is left as it is.");

        /// <summary>
        /// "Volume from the playhead on"
        /// </summary>
        public static LocalisableString LaneVolume => new TranslatableString(getKey(@"lane_volume"), @"Volume from the playhead on");

        /// <summary>
        /// "Hit sound of a circle with none"
        /// </summary>
        public static LocalisableString FallbackSound => new TranslatableString(getKey(@"fallback_sound"), @"Hit sound of a circle with none");

        /// <summary>
        /// "For an osu!mania beatmap in v14 format, while improved stable compatibility is on"
        /// </summary>
        public static LocalisableString ConvertManiaUnavailable => new TranslatableString(getKey(@"convert_mania_unavailable"),
            @"For an osu!mania beatmap in v14 format, while improved stable compatibility is on");

        /// <summary>
        /// "There are no notes to turn into circles"
        /// </summary>
        public static LocalisableString NothingToConvert => new TranslatableString(getKey(@"nothing_to_convert"), @"There are no notes to turn into circles");

        /// <summary>
        /// "These sounds come to one sound of a circle and could not be mixed into one file: {0}"
        /// </summary>
        public static LocalisableString CannotMixSounds(string sounds) => new TranslatableString(getKey(@"cannot_mix_sounds"),
            @"These sounds come to one sound of a circle and could not be mixed into one file: {0}", sounds);

        /// <summary>
        /// "The beatmap already has sound files of these names, with other sounds in them: {0}. Replace them?"
        /// </summary>
        public static LocalisableString ReplaceSoundFiles(string files) => new TranslatableString(getKey(@"replace_sound_files"),
            @"The beatmap already has sound files of these names, with other sounds in them: {0}. Replace them?", files);

        /// <summary>
        /// "Turned into {0}: {1} circles, {2} sound files"
        /// </summary>
        public static LocalisableString ConvertedToHitsoundDifficulty(string name, int circles, int files) => new TranslatableString(getKey(@"converted_to_hitsound_difficulty"),
            @"Turned into {0}: {1} circles, {2} sound files", name, circles, files);

        /// <summary>
        /// "Presets"
        /// </summary>
        public static LocalisableString LanePresets => new TranslatableString(getKey(@"lane_presets"), @"Presets");

        /// <summary>
        /// "Choose a preset"
        /// </summary>
        public static LocalisableString ChooseALanePreset => new TranslatableString(getKey(@"choose_a_lane_preset"), @"Choose a preset");

        /// <summary>
        /// "Save as a preset"
        /// </summary>
        public static LocalisableString SaveAsALanePreset => new TranslatableString(getKey(@"save_as_a_lane_preset"), @"Save as a preset");

        /// <summary>
        /// "No presets saved yet."
        /// </summary>
        public static LocalisableString NoLanePresetsYet => new TranslatableString(getKey(@"no_lane_presets_yet"), @"No presets saved yet.");

        /// <summary>
        /// "Preset name"
        /// </summary>
        public static LocalisableString LanePresetName => new TranslatableString(getKey(@"lane_preset_name"), @"Preset name");

        /// <summary>
        /// "Save"
        /// </summary>
        public static LocalisableString SaveLanePreset => new TranslatableString(getKey(@"save_lane_preset"), @"Save");

        /// <summary>
        /// "Overwrite"
        /// </summary>
        public static LocalisableString OverwriteLanePreset => new TranslatableString(getKey(@"overwrite_lane_preset"), @"Overwrite");

        /// <summary>
        /// "No lane has a sound yet."
        /// </summary>
        public static LocalisableString NoLaneSoundsToSave => new TranslatableString(getKey(@"no_lane_sounds_to_save"), @"No lane has a sound yet.");

        /// <summary>
        /// "These sounds are no longer in the hitsound folder, and were left out: {0}"
        /// </summary>
        public static LocalisableString LanePresetSoundsMissing(string sounds) => new TranslatableString(getKey(@"lane_preset_sounds_missing"),
            @"These sounds are no longer in the hitsound folder, and were left out: {0}", sounds);

        /// <summary>
        /// "These sounds are no longer in the hitsound folder: {0}"
        /// </summary>
        public static LocalisableString LaneSoundsMissing(string sounds) => new TranslatableString(getKey(@"lane_sounds_missing"),
            @"These sounds are no longer in the hitsound folder: {0}", sounds);

        /// <summary>
        /// "Turn on "Improve compatibility with stable""
        /// </summary>
        public static LocalisableString NeedsCompatibility => new TranslatableString(getKey(@"needs_compatibility"), @"Turn on ""Improve compatibility with stable""");

        /// <summary>
        /// "Convert this difficulty to v14 format"
        /// </summary>
        public static LocalisableString NeedsConversion => new TranslatableString(getKey(@"needs_conversion"), @"Convert this difficulty to v14 format");

        /// <summary>
        /// "Turn on "Improve compatibility with stable" and convert this difficulty to v14 format"
        /// </summary>
        public static LocalisableString NeedsCompatibilityAndConversion => new TranslatableString(getKey(@"needs_compatibility_and_conversion"),
            @"Turn on ""Improve compatibility with stable"" and convert this difficulty to v14 format");

        /// <summary>
        /// "The green line, copying from another difficulty, making a hitsound difficulty and the list of lines that may be unsnapped are for a difficulty kept in stable's format."
        /// </summary>
        public static LocalisableString StableFormatNeededFor => new TranslatableString(getKey(@"stable_format_needed_for"),
            @"The green line, copying from another difficulty, making a hitsound difficulty and the list of lines that may be unsnapped are for a difficulty kept in stable's format.");

        /// <summary>
        /// "Turn on"
        /// </summary>
        public static LocalisableString TurnOnCompatibility => new TranslatableString(getKey(@"turn_on_compatibility"), @"Turn on");

        /// <summary>
        /// "Turn on and convert to v14 format"
        /// </summary>
        public static LocalisableString TurnOnAndConvert => new TranslatableString(getKey(@"turn_on_and_convert"), @"Turn on and convert to v14 format");

        /// <summary>
        /// "Green line"
        /// </summary>
        public static LocalisableString LineUnavailable => new TranslatableString(getKey(@"line_unavailable"), @"Green line");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
