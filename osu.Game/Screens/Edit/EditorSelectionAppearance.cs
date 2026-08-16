// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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

        private readonly OsuConfigManager config;

        /// <summary>
        /// Bindables are fetched on first use rather than up front.
        /// </summary>
        /// <remarks>
        /// One of these is built per drawable that draws a selection, and blueprints are created and thrown away
        /// constantly as playback brings objects in and out. Each consumer reads two or three of these settings, so
        /// fetching all six every time was mostly waste.
        /// </remarks>
        private readonly Dictionary<OsuSetting, IBindable> bindables = new Dictionary<OsuSetting, IBindable>();

        /// <summary>
        /// Invoked whenever any of the settings this instance has read change.
        /// </summary>
        public event Action? Changed;

        public EditorSelectionAppearance(OsuConfigManager config)
        {
            this.config = config;
        }

        private Bindable<T> get<T>(OsuSetting setting)
        {
            if (bindables.TryGetValue(setting, out var existing))
                return (Bindable<T>)existing;

            var bindable = config.GetBindable<T>(setting);
            bindable.ValueChanged += _ => Changed?.Invoke();

            bindables[setting] = bindable;
            return bindable;
        }

        /// <param name="fallback">The colour to use while the user has not chosen an override.</param>
        public Color4 GetColour(Color4 fallback) => parseColour(get<string>(OsuSetting.EditorSelectionColour).Value, fallback);

        /// <summary>
        /// The colour of the drag handles on the selection box, which the user sets separately from the outline.
        /// </summary>
        /// <param name="fallback">The colour to use while the user has not chosen an override.</param>
        public Color4 GetHandleColour(Color4 fallback) => parseColour(get<string>(OsuSetting.EditorSelectionHandleColour).Value, fallback);

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
        public float GetHandleSize(float baseSize) => baseSize * get<float>(OsuSetting.EditorSelectionHandleScale).Value;

        /// <param name="baseSize">A slider anchor's stock size, which the user's setting scales.</param>
        public float GetAnchorSize(float baseSize) => baseSize * get<float>(OsuSetting.EditorSliderAnchorScale).Value;

        /// <param name="baseThickness">The piece's stock border thickness, which the user's setting scales.</param>
        public float GetThickness(float baseThickness) => baseThickness * get<float>(OsuSetting.EditorSelectionThickness).Value;

        public float Opacity => get<float>(OsuSetting.EditorSelectionOpacity).Value;
    }
}
