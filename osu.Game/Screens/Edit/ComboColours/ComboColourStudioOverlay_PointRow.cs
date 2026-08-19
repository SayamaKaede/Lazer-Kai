// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Game.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays;
using osu.Game.Localisation;
using osu.Game.Rulesets.Edit;
using osuTK;

namespace osu.Game.Screens.Edit.ComboColours
{
    public partial class ComboColourStudioOverlay
    {
        /// <summary>
        /// One colour point: when it starts, how it behaves, and the colours it walks through.
        /// </summary>
        private partial class PointRow : CompositeDrawable
        {
            private readonly ComboColourPoint point;
            private readonly Func<int> paletteSize;
            private readonly Action changed;
            private readonly Action remove;

            [Resolved]
            private EditorBeatmap editorBeatmap { get; set; } = null!;

            [Resolved]
            private EditorClock editorClock { get; set; } = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            private FillFlowContainer sequence = null!;
            private OsuTextBox timeBox = null!;

            public PointRow(ComboColourPoint point, Func<int> paletteSize, Action changed, Action remove)
            {
                this.point = point;
                this.paletteSize = paletteSize;
                this.changed = changed;
                this.remove = remove;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                RelativeSizeAxes = Axes.X;
                AutoSizeAxes = Axes.Y;

                InternalChildren = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider.Background5,
                        Alpha = 0.6f,
                    },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Padding = new MarginPadding(6),
                        Spacing = new Vector2(6, 0),
                        Children = new Drawable[]
                        {
                            timeBox = new OsuTextBox
                            {
                                Width = 96,
                                Height = 30,
                                Text = point.Time.ToEditorFormattedString(),
                                CommitOnFocusLost = true,
                                TabbableContentContainer = null,
                            },
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.Crosshairs,
                                Scale = new Vector2(0.6f),
                                TooltipText = ComboColourStudioStrings.UseCurrentTime,
                                Action = () =>
                                {
                                    point.Time = Math.Round(editorClock.CurrentTime);
                                    timeBox.Text = point.Time.ToEditorFormattedString();
                                    changed();
                                },
                            },
                            new ModeButton(point, changed),
                            sequence = new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(3, 0),
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.Plus,
                                Scale = new Vector2(0.6f),
                                TooltipText = ComboColourStudioStrings.AddColour,
                                Action = () =>
                                {
                                    if (paletteSize() == 0)
                                        return;

                                    point.Colours.Add(0);
                                    rebuildSequence();
                                    changed();
                                },
                            },
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.Minus,
                                Scale = new Vector2(0.6f),
                                TooltipText = ComboColourStudioStrings.RemoveColour,
                                Action = () =>
                                {
                                    if (point.Colours.Count == 0)
                                        return;

                                    point.Colours.RemoveAt(point.Colours.Count - 1);
                                    rebuildSequence();
                                    changed();
                                },
                            },
                            new IconButton
                            {
                                Icon = FontAwesome.Solid.TrashAlt,
                                Scale = new Vector2(0.6f),
                                TooltipText = ComboColourStudioStrings.RemovePoint,
                                Action = remove,
                            },
                        }
                    },
                };

                // takes the same formats the editor accepts elsewhere, so a timestamp copied from chat can be pasted in.
                timeBox.OnCommit += (_, _) =>
                {
                    if (EditorTimestampParser.TryParse(timeBox.Text, out var parsed, out _))
                        point.Time = Math.Round(parsed.Value.TotalMilliseconds);

                    timeBox.Text = point.Time.ToEditorFormattedString();
                    changed();
                };

                rebuildSequence();
            }

            /// <summary>
            /// The colours this point walks through, one circle each. Clicking a circle moves it to the next colour in
            /// the beatmap's palette, which is quicker than a picker for a list this short.
            /// </summary>
            private void rebuildSequence()
            {
                sequence.Clear();

                var palette = editorBeatmap.BeatmapSkin?.ComboColours;

                for (int i = 0; i < point.Colours.Count; i++)
                {
                    int index = i;

                    sequence.Add(new ColourCircle(() => palette != null && point.Colours[index] < palette.Count ? palette[point.Colours[index]] : Colour4.Gray)
                    {
                        Action = () =>
                        {
                            if (paletteSize() == 0)
                                return;

                            point.Colours[index] = (point.Colours[index] + 1) % paletteSize();
                            rebuildSequence();
                            changed();
                        }
                    });
                }
            }

            private partial class ColourCircle : OsuClickableContainer
            {
                private readonly Func<Colour4> colour;

                public ColourCircle(Func<Colour4> colour)
                {
                    this.colour = colour;

                    Size = new Vector2(22);
                    Masking = true;
                    CornerRadius = 11;
                    Anchor = Anchor.CentreLeft;
                    Origin = Anchor.CentreLeft;
                }

                [BackgroundDependencyLoader]
                private void load()
                {
                    Child = new Box { RelativeSizeAxes = Axes.Both, Colour = colour() };
                }
            }

            /// <summary>
            /// Normal or burst, toggled by clicking rather than through a dropdown: there are only two.
            /// </summary>
            private partial class ModeButton : OsuClickableContainer
            {
                private readonly ComboColourPoint point;
                private readonly Action changed;

                private OsuSpriteText text = null!;

                public ModeButton(ComboColourPoint point, Action changed)
                {
                    this.point = point;
                    this.changed = changed;

                    Size = new Vector2(62, 30);
                    Anchor = Anchor.CentreLeft;
                    Origin = Anchor.CentreLeft;
                }

                [BackgroundDependencyLoader]
                private void load(OverlayColourProvider colourProvider)
                {
                    Masking = true;
                    CornerRadius = 5;

                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = colourProvider.Background2 },
                        text = new OsuSpriteText
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Font = OsuFont.Default.With(size: 12),
                        },
                    };

                    Action = () =>
                    {
                        point.Mode = point.Mode == ComboColourPointMode.Normal ? ComboColourPointMode.Burst : ComboColourPointMode.Normal;
                        updateText();
                        changed();
                    };

                    updateText();
                }

                private void updateText() => text.Text = point.Mode == ComboColourPointMode.Normal
                    ? ComboColourStudioStrings.ModeNormal
                    : ComboColourStudioStrings.ModeBurst;
            }
        }
    }
}
