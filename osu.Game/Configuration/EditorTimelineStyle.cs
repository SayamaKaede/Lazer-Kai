// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Configuration
{
    /// <summary>
    /// How the editor's timeline lays out what it shows.
    /// </summary>
    public enum EditorTimelineStyle
    {
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.TimelineStyleLazer))]
        Lazer,

        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.TimelineStyleStable))]
        Stable,

        /// <summary>
        /// Stable's layout to start with, and where anything this fork adds to the timeline goes from here on.
        /// </summary>
        [LocalisableDescription(typeof(EditorStrings), nameof(EditorStrings.TimelineStyleLazerKai))]
        LazerKai,
    }

    public static class EditorTimelineStyleExtensions
    {
        /// <summary>
        /// Whether the layout rests its objects on the timeline's bottom edge, with the velocity and volume rows
        /// stacked above them and the ticks kept down there with the objects.
        /// </summary>
        public static bool RestsOnTheBottomEdge(this EditorTimelineStyle style) => style != EditorTimelineStyle.Lazer;
    }
}
