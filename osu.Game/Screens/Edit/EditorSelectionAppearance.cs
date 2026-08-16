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
        private readonly Bindable<string> handleColour;
        private readonly Bindable<float> handleScale;
        private readonly Bindable<float> anchorScale;

        /// <summary>
        /// Invoked whenever any of the underlying settings change.
        /// </summary>
        public event Action? Changed;

        public EditorSelectionAppearance(OsuConfigManager config)
        {
            colour = config.GetBindable<string>(OsuSetting.EditorSelectionColour);
            thickness = config.GetBindable<float>(OsuSetting.EditorSelectionThickness);
            opacity = config.GetBindable<float>(OsuSetting.EditorSelectionOpacity);

            handleColour = config.GetBindable<string>(OsuSetting.EditorSelectionHandleColour);
            handleScale = config.GetBindable<float>(OsuSetting.EditorSelectionHandleScale);
            anchorScale = config.GetBindable<float>(OsuSetting.EditorSliderAnchorScale);

            colour.ValueChanged += _ => Changed?.Invoke();
            thickness.ValueChanged += _ => Changed?.Invoke();
            opacity.ValueChanged += _ => Changed?.Invoke();
            handleColour.ValueChanged += _ => Changed?.Invoke();
            handleScale.ValueChanged += _ => Changed?.Invoke();
            anchorScale.ValueChanged += _ => Changed?.Invoke();
        }

        /// <param name="fallback">The colour to use while the user has not chosen an override.</param>
        public Color4 GetColour(Color4 fallback) => parseColour(colour.Value, fallback);

        /// <summary>
        /// The colour of the drag handles on the selection box, which the user sets separately from the outline.
        /// </summary>
        /// <param name="fallback">The colour to use while the user has not chosen an override.</param>
        public Color4 GetHandleColour(Color4 fallback) => parseColour(handleColour.Value, fallback);

        private static Color4 parseColour(string value, Color4 fallback)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            try
            {
                return Color4Extensions.FromHex(value);
            }
            catch (Exception)
            {
                // a malformed value should never take the editor down; fall back to the stock colour.
                return fallback;
            }
        }

        /// <param name="baseSize">The handle's stock size, which the user's setting scales.</param>
        public float GetHandleSize(float baseSize) => baseSize * handleScale.Value;

        /// <param name="baseSize">A slider anchor's stock size, which the user's setting scales.</param>
        public float GetAnchorSize(float baseSize) => baseSize * anchorScale.Value;

        /// <param name="baseThickness">The piece's stock border thickness, which the user's setting scales.</param>
        public float GetThickness(float baseThickness) => baseThickness * thickness.Value;

        public float Opacity => opacity.Value;
    }
}
