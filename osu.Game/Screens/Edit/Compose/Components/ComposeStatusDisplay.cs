// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Extensions.ObjectExtensions;
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
        private EditorDifficultyTracker? difficultyTracker { get; set; }

        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        private OsuSpriteText starRating = null!;
        private OsuSpriteText maxPerformance = null!;
        private OsuSpriteText maxScore = null!;
        private OsuSpriteText maxLegacyScore = null!;
        private OsuSpriteText sliderVelocity = null!;
        private OsuSpriteText normalBank = null!;
        private OsuSpriteText additionBank = null!;
        private OsuSpriteText sampleVolume = null!;
        private OsuSpriteText kiai = null!;
        private OsuSpriteText objectCount = null!;
        private OsuSpriteText drainTime = null!;
        private OsuSpriteText approachRate = null!;
        private OsuSpriteText circleSize = null!;
        private OsuSpriteText overallDifficulty = null!;
        private OsuSpriteText drainRate = null!;
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
                    shownWhen(config, OsuSetting.EditorStatusShowMaxPerformance, createRow(@"MAX PP", out maxPerformance, colours.Blue1)),
                    shownWhen(config, OsuSetting.EditorStatusShowScore, createRow(@"SCORE", out maxScore, colours.Purple1)),
                    shownWhen(config, OsuSetting.EditorStatusShowLegacyScore, createRow(@"SCORE V1", out maxLegacyScore, colours.Purple1)),
                    shownWhen(config, OsuSetting.EditorStatusShowSliderVelocity, createRow(@"SV", out sliderVelocity, colours.Lime1)),
                    shownWhen(config, OsuSetting.EditorStatusShowNormalBank, createRow(@"Normal", out normalBank, colours.Pink1)),
                    shownWhen(config, OsuSetting.EditorStatusShowAdditionBank, createRow(@"Addition", out additionBank, colours.Pink2)),
                    shownWhen(config, OsuSetting.EditorStatusShowSampleVolume, createRow(@"Volume", out sampleVolume, colours.Pink1)),
                    shownWhen(config, OsuSetting.EditorStatusShowKiai, createRow(@"Kiai", out kiai, colours.Orange1)),
                    shownWhen(config, OsuSetting.EditorStatusShowObjectCount, createRow(@"Objects", out objectCount, colours.Green1)),
                    shownWhen(config, OsuSetting.EditorStatusShowDrainTime, createRow(@"Drain", out drainTime, colours.Green1)),
                    shownWhen(config, OsuSetting.EditorStatusShowDifficultySettings, createDifficultyRow(colours)),
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

        /// <summary>
        /// The four difficulty settings, side by side rather than a row each.
        /// </summary>
        /// <remarks>
        /// Four rows of one number would cost the panel more of the beatmap behind it than the numbers are worth, and
        /// these are read together anyway — a spread is checked by looking at all four at once. They wrap onto a
        /// second line rather than run off the edge, so a narrowed toolbox still shows all of them.
        /// </remarks>
        private Drawable createDifficultyRow(OsuColour colours)
        {
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Full,
                Spacing = new Vector2(9, 2),
                Children = new[]
                {
                    createAttribute(@"AR", out approachRate, colours.Green1),
                    createAttribute(@"CS", out circleSize, colours.Green1),
                    createAttribute(@"OD", out overallDifficulty, colours.Green1),
                    createAttribute(@"HP", out drainRate, colours.Green1),
                }
            };
        }

        /// <summary>
        /// One difficulty setting, as a label with its number beside it.
        /// </summary>
        private static Drawable createAttribute(LocalisableString label, out OsuSpriteText value, ColourInfo valueColour)
        {
            OsuSpriteText valueText;

            var attribute = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(3, 0),
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
                        Font = OsuFont.Default.With(size: 12, weight: FontWeight.Bold),
                        Colour = valueColour,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    }
                }
            };

            value = valueText;
            return attribute;
        }

        /// <summary>
        /// How long a beatmap drains for, counted the way a mapper is judged on it.
        /// </summary>
        /// <remarks>
        /// A break is written into the beatmap with a gap held before and after it — two hundred milliseconds after
        /// the object it follows, six hundred before the one it leads to — so the period stored in the file is
        /// shorter than the stretch where nothing is actually played. The game's own reckoning takes the stored
        /// period off, which leaves a beatmap draining for most of a second longer than the tools a mapper is
        /// checked against report. Here the whole gap comes off instead, so this agrees with them.
        ///
        /// The gap is measured from where the object before the break finishes: a slider running into one is played
        /// rather than rested through.
        /// </remarks>
        public static double DrainTimeFor(IBeatmap beatmap)
        {
            double length = beatmap.CalculatePlayableLength();

            foreach (var period in beatmap.Breaks)
            {
                double? before = null;
                double? after = null;

                foreach (var hitObject in beatmap.HitObjects)
                {
                    double end = hitObject.GetEndTime();

                    if (end <= period.StartTime)
                        before = before == null ? end : Math.Max(before.Value, end);

                    if (hitObject.StartTime >= period.EndTime && after == null)
                        after = hitObject.StartTime;
                }

                // a break with no beatmap on one side of it is not a gap between two objects, and there is nothing
                // to measure it against. What the file says is the best that can be said.
                length -= before != null && after != null ? after.Value - before.Value : period.Duration;
            }

            return Math.Max(length, 0);
        }

        /// <summary>
        /// A length as minutes, seconds and milliseconds.
        /// </summary>
        /// <remarks>
        /// Milliseconds are kept because they are the unit the editor works in: every timestamp a mapper reads,
        /// writes, or is handed in a modding post carries them, so a length rounded to the second is a length that
        /// has to be worked out again before it can be used.
        ///
        /// Minutes keep counting past sixty rather than rolling over into hours — a marathon is still talked about in
        /// minutes, and an hour-long beatmap reading as "1:01" would be read as a minute long.
        ///
        /// The fraction of a millisecond is rounded rather than dropped, so this agrees to the millisecond with the
        /// tools a mapper checks a beatmap against. A slider ending on a fraction is common enough that dropping it
        /// would leave the two disagreeing on most beatmaps.
        /// </remarks>
        public static string DrainTimeReadout(double milliseconds)
        {
            int total = (int)Math.Round(Math.Max(milliseconds, 0), MidpointRounding.AwayFromZero);

            return $@"{total / 60000}:{total / 1000 % 60:00}.{total % 1000:000}";
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

            // moving an object or editing the breaks changes how long the beatmap drains for without changing how
            // many objects there are, which is the only thing the frame-by-frame comparison would notice.
            editorBeatmap.HitObjectUpdated += onBeatmapLengthChanged;
            editorBeatmap.BeatmapReprocessed += invalidateDrainTime;

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

            if (difficultyTracker == null)
            {
                starRating.Text = @"-";
                maxPerformance.Text = @"-";
                maxScore.Text = @"-";
                maxLegacyScore.Text = @"-";
            }
            else
            {
                // formatted the way the rest of the game does, which floors rather than rounds.
                difficultyTracker.StarRating.BindValueChanged(stars => starRating.Text = stars.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{stars.NewValue.Value.FormatStarRating()}*"), true);

                // whole numbers: the hundredths of a pp move with every recalculation of the difficulty algorithm and
                // say nothing about the beatmap.
                difficultyTracker.MaxPerformance.BindValueChanged(performance => maxPerformance.Text = performance.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{performance.NewValue.Value:N0}pp"), true);

                difficultyTracker.MaxScore.BindValueChanged(score => maxScore.Text = score.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{score.NewValue.Value:N0}"), true);

                difficultyTracker.MaxLegacyScore.BindValueChanged(score => maxLegacyScore.Text = score.NewValue == null
                    ? @"-"
                    : LocalisableString.Interpolate($@"{score.NewValue.Value:N0}"), true);
            }
        }

        private double? lastTime;

        protected override void Update()
        {
            base.Update();

            updateValuesForWholeBeatmap();

            if (lastTime == editorClock.CurrentTime)
                return;

            lastTime = editorClock.CurrentTime;
            updateValuesAtCurrentTime();
        }

        private int? lastObjectCount;
        private double? lastDrainTime;
        private (float ar, float cs, float od, float hp)? lastDifficulty;

        /// <summary>
        /// Updates the rows describing the beatmap rather than the place in it.
        /// </summary>
        /// <remarks>
        /// Worked out here rather than from the editor's change events because the difficulty settings are edited on
        /// another screen entirely and come back without one. Each value is compared before its text is touched, so
        /// an untouched beatmap costs a count and four floats a frame and nothing is laid out again.
        /// </remarks>
        private void updateValuesForWholeBeatmap()
        {
            int count = editorBeatmap.HitObjects.Count;

            if (count != lastObjectCount)
            {
                lastObjectCount = count;
                objectCount.Text = count.ToString(@"N0");

                // the drain depends on where the objects start and end, so it is only worth working out again when
                // the beatmap has actually moved. it walks every object, which the rest of this method does not.
                lastDrainTime = null;
            }

            if (lastDrainTime == null)
            {
                double drain = DrainTimeFor(editorBeatmap);

                lastDrainTime = drain;
                drainTime.Text = DrainTimeReadout(drain);
            }

            var difficulty = editorBeatmap.Difficulty;
            var current = (difficulty.ApproachRate, difficulty.CircleSize, difficulty.OverallDifficulty, difficulty.DrainRate);

            if (lastDifficulty != current)
            {
                lastDifficulty = current;

                approachRate.Text = attributeReadout(difficulty.ApproachRate);
                circleSize.Text = attributeReadout(difficulty.CircleSize);
                overallDifficulty.Text = attributeReadout(difficulty.OverallDifficulty);
                drainRate.Text = attributeReadout(difficulty.DrainRate);
            }
        }

        /// <summary>
        /// A difficulty setting, written the way the setup screen and the beatmap file write it.
        /// </summary>
        /// <remarks>
        /// A tenth is as fine as these go, and a whole number is left whole rather than padded to "9.0" — that is how
        /// mappers say them out loud, and the column has no room to spare.
        /// </remarks>
        private static string attributeReadout(float value) => value.ToString(@"0.##");

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

            normalBank.Text = bankReadout(SamplePointPiece.GetBankValue(samples), samples.Where(s => s.Name == HitSampleInfo.HIT_NORMAL));

            // an addition always plays from the same set as the normal beside it, so saying which one twice only
            // takes room the panel does not have.
            additionBank.Text = SamplePointPiece.AbbreviateBank(SamplePointPiece.GetAdditionBankValue(samples)) ?? @"-";

            sampleVolume.Text = @$"{SamplePointPiece.GetVolumeValue(samples)}%";
        }

        /// <summary>
        /// A bank the way the timeline writes it a few centimetres away: one letter, followed by which sample set it
        /// plays from when that is not the skin's own.
        /// </summary>
        /// <remarks>
        /// Set 1 is the beatmap's own files with no number on them, which is a different thing from the skin's and
        /// worth saying out loud. Only set 0 — the skin's — reads as a bank on its own.
        /// </remarks>
        private static string bankReadout(string? bank, IEnumerable<HitSampleInfo> samples)
        {
            if (bank == null)
                return @"-";

            string letter = SamplePointPiece.AbbreviateBank(bank) ?? bank;
            int? set = sampleSetOf(samples);

            return set is null or 0 ? letter : $@"{letter}:{set}";
        }

        /// <summary>
        /// Which sample set every one of <paramref name="samples"/> plays from, or null when they disagree.
        /// </summary>
        /// <remarks>
        /// Worked out the same way the sample popover works out which of its buttons to light up: no suffix means the
        /// skin's or the beatmap's depending on which files are being used, and a suffix is the number itself.
        /// </remarks>
        private static int? sampleSetOf(IEnumerable<HitSampleInfo> samples)
        {
            int? common = null;

            foreach (var sample in samples)
            {
                int set = sample.Suffix == null
                    ? sample.UseBeatmapSamples ? 1 : 0
                    : int.TryParse(sample.Suffix, out int suffix) ? suffix : 0;

                if (common != null && common != set)
                    return null;

                common = set;
            }

            return common;
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

        private void onBeatmapLengthChanged(HitObject hitObject) => invalidateDrainTime();

        private void invalidateDrainTime() => lastDrainTime = null;

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (editorBeatmap.IsNotNull())
            {
                editorBeatmap.HitObjectUpdated -= onBeatmapLengthChanged;
                editorBeatmap.BeatmapReprocessed -= invalidateDrainTime;
            }
        }
    }
}
