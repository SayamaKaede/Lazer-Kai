// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Beatmaps;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.BeatmapFiles
{
    /// <summary>
    /// A screen on which the files of the beatmap set being edited are seen to from inside the editor (2026-10-13).
    /// </summary>
    /// <remarks>
    /// A set has no folder here: one is written out for it to be opened, and the editor waits until it is done with,
    /// so the folders of two sets are never open side by side to copy from one to the other. What a mapper opened the
    /// folder for is done on these instead, on the files as the game keeps them: one screen for each thing done, each
    /// under its own name in the File menu (asked for in place of one screen with a tab for each).
    ///
    /// The editor is saved before one is opened, as before the folder is: what is done is written to the set at once,
    /// beside the difficulty as it was last saved.
    /// </remarks>
    public abstract partial class BeatmapFilesScreen : OsuScreen
    {
        // as in the editor this is a part of: the toolbar would lead out of it by a way that is not this screen's.
        public override bool HideOverlaysOnEnter => true;

        /// <summary>
        /// What the screen is for, as the File menu calls it.
        /// </summary>
        protected abstract LocalisableString Heading { get; }

        /// <summary>
        /// Whether the files of the set were changed here: the editor then has to open the set again to have them.
        /// </summary>
        protected abstract bool Changed { get; }

        protected abstract Drawable CreateContent();

        [Resolved]
        private Editor editor { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        private bool reopening;

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            InternalChild = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                RelativeSizeAxes = Axes.Both,
                Size = new Vector2(0.8f),
                Masking = true,
                CornerRadius = 20,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(20),
                        Child = new GridContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            RowDimensions = new[]
                            {
                                new Dimension(GridSizeMode.AutoSize),
                                new Dimension(GridSizeMode.Absolute, 15),
                                new Dimension(),
                            },
                            Content = new[]
                            {
                                new Drawable[]
                                {
                                    new OsuSpriteText
                                    {
                                        Text = Heading,
                                        Font = OsuFont.Default.With(size: 24, weight: FontWeight.SemiBold),
                                    },
                                },
                                new[] { Empty() },
                                new[] { CreateContent() },
                            },
                        },
                    },
                },
            };
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            // the editor has the set as it was when it opened it: what was brought in is not among the difficulties it
            // lists, and a sound it looked for and did not find it does not look for again. It opens the set again.
            if (Changed && !reopening)
            {
                reopening = true;

                var edited = beatmaps.QueryBeatmap(b => b.ID == editorBeatmap.BeatmapInfo.ID);

                if (edited != null)
                {
                    // not from in here: the editor is left by way of this screen, which is in the middle of being left.
                    Schedule(() => editor.SwitchToDifficulty(edited));
                    return true;
                }
            }

            return base.OnExiting(e);
        }
    }
}
