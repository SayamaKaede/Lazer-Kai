// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Overlays.Settings
{
    /// <summary>
    /// A subsection listed under its section's button in the sidebar.
    /// </summary>
    /// <remarks>
    /// This fork's sections hold more subsections than the sidebar has sections, so jumping to a section still left a
    /// long scroll to the group wanted. Only the subsections of the section being read are listed: all of them at once
    /// would not fit the sidebar's height.
    /// </remarks>
    public partial class SidebarSubsectionButton : SidebarButton
    {
        public required SettingsSection Section { get; init; }

        public required SettingsSubsection Subsection { get; init; }

        // settled against the real thing through tuning sliders (2026-09-15).
        private const float row_height = 18;
        private const float indent = 60;
        private const float font_size = 14;

        private TruncatingSpriteText text = null!;

        private bool selected;

        /// <summary>
        /// Whether this is the subsection being read.
        /// </summary>
        public bool Selected
        {
            get => selected;
            set
            {
                selected = value;

                if (IsLoaded)
                    UpdateState();
            }
        }

        public SidebarSubsectionButton()
        {
            RelativeSizeAxes = Axes.X;
            Height = row_height;
            Padding = new MarginPadding { Left = indent, Right = 10 };
            Alpha = 0;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // a long name is cut short, and shows in full as a tooltip.
            Add(text = new TruncatingSpriteText
            {
                Text = Subsection.SidebarHeader,
                Font = OsuFont.Default.With(size: font_size),
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                RelativeSizeAxes = Axes.X,
            });
        }

        /// <summary>
        /// Lists or unlists this row. Called from outside every frame: once hidden, a drawable is no longer updated, so
        /// it cannot notice for itself when to come back.
        /// </summary>
        /// <param name="sectionBeingRead">Whether <see cref="Section"/> is the section being read.</param>
        public void UpdateShown(bool sectionBeingRead)
        {
            // a subsection the settings search has filtered out is not listed either.
            Alpha = sectionBeingRead && Subsection.IsPresent ? 1 : 0;
        }

        protected override void UpdateState()
        {
            base.UpdateState();

            text.FadeColour(Selected ? ColourProvider.Content1 : IsHovered ? ColourProvider.Light1 : ColourProvider.Light3, FADE_DURATION, Easing.OutQuint);
        }
    }
}
