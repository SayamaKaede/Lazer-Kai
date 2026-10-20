// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class MappingTimeStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.LazerKaiMappingTime";

        /// <summary>
        /// "Statistics"
        /// </summary>
        public static LocalisableString Title => new TranslatableString(getKey(@"title"), @"Statistics");

        /// <summary>
        /// "How long you mapped each day, and what"
        /// </summary>
        public static LocalisableString Description => new TranslatableString(getKey(@"description"), @"How long you mapped each day, and what");

        /// <summary>
        /// "{0:MMMM yyyy}"
        /// </summary>
        public static LocalisableString Month(DateTime month) => new TranslatableString(getKey(@"month"), @"{0:MMMM yyyy}", month);

        /// <summary>
        /// "{0:dddd, MMMM d}"
        /// </summary>
        public static LocalisableString Day(DateTime day) => new TranslatableString(getKey(@"day"), @"{0:dddd, MMMM d}", day);

        /// <summary>
        /// "Total {0}"
        /// </summary>
        public static LocalisableString Total(LocalisableString time) => new TranslatableString(getKey(@"total"), @"Total {0}", time);

        /// <summary>
        /// "Running {0} · In the editor {1} · Editing time {2}"
        /// </summary>
        public static LocalisableString DayTimes(LocalisableString game, LocalisableString open, LocalisableString edit) =>
            new TranslatableString(getKey(@"day_times"), @"Running {0} · In the editor {1} · Editing time {2}", game, open, edit);

        /// <summary>
        /// "{0}h {1}m"
        /// </summary>
        public static LocalisableString HoursAndMinutes(int hours, int minutes) => new TranslatableString(getKey(@"hours_and_minutes"), @"{0}h {1}m", hours, minutes);

        /// <summary>
        /// "{0}m"
        /// </summary>
        public static LocalisableString Minutes(int minutes) => new TranslatableString(getKey(@"minutes"), @"{0}m", minutes);

        /// <summary>
        /// "Nothing was changed on this day."
        /// </summary>
        public static LocalisableString NothingThisDay => new TranslatableString(getKey(@"nothing_this_day"), @"Nothing was changed on this day.");

        /// <summary>
        /// The column headings of the calendar, Sunday first.
        /// </summary>
        public static LocalisableString Weekday(DayOfWeek day) => day switch
        {
            DayOfWeek.Sunday => new TranslatableString(getKey(@"sunday"), @"Su"),
            DayOfWeek.Monday => new TranslatableString(getKey(@"monday"), @"Mo"),
            DayOfWeek.Tuesday => new TranslatableString(getKey(@"tuesday"), @"Tu"),
            DayOfWeek.Wednesday => new TranslatableString(getKey(@"wednesday"), @"We"),
            DayOfWeek.Thursday => new TranslatableString(getKey(@"thursday"), @"Th"),
            DayOfWeek.Friday => new TranslatableString(getKey(@"friday"), @"Fr"),
            _ => new TranslatableString(getKey(@"saturday"), @"Sa"),
        };

        /// <summary>
        /// A length of time, in hours and minutes, or in minutes alone when it is under an hour.
        /// </summary>
        public static LocalisableString Length(TimeSpan length)
        {
            int minutes = (int)length.TotalMinutes;

            return minutes < 60 ? Minutes(minutes) : HoursAndMinutes(minutes / 60, minutes % 60);
        }

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
