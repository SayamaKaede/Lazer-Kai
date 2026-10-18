// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// Who is taking which stretch of a collaboration.
    /// </summary>
    public class CollabProject
    {
        /// <summary>
        /// The collaborators, in the order they were added.
        /// </summary>
        /// <remarks>
        /// Kept as a list of its own rather than read back out of the assignments so that somebody can be added
        /// before they are given anything, which is the order a split is actually agreed in.
        /// </remarks>
        [JsonProperty("names")]
        public List<string> Names { get; set; } = new List<string>();

        /// <summary>
        /// The bookmark each stretch opens on, against the name of whoever takes it.
        /// </summary>
        [JsonProperty("assignment")]
        public Dictionary<int, string> Assignment { get; set; } = new Dictionary<int, string>();

        /// <summary>
        /// Drops assignments whose stretch no longer exists.
        /// </summary>
        /// <remarks>
        /// Adding or removing a bookmark moves where a collaboration is divided, and a stretch that was agreed against
        /// the old division says nothing about the new one. Carrying it over would leave somebody credited with time
        /// nobody agreed to give them, which is worse than being asked to say again.
        /// </remarks>
        public bool DropAssignmentsOutsideOf(IEnumerable<CollabBlock> blocks)
        {
            var keys = blocks.Select(b => b.Key).ToHashSet();
            var stale = Assignment.Keys.Where(k => !keys.Contains(k)).ToArray();

            foreach (int key in stale)
                Assignment.Remove(key);

            return stale.Length > 0;
        }

        /// <summary>
        /// Takes every stretch along with its bookmark, when the bookmarks have all moved by the same amount.
        /// </summary>
        /// <remarks>
        /// That is the division that was agreed, at another time: it is what moving the whole beatmap in time does. An
        /// assignment of the stretch there used to be before the first bookmark (<see cref="CollabBlock.HEAD"/>) is left
        /// as it is rather than moved past the smallest number there is.
        /// </remarks>
        public void MoveAssignments(int by) =>
            Assignment = Assignment.ToDictionary(a => a.Key == CollabBlock.HEAD ? a.Key : a.Key + by, a => a.Value);

        /// <summary>
        /// Gives a collaborator the name they are actually known by, carrying over everything already given to them.
        /// </summary>
        /// <remarks>
        /// A split starts as letters because that is how one is agreed before anybody is named, but a split shared as
        /// text is read by people, and "A" tells them nothing. Two collaborators under one name could not be told
        /// apart in the totals or in the shared text, so a name already taken is refused rather than merged.
        /// </remarks>
        /// <returns>Whether the name was taken.</returns>
        public bool Rename(string from, string to)
        {
            to = to.Trim();

            int index = Names.IndexOf(from);

            if (to.Length == 0 || to == from || index < 0 || Names.Contains(to))
                return false;

            Names[index] = to;

            foreach (int key in Assignment.Where(a => a.Value == from).Select(a => a.Key).ToArray())
                Assignment[key] = to;

            return true;
        }

        /// <summary>
        /// How long each collaborator is taking, and what is not yet anybody's.
        /// </summary>
        public Dictionary<string, CollabTime> TotalsFor(IEnumerable<CollabBlock> blocks)
        {
            var totals = Names.ToDictionary(n => n, _ => default(CollabTime));

            foreach (var block in blocks)
            {
                if (Assignment.TryGetValue(block.Key, out string? name) && totals.TryGetValue(name, out var total))
                    totals[name] = total + CollabTime.Of(block);
            }

            return totals;
        }
    }

    /// <summary>
    /// How much of a beatmap somebody has, both ways it is counted: from bookmark to bookmark, and as played.
    /// </summary>
    public readonly record struct CollabTime(double Length, double DrainTime)
    {
        public static CollabTime Of(CollabBlock block) => new CollabTime(block.Length, block.DrainTime);

        public static CollabTime Of(IEnumerable<CollabBlock> blocks) => blocks.Aggregate(default(CollabTime), (total, block) => total + Of(block));

        public static CollabTime operator +(CollabTime a, CollabTime b) => new CollabTime(a.Length + b.Length, a.DrainTime + b.DrainTime);
    }
}
