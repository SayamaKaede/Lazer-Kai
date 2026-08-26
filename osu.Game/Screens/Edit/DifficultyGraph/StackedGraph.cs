// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// The difficulty over the beatmap, drawn as bands of the skills that make it up.
    /// </summary>
    /// <remarks>
    /// The top of each stack is the height the summary timeline's curve has at the same moment, so the two read as
    /// the same picture with this one taken apart.
    /// </remarks>
    public partial class StackedGraph : CompositeDrawable
    {
        private readonly OverlayColourProvider colourProvider;

        private BufferedContainer<Box> bands = null!;
        private Container playhead = null!;
        private OsuSpriteText playheadTime = null!;
        private Container readout = null!;
        private FillFlowContainer readoutRows = null!;
        private OsuSpriteText readoutTime = null!;

        private DifficultyGraphDetailOverlay.SkillColumn[] columns = Array.Empty<DifficultyGraphDetailOverlay.SkillColumn>();
        private string[] names = Array.Empty<string>();
        private Color4[] colours = Array.Empty<Color4>();
        private double trackLength;
        private double peak;

        public StackedGraph(OverlayColourProvider colourProvider)
        {
            this.colourProvider = colourProvider;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            InternalChildren = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = colourProvider.Background6,
                },
                // buffered for the same reason the timeline's graph is: the bands are drawn a touch wider than their
                // slot so neighbours meet, and alpha applied per drawable would paint the overlaps twice.
                bands = new BufferedContainer<Box>(cachedFrameBuffer: true)
                {
                    RelativeSizeAxes = Axes.Both,
                    RedrawOnScale = false,
                },
                playhead = new Container
                {
                    RelativeSizeAxes = Axes.Y,
                    RelativePositionAxes = Axes.X,
                    Width = 1.5f,
                    Alpha = 0,
                    Child = new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White, Alpha = 0.85f },
                },
                playheadTime = new OsuSpriteText
                {
                    RelativePositionAxes = Axes.X,
                    Font = OsuFont.Default.With(size: 11.5f, fixedWidth: true),
                    Origin = Anchor.BottomCentre,
                    Alpha = 0,
                },
                readout = new Container
                {
                    AutoSizeAxes = Axes.Both,
                    RelativePositionAxes = Axes.X,
                    Alpha = 0,
                    Masking = true,
                    CornerRadius = 4,
                    Margin = new MarginPadding { Top = 20 },
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background3 },
                        new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 4),
                            Padding = new MarginPadding { Horizontal = 12, Vertical = 9 },
                            Children = new Drawable[]
                            {
                                readoutTime = new OsuSpriteText
                                {
                                    Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold, fixedWidth: true),
                                },
                                readoutRows = new FillFlowContainer
                                {
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Vertical,
                                    Spacing = new Vector2(0, 2),
                                },
                            }
                        }
                    }
                },
            };
        }

        /// <summary>
        /// Hands the graph a beatmap to draw.
        /// </summary>
        public void Set(DifficultyGraphDetailOverlay.SkillColumn[] built, string[] skillNames, Color4[] skillColours, double length)
        {
            columns = built;
            names = skillNames;
            colours = skillColours;
            trackLength = length;
            peak = built.Length > 0 ? built.Max(c => c.Total) : 0;

            bands.Clear();

            if (peak <= 0)
            {
                bands.ForceRedraw();
                return;
            }

            float width = 1.02f / columns.Length;

            for (int i = 0; i < columns.Length; i++)
            {
                if (columns[i].Total <= 0)
                    continue;

                float bottom = 0;

                for (int s = 0; s < columns[i].Skills.Length; s++)
                {
                    float height = (float)(columns[i].Skills[s] / peak);

                    if (height <= 0)
                        continue;

                    bands.Add(new Box
                    {
                        RelativePositionAxes = Axes.Both,
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        X = (float)i / columns.Length,
                        Y = -bottom,
                        Width = width,
                        Height = height,
                        Colour = skillColours[s % skillColours.Length],
                    });

                    bottom += height;
                }
            }

            bands.ForceRedraw();
        }

        /// <summary>
        /// Moves the line marking where playback is.
        /// </summary>
        public void SetPlayhead(double time)
        {
            if (trackLength <= 0)
            {
                playhead.Alpha = playheadTime.Alpha = 0;
                return;
            }

            float position = (float)Math.Clamp(time / trackLength, 0, 1);

            playhead.Alpha = 1;
            playhead.X = position;

            playheadTime.Alpha = 1;
            playheadTime.X = position;
            playheadTime.Text = time.ToEditorFormattedString();
        }

        protected override bool OnHover(HoverEvent e) => true;

        protected override void OnHoverLost(HoverLostEvent e) => readout.Alpha = 0;

        protected override bool OnMouseMove(MouseMoveEvent e)
        {
            updateReadout(e.MousePosition.X);
            return base.OnMouseMove(e);
        }

        // deliberately not seekable. Clicking here landed somewhere other than where the cursor was, and the timeline
        // along the bottom of the editor is left reachable for exactly this — a graph is for reading.
        private double timeAt(float x) => Math.Clamp(x / DrawWidth, 0, 1) * trackLength;

        private void updateReadout(float x)
        {
            if (columns.Length == 0 || trackLength <= 0)
            {
                readout.Alpha = 0;
                return;
            }

            int index = Math.Clamp((int)(x / DrawWidth * columns.Length), 0, columns.Length - 1);

            readout.Alpha = 1;
            readout.X = Math.Clamp(x / DrawWidth, 0, 1);

            // the box would hang off the right edge in the last stretch of the song, so it flips to the other side.
            bool nearTheEnd = readout.X > 0.8f;
            readout.Origin = nearTheEnd ? Anchor.TopRight : Anchor.TopLeft;
            readout.Margin = new MarginPadding { Top = 20, Left = nearTheEnd ? 0 : 8, Right = nearTheEnd ? 8 : 0 };

            readoutTime.Text = timeAt(x).ToEditorFormattedString();
            readoutRows.Clear();

            for (int s = 0; s < names.Length; s++)
            {
                double value = s < columns[index].Skills.Length ? columns[index].Skills[s] : 0;

                readoutRows.Add(new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Size = new Vector2(8),
                            Colour = colours[s % colours.Length],
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        // a box of its own so every skill's number starts at the same place however long its name is.
                        new Container
                        {
                            Width = 66,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Child = new OsuSpriteText
                            {
                                Text = names[s],
                                Font = OsuFont.Default.With(size: 11.5f),
                                Colour = colourProvider.Content2,
                            },
                        },
                        new OsuSpriteText
                        {
                            Text = value.ToString(@"N1"),
                            Font = OsuFont.Default.With(size: 11.5f, weight: FontWeight.SemiBold, fixedWidth: true),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                });
            }
        }
    }
}
