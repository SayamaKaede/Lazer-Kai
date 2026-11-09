// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Graphics.UserInterface;
using osu.Game.Utils;

namespace osu.Game.Screens.Edit.Components.Menus
{
    public class DifficultyMenuItem : StatefulMenuItem<bool>
    {
        public BeatmapInfo BeatmapInfo { get; }

        public DifficultyMenuItem(BeatmapInfo beatmapInfo, bool selected, Action<BeatmapInfo> difficultyChangeFunc)
            : base(LabelFor(beatmapInfo), null)
        {
            BeatmapInfo = beatmapInfo;
            State.Value = selected;

            if (!selected)
                Action.Value = () => difficultyChangeFunc.Invoke(beatmapInfo);
        }

        public override IconUsage? GetIconForState(bool state) => state ? FontAwesome.Solid.Check : null;

        /// <summary>
        /// The difficulty's name with its star rating after it, as in "Hard (3.59*)" (2026-11-09).
        /// </summary>
        /// <remarks>
        /// The stars are the ones last saved, floored the way the rest of the game shows them. A rating still waiting to
        /// be worked out is kept below zero, and the name is then shown alone rather than with a made-up number.
        /// </remarks>
        public static LocalisableString LabelFor(BeatmapInfo beatmapInfo)
        {
            string name = string.IsNullOrEmpty(beatmapInfo.DifficultyName) ? "(unnamed)" : beatmapInfo.DifficultyName;

            if (beatmapInfo.StarRating < 0)
                return name;

            return LocalisableString.Interpolate($"{name} ({beatmapInfo.StarRating.FormatStarRating()}*)");
        }
    }
}
