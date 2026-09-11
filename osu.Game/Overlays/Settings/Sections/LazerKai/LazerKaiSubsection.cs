// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Overlays.Settings.Sections.LazerKai
{
    /// <summary>
    /// A group of this fork's settings, headed so that where one group ends and the next begins reads at a glance.
    /// </summary>
    /// <remarks>
    /// The game draws a subsection heading as plain text at the same weight as the rows under it, which is enough
    /// when a section holds three or four groups. This fork's sections hold fifteen between them, and scrolling
    /// through looking for a setting meant reading every row on the way. A line above the heading says where the
    /// group starts; the heading itself is set apart from the rows so the eye can skip between them.
    ///
    /// The heading is built without asking for anything, and coloured afterwards. Dependencies are handed to a
    /// class after its base class has loaded, and the base is what calls <see cref="CreateHeader"/> -- reaching for
    /// them from in there gets null.
    /// </remarks>
    public abstract partial class LazerKaiSubsection : SettingsSubsection
    {
        private const int header_font_size = 20;

        /// <summary>The room left above the line, which is what sets one group apart from the one before it.</summary>
        private const float line_spacing = 25;

        private const float line_thickness = 2;

        /// <summary>
        /// The room above a button that directly follows another row, where the two otherwise touch and read as one
        /// crowded block. Settled against the real thing through a tuning slider (2026-09-11).
        /// </summary>
        protected const float BUTTON_GAP = 10;

        /// <summary>
        /// Whether this group carries the fork's mark behind its rows. Every one of them does.
        /// </summary>
        protected internal virtual bool ShowsMark => true;

        private Box line = null!;
        private OsuSpriteText text = null!;

        /// <summary>
        /// The mark's own window on the world, which is exactly this group's block.
        /// </summary>
        /// <remarks>
        /// It masks so the mark cannot reach into the group above or below: each mark belongs to one group, and one
        /// running past a heading reads as belonging to the wrong one. The window is here rather than on the
        /// subsection itself because masking that would also cut off anything a settings row opens over it.
        ///
        /// It lives inside the header because a subsection is a flow container, so anything added to it becomes
        /// another row. Being in the header also puts it behind the rows, which is where a mark belongs.
        /// </remarks>
        private Container? markWindow;

        private LazerKaiMark? mark;

        protected override Drawable CreateHeader()
        {
            var header = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Child = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Children = new Drawable[]
                    {
                        // inset the way the rows are, so it reads as a divider inside the section rather than a new one.
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = line_thickness,
                            Margin = new MarginPadding { Top = line_spacing },
                            Padding = SettingsPanel.CONTENT_PADDING,
                            Child = line = new Box { RelativeSizeAxes = Axes.Both },
                        },
                        text = new OsuSpriteText
                        {
                            Text = Header,
                            Font = OsuFont.GetFont(size: header_font_size, weight: FontWeight.SemiBold),
                            Margin = new MarginPadding { Vertical = VERTICAL_PADDING, Horizontal = 5 },
                            Padding = SettingsPanel.CONTENT_PADDING,
                        },
                    },
                },
            };

            if (ShowsMark)
            {
                header.Add(markWindow = new Container
                {
                    RelativeSizeAxes = Axes.X,
                    // starts at the dividing line rather than at the very top, so the gap that separates this group
                    // from the one above stays empty.
                    Y = line_spacing,
                    Masking = true,
                    // the header measures itself off the line and the heading alone; the window hangs down over the
                    // rows below and must not push them anywhere.
                    BypassAutoSizeAxes = Axes.Both,
                    // the mark places itself against this corner: it is the only one that stays put as the panel
                    // is resized, and the mark's own settings are measured from it.
                    Child = mark = new LazerKaiMark
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopLeft,
                    },
                });
            }

            return header;
        }

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            line.Colour = colourProvider.Background3;
            text.Colour = colourProvider.Light1;
        }

        protected override void Update()
        {
            base.Update();

            if (markWindow != null)
                markWindow.Height = Math.Max(0, DrawHeight - line_spacing);
        }
    }
}
