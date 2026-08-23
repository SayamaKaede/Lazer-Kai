// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Utils;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Objects;
using osu.Game.Audio;
using osu.Framework.Utils;
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
        private OsuSpriteText kiai = null!;
        private Drawable modsRow = null!;
        private ModDisplay modDisplay = null!;

        /// <summary>
        /// Held so the bindings that show and hide the rows are not collected out from under them.
        /// </summary>
        private readonly List<Bindable<bool>> rowVisibilities = new List<Bindable<bool>>();

        private readonly Bindable<bool> showModsRow = new Bindable<bool>(true);

        private bool anyTestPlayMods;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OsuConfigManager config)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            // the mods row already steps out of the flow when no mods are selected, so the setting joins that rather
            // than writing over it.
            config.BindWith(OsuSetting.EditorStatusShowMods, showModsRow);

            InternalChild = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 2),
                Children = new Drawable[]
                {
                    shownWhen(config, OsuSetting.EditorStatusShowStarRating, createRow(@"SR", out starRating, colours.Yellow)),
                    shownWhen(config, OsuSetting.EditorStatusShowSliderVelocity, createRow(@"SV", out sliderVelocity, colours.Lime1)),
                    shownWhen(config, OsuSetting.EditorStatusShowNormalBank, createRow(@"Normal", out normalBank, colours.Pink1)),
                    shownWhen(config, OsuSetting.EditorStatusShowAdditionBank, createRow(@"Addition", out additionBank, colours.Pink2)),
                    shownWhen(config, OsuSetting.EditorStatusShowSampleVolume, createRow(@"Volume", out sampleVolume, colours.Pink1)),
                    shownWhen(config, OsuSetting.EditorStatusShowKiai, createRow(@"Kiai", out kiai, colours.Orange1)),
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

        /// <summary>
        /// Hides a row the user has turned off. The panel is a narrow column and every row is one less line of the
        /// beatmap visible behind it, so which of them are worth the room is theirs to say.
        /// </summary>
        private Drawable shownWhen(OsuConfigManager config, OsuSetting setting, Drawable row)
        {
            var shown = config.GetBindable<bool>(setting);

            // alpha rather than removal: the flow skips what is not present, so the row's space goes with it.
            shown.BindValueChanged(v => row.Alpha = v.NewValue ? 1 : 0, true);
            rowVisibilities.Add(shown);

            return row;
        }

        private void updateModsRow()
        {
            modsRow.Alpha = showModsRow.Value && anyTestPlayMods ? 1 : 0;
            modsRow.AlwaysPresent = false;
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
                    anyTestPlayMods = mods.NewValue.Any();
                    updateModsRow();
                }, true);
            }

            showModsRow.BindValueChanged(_ => updateModsRow(), true);

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

            // read from the effect point rather than from an object, so it is right in the gaps between objects too.
            kiai.Text = editorBeatmap.ControlPointInfo.EffectPointAt(editorClock.CurrentTime).KiaiMode ? @"ON" : @"-";

            if (current == null)
            {
                normalBank.Text = additionBank.Text = sampleVolume.Text = @"-";
                return;
            }

            var samples = samplesAt(current, editorClock.CurrentTime);

            normalBank.Text = $"{SamplePointPiece.GetBankValue(samples) ?? @"-"}{SamplePointPiece.GetSuffix(samples)}";
            additionBank.Text = SamplePointPiece.GetAdditionBankValue(samples) ?? @"-";
            sampleVolume.Text = @$"{SamplePointPiece.GetVolumeValue(samples)}%";
        }

        /// <summary>
        /// The samples in effect at <paramref name="time"/>, which for anything with repeats is the node most recently
        /// reached rather than the object's own set.
        /// </summary>
        /// <remarks>
        /// A slider's head, repeats and tail each carry their own bank and volume, and the object's own samples are
        /// only the fallback for a node that has none. Reading the object alone showed the head's hitsounds while the
        /// playhead sat on the tail, which is the one place a mapper looks to check what they just set.
        ///
        /// Past the end of an object the last node stays on screen, the same way the rest of this panel keeps showing
        /// the last thing in effect rather than blanking between objects.
        /// </remarks>
        private static IList<HitSampleInfo> samplesAt(HitObject hitObject, double time)
        {
            if (hitObject is not IHasRepeats repeats)
                return hitObject.Samples;

            double spanDuration = repeats.Duration / repeats.SpanCount();

            if (spanDuration <= 0)
                return hitObject.Samples;

            // sitting exactly on a node counts as having reached it, and seeking lands exactly on them.
            int node = (int)Math.Floor((time - hitObject.StartTime) / spanDuration + Precision.DOUBLE_EPSILON);
            node = Math.Clamp(node, 0, repeats.SpanCount());

            return node < repeats.NodeSamples.Count ? repeats.NodeSamples[node] : hitObject.Samples;
        }
    }
}
