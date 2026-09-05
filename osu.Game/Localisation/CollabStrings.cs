// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;

namespace osu.Game.Localisation
{
    public static class CollabStrings
    {
        private const string prefix = @"osu.Game.Resources.Localisation.Collab";

        /// <summary>
        /// "Collab Manager"
        /// </summary>
        public static LocalisableString Header => new TranslatableString(getKey(@"header"), @"Collab Manager");

        /// <summary>
        /// "Collab Manager..."
        /// </summary>
        public static LocalisableString MenuItem => new TranslatableString(getKey(@"menu_item"), @"Collab Manager...");

        /// <summary>
        /// "Add bookmarks where the beatmap is divided, and they become the stretches to hand out."
        /// </summary>
        public static LocalisableString NoBookmarksYet => new TranslatableString(getKey(@"no_bookmarks_yet"),
            @"Add bookmarks where the beatmap is divided, and they become the stretches to hand out.");

        /// <summary>
        /// "unassigned"
        /// </summary>
        public static LocalisableString Unassigned => new TranslatableString(getKey(@"unassigned"), @"unassigned");

        /// <summary>
        /// "Always colour the bottom timeline at the playhead"
        /// </summary>
        public static LocalisableString FollowPlayhead => new TranslatableString(getKey(@"follow_playhead"),
            @"Always colour the bottom timeline at the playhead");

        /// <summary>
        /// "Add collaborator"
        /// </summary>
        public static LocalisableString AddCollaborator => new TranslatableString(getKey(@"add_collaborator"), @"Add collaborator");

        /// <summary>
        /// "Copy split"
        /// </summary>
        public static LocalisableString CopySplit => new TranslatableString(getKey(@"copy_split"), @"Copy split");

        /// <summary>
        /// "whole beatmap {0}"
        /// </summary>
        public static LocalisableString WholeBeatmap(string drainTime) =>
            new TranslatableString(getKey(@"whole_beatmap"), @"whole beatmap {0}", drainTime);

        /// <summary>
        /// "{0} / {1} objects"
        /// </summary>
        public static LocalisableString BlockSummary(string drainTime, int objectCount) =>
            new TranslatableString(getKey(@"block_summary"), @"{0} / {1} objects", drainTime, objectCount);

        /// <summary>
        /// "Rename"
        /// </summary>
        public static LocalisableString Rename => new TranslatableString(getKey(@"rename"), @"Rename");

        /// <summary>
        /// "Collaborator name"
        /// </summary>
        public static LocalisableString CollaboratorName => new TranslatableString(getKey(@"collaborator_name"), @"Collaborator name");

        /// <summary>
        /// "Opacity"
        /// </summary>
        public static LocalisableString PanelOpacity => new TranslatableString(getKey(@"panel_opacity"), @"Opacity");

        private static string getKey(string key) => $@"{prefix}:{key}";
    }
}
