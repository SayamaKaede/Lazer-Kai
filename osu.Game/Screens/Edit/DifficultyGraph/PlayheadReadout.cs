// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// What the beatmap is made of at the moment playback is sitting on, kept on screen.
    /// </summary>
    /// <remarks>
    /// The readout that follows the cursor answers a question being asked; this one is the answer to the question a
    /// mapper is always asking, which is what the part they are listening to right now is made of. Waiting for it to
    /// appear under the pointer means taking a hand off the beatmap to find out.
    /// </remarks>
    public partial class PlayheadReadout : CompositeDrawable
    {
        private OsuSpriteText time = null!;
        private FillFlowContainer skills = null!;
        private OsuSpriteText total = null!;

        private readonly List<OsuSpriteText> values = new List<OsuSpriteText>();

        [BackgroundDependencyLoader]
        private void load(OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.X;
            AutoSizeAxes = Axes.Y;

            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(18, 0),
                Children = new Drawable[]
                {
                    time = new OsuSpriteText
                    {
                        Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold, fixedWidth: true),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    skills = new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(16, 0),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                    },
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = EditorStrings.DifficultyGraphTotal,
                                Font = OsuFont.Default.With(size: 12),
                                Colour = colourProvider.Content2,
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                            total = new OsuSpriteText
                            {
                                Font = OsuFont.Default.With(size: 12, weight: FontWeight.Bold, fixedWidth: true),
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                            },
                        }
                    },
                }
            };
        }

        /// <summary>
        /// Builds a slot for each of the ruleset's skills. What is in them is set as playback moves.
        /// </summary>
        public void SetSkills(IReadOnlyList<string> names, IReadOnlyList<Color4> colours, OverlayColourProvider colourProvider)
        {
            skills.Clear();
            values.Clear();

            for (int i = 0; i < names.Count; i++)
            {
                OsuSpriteText value;

                skills.Add(new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(6, 0),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            Size = new Vector2(9),
                            Colour = colours[i % colours.Count],
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new OsuSpriteText
                        {
                            Text = names[i],
                            Font = OsuFont.Default.With(size: 12),
                            Colour = colourProvider.Content2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        value = new OsuSpriteText
                        {
                            Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold, fixedWidth: true),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                });

                values.Add(value);
            }
        }

        /// <summary>
        /// Puts the moment playback is sitting on into the slots.
        /// </summary>
        public void Set(double at, DifficultyGraphDetailOverlay.SkillColumn? column)
        {
            time.Text = at.ToEditorFormattedString();

            for (int i = 0; i < values.Count; i++)
            {
                double? value = column != null && i < column.Value.Skills.Length ? column.Value.Skills[i] : null;
                values[i].Text = value == null ? @"-" : value.Value.ToString(@"N1");
            }

            total.Text = column == null ? @"-" : column.Value.Total.ToString(@"N1");
        }

        /// <summary>
        /// Held back a little where nothing was measured, so a number the curve is winding down through does not read
        /// as one taken off an object.
        /// </summary>
        public void SetMeasured(bool measured) => Alpha = measured ? 1 : 0.55f;
    }
}
