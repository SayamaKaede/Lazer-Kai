// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Game.Configuration;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit
{
    /// <summary>
    /// Reads the user's preferences for how selection outlines look in the editor (the borders drawn around selected
    /// objects and around a drag selection), and notifies consumers when they change.
    /// </summary>
    /// <remarks>
    /// Each consumer supplies its own default colour and border thickness, so leaving the settings untouched
    /// preserves the stock appearance of every individual piece.
    /// </remarks>
    public class EditorSelectionAppearance
    {
        /// <summary>
        /// Colour setting value meaning "leave each piece at its own default colour".
        /// </summary>
        public const string DEFAULT_COLOUR = "";

        private readonly Bindable<string> colour;
        private readonly Bindable<float> thickness;
        private readonly Bindable<float> opacity;

        /// <summary>
        /// Invoked whenever any of the underlying settings change.
        /// </summary>
        public event Action? Changed;

        public EditorSelectionAppearance(OsuConfigManager config)
        {
            colour = config.GetBindable<string>(OsuSetting.EditorSelectionColour);
            thickness = config.GetBindable<float>(OsuSetting.EditorSelectionThickness);
            opacity = config.GetBindable<float>(OsuSetting.EditorSelectionOpacity);

            colour.ValueChanged += _ => Changed?.Invoke();
            thickness.ValueChanged += _ => Changed?.Invoke();
            opacity.ValueChanged += _ => Changed?.Invoke();
        }

        /// <param name="fallback">The colour to use while the user has not chosen an override.</param>
        public Color4 GetColour(Color4 fallback)
        {
            if (string.IsNullOrEmpty(colour.Value))
                return fallback;

            try
            {
                return Color4Extensions.FromHex(colour.Value);
            }
            catch (Exception)
            {
                // a malformed value should never take the editor down; fall back to the stock colour.
                return fallback;
            }
        }

        /// <param name="baseThickness">The piece's stock border thickness, which the user's setting scales.</param>
        public float GetThickness(float baseThickness) => baseThickness * thickness.Value;

        public float Opacity => opacity.Value;
    }
}
