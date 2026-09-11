// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osuTK;
using osuTK.Input;

namespace osu.Game.Overlays.Profile.Sections.Beatmaps
{
    /// <summary>
    /// Downloads a whole listing, once it has been held down long enough to be sure.
    /// </summary>
    /// <remarks>
    /// Held rather than clicked, because one press can start hundreds of downloads. It fills and ticks the way a
    /// dangerous button in a dialog does, over the same half second, so it reads as the same kind of decision. It is
    /// drawn the way the counter beside it is — the same pill, the same type — so the heading still reads as a heading.
    /// </remarks>
    public partial class DownloadAllButton : CompositeDrawable, IHasTooltip
    {
        public readonly BindableBool Enabled = new BindableBool(true);

        public Action? Action { get; set; }

        public LocalisableString TooltipText { get; set; } = LazerKaiProfileStrings.DownloadAllTooltip;

        private Box background = null!;
        private Box fill = null!;
        private HoldToDownload hold = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new CircularContainer
            {
                AutoSizeAxes = Axes.Both,
                Masking = true,
                Children = new Drawable[]
                {
                    background = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background6
                    },
                    fill = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Width = 0,
                        Colour = colourProvider.Highlight1,
                        Alpha = 0.5f,
                        Blending = BlendingParameters.Additive,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(4, 0),
                        Margin = new MarginPadding { Horizontal = 10, Bottom = 1 },
                        Children = new Drawable[]
                        {
                            new SpriteIcon
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Icon = FontAwesome.Solid.Download,
                                Size = new Vector2(9),
                                Colour = colourProvider.Foreground1
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = LazerKaiProfileStrings.DownloadAll,
                                Font = OsuFont.GetFont(size: 11.2f, weight: FontWeight.Bold),
                                Colour = colourProvider.Foreground1
                            }
                        }
                    },
                    hold = new HoldToDownload(Enabled)
                    {
                        RelativeSizeAxes = Axes.Both,
                        Action = () => Action?.Invoke(),
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            hold.Progress.BindValueChanged(progress => fill.Width = (float)progress.NewValue, true);
            Enabled.BindValueChanged(enabled => this.FadeTo(enabled.NewValue ? 1 : 0.5f, 200, Easing.OutQuint), true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            if (Enabled.Value)
                background.FadeColour(colourProvider.Background5, 200, Easing.OutQuint);

            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background6, 200, Easing.OutQuint);
            base.OnHoverLost(e);
        }

        /// <summary>
        /// The hold itself, behaving the way a dangerous dialog button's does: the same ticks rising as it fills, the
        /// same sound when it lands, and letting go or sliding off takes it back.
        /// </summary>
        private partial class HoldToDownload : HoldToConfirmContainer
        {
            private readonly IBindable<bool> enabled;

            private Sample? tickSample;
            private Sample? confirmSample;
            private double lastTickPlaybackTime;
            private bool mouseDown;

            // held again once a run has finished, so it has to be able to fire more than once in its life.
            protected override bool AllowMultipleFires => true;

            public HoldToDownload(IBindable<bool> enabled)
                : base(isDangerousAction: true)
            {
                this.enabled = enabled;
            }

            [BackgroundDependencyLoader]
            private void load(AudioManager audio)
            {
                tickSample = audio.Samples.Get(@"UI/dialog-dangerous-tick");
                confirmSample = audio.Samples.Get(@"UI/dialog-dangerous-select");
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();
                Progress.BindValueChanged(progressChanged);
            }

            protected override void Confirm()
            {
                confirmSample?.Play();
                base.Confirm();
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button != MouseButton.Left || !enabled.Value)
                    return false;

                BeginConfirm();
                mouseDown = true;
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                if (e.HasAnyButtonPressed)
                    return;

                AbortConfirm();
                mouseDown = false;
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (mouseDown && enabled.Value)
                    BeginConfirm();

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                base.OnHoverLost(e);

                if (mouseDown)
                    AbortConfirm();
            }

            private void progressChanged(ValueChangedEvent<double> progress)
            {
                if (tickSample == null || progress.NewValue < progress.OldValue)
                    return;

                if (Clock.CurrentTime - lastTickPlaybackTime < 40)
                    return;

                var channel = tickSample.GetChannel();

                channel.Frequency.Value = 1 + progress.NewValue;
                channel.Volume.Value = 0.1f + progress.NewValue / 2f;

                channel.Play();

                lastTickPlaybackTime = Clock.CurrentTime;
            }
        }
    }
}
