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
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Screens.Edit.Compose.Components;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Collab
{
    /// <summary>
    /// How much of the beatmap one collaborator has, which is the figure the whole screen exists to produce.
    /// </summary>
    public partial class CollabTotal : CompositeDrawable, IHasPopover, IHasTooltip
    {
        private readonly string name;
        private readonly double drainTime;
        private readonly OverlayColourProvider colourProvider;

        /// <summary>Takes this collaborator off the beatmap, or null for a reading nobody can be taken off.</summary>
        public Action? Removed { get; init; }

        /// <summary>Gives them the name they are known by, or null where there is nobody to name.</summary>
        public Action<string>? Renamed { get; init; }

        /// <summary>Told whether the pointer is resting here, so everything they have can be lit at once.</summary>
        public Action<bool>? Pointed { get; init; }

        private Box background = null!;

        public CollabTotal(string name, double drainTime, OverlayColourProvider colourProvider)
        {
            this.name = name;
            this.drainTime = drainTime;
            this.colourProvider = colourProvider;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;
            Masking = true;
            CornerRadius = 5;

            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(8, 0),
                Padding = new MarginPadding { Horizontal = 10, Vertical = 6 },
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = name,
                        Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Text = ComposeStatusDisplay.DrainTimeReadout(drainTime),
                        Font = OsuFont.Default.With(size: 13),
                        Colour = colourProvider.Foreground1,
                    },
                }
            };

            if (Removed != null)
            {
                flow.Add(new IconButton
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Icon = FontAwesome.Solid.Times,
                    Scale = new Vector2(0.5f),
                    Action = Removed,
                });
            }

            InternalChildren = new Drawable[]
            {
                background = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background2,
                },
                flow,
            };
        }

        /// <summary>
        /// Says what clicking does, which nothing else on the reading would.
        /// </summary>
        public LocalisableString TooltipText => Renamed == null ? default : CollabStrings.Rename;

        public Popover GetPopover() => new RenamePopover(name, renamed =>
        {
            this.HidePopover();
            Renamed?.Invoke(renamed);
        });

        /// <summary>
        /// The whole reading is the target, rather than a pencil beside it: it is a small thing to hit, and there is
        /// nothing else clicking one of these could sensibly mean.
        /// </summary>
        protected override bool OnClick(ClickEvent e)
        {
            if (Renamed == null)
                return false;

            this.ShowPopover();
            return true;
        }

        /// <summary>
        /// Pointing at a reading lights every stretch it counts, which is the other half of the question it answers:
        /// how much they have, and which parts of the song those are.
        /// </summary>
        protected override bool OnHover(HoverEvent e)
        {
            background.FadeColour(colourProvider.Background1, 100, Easing.OutQuint);
            Pointed?.Invoke(true);

            return false;
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background2, 100, Easing.OutQuint);
            Pointed?.Invoke(false);
        }

        /// <summary>
        /// Where a collaborator is given the name they are known by.
        /// </summary>
        /// <remarks>
        /// Committed when the box is left as well as on enter, because a name typed and then clicked away from was
        /// still typed. An empty name, or one somebody else already has, is refused by the panel itself.
        /// </remarks>
        private partial class RenamePopover : OsuPopover
        {
            private readonly string current;
            private readonly Action<string> commit;

            private bool committed;

            private OsuTextBox textBox = null!;

            public RenamePopover(string current, Action<string> commit)
            {
                this.current = current;
                this.commit = commit;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AllowableAnchors = new[] { Anchor.TopCentre, Anchor.BottomCentre };

                // the box is drawn outside the panel, so what lands on it has to be stopped here instead.
                Child = new CollabInputBlocker
                {
                    Width = 220,
                    AutoSizeAxes = Axes.Y,
                    Child = textBox = new OsuTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        Text = current,
                        CommitOnFocusLost = true,
                        PlaceholderText = CollabStrings.CollaboratorName,
                    },
                };

                textBox.OnCommit += (t, _) =>
                {
                    if (committed)
                        return;

                    committed = true;
                    commit(t.Text);
                };
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                // the box is opened to be typed in, so it is what the keyboard reaches without another click.
                ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(textBox));
            }
        }
    }
}
