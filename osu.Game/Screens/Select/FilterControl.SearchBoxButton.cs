// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Select
{
    public sealed partial class FilterControl
    {
        /// <summary>
        /// One of the buttons at the right end of the search box, each opening a popover.
        /// </summary>
        public partial class SearchBoxButton : OsuClickableContainer, IHasPopover
        {
            public const float WIDTH = 40;

            /// <summary>
            /// Makes what the button opens.
            /// </summary>
            public Func<Popover>? CreatePopover { get; set; }

            private readonly Box background;
            private readonly SpriteIcon icon;

            private bool popoverOpen;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public SearchBoxButton(IconUsage iconUsage, LocalisableString tooltip)
            {
                RelativeSizeAxes = Axes.Y;
                Width = WIDTH;
                TooltipText = tooltip;

                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                    },
                    icon = new SpriteIcon
                    {
                        Icon = iconUsage,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(16),
                        Shear = -OsuGame.SHEAR,
                    },
                };

                Action = this.ShowPopover;
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                background.Colour = colourProvider.Background2;
                updateState();
            }

            public Popover? GetPopover()
            {
                var popover = CreatePopover?.Invoke();

                popover?.State.BindValueChanged(s =>
                {
                    popoverOpen = s.NewValue == Visibility.Visible;
                    updateState();
                });

                return popover;
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();
                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
                base.OnHoverLost(e);
            }

            private void updateState()
            {
                bool lit = IsHovered || popoverOpen;

                background.FadeTo(lit ? 1 : 0, 200, Easing.OutQuint);
                icon.FadeColour(lit ? colourProvider.Content1 : colourProvider.Content2, 200, Easing.OutQuint);
            }
        }
    }
}
