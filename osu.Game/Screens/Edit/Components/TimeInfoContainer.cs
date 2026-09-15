// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.LocalisationExtensions;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Game.Graphics.Sprites;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Utils;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osuTK;

namespace osu.Game.Screens.Edit.Components
{
    public partial class TimeInfoContainer : BottomBarContainer
    {
        private OsuSpriteText bpm = null!;
        private OsuSpriteText progress = null!;
        private OsuSpriteText starRating = null!;
        private OsuSpriteText kiai = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        // not always present: the bottom bar can be constructed without a full editor around it (notably in test scenes).
        [Resolved(canBeNull: true)]
        private EditorDifficultyTracker? difficultyTracker { get; set; }

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            Background.Colour = colourProvider.Background5;

            Children = new Drawable[]
            {
                new TimestampControl(),
                bpm = new OsuSpriteText
                {
                    Colour = colours.Orange1,
                    Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold, fixedWidth: true),
                    Spacing = new Vector2(-1, 0),
                    Position = new Vector2(0, 4),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.TopRight,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Anchor = Anchor.CentreLeft,
                    Position = new Vector2(2, 4),
                    Children = new Drawable[]
                    {
                        progress = new OsuSpriteText
                        {
                            Colour = colours.Purple1,
                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold, fixedWidth: true),
                            Spacing = new Vector2(-1, 0),
                        },
                        starRating = new OsuSpriteText
                        {
                            Colour = colours.Yellow,
                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold, fixedWidth: true),
                            Spacing = new Vector2(-1, 0),
                        },
                        // the same colour as kiai on the bottom timeline beside it.
                        kiai = new OsuSpriteText
                        {
                            Text = @"Kiai",
                            Colour = colours.Purple1,
                            // not fixed width: that is for digits that change, and spreads a word's letters apart.
                            Font = OsuFont.Torus.With(size: 14, weight: FontWeight.SemiBold),
                            Alpha = 0,
                        },
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (difficultyTracker == null)
                starRating.Text = @"-";
            else
                // formatted the way the rest of the game does, which floors rather than rounds: a 6.9999* beatmap
                // showing as 7.00* here but 6.99* everywhere else is worse than the lost hundredth.
                difficultyTracker.StarRating.BindValueChanged(stars => starRating.Text = stars.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{stars.NewValue.Value.FormatStarRating()}*"), true);
        }

        private double? lastBPM;
        private double? lastProgress;
        private bool? lastKiai;

        protected override void Update()
        {
            base.Update();

            double newBPM = editorBeatmap.ControlPointInfo.TimingPointAt(editorClock.CurrentTime).BPM;
            // floored to a tenth, like the star rating beside it is floored to a hundredth.
            double newProgress = (int)(editorClock.CurrentTime / editorClock.TrackLength * 1000) / 10.0;
            bool newKiai = editorBeatmap.ControlPointInfo.EffectPointAt(editorClock.CurrentTime).KiaiMode;

            if (lastBPM != newBPM)
            {
                lastBPM = newBPM;
                bpm.Text = @$"{newBPM:0} BPM";
            }

            if (lastProgress != newProgress)
            {
                lastProgress = newProgress;
                progress.Text = LocalisableString.Interpolate($@"{newProgress.ToLocalisableString(@"0.0")}%");
            }

            if (lastKiai != newKiai)
            {
                lastKiai = newKiai;
                kiai.Alpha = newKiai ? 1 : 0;
            }
        }

        private partial class TimestampControl : OsuClickableContainer
        {
            private Container hoverLayer = null!;
            private OsuSpriteText trackTimer = null!;
            private OsuTextBox inputTextBox = null!;

            [Resolved]
            private Editor? editor { get; set; }

            [Resolved]
            private EditorClock editorClock { get; set; } = null!;

            public TimestampControl()
                : base(HoverSampleSet.Button)
            {
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                AutoSizeAxes = Axes.Both;

                AddRangeInternal(new Drawable[]
                {
                    hoverLayer = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding
                        {
                            Top = 4,
                            Bottom = 1,
                            Horizontal = -2
                        },
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            CornerRadius = 5,
                            Masking = true,
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, },
                            }
                        },
                        Alpha = 0,
                    },
                    trackTimer = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Spacing = new Vector2(-2, 0),
                        Font = OsuFont.Torus.With(size: 32, fixedWidth: true, weight: FontWeight.Light),
                    },
                    inputTextBox = new TimestampTextBox
                    {
                        Position = new Vector2(-2, 4),
                        Width = 128,
                        Height = 26,
                        Alpha = 0,
                        CommitOnFocusLost = true,
                    },
                });

                Action = () =>
                {
                    trackTimer.Alpha = 0;
                    inputTextBox.Alpha = 1;
                    inputTextBox.Text = editorClock.CurrentTime.ToEditorFormattedString();
                    Schedule(() =>
                    {
                        GetContainingFocusManager()!.ChangeFocus(inputTextBox);
                        inputTextBox.SelectAll();
                    });
                };

                inputTextBox.Current.BindValueChanged(val => editor?.HandleTimestamp(val.NewValue.Trim()));

                inputTextBox.OnCommit += (_, __) =>
                {
                    trackTimer.Alpha = 1;
                    inputTextBox.Alpha = 0;
                };
            }

            private double? lastTime;
            private bool showingHoverLayer;

            protected override void Update()
            {
                base.Update();

                if (lastTime != editorClock.CurrentTime)
                {
                    lastTime = editorClock.CurrentTime;
                    trackTimer.Text = editorClock.CurrentTime.ToEditorFormattedString();
                }

                bool shouldShowHoverLayer = IsHovered && inputTextBox.Alpha == 0;

                if (shouldShowHoverLayer != showingHoverLayer)
                {
                    hoverLayer.FadeTo(shouldShowHoverLayer ? 0.2f : 0, 400, Easing.OutQuint);
                    showingHoverLayer = shouldShowHoverLayer;
                }
            }

            private partial class TimestampTextBox : OsuTextBox
            {
                public TimestampTextBox()
                {
                    TextContainer.Height = 0.8f;
                }
            }
        }
    }
}
