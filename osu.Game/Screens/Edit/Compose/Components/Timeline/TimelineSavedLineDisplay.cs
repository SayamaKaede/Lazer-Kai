// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Globalization;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Caching;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// Draws the red and green lines of the beatmap's saved file across the timeline, the way stable's timeline has them.
    /// </summary>
    /// <remarks>
    /// These are the file's lines, not the editor's: whatever this game adds or moves on a save shows up here once it
    /// is written, which is the point of having them. Drawn over the objects, as stable does, since under them a line
    /// at an object is hidden by it (2026-09-25); they only show what they hold on hover, and a click goes through them
    /// to the object.
    ///
    /// Only in the styles that keep the ticks down at the objects (<see cref="EditorTimelineStyle.Stable"/> and
    /// <see cref="EditorTimelineStyle.LazerKai"/>). In lazer's own the ticks run the full height too, and a red line
    /// could not be told from a half-beat tick.
    /// </remarks>
    public partial class TimelineSavedLineDisplay : TimelinePart<TimelineSavedLineDisplay.LinePiece>
    {
        /// <summary>
        /// How far either side of the line the mouse can be and still read what it holds.
        /// </summary>
        private const float hover_width = 8;

        private const float line_width = 1;

        private const float line_opacity = 0.6f;

        private const float selected_line_width = 3;

        [Resolved]
        private Timeline timeline { get; set; } = null!;

        [Resolved(CanBeNull = true)]
        private SavedTimingLines? savedTimingLines { get; set; }

        private readonly Bindable<bool> enabled = new Bindable<bool>();
        private readonly Bindable<bool> stableCompatibility = new Bindable<bool>();
        private readonly Bindable<EditorTimelineStyle> style = new Bindable<EditorTimelineStyle>();

        /// <summary>
        /// Always shown with stable compatibility on, which is what the lines are there for (2026-09-25).
        /// </summary>
        private bool visible => (enabled.Value || stableCompatibility.Value) && style.Value != EditorTimelineStyle.Lazer;

        private readonly BindableList<SavedTimingLine> lines = new BindableList<SavedTimingLine>();

        private readonly Cached piecesCache = new Cached();

        private (double min, double max) visibleRange = (double.MinValue, double.MaxValue);

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorTimelineShowSavedLines, enabled);
            config.BindWith(OsuSetting.StableCompatibility, stableCompatibility);
            config.BindWith(OsuSetting.EditorTimelineStyle, style);

            if (savedTimingLines != null)
            {
                lines.BindTo(savedTimingLines.Lines);
                selected.BindTo(savedTimingLines.Selected);
            }
        }

        private readonly Bindable<SavedTimingLine?> selected = new Bindable<SavedTimingLine?>();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            enabled.BindValueChanged(_ => updateVisibility());
            stableCompatibility.BindValueChanged(_ => updateVisibility());
            style.BindValueChanged(_ => updateVisibility(), true);

            lines.BindCollectionChanged((_, _) => piecesCache.Invalidate());
            selected.BindValueChanged(_ => piecesCache.Invalidate());
        }

        private void updateVisibility()
        {
            Alpha = visible ? 1 : 0;
            piecesCache.Invalidate();
        }

        protected override void Update()
        {
            base.Update();

            if (!visible || DrawWidth <= 0)
                return;

            (double, double) newRange = (
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopLeft).X - hover_width) / DrawWidth * Content.RelativeChildSize.X,
                (ToLocalSpace(timeline.ScreenSpaceDrawQuad.TopRight).X + hover_width) / DrawWidth * Content.RelativeChildSize.X);

            if (visibleRange != newRange)
            {
                visibleRange = newRange;
                piecesCache.Invalidate();
            }

            if (!piecesCache.IsValid)
            {
                recreatePieces();
                piecesCache.Validate();
            }
        }

        private void recreatePieces()
        {
            Clear();

            if (!visible)
                return;

            foreach (var line in lines)
            {
                if (line.Time < visibleRange.min || line.Time > visibleRange.max)
                    continue;

                Add(new LinePiece(line) { X = (float)line.Time, IsSelected = line == selected.Value });
            }
        }

        public partial class LinePiece : CompositeDrawable, IHasTooltip
        {
            public readonly SavedTimingLine Line;

            /// <summary>
            /// Whether this is the line the green line panel is editing, drawn thicker and fully opaque so that a line
            /// far from the playhead can be seen to be the one changing.
            /// </summary>
            public bool IsSelected { get; init; }

            public LinePiece(SavedTimingLine line)
            {
                Line = line;

                RelativePositionAxes = Axes.X;
                RelativeSizeAxes = Axes.Y;
                Width = hover_width * 2;
                Origin = Anchor.TopCentre;
            }

            [BackgroundDependencyLoader]
            private void load(OsuColour colours)
            {
                InternalChild = new Box
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    RelativeSizeAxes = Axes.Y,
                    Width = IsSelected ? selected_line_width : line_width,
                    Alpha = IsSelected ? 1 : line_opacity,
                    Colour = Line.Uninherited ? colours.Red1 : colours.Green1,
                };
            }

            public LocalisableString TooltipText
            {
                get
                {
                    string time = Line.Time.ToEditorFormattedString();
                    string sampleSet = sampleSetName(Line.SampleSet);

                    LocalisableString text = Line.Uninherited
                        ? LazerKaiSavedLinesStrings.RedLine(time, Line.BeatsPerMinute.ToString(@"0.##", CultureInfo.InvariantCulture), Line.Meter, sampleSet, Line.SampleIndex, Line.Volume)
                        : LazerKaiSavedLinesStrings.GreenLine(time, Line.SliderVelocity.ToString(@"0.##", CultureInfo.InvariantCulture), sampleSet, Line.SampleIndex, Line.Volume);

                    return Line.Kiai ? LocalisableString.Interpolate($"{text}{LazerKaiSavedLinesStrings.Kiai}") : text;
                }
            }

            /// <summary>
            /// The names stable's own timing panel gives the sample sets, which are the words mappers use for them.
            /// </summary>
            private static string sampleSetName(int sampleSet)
            {
                switch (sampleSet)
                {
                    case 1:
                        return @"Normal";

                    case 2:
                        return @"Soft";

                    case 3:
                        return @"Drum";

                    default:
                        return @"Auto";
                }
            }
        }
    }
}
