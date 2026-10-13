// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using osu.Game.Audio;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// Works out the circles of an osu! hitsound difficulty from what sounds in the lanes of a difficulty made to
    /// hitsound in, and the sound files those circles are to find.
    /// </summary>
    /// <remarks>
    /// Any number of lanes can sound at one time. A circle has one hit sound and up to three added to it, the added
    /// ones all of one sample set, and every sound of a circle is looked up under one custom index. So what sounds
    /// together is fitted to that: the hit sounds all go under the sample set of one of them and the added ones under
    /// the sample set of one of those, two sounds that come to the same place are to be one file with both in it, and
    /// places that are wanted with different sounds at different times are told apart by the custom index.
    ///
    /// It is done as Mapping Tools' Hitsound Studio does it when exporting in its standard mode, which is what the
    /// user has made hitsound difficulties with from difficulties hitsounded in osu!mania, so that one made here comes
    /// out as theirs have (2026-10-13; checked against one of theirs, all 1527 circles of it).
    /// </remarks>
    public static class LaneSoundConversion
    {
        /// <summary>
        /// How far apart in time two sounds can be and still be of one circle, in milliseconds. Hitsound Studio's own.
        /// </summary>
        public const double LENIENCY = 15;

        /// <summary>
        /// A note of the difficulty hitsounded in: the lane it is in sounds at its time.
        /// </summary>
        public readonly record struct Note(double Time, int Lane);

        /// <summary>
        /// A circle of the hitsound difficulty.
        /// </summary>
        /// <param name="Time">When it is.</param>
        /// <param name="NormalBank">The sample set of its hit sound.</param>
        /// <param name="AdditionBank">The sample set of the sounds added to that.</param>
        /// <param name="Whistle">Whether a whistle is added.</param>
        /// <param name="Finish">Whether a finish is added.</param>
        /// <param name="Clap">Whether a clap is added.</param>
        /// <param name="CustomIndex">The custom index its sounds are looked up under.</param>
        public readonly record struct Circle(double Time, string NormalBank, string AdditionBank, bool Whistle, bool Finish, bool Clap, int CustomIndex);

        /// <summary>
        /// A sound file the circles are to find in the beatmap set.
        /// </summary>
        /// <param name="Bank">The sample set it is of.</param>
        /// <param name="Sound">Which of the sample set's four sounds it is.</param>
        /// <param name="CustomIndex">The custom index it is under.</param>
        /// <param name="Sources">The files of the lanes it is made of: the one it is a copy of, or the ones mixed into it.</param>
        public record SoundFile(string Bank, string Sound, int CustomIndex, IReadOnlyList<string> Sources)
        {
            /// <summary>
            /// What stable looks it up as, without the extension: the first custom index has no number.
            /// </summary>
            public string Name => $@"{Bank}-{Sound}{(CustomIndex == 1 ? string.Empty : CustomIndex.ToString(CultureInfo.InvariantCulture))}";
        }

        public record Result(IReadOnlyList<Circle> Circles, IReadOnlyList<SoundFile> Files);

        private static readonly string[] banks = { HitSampleInfo.BANK_NORMAL, HitSampleInfo.BANK_SOFT, HitSampleInfo.BANK_DRUM };

        private static readonly string[] sounds = { HitSampleInfo.HIT_NORMAL, HitSampleInfo.HIT_WHISTLE, HitSampleInfo.HIT_FINISH, HitSampleInfo.HIT_CLAP };

        /// <summary>
        /// Stands in a place for the skin's own sound: what a lane with no file, or one whose file is gone, sounds as.
        /// Nothing else can be put in a place it is in.
        /// </summary>
        private const string of_the_skin = "";

        /// <summary>
        /// Converts <paramref name="notes"/>.
        /// </summary>
        /// <param name="notes">The notes of the difficulty hitsounded in.</param>
        /// <param name="lanes">What each lane sounds as.</param>
        /// <param name="fallback">The hit sound of a circle none of whose lanes is one: every circle has a hit sound.</param>
        /// <param name="exists">Whether a lane's file is there to be copied.</param>
        /// <param name="firstCustomIndex">The lowest custom index to use.</param>
        public static Result Convert(IEnumerable<Note> notes, IReadOnlyList<LaneSound> lanes, LaneSound fallback, Func<string, bool> exists, int firstCustomIndex = 1)
        {
            var together = soundingTogether(notes, lanes, fallback);
            var wanted = together.Select(t => placesOf(t, exists)).ToList();
            var indices = fewest(wanted);

            for (int i = 0; i < indices.Count; i++)
                indices[i].Number = firstCustomIndex + i;

            var circles = new List<Circle>(together.Count);

            for (int at = 0; at < together.Count;)
            {
                // the index that goes on fitting for longest from here, so that it changes as seldom as it can: every
                // change is a green line.
                CustomIndex? longest = null;
                int length = 0;

                foreach (var index in indices)
                {
                    int run = 0;

                    while (at + run < wanted.Count && index.Fits(wanted[at + run]))
                        run++;

                    if (run > length)
                    {
                        longest = index;
                        length = run;
                    }
                }

                if (longest == null)
                    throw new InvalidOperationException($"No custom index has the sounds of the circle at {together[at].Time}.");

                for (int i = 0; i < length; i++)
                    circles.Add(circleOf(together[at + i], longest.Number));

                at += length;
            }

            var files = new List<SoundFile>();

            foreach (var index in indices)
            {
                foreach (var (place, held) in index.Places)
                {
                    var sources = held.Where(s => s != of_the_skin).OrderBy(s => s, StringComparer.Ordinal).ToArray();

                    if (sources.Length > 0)
                        files.Add(new SoundFile(place.Bank, place.Sound, index.Number, sources));
                }
            }

            return new Result(circles, files);
        }

        /// <summary>
        /// The lanes that sound within <see cref="LENIENCY"/> of one another, in the order of their times.
        /// </summary>
        private static List<Together> soundingTogether(IEnumerable<Note> notes, IReadOnlyList<LaneSound> lanes, LaneSound fallback)
        {
            // Hitsound Studio goes through its layers in the order of their names, which are the names of their files,
            // and stable's files are named for their places: drum-hitclap comes before soft-hitwhistle. That order is
            // also which sample set a circle's sounds go under where its lanes are of more than one. Lanes are in no
            // order of their own here, so the names of their places are gone by.
            int[] order = Enumerable.Range(0, lanes.Count)
                                    .OrderBy(l => $@"{bankOf(lanes[l])}-{lanes[l].Sound}", StringComparer.Ordinal)
                                    .ThenBy(l => l)
                                    .ToArray();

            var times = notes.Where(n => n.Lane >= 0 && n.Lane < lanes.Count)
                             .GroupBy(n => n.Lane)
                             .ToDictionary(g => g.Key, g => g.Select(n => n.Time).OrderBy(t => t).ToArray());

            var together = new List<Together>();

            for (int priority = 0; priority < order.Length; priority++)
            {
                int lane = order[priority];

                if (!times.TryGetValue(lane, out double[]? laneTimes))
                    continue;

                foreach (double time in laneTimes)
                {
                    var group = together.FirstOrDefault(t => Math.Abs(t.Time - time) <= LENIENCY);

                    if (group == null)
                        together.Add(group = new Together(time));

                    if (group.Sounds.All(s => s.Lane != lane))
                        group.Sounds.Add(new Sounding(lanes[lane], lane, priority));
                }
            }

            foreach (var group in together)
            {
                if (group.Sounds.All(s => s.Sound.Sound != HitSampleInfo.HIT_NORMAL))
                    group.Sounds.Add(new Sounding(fallback, -1, int.MaxValue));
            }

            return together.OrderBy(t => t.Time).ToList();
        }

        /// <summary>
        /// The places the sounds of <paramref name="group"/> are to be found in, and what is to be in each.
        /// </summary>
        private static CustomIndex placesOf(Together group, Func<string, bool> exists)
        {
            var (normalBank, additionBank) = banksOf(group);
            var index = new CustomIndex();

            foreach (var sounding in group.Sounds)
            {
                var sound = sounding.Sound;
                bool isTheHit = sound.Sound == HitSampleInfo.HIT_NORMAL;

                if (!index.Places.TryGetValue(new Place(isTheHit ? normalBank : additionBank, sound.Sound), out var held))
                    continue;

                held.Add(sound.HasFile && exists(sound.File) ? sound.File : of_the_skin);
            }

            // the skin's sound cannot be mixed with a file, there being none of it to mix.
            foreach (var held in index.Places.Values)
            {
                if (held.Count > 1)
                    held.Remove(of_the_skin);
            }

            return index;
        }

        private static Circle circleOf(Together group, int customIndex)
        {
            var (normalBank, additionBank) = banksOf(group);

            return new Circle(group.Time, normalBank, additionBank,
                group.Sounds.Any(s => s.Sound.Sound == HitSampleInfo.HIT_WHISTLE),
                group.Sounds.Any(s => s.Sound.Sound == HitSampleInfo.HIT_FINISH),
                group.Sounds.Any(s => s.Sound.Sound == HitSampleInfo.HIT_CLAP),
                customIndex);
        }

        /// <summary>
        /// The sample set of the hit sound and the one of the sounds added to it: that of the first lane of each kind,
        /// and where there is none of a kind, that of the other.
        /// </summary>
        private static (string normal, string addition) banksOf(Together group)
        {
            string? ofTheHit = firstBank(group, true);
            string? ofTheAdded = firstBank(group, false);

            return (ofTheHit ?? ofTheAdded ?? HitSampleInfo.BANK_SOFT, ofTheAdded ?? ofTheHit ?? HitSampleInfo.BANK_SOFT);
        }

        private static string? firstBank(Together group, bool ofTheHit)
        {
            Sounding? first = null;

            foreach (var sounding in group.Sounds)
            {
                if ((sounding.Sound.Sound == HitSampleInfo.HIT_NORMAL) == ofTheHit && (first == null || sounding.Priority < first.Value.Priority))
                    first = sounding;
            }

            return first == null ? null : bankOf(first.Value.Sound);
        }

        /// <summary>
        /// A lane's sample set, which is soft for anything that is not one of the three.
        /// </summary>
        private static string bankOf(LaneSound sound) => banks.Contains(sound.Bank) ? sound.Bank : HitSampleInfo.BANK_SOFT;

        /// <summary>
        /// As few custom indices as have, between them, every place the way every circle wants it.
        /// </summary>
        private static List<CustomIndex> fewest(IReadOnlyList<CustomIndex> wanted)
        {
            var indices = new List<CustomIndex>();

            // each circle's places go into the first index they do not disagree with.
            foreach (var want in wanted)
            {
                var taking = indices.FirstOrDefault(i => i.CanTake(want));

                if (taking == null)
                    indices.Add(want.Copy());
                else
                    taking.Take(want);
            }

            // an index is not needed if every circle it fits is fitted by another: what was put in an earlier one may
            // all have been put in later ones since.
            foreach (var index in indices.ToArray())
            {
                if (!wanted.Any(want => index.Fits(want) && indices.All(other => other == index || !other.Fits(want))))
                    indices.Remove(index);
            }

            return indices;
        }

        private readonly record struct Place(string Bank, string Sound);

        private readonly record struct Sounding(LaneSound Sound, int Lane, int Priority);

        private class Together
        {
            public readonly double Time;

            public readonly List<Sounding> Sounds = new List<Sounding>();

            public Together(double time)
            {
                Time = time;
            }
        }

        /// <summary>
        /// The twelve places of a custom index, a sample set's four sounds for each of the three sample sets, and the
        /// files that are to sound together in each.
        /// </summary>
        private class CustomIndex
        {
            public int Number = -1;

            public readonly Dictionary<Place, HashSet<string>> Places = banks.SelectMany(b => sounds.Select(s => new Place(b, s)))
                                                                             .ToDictionary(p => p, _ => new HashSet<string>(StringComparer.Ordinal));

            /// <summary>
            /// Whether every place <paramref name="other"/> has something in is the same here.
            /// </summary>
            public bool Fits(CustomIndex other) => Places.All(p => other.Places[p.Key].Count == 0 || p.Value.SetEquals(other.Places[p.Key]));

            /// <summary>
            /// Whether no place has one thing here and another in <paramref name="other"/>.
            /// </summary>
            public bool CanTake(CustomIndex other) =>
                Places.All(p => p.Value.Count == 0 || other.Places[p.Key].Count == 0 || p.Value.SetEquals(other.Places[p.Key]));

            public void Take(CustomIndex other)
            {
                foreach (var (place, held) in Places)
                    held.UnionWith(other.Places[place]);
            }

            public CustomIndex Copy()
            {
                var copy = new CustomIndex();
                copy.Take(this);
                return copy;
            }
        }
    }
}
