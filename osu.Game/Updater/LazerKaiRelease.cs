// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Globalization;
using Newtonsoft.Json;

namespace osu.Game.Updater
{
    /// <summary>
    /// What the fork's update feed says the newest distributed build is.
    /// </summary>
    /// <remarks>
    /// The feed is a small file the maintainer edits by hand for each release, so this reads only what they have to
    /// type: the version, and where to send whoever wants it.
    /// </remarks>
    public class LazerKaiRelease
    {
        [JsonProperty("version")]
        public string Version { get; set; } = string.Empty;

        [JsonProperty("url")]
        public string Url { get; set; } = string.Empty;

        /// <summary>
        /// A line about this release in particular, shown under the notification. Optional.
        /// </summary>
        [JsonProperty("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Whether this release is newer than <paramref name="running"/>.
        /// </summary>
        /// <remarks>
        /// Distributed versions read <c>yyyyMMdd.N</c>: the day, and which release of that day it is. Anything else is
        /// a build made outside the release process — a portable one, which is nobody's to update.
        /// </remarks>
        public bool IsNewerThan(string running)
        {
            if (!TryReadVersion(Version, out var mine) || !TryReadVersion(running, out var theirs))
                return false;

            return mine.CompareTo(theirs) > 0;
        }

        /// <summary>
        /// Reads a distributed version, <c>yyyyMMdd.N</c>, as a pair that sorts in release order.
        /// </summary>
        /// <returns>False for anything else, a portable build's version among them.</returns>
        public static bool TryReadVersion(string version, out (int Day, int Revision) parsed)
        {
            parsed = default;

            string[] parts = version.Split('.');

            if (parts.Length != 2)
                return false;

            if (!int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out int day)
                || !int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out int revision))
                return false;

            parsed = (day, revision);
            return true;
        }

        public static LazerKaiRelease? FromJson(string json) => JsonConvert.DeserializeObject<LazerKaiRelease>(json);
    }
}
