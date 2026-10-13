// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class LazerKaiBeatmapFilesStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiBeatmapFiles";

        /// <summary>
        /// "Edit file names"
        /// </summary>
        public static LocalisableString EditFileNames => new TranslatableString(getKey(@"edit_file_names"), @"Edit file names");

        /// <summary>
        /// "Drop .wav, .mp3 or .ogg files on this screen to add them to the beatmap (shorter than 15 seconds)."
        /// </summary>
        public static LocalisableString DropHint => new TranslatableString(getKey(@"drop_hint"),
            @"Drop .wav, .mp3 or .ogg files on this screen to add them to the beatmap (shorter than 15 seconds).");

        /// <summary>
        /// "Added {0} sound files."
        /// </summary>
        public static LocalisableString SoundsAdded(int count) => new TranslatableString(getKey(@"sounds_added"), @"Added {0} sound files.", count);

        /// <summary>
        /// "Files of 15 seconds or longer cannot be added: {0}"
        /// </summary>
        public static LocalisableString TooLongToAdd(string files) => new TranslatableString(getKey(@"too_long_to_add"), @"Files of 15 seconds or longer cannot be added: {0}", files);

        /// <summary>
        /// "These files could not be read as sound, and were not added: {0}"
        /// </summary>
        public static LocalisableString NotReadAsSound(string files) => new TranslatableString(getKey(@"not_read_as_sound"),
            @"These files could not be read as sound, and were not added: {0}", files);

        /// <summary>
        /// "Files with the name of this beatmap's song cannot be added: {0}"
        /// </summary>
        public static LocalisableString NameOfTheSongNotAdded(string files) => new TranslatableString(getKey(@"name_of_the_song_not_added"),
            @"Files with the name of this beatmap's song cannot be added: {0}", files);

        /// <summary>
        /// "Import from another beatmap"
        /// </summary>
        public static LocalisableString ImportFromAnotherSet => new TranslatableString(getKey(@"import_from_another_set"), @"Import from another beatmap");

        /// <summary>
        /// "Search beatmaps"
        /// </summary>
        public static LocalisableString SearchSets => new TranslatableString(getKey(@"search_sets"), @"Search beatmaps");

        /// <summary>
        /// "{0} more. Search to narrow them down."
        /// </summary>
        public static LocalisableString MoreSets(int count) => new TranslatableString(getKey(@"more_sets"), @"{0} more. Search to narrow them down.", count);

        /// <summary>
        /// "No beatmaps found."
        /// </summary>
        public static LocalisableString NoSetsFound => new TranslatableString(getKey(@"no_sets_found"), @"No beatmaps found.");

        /// <summary>
        /// "Choose a beatmap on the left."
        /// </summary>
        public static LocalisableString ChooseASet => new TranslatableString(getKey(@"choose_a_set"), @"Choose a beatmap on the left.");

        /// <summary>
        /// "{0} · {1} difficulties"
        /// </summary>
        public static LocalisableString SetSummary(string creator, int difficulties) => new TranslatableString(getKey(@"set_summary"), @"{0} · {1} difficulties", creator, difficulties);

        /// <summary>
        /// "Difficulties"
        /// </summary>
        public static LocalisableString Difficulties => new TranslatableString(getKey(@"difficulties"), @"Difficulties");

        /// <summary>
        /// "Sound files"
        /// </summary>
        public static LocalisableString Sounds => new TranslatableString(getKey(@"sounds"), @"Sound files");

        /// <summary>
        /// "This beatmap has no sound files."
        /// </summary>
        public static LocalisableString NoSounds => new TranslatableString(getKey(@"no_sounds"), @"This beatmap has no sound files.");

        /// <summary>
        /// "Select all"
        /// </summary>
        public static LocalisableString SelectAll => new TranslatableString(getKey(@"select_all"), @"Select all");

        /// <summary>
        /// "Select none"
        /// </summary>
        public static LocalisableString SelectNone => new TranslatableString(getKey(@"select_none"), @"Select none");

        /// <summary>
        /// "Play"
        /// </summary>
        public static LocalisableString PlaySound => new TranslatableString(getKey(@"play_sound"), @"Play");

        /// <summary>
        /// "Already in this beatmap"
        /// </summary>
        public static LocalisableString ThereAlready => new TranslatableString(getKey(@"there_already"), @"Already in this beatmap");

        /// <summary>
        /// "Another sound has this name. Tick to overwrite it."
        /// </summary>
        public static LocalisableString InPlaceOfAnother => new TranslatableString(getKey(@"in_place_of_another"), @"Another sound has this name. Tick to overwrite it.");

        /// <summary>
        /// "Has the name of this beatmap's song, and cannot be imported"
        /// </summary>
        public static LocalisableString NameOfTheSong => new TranslatableString(getKey(@"name_of_the_song"), @"Has the name of this beatmap's song, and cannot be imported");

        /// <summary>
        /// "Import ({0} difficulties, {1} sounds)"
        /// </summary>
        public static LocalisableString Import(int difficulties, int sounds) => new TranslatableString(getKey(@"import"), @"Import ({0} difficulties, {1} sounds)", difficulties, sounds);

        /// <summary>
        /// "These sounds of this beatmap will be overwritten: {0}. Continue?"
        /// </summary>
        public static LocalisableString OverwriteSounds(string sounds) => new TranslatableString(getKey(@"overwrite_sounds"),
            @"These sounds of this beatmap will be overwritten: {0}. Continue?", sounds);

        /// <summary>
        /// "Imported {0} difficulties and {1} sounds."
        /// </summary>
        public static LocalisableString Imported(int difficulties, int sounds) => new TranslatableString(getKey(@"imported"), @"Imported {0} difficulties and {1} sounds.", difficulties, sounds);

        /// <summary>
        /// "Choose a sound on the left."
        /// </summary>
        public static LocalisableString ChooseASound => new TranslatableString(getKey(@"choose_a_sound"), @"Choose a sound on the left.");

        /// <summary>
        /// "Sample set"
        /// </summary>
        public static LocalisableString SampleSet => new TranslatableString(getKey(@"sample_set"), @"Sample set");

        /// <summary>
        /// "Sound"
        /// </summary>
        public static LocalisableString SoundKind => new TranslatableString(getKey(@"sound_kind"), @"Sound");

        /// <summary>
        /// "Number"
        /// </summary>
        public static LocalisableString CustomIndex => new TranslatableString(getKey(@"custom_index"), @"Number");

        /// <summary>
        /// "1 is the name with no number."
        /// </summary>
        public static LocalisableString CustomIndexHint => new TranslatableString(getKey(@"custom_index_hint"), @"1 is the name with no number.");

        /// <summary>
        /// "New name: {0}"
        /// </summary>
        public static LocalisableString NewName(string name) => new TranslatableString(getKey(@"new_name"), @"New name: {0}", name);

        /// <summary>
        /// "This name is in use: {0}"
        /// </summary>
        public static LocalisableString NameTaken(string by) => new TranslatableString(getKey(@"name_taken"), @"This name is in use: {0}", by);

        /// <summary>
        /// "Rename"
        /// </summary>
        public static LocalisableString Rename => new TranslatableString(getKey(@"rename"), @"Rename");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
