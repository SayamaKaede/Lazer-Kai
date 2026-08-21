// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Whether kiai's flashing is to be left out of whatever is drawn below.
    /// </summary>
    /// <remarks>
    /// Only the editor puts this in place, so anything drawing a kiai flash outside one finds nothing here and carries
    /// on flashing. Placing the choice here rather than reading the setting at each flash keeps those drawables from
    /// having to work out whether they are in an editor at all.
    /// </remarks>
    public class KiaiFlashSuppression : BindableBool
    {
        protected override Bindable<bool> CreateInstance() => new KiaiFlashSuppression();
    }
}
