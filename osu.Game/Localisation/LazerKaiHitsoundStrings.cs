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
        /// "Auto only applies to objects as they are placed"
        /// </summary>
        public static LocalisableString AutoSamplesetUnavailable => new TranslatableString(getKey(@"auto_sampleset_unavailable"), @"Auto only applies to objects as they are placed");

        /// <summary>
        /// "{0} selected objects"
        /// </summary>
        public static LocalisableString Selected(int count) => new TranslatableString(getKey(@"selected"), @"{0} selected objects", count);

        /// <summary>
        /// "Nothing sounds at the playhead"
        /// </summary>
        public static LocalisableString NothingAtThePlayhead => new TranslatableString(getKey(@"nothing_at_the_playhead"), @"Nothing sounds at the playhead");

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
        /// "Selection outline on the timeline"
        /// </summary>
        public static LocalisableString OutlineHeader => new TranslatableString(getKey(@"outline_header"), @"Selection outline on the timeline");

        /// <summary>
        /// "Colour"
        /// </summary>
        public static LocalisableString OutlineColour => new TranslatableString(getKey(@"outline_colour"), @"Colour");

        /// <summary>
        /// "Thickness"
        /// </summary>
        public static LocalisableString OutlineThickness => new TranslatableString(getKey(@"outline_thickness"), @"Thickness");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString OutlineOpacity => new TranslatableString(getKey(@"outline_opacity"), @"Opacity");

        /// <summary>
        /// "New difficulty"
        /// </summary>
        public static LocalisableString NewDifficultyHeader => new TranslatableString(getKey(@"new_difficulty_header"), @"New difficulty");

        /// <summary>
        /// "Display"
        /// </summary>
        public static LocalisableString DisplayHeader => new TranslatableString(getKey(@"display_header"), @"Display");

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

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
