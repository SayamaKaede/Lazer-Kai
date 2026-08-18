// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;

namespace osu.Game.Beatmaps.ControlPoints
{
    /// <summary>
    /// Control points set aside when a beatmap was opened for editing, so they can be written back out.
    /// </summary>
    /// <remarks>
    /// The editor holds volume, sample bank and slider velocity on the objects themselves, and drops the green lines
    /// they came from — leaving those in place would let them overwrite edits. But a green line part way through a
    /// slider, or inside a spinner, belongs to no object and has nowhere else to go, so dropping it loses work that
    /// was done in stable and cannot be redone here.
    ///
    /// They are kept away from <see cref="ControlPointInfo.AllControlPoints"/> on purpose: nothing should read them
    /// while editing. They exist to be written back when the beatmap is saved, and only where the beatmap's own
    /// contents did not already produce a line at that time.
    /// </remarks>
    public class PreservedLegacyControlPoints : ControlPointInfo
    {
        private readonly List<ControlPoint> preserved = new List<ControlPoint>();

        public IReadOnlyList<ControlPoint> Preserved => preserved;

        public void Preserve(ControlPoint controlPoint) => preserved.Add(controlPoint);

        public override void Clear()
        {
            base.Clear();
            preserved.Clear();
        }

        /// <remarks>
        /// <see cref="ControlPointInfo.DeepClone"/> is not virtual and copies only the points it knows about, so the
        /// preserved ones are carried over here. Without this they would be lost the next time the beatmap is cloned,
        /// which happens on the way to gameplay.
        /// </remarks>
        public PreservedLegacyControlPoints DeepCloneWithPreserved()
        {
            var clone = (PreservedLegacyControlPoints)DeepClone();

            foreach (var point in preserved)
                clone.Preserve(point.DeepClone());

            return clone;
        }
    }
}
