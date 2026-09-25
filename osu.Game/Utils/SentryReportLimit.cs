// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;

namespace osu.Game.Utils
{
    /// <summary>
    /// How many errors one run of the game sends.
    /// </summary>
    /// <remarks>
    /// The reports come out of a monthly allowance shared by everybody who plays. An error thrown every frame would
    /// send thousands from one run and use it up, and nobody else's would arrive for the rest of the month. The same
    /// error twice says nothing the first did not, so it is sent once; and past a handful, a run has said enough.
    /// </remarks>
    public class SentryReportLimit
    {
        /// <summary>
        /// The most errors sent from one run.
        /// </summary>
        public const int MAX_PER_RUN = 20;

        private readonly HashSet<string> sent = new HashSet<string>();

        /// <summary>
        /// Whether to send this error, counting it as sent if so.
        /// </summary>
        public bool Allows(Exception exception)
        {
            string key = $"{exception.GetType().FullName}\n{exception.Message}\n{exception.StackTrace}";

            lock (sent)
            {
                if (sent.Count >= MAX_PER_RUN)
                    return false;

                return sent.Add(key);
            }
        }
    }
}
