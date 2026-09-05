// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Game.IO.Serialization;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Screens.Edit.Presets
{
    /// <summary>
    /// A shape kept aside to be placed again later.
    /// </summary>
    /// <remarks>
    /// The objects are held in the same form the clipboard uses, so whatever can be copied can be kept here: this
    /// starts out being reached for with sliders, but a run of circles is the same thing to everything below.
    /// </remarks>
    public class PlacementPreset
    {
        /// <summary>
        /// The digit this answers to while the list is open, or null if it has none.
        /// </summary>
        /// <remarks>
        /// A digit rather than a key combination: this is read while the list is open and the hand is already over
        /// the number row. Stored as the digit itself rather than as an <c>InputKey</c>, whose numbering belongs to
        /// the framework and is free to change under us.
        /// </remarks>
        public int? Digit { get; set; }

        /// <summary>
        /// The objects, serialised the way the clipboard serialises them.
        /// </summary>
        /// <remarks>
        /// Kept as text rather than as objects so that this file's own shape does not depend on the serialiser
        /// settings the hit object converters need. Reading it back is the same call a paste makes.
        /// </remarks>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Reads the objects back out, or an empty list if this preset cannot be read.
        /// </summary>
        public IList<HitObject> GetObjects()
        {
            if (string.IsNullOrEmpty(Content))
                return new List<HitObject>();

            return Content.Deserialize<ClipboardContent>().HitObjects ?? new List<HitObject>();
        }

        /// <summary>
        /// Builds a preset holding the given objects.
        /// </summary>
        public static PlacementPreset From(IEnumerable<HitObject> hitObjects) => new PlacementPreset
        {
            Content = new ClipboardContent { HitObjects = new List<HitObject>(hitObjects) }.Serialize(),
        };
    }
}
