// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Specialized;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using System.Linq;
using osu.Framework.Threading;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Screens.Edit.Compose.Components
{
    public partial class HitObjectInspector : EditorInspector
    {
        protected override void LoadComplete()
        {
            base.LoadComplete();

            EditorBeatmap.SelectedHitObjects.CollectionChanged += selectionChanged;
            placementObject.BindTo(EditorBeatmap.PlacementObject);
            placementObject.BindValueChanged(_ => updateInspectorText());
            EditorBeatmap.TransactionBegan += updateInspectorText;
            EditorBeatmap.TransactionEnded += updateInspectorText;
            updateInspectorText();
        }

        /// <summary>
        /// A bound copy, so leaving takes the subscription with it. The editor rebuilds the composer while keeping its
        /// beatmap, and anything still listening afterwards keeps this inspector alive for the rest of the session.
        /// </summary>
        private readonly Bindable<HitObject?> placementObject = new Bindable<HitObject?>();

        private void selectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => updateInspectorText();

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (EditorBeatmap.IsNotNull())
            {
                EditorBeatmap.SelectedHitObjects.CollectionChanged -= selectionChanged;
                EditorBeatmap.TransactionBegan -= updateInspectorText;
                EditorBeatmap.TransactionEnded -= updateInspectorText;
            }
        }

        private ScheduledDelegate? rollingTextUpdate;

        private void updateInspectorText()
        {
            BeginInspectorText();
            rollingTextUpdate?.Cancel();
            rollingTextUpdate = null;

            HitObject[] objects;

            if (EditorBeatmap.SelectedHitObjects.Count > 0)
                objects = EditorBeatmap.SelectedHitObjects.ToArray();
            else if (EditorBeatmap.PlacementObject.Value != null)
                objects = new[] { EditorBeatmap.PlacementObject.Value };
            else
                objects = Array.Empty<HitObject>();

            AddInspectorValues(objects);

            // I'd hope there's a better way to do this, but I don't want to bind to each and every property above to watch for changes.
            // This is a good middle-ground for the time being.
            if (objects.Length > 0)
                rollingTextUpdate ??= Scheduler.AddDelayed(updateInspectorText, 250);
        }

        /// <summary>
        /// A time as the inspector writes it. The strip leaves off the fraction of a millisecond: a slider's end
        /// carries one, and it is three characters that push the line over the width the strip has.
        /// </summary>
        private string time(double milliseconds) => Compact ? $"{milliseconds:#,0}" : $"{milliseconds:#,0.##}";

        /// <summary>
        /// The unit after a time, which the strip leaves off: every number on that line is a time already.
        /// </summary>
        private string ms => Compact ? string.Empty : " ms";

        protected virtual void AddInspectorValues(HitObject[] objects)
        {
            switch (objects.Length)
            {
                case 0:
                    AddValue("No selection");
                    break;

                case 1:
                    var selected = objects.Single();

                    if (selected is IHasDuration duration)
                    {
                        AddHeader("Time", line: CompactLine.NewLine);
                        AddValue($"{time(selected.StartTime)} - {time(duration.EndTime)}{ms}");

                        if (selected is IHasRepeats repeats && repeats.RepeatCount > 0)
                        {
                            AddHeader("Duration", line: CompactLine.LeftOut);
                            AddValue($"{duration.Duration:#,0.##} ms");
                            AddValue($"({repeats.RepeatCount:#,0.##} repeats)");
                        }
                        else
                        {
                            AddHeader("Duration", line: CompactLine.LeftOut);
                            AddValue($"{duration.Duration:#,0.##} ms");
                        }
                    }
                    else
                    {
                        AddHeader("Time", line: CompactLine.NewLine);
                        AddValue($"{time(selected.StartTime)}{ms}");
                    }

                    switch (selected)
                    {
                        case IHasPosition pos:
                            AddHeader("Position", line: CompactLine.NewLine);
                            AddValue($"({pos.X:#,0.##}, {pos.Y:#,0.##})");
                            break;

                        case IHasXPosition x:
                            AddHeader("Position", line: CompactLine.NewLine);
                            AddValue($"{x.X:#,0.##}");
                            break;

                        case IHasYPosition y:
                            AddHeader("Position", line: CompactLine.NewLine);
                            AddValue($"{y.Y:#,0.##}");
                            break;
                    }

                    if (selected is IHasSliderVelocity sliderVelocity)
                    {
                        AddHeader("Slider Velocity", line: CompactLine.LeftOut);
                        AddValue($"{sliderVelocity.SliderVelocityMultiplier:#,0.00}x");
                        AddValue($"(actual: {sliderVelocity.SliderVelocityMultiplier * EditorBeatmap.Difficulty.SliderMultiplier:#,0.00}x)");
                    }

                    if (selected is IHasDistance distance)
                    {
                        AddHeader("Distance covered", line: CompactLine.LeftOut);
                        AddValue($"{distance.Distance:#,0.##} px");
                    }

                    break;

                default:
                    AddHeader("Object count", line: CompactLine.LeftOut);
                    AddValue($"{objects.Length:#,0.##}");

                    AddHeader("Selection", line: CompactLine.NewLine);
                    AddValue($"{time(objects.Min(o => o.StartTime))}{ms} - {time(objects.Max(o => o.GetEndTime()))}{ms}");
                    break;
            }
        }
    }
}
