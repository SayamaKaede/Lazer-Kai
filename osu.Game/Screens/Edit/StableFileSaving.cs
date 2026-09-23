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
        public static void Save(BeatmapManager beatmaps, WorkingBeatmap working, bool keepStableFiles, Action<IBeatmap> change)
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
                    return;
                }

                Logger.Log($@"{info.GetDisplayTitle()} could not be kept in stable's format: {keeper.LastFailure ?? @"no reason given"}");
            }
            else
                change(playable);

            beatmaps.Save(info, playable, skin, storyboard);
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
