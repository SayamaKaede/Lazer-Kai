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
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Compose.Components.Timeline;
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

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private OsuSpriteText starRating = null!;
        private OsuSpriteText sliderVelocity = null!;
        private OsuSpriteText sampleBank = null!;
        private OsuSpriteText sampleVolume = null!;

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
                    createRow(@"Bank", out sampleBank, colours.Pink1),
                    createRow(@"Volume", out sampleVolume, colours.Pink1),
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
                editor.Saved += updateStarRating;

            updateStarRating();
        }

        /// <summary>
        /// Star rating is only recalculated as part of saving, so this is refreshed on save rather than continuously.
        /// </summary>
        private void updateStarRating() => Schedule(() =>
        {
            // the editor operates on a detached copy of `BeatmapInfo`, so the recalculated value has to be read back from realm.
            double stars = realm.Run(r => r.Find<BeatmapInfo>(editorBeatmap.BeatmapInfo.ID)?.StarRating)
                           ?? editorBeatmap.BeatmapInfo.StarRating;

            starRating.Text = @$"{stars:0.00}*";
        });

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
            var current = objectAtCurrentTime();

            if (current == null)
            {
                sliderVelocity.Text = sampleBank.Text = sampleVolume.Text = @"-";
                return;
            }

            sliderVelocity.Text = current is IHasSliderVelocity sv ? @$"{sv.SliderVelocityMultiplier:0.00}x" : @"-";

            string bank = SamplePointPiece.GetBankValue(current.Samples) ?? @"-";
            sampleBank.Text = $"{bank}{SamplePointPiece.GetSuffix(current.Samples)}";
            sampleVolume.Text = @$"{SamplePointPiece.GetVolumeValue(current.Samples)}%";
        }

        /// <summary>
        /// The most recent object at or before the current time, i.e. the one whose settings are "in effect" right now.
        /// </summary>
        private HitObject? objectAtCurrentTime()
        {
            HitObject? result = null;

            // hit objects are kept in start time order, so the last one which has started is the relevant one.
            foreach (var hitObject in editorBeatmap.HitObjects)
            {
                if (hitObject.StartTime > editorClock.CurrentTime)
                    break;

                result = hitObject;
            }

            return result;
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editor != null)
                editor.Saved -= updateStarRating;
        }
    }
}
