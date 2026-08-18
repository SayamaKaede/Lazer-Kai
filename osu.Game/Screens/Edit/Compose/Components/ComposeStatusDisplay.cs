// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Utils;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
using osu.Game.Screens.Play.HUD;
using osuTK;

namespace osu.Game.Screens.Edit.Compose.Components
{
    /// <summary>
    /// An always-visible readout of the values a mapper most often wants to check without hunting around the timeline:
    /// the difficulty's star rating, and the slider velocity / sample settings in effect at the current time.
    /// </summary>
    public partial class ComposeStatusDisplay : CompositeDrawable
    {
        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        // not always present: the composer can be constructed without a full editor around it (notably in test scenes).
        [Resolved(canBeNull: true)]
        private EditorStarRating? starRatingTracker { get; set; }

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private OsuSpriteText starRating = null!;
        private OsuSpriteText sliderVelocity = null!;
        private OsuSpriteText normalBank = null!;
        private OsuSpriteText additionBank = null!;
        private OsuSpriteText sampleVolume = null!;
        private Drawable modsRow = null!;
        private ModDisplay modDisplay = null!;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    createRow(@"SR", out starRating, colours.Yellow),
                    createRow(@"SV", out sliderVelocity, colours.Lime1),
                    createRow(@"Normal", out normalBank, colours.Pink1),
                    createRow(@"Addition", out additionBank, colours.Pink2),
                    createRow(@"Volume", out sampleVolume, colours.Pink1),
                    createModRow(),
                }
            };
        }

        /// <summary>
        /// The mods a test play would run with, which are also the mods the star rating above is calculated with.
        /// </summary>
        /// <remarks>
        /// The icons sit on top of one another until hovered, the same way they do everywhere else in the game, so a
        /// long list does not push the panel wide.
        /// </remarks>
        private Drawable createModRow()
        {
            return modsRow = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = @"Mods",
                        Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                        Alpha = 0.6f,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    modDisplay = new ModDisplay(showExtendedInformation: false)
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                        // the panel is a narrow column, and a mod icon is drawn at a size meant for the gameplay HUD.
                        Scale = new Vector2(0.4f),
                    }
                }
            };
        }

        private static Drawable createRow(LocalisableString label, out OsuSpriteText value, ColourInfo valueColour)
        {
            OsuSpriteText valueText;

            var row = new Container
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = label,
                        Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                        Alpha = 0.6f,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    valueText = new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 12, weight: FontWeight.Bold, fixedWidth: true),
                        Colour = valueColour,
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.CentreRight,
                    }
                }
            };

            value = valueText;
            return row;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (editor != null)
            {
                modDisplay.Current = editor.TestPlayMods.Mods;

                // with nothing selected the row would be a label with nothing beside it, so it steps out of the flow.
                editor.TestPlayMods.Mods.BindValueChanged(mods =>
                {
                    modsRow.Alpha = mods.NewValue.Any() ? 1 : 0;
                    modsRow.AlwaysPresent = false;
                }, true);
            }
            else
                modsRow.Alpha = 0;

            if (starRatingTracker == null)
                starRating.Text = @"-";
            else
                // formatted the way the rest of the game does, which floors rather than rounds.
                starRatingTracker.Current.BindValueChanged(stars => starRating.Text = stars.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{stars.NewValue.Value.FormatStarRating()}*"), true);
        }

        private double? lastTime;

        protected override void Update()
        {
            base.Update();

            if (lastTime == editorClock.CurrentTime)
                return;

            lastTime = editorClock.CurrentTime;
            updateValuesAtCurrentTime();
        }

        private void updateValuesAtCurrentTime()
        {
            HitObject? current = null;
            IHasSliderVelocity? currentWithVelocity = null;

            // hit objects are kept in start time order, so the last one which has started is the relevant one.
            foreach (var hitObject in editorBeatmap.HitObjects)
            {
                if (hitObject.StartTime > editorClock.CurrentTime)
                    break;

                current = hitObject;

                // slider velocity is tracked separately and deliberately not cleared by objects which don't carry one
                // (circles, spinners), so the last velocity in effect stays on screen until the next slider.
                if (hitObject is IHasSliderVelocity hasVelocity)
                    currentWithVelocity = hasVelocity;
            }

            sliderVelocity.Text = currentWithVelocity == null ? @"-" : @$"{currentWithVelocity.SliderVelocityMultiplier:0.00}x";

            if (current == null)
            {
                normalBank.Text = additionBank.Text = sampleVolume.Text = @"-";
                return;
            }

            normalBank.Text = $"{SamplePointPiece.GetBankValue(current.Samples) ?? @"-"}{SamplePointPiece.GetSuffix(current.Samples)}";
            additionBank.Text = SamplePointPiece.GetAdditionBankValue(current.Samples) ?? @"-";
            sampleVolume.Text = @$"{SamplePointPiece.GetVolumeValue(current.Samples)}%";
        }
    }
}
