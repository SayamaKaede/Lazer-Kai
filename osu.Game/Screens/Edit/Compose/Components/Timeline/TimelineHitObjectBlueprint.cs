// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Effects;
using osu.Framework.Graphics.Primitives;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Rulesets.Edit;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    public partial class TimelineHitObjectBlueprint : SelectionBlueprint<HitObject>
    {
        private const float circle_size = 32;

        /// <summary>
        /// The height to draw at once the velocity and volume rows are hidden, which is theirs to give up.
        /// </summary>
        private const float expanded_circle_size = 52;

        /// <summary>
        /// The two heights above, for the stable layout.
        /// </summary>
        private const float stable_circle_size = 45;

        private const float stable_expanded_circle_size = 64;

        /// <summary>
        /// How tall one velocity or volume row is drawn.
        /// </summary>
        public const float POINT_ROW_HEIGHT = 15;

        /// <summary>
        /// What the objects are drawn at with only the volume row hidden.
        /// </summary>
        /// <remarks>
        /// Exactly what the row gives up. Settled by eye against the real timeline on 2026-09-08, and it landed on
        /// the arithmetic rather than beside it.
        /// </remarks>
        private const float volume_hidden_circle_size = circle_size + POINT_ROW_HEIGHT;

        private const float stable_volume_hidden_circle_size = stable_circle_size + POINT_ROW_HEIGHT;


        /// <summary>
        /// How solid the combo colour is drawn while the skin's pieces are being used.
        /// </summary>
        /// <remarks>
        /// Thinner in the Lazer-Kai layout, where the ticks reach in from both edges and there is more behind the
        /// objects worth seeing. Settled by eye against a real beatmap rather than worked out.
        /// </remarks>
        private static float skinOpacityFor(EditorTimelineStyle style) => style == EditorTimelineStyle.LazerKai ? 0.3f : 0.5f;

        /// <summary>
        /// What stable draws a spinner in, which is nobody's combo colour.
        /// </summary>
        private static readonly Color4 spinner_grey = OsuColour.Gray(0.6f);

        private readonly Container skinPieces;

        /// <summary>The end the drag handle sits on, which is what says it is being held.</summary>
        private TimelineSkinCircle? skinTail;

        private Container? repeatsContainer;

        public Action<DragEvent?>? OnDragHandled = null!;

        [UsedImplicitly]
        private readonly Bindable<double> startTime;

        private Bindable<int>? indexInCurrentComboBindable;

        private Bindable<int>? comboIndexBindable;
        private Bindable<int>? comboIndexWithOffsetsBindable;

        private Bindable<Color4> displayColourBindable = null!;

        private readonly ExtendableCircle circle;
        private readonly Border border;

        private readonly Container colouredComponents;
        private readonly Container sampleComponents;

        private readonly LineVolumeTicks? lineVolumeTicks;
        private readonly OsuSpriteText comboIndexText;
        private readonly SamplePointPiece samplePointPiece;
        private readonly DifficultyPointPiece? difficultyPointPiece;
        private readonly DragArea? dragArea;

        private readonly UnsnapMarker startUnsnap;
        private readonly UnsnapMarker? endUnsnap;
        private readonly Container<UnsnapMarker>? repeatUnsnaps;

        private readonly Bindable<bool> hideTimelinePoints = new Bindable<bool>();

        private readonly Bindable<bool> hideTimelineVolume = new Bindable<bool>();

        [Resolved(CanBeNull = true)]
        private EditorReferenceDifficulty? referenceDifficulty { get; set; }

        [Resolved]
        private ISkinSource skin { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private SamplePointSelection? samplePoints { get; set; }

        [Resolved(CanBeNull = true)]
        private Editor? editor { get; set; }

        private IBindable<EditorScreenMode>? editorMode;

        /// <summary>
        /// The sound point under a position on screen, as the hitsound screen selects one by clicking: a node where its
        /// circle is — the head, a reverse, the tail — and otherwise the object's own sounds, which for a slider is its
        /// body (2026-10-08, as un!stable's timeline does).
        /// </summary>
        public int SamplePointAt(Vector2 screenSpacePosition)
        {
            if (Item is not IHasRepeats repeats || repeats.NodeSamples.Count == 0)
                return SamplePointSelection.Body;

            float x = ToLocalSpace(screenSpacePosition).X;
            int spans = repeats.SpanCount();

            // the nearest node, so that the ends of a slider too short for its circles to keep apart still go to one.
            int nearest = Enumerable.Range(0, repeats.NodeSamples.Count).MinBy(node => Math.Abs(x - DrawWidth * node / spans));

            return Math.Abs(x - DrawWidth * nearest / spans) <= DrawHeight / 2 ? nearest : SamplePointSelection.Body;
        }

        private bool onHitsoundScreen => editorMode?.Value == EditorScreenMode.Hitsounds && samplePoints != null;

        private bool allPointsSelected => SamplePointSelection.PointsOf(Item).All(p => samplePoints!.Contains(p.hitObject, p.point));

        /// <summary>
        /// Whether the object is outlined as a whole. On the hitsound screen, where some of an object can be selected,
        /// a slider is so only for its body or all of it, the ends selected of one being ringed instead, and a spinner,
        /// which sounds only as it ends, never: its end is ringed (2026-10-08).
        /// </summary>
        public bool OutlinedWhole
        {
            get
            {
                if (!IsSelected)
                    return false;

                if (!onHitsoundScreen)
                    return true;

                if (isSpinner)
                    return false;

                if (Item is IHasRepeats repeats && repeats.NodeSamples.Count > 0)
                    return allPointsSelected || samplePoints!.Contains(Item, SamplePointSelection.Body);

                return true;
            }
        }

        /// <summary>
        /// Where along the object the points selected of it are ringed on the hitsound screen, as fractions of its length.
        /// </summary>
        public IEnumerable<float> OutlinedPoints => pointOutlines.Select(o => o.Fraction);

        private readonly Container<PointOutline> pointOutlines;

        private EditorSelectionAppearance? pointAppearance;

        private IEnumerable<float> pointsToOutline()
        {
            if (!IsSelected || !onHitsoundScreen)
                yield break;

            if (isSpinner)
            {
                if (samplePoints!.Contains(Item, SamplePointSelection.Body))
                    yield return 1;

                yield break;
            }

            if (Item is not IHasRepeats repeats || repeats.NodeSamples.Count == 0 || allPointsSelected)
                yield break;

            int spans = repeats.SpanCount();

            for (int node = 0; node < repeats.NodeSamples.Count; node++)
            {
                if (samplePoints!.Contains(Item, node))
                    yield return (float)node / spans;
            }
        }

        private void updatePointOutlines()
        {
            float[] fractions = pointsToOutline().ToArray();

            if (!pointOutlines.Select(o => o.Fraction).SequenceEqual(fractions))
            {
                pointOutlines.Clear();

                foreach (float fraction in fractions)
                    pointOutlines.Add(new PointOutline(fraction));
            }

            if (pointAppearance == null)
                return;

            foreach (var outline in pointOutlines)
            {
                outline.BorderColour = pointAppearance.GetHitsoundOutlineColour(colours.Yellow);
                outline.BorderThickness = pointAppearance.GetHitsoundOutlineThickness(4);
                outline.Alpha = pointAppearance.HitsoundOutlineOpacity;
            }
        }

        private void samplePointsChanged() => Scheduler.AddOnce(updateColour);

        /// <summary>
        /// A ring around one of an object's circles: the head, a reverse or the tail.
        /// </summary>
        private partial class PointOutline : CircularContainer
        {
            public readonly float Fraction;

            public PointOutline(float fraction)
            {
                Fraction = fraction;

                RelativePositionAxes = Axes.X;
                X = fraction;
                RelativeSizeAxes = Axes.Y;
                Height = 1;
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.Centre;
                Masking = true;

                Child = new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    AlwaysPresent = true,
                };
            }

            protected override void Update()
            {
                base.Update();

                // as round as the circles it goes around, however tall the timeline makes them.
                Width = DrawHeight;
            }
        }

        public TimelineHitObjectBlueprint(HitObject item)
            : base(item)
        {
            Anchor = Anchor.CentreLeft;
            Origin = Anchor.CentreLeft;

            startTime = item.StartTimeBindable.GetBoundCopy();
            startTime.BindValueChanged(time => X = (float)time.NewValue, true);

            RelativePositionAxes = Axes.X;

            RelativeSizeAxes = Axes.X;
            Height = circle_size;

            AddRangeInternal(new Drawable[]
            {
                circle = new ExtendableCircle
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                // over the bar so the ends read as ends, under the selection outline so a selected object still
                // says so.
                skinPieces = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                border = new Border
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                pointOutlines = new Container<PointOutline>
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                },
                colouredComponents = new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        comboIndexText = new OsuSpriteText
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.Centre,
                            Y = -1,
                            Font = OsuFont.Default.With(size: circle_size * 0.5f, weight: FontWeight.Regular),
                        },
                    }
                },
                samplePointPiece = new SamplePointPiece(Item)
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.TopCentre,
                    RelativePositionAxes = Axes.X,
                    AlternativeColor = Item is IHasRepeats
                },
                sampleComponents = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                },
            });

            if (item is IHasDuration)
            {
                // the volume part way through, which is only the green lines' to say.
                AddInternal(lineVolumeTicks = new LineVolumeTicks(item, () => IsSelected));

                colouredComponents.Add(dragArea = new DragArea(item)
                {
                    OnDragHandled = e => OnDragHandled?.Invoke(e),
                    GrabChanged = grabbed =>
                    {
                        if (skinTail != null)
                            skinTail.Grabbed = grabbed;
                    },
                });
            }

            if (item is IHasSliderVelocity)
            {
                AddInternal(difficultyPointPiece = new DifficultyPointPiece(Item)
                {
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.BottomCentre
                });
            }

            // over the object rather than on it: the timeline is read at a glance while placing, and a mark
            // inside the circle would be one more thing on top of the combo number and the sample rows.
            AddInternal(startUnsnap = new UnsnapMarker
            {
                Anchor = Anchor.TopLeft,
                Origin = Anchor.BottomLeft,
            });

            if (item is IHasDuration)
            {
                AddInternal(endUnsnap = new UnsnapMarker
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.BottomRight,
                });
            }

            // a reverse is a moment the player has to hit, and it can be off the beat while both ends are on it.
            // The verifier has always reported these; the timeline was only marking where the object began and
            // ended, so a slider could be marked nowhere and still be called a problem.
            if (item is IHasRepeats)
            {
                AddInternal(repeatUnsnaps = new Container<UnsnapMarker>
                {
                    RelativeSizeAxes = Axes.Both,
                });
            }
        }

        private OsuColour colours = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, OsuColour colours)
        {
            this.colours = colours;

            // the hitsound screen's own outline, which the rings around the points selected are drawn with as well.
            pointAppearance = new EditorSelectionAppearance(config);
            pointAppearance.Changed += updatePointOutlines;

            config.BindWith(OsuSetting.EditorHideTimelinePoints, hideTimelinePoints);
            config.BindWith(OsuSetting.EditorHideTimelineVolume, hideTimelineVolume);
            config.BindWith(OsuSetting.EditorTimelineStyle, timelineStyle);
            config.BindWith(OsuSetting.EditorTimelineUseSkin, useSkin);

            hideTimelinePoints.BindValueChanged(_ => updateSizing());
            hideTimelineVolume.BindValueChanged(_ => updateSizing());

            timelineStyle.BindValueChanged(_ =>
            {
                updateSizing();

                // stable leaves a spinner out of the combo colours, and which of the two this timeline is follows
                // the style rather than a setting of its own.
                updateColour();
            });

            referenceDifficulty?.Beatmap.BindValueChanged(_ => updateSizing());

            useSkin.BindValueChanged(_ => updateSkinPieces());

            // a spinner sounds once the spinning is over, so its sound is edited at its end.
            if (SamplePointPiece.SoundsAtItsEnd(Item, editorBeatmap))
            {
                samplePointPiece.AtTheEnd = true;
                samplePointPiece.X = 1;
            }

            updateSkinPieces();
            updateSizing();
        }

        private readonly Bindable<EditorTimelineStyle> timelineStyle = new Bindable<EditorTimelineStyle>();

        private readonly BindableBool useSkin = new BindableBool();


        /// <summary>
        /// How tall an object is drawn, given what else the timeline has to fit around it.
        /// </summary>
        /// <remarks>
        /// Stable's are bigger than lazer's at both sizes: resting the objects on the bottom edge puts the two point
        /// rows over them rather than one on either side, which leaves more of the height for the objects themselves.
        /// The four numbers were settled by eye against a real beatmap rather than worked out.
        /// </remarks>
        /// <param name="pointRowsHidden">Whether both rows have given up their space.</param>
        /// <param name="volumeRowHidden">Whether the volume row alone has.</param>
        /// <param name="style">Which layout the timeline is being read in.</param>
        public static float CircleSizeFor(bool pointRowsHidden, bool volumeRowHidden, EditorTimelineStyle style)
        {
            if (style.RestsOnTheBottomEdge())
                return pointRowsHidden ? stable_expanded_circle_size
                    : volumeRowHidden ? stable_volume_hidden_circle_size
                    : stable_circle_size;

            return pointRowsHidden ? expanded_circle_size
                : volumeRowHidden ? volume_hidden_circle_size
                : circle_size;
        }

        /// <summary>
        /// Whether the objects are resting on the timeline's bottom edge, with everything else above them.
        /// </summary>
        private bool stableLayout => timelineStyle.Value.RestsOnTheBottomEdge() && referenceDifficulty?.Beatmap.Value == null;

        /// <summary>
        /// Puts the velocity and volume rows where the current layout wants them: on either side of the object in the
        /// lazer one, both stacked above it in stable's, where there is nothing below the object to put them in.
        /// </summary>
        private void updatePointRowPlacement()
        {
            bool above = stableLayout;

            samplePointPiece.Anchor = above ? Anchor.TopLeft : Anchor.BottomLeft;
            samplePointPiece.Origin = above ? Anchor.BottomCentre : Anchor.TopCentre;

            if (lineVolumeTicks != null)
                lineVolumeTicks.Above = above;

            foreach (var node in sampleComponents.Children.OfType<NodeSamplePointPiece>())
            {
                node.Anchor = above ? Anchor.TopLeft : Anchor.BottomLeft;
                node.Origin = above ? Anchor.BottomCentre : Anchor.TopCentre;
            }

            // velocity goes above volume rather than the other way round: volume belongs to the object itself, so it
            // reads better sitting right on it. With volume away, velocity takes the row it left rather than leaving
            // a gap where it was -- the object has already grown into that space, and the two together would reach
            // past the top of the timeline.
            if (difficultyPointPiece != null)
                difficultyPointPiece.Y = above && !volumeRowHidden ? -POINT_ROW_HEIGHT : 0;
        }

        /// <summary>
        /// The height objects are currently drawn at, which grows into the space the velocity and volume rows leave behind.
        /// </summary>
        /// <summary>
        /// Whether the velocity and volume rows are giving up their space, either because the user asked or because
        /// another difficulty is sharing the timeline and there is not room for both.
        /// </summary>
        private bool pointRowsHidden => hideTimelinePoints.Value || referenceDifficulty?.Beatmap.Value != null;

        /// <summary>
        /// Whether the volume row alone is giving up its space. Never while both rows are already away.
        /// </summary>
        private bool volumeRowHidden => hideTimelineVolume.Value && !pointRowsHidden;

        // half of what the rows give up goes to the difficulty being referenced, so the objects stay their stock size
        // rather than growing into all of it.
        private float currentCircleSize => referenceDifficulty?.Beatmap.Value != null ? circle_size
            : CircleSizeFor(hideTimelinePoints.Value, volumeRowHidden, timelineStyle.Value);

        private void updateSizing()
        {
            updatePointRowPlacement();

            float size = currentCircleSize;

            Height = size;
            circle.CircleSize = size;
            border.CircleSize = size;
            comboIndexText.Font = OsuFont.Default.With(size: size * 0.5f, weight: FontWeight.Regular);

            if (repeatsContainer != null)
            {
                foreach (var tick in repeatsContainer.Children.OfType<Tick>())
                    tick.CircleSize = size;

                // the skin draws its own arrows where these dots would be.
                repeatsContainer.Alpha = skinned ? 0 : 1;
            }

            foreach (var piece in skinPieces.Children.OfType<TimelineSkinCircle>())
                piece.CircleSize = size;

            if (dragArea != null)
            {
                // the handle is stable's whole slider end rather than a grip drawn on it: the ring is what there is
                // to take hold of, so it is the whole of it that answers, and nothing is drawn over the skin.
                dragArea.Invisible = skinned;
                dragArea.CircleSize = size;
            }

            // hidden rather than removed, so the pieces come back the moment the setting does.
            float pointAlpha = pointRowsHidden ? 0 : 1;

            // the volume row can be given up on its own. The velocity row sits above the object rather than on it,
            // so it rides up with the object as it grows and needs nothing doing to it.
            float volumeAlpha = pointRowsHidden || volumeRowHidden ? 0 : 1;

            samplePointPiece.Alpha = volumeAlpha;
            sampleComponents.Alpha = volumeAlpha;

            if (lineVolumeTicks != null)
                lineVolumeTicks.Alpha = volumeAlpha;

            if (difficultyPointPiece != null)
                difficultyPointPiece.Alpha = pointAlpha;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            if (samplePoints != null)
                samplePoints.Changed += samplePointsChanged;

            if (editor != null)
            {
                editorMode = editor.Mode.GetBoundCopy();
                editorMode.BindValueChanged(_ => samplePointsChanged());
            }

            switch (Item)
            {
                case IHasDisplayColour displayColour:
                    displayColourBindable = displayColour.DisplayColour.GetBoundCopy();
                    displayColourBindable.BindValueChanged(_ => updateColour(), true);
                    break;

                case IHasComboInformation comboInfo:
                    indexInCurrentComboBindable = comboInfo.IndexInCurrentComboBindable.GetBoundCopy();
                    indexInCurrentComboBindable.BindValueChanged(_ =>
                    {
                        comboIndexText.Text = (indexInCurrentComboBindable.Value + 1).ToString();

                        // the skin's number is a sprite per digit, so it is rebuilt rather than reworded.
                        updateSkinPieces();
                    }, true);

                    comboIndexBindable = comboInfo.ComboIndexBindable.GetBoundCopy();
                    comboIndexWithOffsetsBindable = comboInfo.ComboIndexWithOffsetsBindable.GetBoundCopy();

                    comboIndexBindable.BindValueChanged(_ => updateColour());
                    comboIndexWithOffsetsBindable.BindValueChanged(_ => updateColour(), true);
                    break;
            }

            // a different skin is different pieces as well as different colours, and may be one that has none.
            skin.SourceChanged += updateSkinPieces;
        }

        protected override void OnSelected()
        {
            // base logic hides selected blueprints when not selected, but timeline doesn't do that.
            updateColour();
        }

        protected override void OnDeselected()
        {
            // base logic hides selected blueprints when not selected, but timeline doesn't do that.
            updateColour();
        }

        /// <summary>
        /// Whether the skin's own circles, ends and reverse arrows are what this object is drawn with.
        /// </summary>
        /// <remarks>
        /// The pieces are osu!'s, and every ruleset gets them (asked for on 2026-09-11): a taiko hit or a mania note
        /// drawn as a circle still reads as a note at its moment, and before this, mapping in another mode meant the
        /// timeline switched looks along with it. A skin that does not draw in the stable format has none of them,
        /// and the timeline stays as it was rather than losing its objects.
        /// </remarks>
        private bool skinned => useSkin.Value && TimelineSkinPieces.Available(skin);

        private bool osuRuleset => editorBeatmap.BeatmapInfo.Ruleset.OnlineID == 0;

        /// <summary>
        /// A spinner, which is the one osu! object with a length and no path.
        /// </summary>
        private bool isSpinner => osuRuleset && Item is IHasDuration && Item is not IHasPath;

        /// <summary>
        /// Builds the skin's pieces for this object, or takes them away again.
        /// </summary>
        private void updateSkinPieces()
        {
            skinPieces.Clear();
            skinTail = null;

            // the plain number belongs to the plain bar; the skin draws its own on the circle.
            comboIndexText.Alpha = skinned ? 0 : 1;

            if (skinned)
            {
                skinPieces.Add(new TimelineSkinCircle((Item as IHasComboInformation)?.IndexInCurrentCombo + 1)
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.Centre,
                });

                if (Item is IHasDuration)
                {
                    skinPieces.Add(skinTail = new TimelineSkinCircle(null)
                    {
                        Anchor = Anchor.CentreRight,
                        Origin = Anchor.Centre,
                    });
                }

                if (Item is IHasRepeats repeats)
                {
                    for (int i = 0; i < repeats.RepeatCount; i++)
                    {
                        // a circle of its own rather than a bare arrow: a reverse is a moment the player hits, and
                        // stable's timeline draws it as one of these with the arrow where the number would be.
                        skinPieces.Add(new TimelineSkinCircle(null, reverse: true)
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.Centre,
                            RelativePositionAxes = Axes.X,
                            X = (float)(i + 1) / (repeats.RepeatCount + 1),
                        });
                    }
                }
            }

            updateSizing();
            updateColour();
        }

        private void updateColour()
        {
            Color4 colour;

            switch (Item)
            {
                case IHasDisplayColour displayColour:
                    colour = displayColour.DisplayColour.Value;
                    break;

                // stable's timeline leaves a spinner out of the combo colours, and so does this fork's layout: only
                // upstream's own layout colours one. It reads as what it is either way -- there is only ever one of
                // them at a time -- and the colour is better spent on the notes around it.
                case IHasComboInformation when isSpinner && timelineStyle.Value != EditorTimelineStyle.Lazer:
                    colour = spinner_grey;
                    break;

                case IHasComboInformation combo:
                    colour = combo.GetComboColour(skin);
                    break;

                default:
                    colour = colourProvider.Highlight1;
                    break;
            }

            if (OutlinedWhole)
                border.Show();
            else
                border.Hide();

            updatePointOutlines();

            // stable's timeline lets the waveform through the objects, which is how a moment is read against the
            // sound it belongs to. Only the colour is thinned: the ring, the number and the arrow stay solid, or
            // there is nothing left to read the position off.
            if (skinned)
                colour = colour.Opacity(skinOpacityFor(timelineStyle.Value));

            if (Item is IHasDuration duration && duration.Duration > 0)
                circle.Colour = ColourInfo.GradientHorizontal(colour, colour.Lighten(0.4f));
            else
                circle.Colour = colour;

            var averageColour = Interpolation.ValueAt(0.5, circle.Colour.TopLeft, circle.Colour.TopRight, 0, 1);
            colouredComponents.Colour = OsuColour.ForegroundTextColourFor(averageColour);

            // each end takes the colour of the bar it sits on, so the two do not read as different objects.
            foreach (var piece in skinPieces.Children.OfType<TimelineSkinCircle>())
                piece.CircleColour = piece.Anchor == Anchor.CentreRight ? circle.Colour.TopRight : circle.Colour.TopLeft;
        }

        protected override void Update()
        {
            base.Update();

            // no bindable so we perform this every update
            float duration = (float)(Item.GetEndTime() - Item.StartTime);

            if (Width != duration)
            {
                Width = duration;

                // kind of haphazard but yeah, no bindables.
                if (Item is IHasRepeats repeats)
                    updateRepeats(repeats);
            }

            // read every update for the same reason the width is: an object's times move with the timing behind
            // it as well as with the object, and neither says so.
            startUnsnap.SetTime(Item.StartTime);
            endUnsnap?.SetTime(Item.GetEndTime());

            if (repeatUnsnaps != null && Item is IHasRepeats hasRepeats)
            {
                // the same arithmetic the verifier uses, so a reverse it calls unsnapped is the one marked here.
                double spanDuration = (Item.GetEndTime() - Item.StartTime) / (hasRepeats.RepeatCount + 1);

                for (int i = 0; i < repeatUnsnaps.Children.Count; i++)
                    repeatUnsnaps.Children[i].SetTime(Item.StartTime + spanDuration * (i + 1));
            }
        }

        private void updateRepeats(IHasRepeats repeats)
        {
            repeatsContainer?.Expire();

            colouredComponents.Add(repeatsContainer = new Container
            {
                RelativeSizeAxes = Axes.Both,
            });

            repeatUnsnaps?.Clear();

            for (int i = 0; i < repeats.RepeatCount; i++)
            {
                repeatsContainer.Add(new Tick
                {
                    CircleSize = currentCircleSize,
                    X = (float)(i + 1) / (repeats.RepeatCount + 1)
                });

                // over the reverse arrow, the same way the other two sit over the ends they are about.
                repeatUnsnaps?.Add(new UnsnapMarker
                {
                    RelativePositionAxes = Axes.X,
                    X = (float)(i + 1) / (repeats.RepeatCount + 1),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.BottomCentre,
                });
            }

            // Add node sample pieces
            sampleComponents.Clear();

            for (int i = 0; i < repeats.RepeatCount + 2; i++)
            {
                sampleComponents.Add(new NodeSamplePointPiece(Item, i)
                {
                    X = (float)i / (repeats.RepeatCount + 1),
                    RelativePositionAxes = Axes.X,
                    Anchor = stableLayout ? Anchor.TopLeft : Anchor.BottomLeft,
                    Origin = stableLayout ? Anchor.BottomCentre : Anchor.TopCentre
                });
            }

            samplePointPiece.X = 1f / (repeats.RepeatCount + 1) / 2;

            // one arrow per reverse, so the skin's pieces follow a reverse being added or taken away.
            updateSkinPieces();
        }

        protected override bool ShouldBeConsideredForInput(Drawable child) => true;

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) =>
            circle.ReceivePositionalInputAt(screenSpacePos);

        public override Quad SelectionQuad => circle.ScreenSpaceDrawQuad;

        public override Vector2 ScreenSpaceSelectionPoint => ScreenSpaceDrawQuad.TopLeft;

        protected override bool ComputeIsMaskedAway(RectangleF maskingBounds)
        {
            // Since children are exceeding the component size, we need to use a custom quad to compute whether it should be masked away.

            // If the component isn't considered masked away by itself, there's no need to apply custom logic.
            if (!base.ComputeIsMaskedAway(maskingBounds))
                return false;

            // If the component is considered masked away, we'll use children to create an extended quad that encapsulates all parts of this blueprint
            // to ensure it doesn't pop in and out of existence abruptly when scrolling the timeline.
            var rect = RectangleF.Union(ScreenSpaceDrawQuad.AABBFloat, circle.ScreenSpaceDrawQuad.AABBFloat);
            rect = RectangleF.Union(rect, samplePointPiece.ScreenSpaceDrawQuad.AABBFloat);

            if (difficultyPointPiece != null)
                rect = RectangleF.Union(rect, difficultyPointPiece.ScreenSpaceDrawQuad.AABBFloat);

            return !Precision.AlmostIntersects(maskingBounds, rect);
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (skin.IsNotNull())
                skin.SourceChanged -= updateSkinPieces;

            if (samplePoints != null)
                samplePoints.Changed -= samplePointsChanged;
        }

        private partial class Tick : Circle
        {
            /// <summary>
            /// The height objects are drawn at, which this is sized from.
            /// </summary>
            public float CircleSize
            {
                set => Size = new Vector2(value / 4);
            }

            public Tick()
            {
                Size = new Vector2(circle_size / 4);
                Anchor = Anchor.CentreLeft;
                Origin = Anchor.Centre;
                RelativePositionAxes = Axes.X;
            }
        }

        public partial class DragArea : Circle
        {
            private readonly HitObject? hitObject;

            private readonly List<HitObject> objsToAdjust = new List<HitObject>();

            [Resolved]
            private EditorBeatmap beatmap { get; set; } = null!;

            [Resolved]
            private IBeatSnapProvider beatSnapProvider { get; set; } = null!;

            [Resolved]
            private Timeline timeline { get; set; } = null!;

            [Resolved]
            private IEditorChangeHandler? changeHandler { get; set; }

            private ScheduledDelegate? dragOperation;

            private readonly Bindable<float> repeatDragThreshold = new Bindable<float>(0.8f);
            private readonly Bindable<float> restingScale = new Bindable<float>(resting_scale);

            /// <summary>
            /// The scale the handle sits at while it is not being pointed at, which the user's setting replaces.
            /// </summary>
            private const float resting_scale = 0.5f;

            public Action<DragEvent?>? OnDragHandled;

            /// <summary>Told whether the handle is being held, so the end it sits on can say so.</summary>
            public Action<bool>? GrabChanged;

            public override bool HandlePositionalInput => hitObject != null;

            /// <summary>
            /// The blueprints around this one, for <see cref="ReceivePositionalInputAt"/> below.
            /// </summary>
            private TimelineBlueprintContainer? blueprints;

            /// <summary>
            /// Stands aside where a selected object is under the cursor, and where the object this handle belongs to has
            /// repeats and is one of several selected.
            /// </summary>
            /// <remarks>
            /// The handle covers the end of its object, so anything landing on that end is behind it: the handle
            /// answered every click there, and dragging lengthened the slider rather than moving what was under it.
            /// Only the sliver of the object poking out could be grabbed. What is selected is what the user has
            /// already said they are working on, so it wins.
            ///
            /// Declining positional input rather than the click itself, so the handle does not grow under the
            /// cursor either: offering a grab that will not happen is the same problem one step earlier.
            ///
            /// When the object this handle belongs to is selected, the handle is the point: that end is as much what
            /// the user is working on as anything under it, and the handle is what is drawn on top. Standing aside
            /// then left two selected hold notes ending together each waiting on the other, so neither end could be
            /// taken hold of and the drag moved the whole selection instead.
            ///
            /// Except for an object with repeats. Several objects selected are only ever being moved, and taking hold of
            /// a slider's end among them added a repeat to it instead. Hold notes and the like keep the handle, which
            /// lengthens every selected one starting and ending with it.
            /// </remarks>
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            {
                if (!base.ReceivePositionalInputAt(screenSpacePos))
                    return false;

                if (blueprints == null)
                    return true;

                if (beatmap.SelectedHitObjects.Contains(hitObject))
                    return !(hitObject is IHasRepeats && beatmap.SelectedHitObjects.Count > 1);

                foreach (var blueprint in blueprints.SelectionHandler.SelectedBlueprints)
                {
                    if (blueprint.ReceivePositionalInputAt(screenSpacePos))
                        return false;
                }

                return true;
            }

            /// <summary>
            /// The height objects are drawn at, which this is sized from so it stays round.
            /// </summary>
            public float CircleSize
            {
                set
                {
                    CornerRadius = value / 2;
                    Size = new Vector2(value, 1);
                }
            }

            private Box handle = null!;

            /// <summary>
            /// Whether the handle is drawn. It still answers the pointer either way: with the skin's own end drawn
            /// over it there is nothing for a grip of its own to add.
            /// </summary>
            public bool Invisible
            {
                set
                {
                    invisible = value;
                    updateState();
                }
            }

            private bool invisible;

            public DragArea(HitObject? hitObject)
            {
                this.hitObject = hitObject;

                CornerRadius = circle_size / 2;
                Masking = true;
                Size = new Vector2(circle_size, 1);
                Anchor = Anchor.CentreRight;
                Origin = Anchor.Centre;
                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    handle = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                    }
                };
            }

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config)
            {
                config.BindWith(OsuSetting.EditorTimelineRepeatDragThreshold, repeatDragThreshold);
                config.BindWith(OsuSetting.EditorTimelineDragAreaScale, restingScale);

                restingScale.BindValueChanged(_ => updateState());
            }

            /// <summary>
            /// The repeat count a slider dragged out to <paramref name="proposedDuration"/> should take.
            /// </summary>
            /// <param name="proposedDuration">How long the drag has made the object, measured from its start.</param>
            /// <param name="lengthOfOneRepeat">How long one pass of the slider takes.</param>
            /// <param name="threshold">How far towards the next repeat the drag has to reach before it counts.</param>
            /// <remarks>
            /// <c>Math.Round</c> used to decide this, which puts the change exactly halfway but rounds its midpoint to
            /// even - so a drag landing on the halfway beat, which is exactly where the snapping puts it, did nothing,
            /// and the next tick along was already three quarters of the way there. Measuring the fraction directly
            /// keeps the point where it says it is, and lets the user move it.
            /// </remarks>
            internal static int RepeatCountFor(double proposedDuration, double lengthOfOneRepeat, float threshold)
            {
                double repeatsDragged = proposedDuration / lengthOfOneRepeat - threshold;

                // the drag lands on a snapped time, so the user can hit the boundary exactly. nudge it rather than let
                // floating point decide which side of it they are on.
                return Math.Max(0, (int)Math.Floor(repeatsDragged + Precision.FLOAT_EPSILON));
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                blueprints = this.FindClosestParent<TimelineBlueprintContainer>();

                updateState();
                FinishTransforms();
            }

            protected override bool OnHover(HoverEvent e)
            {
                updateState();

                return base.OnHover(e);
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                updateState();
                base.OnHoverLost(e);
            }

            private bool hasMouseDown;

            [Resolved(canBeNull: true)]
            private SamplePointSelection? samplePoints { get; set; }

            [Resolved(canBeNull: true)]
            private Editor? editor { get; set; }

            [Resolved(canBeNull: true)]
            private EditorAsmr? asmr { get; set; }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                // only the button that drags an end takes hold of it. Everything else a button does here is about the
                // object rather than its end -- deleting it with the wheel button or shift and the right, the context
                // menu -- and has to reach the blueprint underneath, which is where those are handled.
                if (e.Button != MouseButton.Left)
                    return false;

                // the points selected on the hitsound screen, before this selects the object as a whole.
                if (samplePoints != null)
                    samplePoints.PointsBeforePress = editor?.Mode.Value == EditorScreenMode.Hitsounds ? samplePoints.All.ToHashSet() : null;

                hasMouseDown = true;

                // taking hold of an end is working on that object, and with the skin's own end drawn here there is
                // nothing left of the object to click instead. Without this the handle answers while something else
                // entirely stays selected.
                if (hitObject != null && !beatmap.SelectedHitObjects.Contains(hitObject))
                {
                    beatmap.SelectedHitObjects.Clear();
                    beatmap.SelectedHitObjects.Add(hitObject);
                }

                selectEnd(e, isThePress: true);

                updateState();
                return true;
            }

            protected override void OnMouseUp(MouseUpEvent e)
            {
                hasMouseDown = false;
                updateState();
                base.OnMouseUp(e);
            }

            /// <remarks>
            /// On the hitsound screen, pressing the end of an object selects that end alone: a slider's tail, or what a
            /// spinner sounds as it ends. The handle lies over the tail's circle, so a press there would otherwise only
            /// ever take the whole slider (2026-10-08).
            ///
            /// It is selected as the mouse goes down, as the rest of an object is on the timeline, and here again for what
            /// the press could not do (2026-10-11, see <see cref="TimelineBlueprintContainer"/>).
            /// </remarks>
            protected override bool OnClick(ClickEvent e) => selectEnd(e) || base.OnClick(e);

            /// <remarks>
            /// Pressed soon after a click here, this is a double click rather than a click, which would otherwise go on to
            /// the timeline and take the object's start instead. One whose second press was anywhere else is not this
            /// end's to answer.
            /// </remarks>
            protected override bool OnDoubleClick(DoubleClickEvent e) => (hasMouseDown && selectEnd(e)) || base.OnDoubleClick(e);

            /// <param name="e">The press, or the click or double click it became.</param>
            /// <param name="isThePress">Whether the mouse has only gone down, when it may yet be the start of a drag.</param>
            private bool selectEnd(MouseButtonEvent e, bool isThePress = false)
            {
                var before = samplePoints?.PointsBeforePress;

                if (before == null || samplePoints == null || hitObject == null || e.Button != MouseButton.Left)
                    return false;

                int end = hitObject is IHasRepeats repeats && repeats.NodeSamples.Count > 0 ? repeats.NodeSamples.Count - 1 : SamplePointSelection.Body;

                if (isThePress && samplePoints.WouldDeselectAnObject(hitObject, end, e.ControlPressed, before))
                    return false;

                samplePoints.SelectPoint(hitObject, end, e.ControlPressed, before);
                return true;
            }

            private void updateState()
            {
                // pointing at the handle grows it, and pressing it settles back part of the way. those are the
                // affordance, so they are kept as a proportion of whatever size the user asked for rather than
                // as the fixed sizes they used to be.
                handle.Alpha = invisible ? 0 : 1;

                GrabChanged?.Invoke(hasMouseDown);

                // undrawn, it is the ring behind it that is being aimed at, and that does not grow or shrink.
                float scale = invisible ? 1 : restingScale.Value;

                if (!invisible)
                {
                    if (hasMouseDown)
                        scale *= 1.2f;
                    else if (IsHovered)
                        scale *= 1.4f;
                }

                this.ScaleTo(scale, 200, Easing.OutQuint);
                this.FadeTo(IsHovered || hasMouseDown ? 1f : 0.9f, 200, Easing.OutQuint);
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                changeHandler?.BeginChange();

                var selectionItems = beatmap.SelectedHitObjects;

                if (!selectionItems.Contains(hitObject))
                    return true;

                foreach (var item in selectionItems)
                {
                    if (item == hitObject || item is not IHasDuration durationItem) continue;

                    if (Precision.AlmostEquals(durationItem.Duration, (hitObject as IHasDuration)!.Duration, 1) &&
                        Precision.AlmostEquals(item.StartTime, hitObject!.StartTime, 1))
                    {
                        objsToAdjust.Add(item);
                    }
                }

                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                base.OnDrag(e);

                // schedule is temporary to ensure we don't process multiple times on a single update frame. we need to find a better method of doing this.
                // without it, a hitobject's endtime may not always be in a valid state (ie. sliders, which needs to recompute their path).
                dragOperation?.Cancel();
                dragOperation = Scheduler.Add(() =>
                {
                    OnDragHandled?.Invoke(e);

                    if (timeline.FindSnappedPositionAndTime(e.ScreenSpaceMousePosition).Time is double time)
                    {
                        switch (hitObject)
                        {
                            case IHasRepeats repeatHitObject:
                                double proposedDuration = time - hitObject.StartTime;

                                if (e.CurrentState.Keyboard.ShiftPressed && hitObject is IHasSliderVelocity hasSliderVelocity)
                                {
                                    double newVelocity = hasSliderVelocity.SliderVelocityMultiplier * (repeatHitObject.Duration / proposedDuration);

                                    if (Precision.AlmostEquals(newVelocity, hasSliderVelocity.SliderVelocityMultiplier))
                                        return;

                                    hasSliderVelocity.SliderVelocityMultiplier = newVelocity;
                                    beatmap.Update(hitObject);
                                }
                                else
                                {
                                    // find the number of repeats which can fit in the requested time.
                                    double lengthOfOneRepeat = repeatHitObject.Duration / (repeatHitObject.RepeatCount + 1);
                                    int proposedCount = RepeatCountFor(proposedDuration, lengthOfOneRepeat, repeatDragThreshold.Value);

                                    if (proposedCount == repeatHitObject.RepeatCount || Precision.AlmostEquals(lengthOfOneRepeat, 0, 1))
                                        return;

                                    repeatHitObject.RepeatCount = proposedCount;
                                    beatmap.Update(hitObject);

                                    asmr?.Play(AsmrSound.SliderRepeat, (hitObject as IHasXPosition)?.X, EditorAsmr.PitchForRepeats(proposedCount));
                                }

                                break;

                            case IHasDuration endTimeHitObject:
                                double snappedTime = Math.Max(hitObject.StartTime + beatSnapProvider.GetBeatLengthAtTime(hitObject.StartTime), beatSnapProvider.SnapTime(time));

                                if (endTimeHitObject.EndTime == snappedTime)
                                    return;

                                if (!objsToAdjust.Contains(hitObject))
                                    objsToAdjust.Add(hitObject);

                                foreach (var obj in objsToAdjust)
                                {
                                    (obj as IHasDuration)!.Duration = snappedTime - obj.StartTime;
                                    beatmap.Update(obj);
                                }

                                asmr?.Play(AsmrSound.SliderLength, (hitObject as IHasXPosition)?.X, EditorAsmr.PitchForDuration(endTimeHitObject.Duration));

                                break;
                        }
                    }
                });
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                base.OnDragEnd(e);

                dragOperation?.Cancel();
                dragOperation = null;

                changeHandler?.EndChange();
                OnDragHandled?.Invoke(null);
                objsToAdjust.Clear();
            }
        }

        public partial class Border : ExtendableCircle
        {
            /// <summary>
            /// Matches the border thickness <see cref="ExtendableCircle"/> sets up.
            /// </summary>
            private const float default_border_thickness = 4;

            /// <summary>
            /// Held onto rather than left to the loading method. Nothing else refers to one of these, and the
            /// settings are bound weakly, so a collected one stops hearing about changes and the outline only
            /// catches up on the next visit to the editor.
            /// </summary>
            private EditorSelectionAppearance? appearance;

            private OsuColour colours = null!;

            /// <summary>
            /// Whether the object this is drawn on is selected, which <see cref="Show"/> and <see cref="Hide"/> are
            /// told. Kept here rather than read back off <see cref="Drawable.Alpha"/>, which the opacity setting
            /// writes as well: without it, changing that setting drew an outline on every object in the timeline.
            /// </summary>
            private bool shown;

            public override void Show()
            {
                shown = true;
                updateAppearance();
            }

            public override void Hide()
            {
                shown = false;
                updateAppearance();
            }

            [Resolved(canBeNull: true)]
            private Editor? editor { get; set; }

            /// <summary>
            /// Which screen the editor shows: the hitsound screen has an outline of its own (2026-10-08).
            /// </summary>
            private IBindable<EditorScreenMode>? editorMode;

            [BackgroundDependencyLoader]
            private void load(OsuColour colours, OsuConfigManager config)
            {
                this.colours = colours;

                Content.Child.Alpha = 0;
                Content.Child.AlwaysPresent = true;

                Content.EdgeEffect = new EdgeEffectParameters();

                appearance = new EditorSelectionAppearance(config);
                appearance.Changed += updateAppearance;

                if (editor != null)
                {
                    editorMode = editor.Mode.GetBoundCopy();
                    editorMode.BindValueChanged(_ => updateAppearance());
                }

                updateAppearance();
            }

            private void updateAppearance()
            {
                // the selection state can be set before this has loaded, in which case the loading method applies it.
                if (appearance == null)
                    return;

                if (editorMode?.Value == EditorScreenMode.Hitsounds)
                {
                    Content.BorderColour = appearance.GetHitsoundOutlineColour(colours.Yellow);
                    Content.BorderThickness = appearance.GetHitsoundOutlineThickness(default_border_thickness);
                    Alpha = shown ? appearance.HitsoundOutlineOpacity : 0;
                    return;
                }

                Content.BorderColour = appearance.GetObjectColour(colours.Yellow);
                Content.BorderThickness = appearance.GetObjectThickness(default_border_thickness);
                Alpha = shown ? appearance.ObjectOpacity : 0;
            }
        }

        /// <summary>
        /// A circle with externalised end caps so it can take up the full width of a relative width area.
        /// </summary>
        public partial class ExtendableCircle : CompositeDrawable
        {
            public new ColourInfo Colour
            {
                get => Content.Colour;
                set => Content.Colour = value;
            }

            protected readonly Circle Content;

            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Content.ReceivePositionalInputAt(screenSpacePos);

            public override Quad ScreenSpaceDrawQuad => Content.ScreenSpaceDrawQuad;

            /// <summary>
            /// The height objects are drawn at, which decides how far the end caps reach past the object's bounds.
            /// </summary>
            public float CircleSize
            {
                set => Padding = new MarginPadding { Horizontal = -value / 2f };
            }

            public ExtendableCircle()
            {
                Padding = new MarginPadding { Horizontal = -circle_size / 2f };
                InternalChild = Content = new Circle
                {
                    BorderColour = OsuColour.Gray(0.75f),
                    BorderThickness = 4,
                    Masking = true,
                    RelativeSizeAxes = Axes.Both,
                    EdgeEffect = new EdgeEffectParameters
                    {
                        Type = EdgeEffectType.Shadow,
                        Radius = 5,
                        Colour = Color4.Black.Opacity(0.05f)
                    }
                };
            }
        }
    }
}
