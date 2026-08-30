// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace osu.Game.Utils
{
    /// <summary>
    /// Puts a file path on screen without putting whoever owns the machine on screen with it.
    /// </summary>
    /// <remarks>
    /// A Windows path very often carries an account name, and an account name is very often a real one. Mappers
    /// stream and screenshot the editor, so a path shown by the game is a path shown to everybody watching, and
    /// it was never the part they meant to show.
    ///
    /// Only what is displayed is masked. What is stored, compared and opened is always the real path — a setting
    /// that saved what it had drawn would point at a folder called "*****".
    /// </remarks>
    public static class PathDisplay
    {
        public const string MASK = @"*****";

        private const string users_directory = @"Users";

        /// <summary>
        /// The same path with any account name in it replaced by <see cref="MASK"/>.
        /// </summary>
        public static string Mask(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            string[] parts = path.Split('\\', '/');

            string? accountName = accountNameOf(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            for (int i = 0; i < parts.Length; i++)
            {
                // the segment after "Users" is somebody's account name, whether or not it is this machine's own:
                // a path handed over from elsewhere names that person just as plainly.
                bool followsUsersDirectory = i > 0 && string.Equals(parts[i - 1], users_directory, StringComparison.OrdinalIgnoreCase);

                // and the account this machine is signed in as, wherever it turns up. A profile is not always
                // under "Users", and a folder can be named after its owner without being their profile at all.
                bool isThisAccount = accountName != null && string.Equals(parts[i], accountName, StringComparison.OrdinalIgnoreCase);

                if (followsUsersDirectory || isThisAccount)
                    parts[i] = MASK;
            }

            // put back together with the separator the path arrived with, so a masked path still reads as one.
            return string.Join(path.Contains('\\', StringComparison.Ordinal) ? '\\' : '/', parts);
        }

        /// <summary>
        /// The same for a line of free text — a log line, an exception message — rather than a bare path.
        /// </summary>
        /// <remarks>
        /// <see cref="Mask"/> takes a path apart on its separators and puts it back together, which would wreck
        /// anything that is not one. This only replaces the parts that name somebody and leaves the line alone.
        /// </remarks>
        public static string MaskIn(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // the segment after "Users", however the separators fall, and whichever drive it is on.
            text = Regex.Replace(text, @"(?<=[A-Za-z]:[\\/]{1,2}Users[\\/]{1,2})[^\\/\s""'<>|]+", MASK, RegexOptions.IgnoreCase);

            string? accountName = accountNameOf(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));

            // and this machine's own account name wherever else it turns up — a folder named after its owner
            // that is not a profile, a window title, a message that quotes one.
            if (accountName != null)
                text = Regex.Replace(text, Regex.Escape(accountName), MASK, RegexOptions.IgnoreCase);

            return text;
        }

        private static string? accountNameOf(string userProfilePath)
        {
            string? last = userProfilePath.Split('\\', '/').LastOrDefault(p => p.Length > 0);

            // a drive root as a profile path would mask every segment that happens to match "C:", which is not
            // an account name and not worth hiding.
            return string.IsNullOrEmpty(last) || last.EndsWith(':') ? null : last;
        }
    }
}
