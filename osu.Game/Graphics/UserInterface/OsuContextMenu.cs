// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osuTK.Graphics;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.UserInterface;

namespace osu.Game.Graphics.UserInterface
{
    public partial class OsuContextMenu : OsuMenu
    {
        [Resolved]
        private OsuMenuSamples menuSamples { get; set; } = null!;

        public OsuContextMenu(bool playSamples)
            : base(Direction.Vertical, topLevelMenu: false, playSamples)
        {
            MaskingContainer.CornerRadius = 5;
            MaskingContainer.EdgeEffect = new EdgeEffectParameters
            {
                Type = EdgeEffectType.Shadow,
                Colour = Color4.Black.Opacity(0.1f),
                Radius = 4,
            };

            ItemsContainer.Padding = new MarginPadding { Vertical = DrawableOsuMenuItem.MARGIN_VERTICAL };

            MaxHeight = default_max_height;
        }

        /// <summary>Upstream's cap, kept as the smallest this menu will offer.</summary>
        private const float default_max_height = 250;

        /// <summary>Room left between the menu and the edges of the screen.</summary>
        private const float screen_margin = 10;

        protected override void Update()
        {
            base.Update();

            // a menu taller than its cap scrolls, and a menu item behind a scroll is one nobody knows is there. It is
            // allowed to grow to the height of what holds it instead, which is the screen.
            float available = (Parent?.DrawHeight ?? 0) - screen_margin * 2;

            MaxHeight = Math.Max(default_max_height, available);
        }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            BackgroundColour = colours.ContextMenuGray;
        }

        protected override void AnimateOpen()
        {
            if (PlaySamples && !WasOpened)
                menuSamples.PlayClickSample();

            base.AnimateOpen();
        }

        protected override Menu CreateSubMenu() => new OsuContextMenu(false); // sub menu samples are handled by OsuMenu.OnSubmenuOpen.
    }
}
