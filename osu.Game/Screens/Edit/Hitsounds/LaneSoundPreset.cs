// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using osu.Game.Audio;

namespace osu.Game.Screens.Edit.Hitsounds
{
    /// <summary>
    /// The sounds of the lanes kept under a name, to be put on the lanes of another difficulty as a whole: which sound
    /// of the library each lane is, and what it is to be once turned into an osu! hitsound difficulty.
    /// </summary>
    /// <remarks>
    /// The sounds are named as the library has them and are not kept with the preset: the library is the mapper's own
    /// folder beside the game, the same whichever beatmap is open. How loud notes are is not a part of it, being said
    /// for the times of one song.
    /// </remarks>
    public class LaneSoundPreset
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A sound for each lane from the first on. A lane with none has one all the same, for the lanes after it to
        /// keep their places.
        /// </summary>
        [JsonProperty("lanes")]
        public List<LaneSoundPresetSound> Lanes { get; set; } = new List<LaneSoundPresetSound>();

        /// <summary>
        /// The hit sound of a circle none of whose lanes is one; see <see cref="LaneSoundProject.Fallback"/>.
        /// </summary>
        [JsonProperty("fallback")]
        public LaneSoundPresetSound Fallback { get; set; } = new LaneSoundPresetSound { Bank = HitSampleInfo.BANK_SOFT };

        /// <summary>
        /// Whether there is a sound in it at all: one with none says nothing worth keeping.
        /// </summary>
        [JsonIgnore]
        public bool HasSounds => Lanes.Any(l => l.HasSound) || Fallback.HasSound;

        public static LaneSoundPreset From(string name, IEnumerable<LaneSound> lanes, LaneSound fallback) => new LaneSoundPreset
        {
            Name = name.Trim(),
            Lanes = lanes.Select(LaneSoundPresetSound.From).ToList(),
            Fallback = LaneSoundPresetSound.From(fallback),
        };
    }

    /// <summary>
    /// One sound of a <see cref="LaneSoundPreset"/>.
    /// </summary>
    public class LaneSoundPresetSound
    {
        /// <summary>
        /// The sound, as <see cref="HitsoundLibrary.Files"/> names it. Empty for none.
        /// </summary>
        [JsonProperty("source")]
        public string Source { get; set; } = string.Empty;

        /// <summary>
        /// The sample set it is to be in once turned into an osu! hitsound difficulty.
        /// </summary>
        [JsonProperty("bank")]
        public string Bank { get; set; } = HitSampleInfo.BANK_NORMAL;

        /// <summary>
        /// Which of a sample set's four sounds it is to be there.
        /// </summary>
        [JsonProperty("sound")]
        public string Sound { get; set; } = HitSampleInfo.HIT_NORMAL;

        [JsonIgnore]
        public bool HasSound => !string.IsNullOrEmpty(Source);

        /// <summary>
        /// What to call the sound where there is little room: its file's name, without the folder or the extension.
        /// </summary>
        [JsonIgnore]
        public string DisplayName => HasSound ? Path.GetFileNameWithoutExtension(Source) : string.Empty;

        public static LaneSoundPresetSound From(LaneSound sound) => new LaneSoundPresetSound
        {
            // a lane sounds as the file it has in the beatmap; where that came from is all a preset can name it by.
            Source = sound.HasFile ? sound.Source : string.Empty,
            Bank = sound.Bank,
            Sound = sound.Sound,
        };
    }
}
