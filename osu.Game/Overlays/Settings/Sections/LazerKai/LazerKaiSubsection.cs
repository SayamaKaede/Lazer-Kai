// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

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

        private Box line = null!;
        private OsuSpriteText text = null!;

        protected override Drawable CreateHeader() => new FillFlowContainer
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
        };

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            line.Colour = colourProvider.Background3;
            text.Colour = colourProvider.Light1;
        }
    }
}
