// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;

namespace osu.Game.IO
{
    /// <summary>
    /// The manual that ships beside the game.
    /// </summary>
    /// <remarks>
    /// One page, written by hand, kept outside the repository and copied into the build by publish.ps1. It travels
    /// with the build rather than sitting on the web: a page anyone can open by URL is a page a search engine can
    /// list, and this one is written for the people the build is handed to.
    ///
    /// It lives beside the executable, which is the folder velopack replaces wholesale on every update -- so a new
    /// build carries its own manual in with it, and there is never a copy left over from an older one.
    /// </remarks>
    public static class LazerKaiManual
    {
        /// <summary>The name the manual is written and shipped under.</summary>
        public const string FILENAME = @"説明書.html";

        /// <summary>
        /// The name the getting-started guide is written and shipped under.
        /// </summary>
        /// <remarks>
        /// The manual goes through every feature, which is more than somebody who has just installed the build can
        /// find their way through (2026-10-14). The guide is the short way in: the main features by name alone, each
        /// pointing on into the manual, and the questions that keep coming up. It ships the same way the manual
        /// does.
        /// </remarks>
        public const string GUIDE_FILENAME = @"はじめに.html";

        /// <summary>
        /// The manual inside <paramref name="directory"/>, or null when there is none there.
        /// </summary>
        /// <remarks>
        /// Null is the ordinary answer for a build that was never published: nothing copies the manual next to a
        /// binary built straight out of the repository. What reads this hides itself rather than offering a button
        /// that would open nothing.
        /// </remarks>
        public static string? Find(string? directory) => find(directory, FILENAME);

        /// <summary>
        /// The getting-started guide inside <paramref name="directory"/>, or null when there is none there, for the
        /// same reason as <see cref="Find"/>.
        /// </summary>
        public static string? FindGuide(string? directory) => find(directory, GUIDE_FILENAME);

        private static string? find(string? directory, string filename)
        {
            if (string.IsNullOrEmpty(directory))
                return null;

            string path = Path.Combine(directory, filename);

            return File.Exists(path) ? path : null;
        }
    }
}
