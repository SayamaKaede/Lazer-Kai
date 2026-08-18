// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;
using osu.Game.Overlays.OSD;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// A toast reporting something the editor just did, shown over the middle of the screen.
    /// </summary>
    /// <remarks>
    /// Used for actions which have a key binding but no other visible result — pressing the key and seeing nothing
    /// leaves the user unsure whether it registered.
    /// </remarks>
    public partial class EditorToast : Toast
    {
        public EditorToast(LocalisableString value, LocalisableString extraText = default)
            : base(InputSettingsStrings.EditorSection, value)
        {
            ExtraText = extraText;
        }
    }
}
