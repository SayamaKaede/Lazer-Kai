// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
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
        [JsonProperty("lanes")]
        public List<LaneSound> Lanes { get; set; } = new List<LaneSound>();

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
