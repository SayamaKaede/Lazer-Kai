// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Rulesets.Osu.Edit
{
    /// <summary>
    /// Which stretch of a stream the settings below are about, and the way between them.
    /// </summary>
    /// <remarks>
    /// A shape that turns corners is laid down a stretch at a time, and each stretch is told what to do on its own.
    /// The arrows stay put whether there is one stretch or six -- a row that appeared the moment a corner was added
    /// would move everything under it, and the settings are being read while the shape is being pulled about.
    /// </remarks>
    public partial class StreamStretchSelector : CompositeDrawable
    {
        public readonly BindableInt Current = new BindableInt { MinValue = 0 };

        /// <summary>How many stretches there are to choose between.</summary>
        public readonly BindableInt Count = new BindableInt(1) { MinValue = 1 };

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        private OsuSpriteText text = null!;
        private IconButton previous = null!;
        private IconButton next = null!;

        public StreamStretchSelector()
        {
            RelativeSizeAxes = Axes.X;
            Height = 34;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background5,
                },
                previous = new IconButton
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 4 },
                    Scale = new Vector2(0.6f),
                    Icon = FontAwesome.Solid.ChevronLeft,
                    Action = () => Current.Value--,
                },
                text = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Font = OsuFont.Default.With(size: 14, weight: FontWeight.SemiBold),
                },
                next = new IconButton
                {
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 4 },
                    Scale = new Vector2(0.6f),
                    Icon = FontAwesome.Solid.ChevronRight,
                    Action = () => Current.Value++,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Count.BindValueChanged(c =>
            {
                Current.MaxValue = c.NewValue - 1;
                update();
            });

            Current.BindValueChanged(_ => update(), true);
        }

        private void update()
        {
            text.Text = $@"{Current.Value + 1} / {Count.Value}";

            // dimmed rather than taken away, so the row does not change shape as corners come and go.
            previous.Enabled.Value = Current.Value > 0;
            next.Enabled.Value = Current.Value < Count.Value - 1;

            previous.Alpha = previous.Enabled.Value ? 1 : 0.3f;
            next.Alpha = next.Enabled.Value ? 1 : 0.3f;
            text.Alpha = Count.Value > 1 ? 1 : 0.5f;
        }
    }
}
