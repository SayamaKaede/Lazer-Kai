// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Localisation;
using osu.Game.Overlays.Dialog;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Asks, from the hitsound screen, for what keeping the difficulty in stable's format is missing: the option, the
    /// conversion, or both. Says only what is going to be done.
    /// </summary>
    public partial class UseStableFormatDialog : DangerousActionDialog
    {
        /// <param name="done">What is going to be done, which can be more than was missing: an unsaved change to a file
        /// already in stable's format is converted along with the rest.</param>
        /// <param name="action">Does it.</param>
        public UseStableFormatDialog(StableFormatNeeds done, Action action)
        {
            bool compatibility = done.HasFlag(StableFormatNeeds.Compatibility);
            bool conversion = done.HasFlag(StableFormatNeeds.Conversion);

            if (compatibility && conversion)
            {
                HeaderText = LazerKaiStableCompatibilityStrings.TurnOnAndConvertHeader;
                BodyText = LazerKaiStableCompatibilityStrings.TurnOnAndConvertBody(LazerKaiStableCompatibilityStrings.ConvertToStableFormatBody);
            }
            else if (compatibility)
            {
                HeaderText = LazerKaiStableCompatibilityStrings.TurnOnCompatibilityHeader;
                BodyText = LazerKaiStableCompatibilityStrings.TurnOnCompatibilityBody;
            }
            else
            {
                HeaderText = LazerKaiStableCompatibilityStrings.ConvertToStableFormatHeader;
                BodyText = LazerKaiStableCompatibilityStrings.ConvertToStableFormatBody;
            }

            DangerousAction = action;
        }
    }
}
