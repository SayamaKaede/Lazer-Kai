// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Cursor;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.GreenLines;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Compose.Components.Timeline
{
    /// <summary>
    /// A tick of the beat divisor inside a selected slider or spinner, saying the volume the green lines give there.
    /// Setting it puts a green line on the tick, or changes the one already on it.
    /// </summary>
    /// <remarks>
    /// This is how a volume change part way through a slider is made without the green line panel: stable decides it by
    /// the line, and nothing the object itself carries can say it.
    /// </remarks>
    public partial class LineVolumePointPiece : HitObjectPointPiece, IHasPopover
    {
        public readonly HitObject HitObject;

        /// <summary>
        /// The tick's time, on the beat divisor.
        /// </summary>
        public readonly double TickTime;

        /// <summary>
        /// The volume the lines give at the tick.
        /// </summary>
        public int Volume { get; private set; }

        [Resolved]
        private SavedTimingLines? savedTimingLines { get; set; }

        private readonly BindableList<SavedTimingLine> lines = new BindableList<SavedTimingLine>();

        public LineVolumePointPiece(HitObject hitObject, double time)
        {
            HitObject = hitObject;
            TickTime = time;
            Y = 2.5f;
        }

        // the colour of the object's own sound points: this is a volume like theirs, and it is the object's.
        protected override Color4 GetRepresentingColour(OsuColour colours) => HitObject is IHasRepeats ? colours.Pink2 : colours.Pink1;

        [BackgroundDependencyLoader]
        private void load()
        {
            Label.AllowMultiline = false;

            if (savedTimingLines != null)
                lines.BindTo(savedTimingLines.Lines);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            lines.BindCollectionChanged((_, _) => updateVolume(), true);
        }

        private void updateVolume()
        {
            Volume = 100;

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (lines[i].Time <= TickTime)
                {
                    Volume = lines[i].Volume;
                    break;
                }
            }

            Label.Text = Volume.ToString();
        }

        protected override bool OnClick(ClickEvent e)
        {
            this.ShowPopover();
            return true;
        }

        // stable's lines stand on whole milliseconds, and one just before the tick is still in effect on it.
        public Popover GetPopover() => new LineVolumePopover(Math.Floor(TickTime), Volume);

        public partial class LineVolumePopover : OsuPopover
        {
            private readonly double lineTime;
            private readonly int volume;

            [Resolved]
            private EditorBeatmap beatmap { get; set; } = null!;

            [Resolved]
            private SavedTimingLines savedTimingLines { get; set; } = null!;

            public LineVolumePopover(double lineTime, int volume)
            {
                this.lineTime = lineTime;
                this.volume = volume;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                SamplePointPiece.VolumeControl control;

                Child = new FillFlowContainer
                {
                    Width = 220,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 10),
                    Child = control = new SamplePointPiece.VolumeControl
                    {
                        Caption = EditorStrings.SampleVolume,
                        Current = new BindableInt(volume)
                        {
                            MinValue = DrawableHitObject.MINIMUM_SAMPLE_VOLUME,
                            MaxValue = 100,
                        }
                    },
                };

                control.Current.BindValueChanged(v => setVolume(v.NewValue));
            }

            /// <summary>
            /// Puts the volume on a green line at the tick, and carries it over to the objects under it the way changing
            /// it in the panel does. Each change is a step to undo of its own, as a sound point's are.
            /// </summary>
            private void setVolume(int newVolume)
            {
                var editing = new GreenLineEditing(beatmap, savedTimingLines);
                var line = editing.Add(lineTime);

                if (line == null)
                    return;

                editing.Replace(line.Value, line.Value.WithVolume(newVolume));
                editing.Confirm();
            }
        }
    }
}
