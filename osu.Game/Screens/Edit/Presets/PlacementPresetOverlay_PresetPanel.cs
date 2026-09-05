// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Presets
{
    public partial class PlacementPresetOverlay
    {
        /// <summary>
        /// One kept shape in the list: what it looks like, the digit it answers to, and the two things that can be
        /// done to it.
        /// </summary>
        private partial class PresetPanel : CompositeDrawable
        {
            private const float size = 100;

            public Action? Place;
            public Action? AssignKey;
            public Action? Remove;

            private readonly PlacementPreset preset;
            private readonly float objectRadius;

            private FillFlowContainer buttons = null!;

            public PresetPanel(PlacementPreset preset, float objectRadius)
            {
                this.preset = preset;
                this.objectRadius = objectRadius;

                Size = new Vector2(size);
            }

            [BackgroundDependencyLoader]
            private void load(OverlayColourProvider colourProvider)
            {
                Masking = true;
                CornerRadius = 6;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new PlacementPresetThumbnail(preset.GetObjects(), objectRadius)
                    {
                        RelativeSizeAxes = Axes.Both,
                    },
                    new Container
                    {
                        Anchor = Anchor.TopLeft,
                        Origin = Anchor.TopLeft,
                        Margin = new MarginPadding(4),
                        Size = new Vector2(18),
                        Masking = true,
                        CornerRadius = 4,
                        Alpha = preset.Digit == null ? 0 : 1,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                RelativeSizeAxes = Axes.Both,
                                Colour = colourProvider.Background3,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Font = OsuFont.Default.With(size: 12, weight: FontWeight.Bold),
                                Text = preset.Digit?.ToString() ?? string.Empty,
                            },
                        }
                    },
                    buttons = new FillFlowContainer
                    {
                        Anchor = Anchor.BottomRight,
                        Origin = Anchor.BottomRight,
                        Margin = new MarginPadding(2),
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Alpha = 0,
                        Children = new Drawable[]
                        {
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.Keyboard,
                                Scale = new Vector2(0.5f),
                                TooltipText = PlacementPresetStrings.AssignAKey,
                                Action = () => AssignKey?.Invoke(),
                            },
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.Times,
                                Scale = new Vector2(0.5f),
                                TooltipText = PlacementPresetStrings.Remove,
                                Action = () => Remove?.Invoke(),
                            },
                        }
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                buttons.FadeIn(100);
                return false;
            }

            protected override void OnHoverLost(HoverLostEvent e) => buttons.FadeOut(100);

            protected override bool OnClick(ClickEvent e)
            {
                Place?.Invoke();
                return true;
            }
        }
    }
}
