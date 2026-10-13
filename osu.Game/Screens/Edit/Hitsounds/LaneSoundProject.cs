// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Game.Audio;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// What each lane of a difficulty made to hitsound in sounds as: the lanes are the sounds, and a note is the sound of
    /// the lane it is put in.
    /// </summary>
    /// <remarks>
    /// Hitsounds for osu! are worked out in osu!mania, where any number of sounds can be played at one time, and turned
    /// into a hitsound difficulty afterwards. In stable a note cannot be given a sound, so it is done by copying notes
    /// that already have one; here the lane is given the sound, and a note put in it has it (2026-10-11).
    /// </remarks>
    public class LaneSoundProject
    {
        /// <summary>
        /// How loud a note is before the first of <see cref="Volumes"/>, in percent.
        /// </summary>
        public const int FULL_VOLUME = 100;

        [JsonProperty("lanes")]
        public List<LaneSound> Lanes { get; set; } = new List<LaneSound>();

        /// <summary>
        /// The hit sound of a circle none of whose lanes is one, once turned into an osu! hitsound difficulty: every
        /// circle has a hit sound. The soft one of the skin, until it is said otherwise.
        /// </summary>
        [JsonProperty("fallback")]
        public LaneSound Fallback { get; set; } = new LaneSound { Bank = HitSampleInfo.BANK_SOFT };

        /// <summary>
        /// Where the volume changes, in the order of their times: notes are as loud as one of these says from its time
        /// until the next, as objects are by the green lines of a difficulty of stable's.
        /// </summary>
        [JsonProperty("volumes")]
        public List<LaneVolume> Volumes { get; set; } = new List<LaneVolume>();

        /// <summary>
        /// Whether <see cref="Volumes"/> go into the hitsound difficulty this is turned into. Left out, the volumes of
        /// the one it was last turned into are kept, for the volume to be set there instead (2026-10-13).
        /// </summary>
        [JsonProperty("convert_volumes")]
        public bool ConvertVolumes { get; set; } = true;

        /// <summary>
        /// Whether the notes sound as themselves rather than as their lanes: a difficulty hitsounded in stable, where a
        /// note is given a sound by copying one that has it. Its lanes are not sounds and it has no screen for them;
        /// this is kept for it only to say what it was last turned into (2026-10-13).
        /// </summary>
        [JsonProperty("notes_sound_as_themselves")]
        public bool NotesSoundAsThemselves { get; set; }

        /// <summary>
        /// What this was last turned into, which is what turning it into one again puts another in place of.
        /// </summary>
        [JsonProperty("converted")]
        public ConvertedDifficulty? Converted { get; set; }

        /// <summary>
        /// How loud a note at <paramref name="time"/> is, in percent.
        /// </summary>
        public int VolumeAt(double time)
        {
            int volume = FULL_VOLUME;

            foreach (var change in Volumes)
            {
                // a note is on a fraction of a millisecond, and a change put in for it may not be on the same one.
                if (change.Time > time + 1)
                    break;

                volume = change.Volume;
            }

            return volume;
        }

        /// <summary>
        /// Makes notes <paramref name="volume"/> loud from <paramref name="time"/> until the next change, changing the
        /// one that is at <paramref name="time"/> if there is one, the way a green line is changed on the hitsound
        /// screen of an osu! difficulty.
        /// </summary>
        public void SetVolume(double time, int volume)
        {
            var standing = Volumes.FirstOrDefault(v => Math.Abs(v.Time - time) <= HitPoints.LENIENCY);

            if (standing != null)
                standing.Volume = volume;
            else
                Volumes.Add(new LaneVolume { Time = time, Volume = volume });

            Volumes.Sort((a, b) => a.Time.CompareTo(b.Time));

            // one that says what the one before it does says nothing.
            int before = FULL_VOLUME;

            Volumes.RemoveAll(v =>
            {
                bool saysNothing = v.Volume == before;
                before = v.Volume;
                return saysNothing;
            });
        }

        /// <summary>
        /// The sound of <paramref name="lane"/>, which is one with no file for a lane nothing is kept for.
        /// </summary>
        public LaneSound this[int lane] => lane >= 0 && lane < Lanes.Count ? Lanes[lane] : new LaneSound();

        /// <summary>
        /// Makes room for <paramref name="count"/> lanes, keeping what is said for any beyond them: a lane taken away and
        /// put back has its sound still.
        /// </summary>
        public void EnsureLanes(int count)
        {
            while (Lanes.Count < count)
                Lanes.Add(new LaneSound());
        }
    }

    /// <summary>
    /// How loud notes are from a time on.
    /// </summary>
    public class LaneVolume
    {
        [JsonProperty("time")]
        public double Time { get; set; }

        /// <summary>
        /// In percent.
        /// </summary>
        [JsonProperty("volume")]
        public int Volume { get; set; } = LaneSoundProject.FULL_VOLUME;
    }

    /// <summary>
    /// The hitsound difficulty a difficulty hitsounded in was turned into.
    /// </summary>
    public class ConvertedDifficulty
    {
        [JsonProperty("difficulty")]
        public Guid Difficulty { get; set; }

        /// <summary>
        /// The sound files written for it, by their names in the beatmap set, and the hash of what was written to each.
        /// One that has since become something else is somebody's own, and is not written over without asking.
        /// </summary>
        [JsonProperty("files")]
        public Dictionary<string, string> Files { get; set; } = new Dictionary<string, string>();
    }

    /// <summary>
    /// The sound of one lane.
    /// </summary>
    public class LaneSound
    {
        /// <summary>
        /// The file played, by its name in the beatmap set. Empty for a lane with no sound of its own.
        /// </summary>
        [JsonProperty("file")]
        public string File { get; set; } = string.Empty;

        /// <summary>
        /// Where in the library the file was copied from, as <see cref="HitsoundLibrary.Files"/> names it.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The sample set it is to be in once turned into an osu! hitsound difficulty.
        /// </summary>
        [JsonProperty("bank")]
        public string Bank { get; set; } = HitSampleInfo.BANK_NORMAL;

        /// <summary>
        /// Which of a sample set's four sounds it is to be there: the hit, or the whistle, finish or clap added to it.
        /// </summary>
        [JsonProperty("sound")]
        public string Sound { get; set; } = HitSampleInfo.HIT_NORMAL;

        [JsonIgnore]
        public bool HasFile => !string.IsNullOrEmpty(File);

        /// <summary>
        /// What to call the sound where there is little room: its file's name, without the folder or the extension.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => HasFile ? Path.GetFileNameWithoutExtension(File) : string.Empty;
    }
}
