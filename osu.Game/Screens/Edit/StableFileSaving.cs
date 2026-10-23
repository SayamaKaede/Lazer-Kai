// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Extensions;
using osu.Framework.Logging;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.Extensions;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Saves a difficulty other than the one being edited, after something done to the whole set changed it: a
    /// background or video given to every difficulty, or bookmarks copied to them.
    /// </summary>
    /// <remarks>
    /// Where stable compatibility is on and the difficulty's file is still stable's, only the lines that change are
    /// written, the way the editor saves the difficulty it has open. Otherwise, or if that cannot be done, it is saved
    /// in lazer's format, as it always was.
    /// </remarks>
    public static class StableFileSaving
    {
        /// <param name="beatmaps">Where the difficulty is saved.</param>
        /// <param name="working">The difficulty.</param>
        /// <param name="keepStableFiles">Whether stable compatibility is on.</param>
        /// <param name="change">The change to make to the difficulty, once what it was has been taken note of.</param>
        /// <returns>
        /// Whether it was saved: one still in stable's format that cannot stay in it is left as it was (2026-10-23). Saved
        /// in lazer's format, its green lines went where its objects are, without the mapper having opened it or been
        /// asked; the difficulty open in the editor asks first, and this one is not even on screen.
        /// </returns>
        /// <remarks>
        /// Nothing these changes touch moves an object or a sound, so the only way one cannot stay is a file without the
        /// section or the line the change goes in. That also means a difficulty left as it was by a new background, video or song
        /// for every difficulty never named the old one, which is taken out of the set before this is called.
        /// </remarks>
        public static bool Save(BeatmapManager beatmaps, WorkingBeatmap working, bool keepStableFiles, Action<IBeatmap> change)
        {
            var info = working.BeatmapInfo;
            var playable = working.GetPlayableBeatmap(info.Ruleset);
            var skin = working.GetSkin();
            var storyboard = working.Storyboard;

            byte[]? file = keepStableFiles ? readFile(working) : null;

            if (file != null && StableFileKeeper.IsStableFile(file))
            {
                var keeper = new StableFileKeeper(file, playable, skin, storyboard);

                change(playable);

                byte[]? written = keeper.Write(playable, skin, storyboard, null, out _);

                if (written != null)
                {
                    beatmaps.SaveFile(info, playable, written);
                    return true;
                }

                Logger.Log($@"{info.GetDisplayTitle()} could not be kept in stable's format, and was left as it was: {keeper.LastFailure ?? @"no reason given"}");
                return false;
            }

            change(playable);
            beatmaps.Save(info, playable, skin, storyboard);
            return true;
        }

        private static byte[]? readFile(WorkingBeatmap working)
        {
            string? path = working.BeatmapInfo.Path == null ? null : working.BeatmapSetInfo?.GetPathForFile(working.BeatmapInfo.Path);

            if (path == null)
                return null;

            using (var stream = working.GetStream(path))
                return stream?.ReadAllRemainingBytesToArray();
        }
    }
}
