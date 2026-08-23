// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;

namespace osu.Game.Screens.Edit.Components
{
    /// <summary>
    /// Stretches a toolbox button towards being square, by however much the user asked for.
    /// </summary>
    /// <remarks>
    /// The stock buttons are wide and short. Stable's are close to square, and hitting a square target is easier,
    /// so the height is adjustable — but only upwards from stock, and only as far as square, since past that the
    /// column would run off the screen.
    ///
    /// The amount is a fraction rather than a height in pixels because the groups are not the same width: the tools
    /// fill the toolbox while the toggles sit two to a row. Each button works its own square out from its own width.
    /// </remarks>
    public partial class EditorButtonSquareness : Component
    {
        /// <summary>
        /// The height a toolbox button has when nothing has been adjusted.
        /// </summary>
        public const float STOCK_HEIGHT = 40;

        private readonly Drawable button;
        private readonly float stockHeight;

        /// <summary>
        /// Which setting sizes this button. Columns share button types, so which one a button listens to is not
        /// something its type can be read off.
        /// </summary>
        public OsuSetting Setting { get; }

        private readonly Bindable<float> squareness = new Bindable<float>();

        [Resolved(CanBeNull = true)]
        private IExpandingContainer? expandingContainer { get; set; }

        /// <param name="button">The button to size.</param>
        /// <param name="setting">Which setting decides its height.</param>
        /// <param name="stockHeight">The height this button has when nothing has been adjusted.</param>
        public EditorButtonSquareness(Drawable button, OsuSetting setting, float stockHeight = STOCK_HEIGHT)
        {
            this.button = button;
            this.stockHeight = stockHeight;
            Setting = setting;
        }

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(Setting, squareness);
        }

        protected override void Update()
        {
            base.Update();

            // a contracted toolbox shows icons only, where a tall button would just be a tall icon.
            bool expanded = expandingContainer?.Expanded.Value != false;

            float target = stockHeight;

            if (expanded && squareness.Value > 0 && button.DrawWidth > stockHeight)
                target = stockHeight + (button.DrawWidth - stockHeight) * squareness.Value;

            button.Height = target;
        }
    }
}
