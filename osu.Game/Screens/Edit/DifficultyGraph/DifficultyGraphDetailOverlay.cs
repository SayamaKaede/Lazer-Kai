// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Game.Input.Bindings;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Extensions;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Screens.Edit.Components.Timelines.Summary.Parts;
using osu.Game.Screens.Play.HUD;
using osu.Game.Utils;
using osuTK;
using osuTK.Graphics;
using osuTK.Input;

namespace osu.Game.Screens.Edit.DifficultyGraph
{
    /// <summary>
    /// A room-sized view of what makes a beatmap hard and where.
    /// </summary>
    /// <remarks>
    /// The summary timeline's curve says where the work is; this says what the work is made of. They are the same
    /// numbers — the bands here add up to the height there — so one can be read against the other.
    ///
    /// Modal, unlike the combo colour studio beside it in the menus. That one is built by listening to the song and
    /// watching the beatmap, so it leaves the editor usable underneath. This one is opened, read, and closed.
    /// </remarks>
    public partial class DifficultyGraphDetailOverlay : VisibilityContainer, IKeyBindingHandler<GlobalAction>
    {
        /// <summary>
        /// How tall a single skill's row is.
        /// </summary>
        private const float skill_row_height = 46;

        [Resolved]
        private EditorBeatmap editorBeatmap { get; set; } = null!;

        [Resolved]
        private EditorClock editorClock { get; set; } = null!;

        [Resolved]
        private IBindable<WorkingBeatmap> workingBeatmap { get; set; } = null!;

        [Resolved]
        private Bindable<RulesetInfo> ruleset { get; set; } = null!;

        // not always present: the editor's components are built in test scenes without a whole editor around them.
        [Resolved(canBeNull: true)]
        private Editor? editor { get; set; }

        [Resolved(canBeNull: true)]
        private EditorDifficultyTracker? difficultyTracker { get; set; }

        // a number rather than a plain bindable so the slider in the footer has a range to work against.
        private readonly BindableFloat detail = new BindableFloat(DifficultyGraphPart.DEFAULT_DETAIL);
        private readonly Bindable<bool> showSkills = new Bindable<bool>(true);
        private readonly Bindable<bool> applyMods = new Bindable<bool>();

        private StackedGraph stacked = null!;
        private FillFlowContainer skillRows = null!;
        private SpriteIcon foldIcon = null!;
        private OsuSpriteText starRating = null!;
        private ModDisplay modDisplay = null!;

        private CancellationTokenSource? calculation;
        private DifficultyBreakdown? breakdown;

        private Color4[] skillColours = Array.Empty<Color4>();

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config, OsuColour colours, OverlayColourProvider colourProvider)
        {
            RelativeSizeAxes = Axes.Both;

            // the bar at the bottom is left out of the shade. Scrubbing the timeline while reading the graph is the
            // one thing worth keeping hold of, and it is the part of the editor that says where you are rather than
            // changing what is there — nothing can be edited by mistake through it.
            Padding = new MarginPadding { Bottom = BottomBar.HEIGHT };

            config.BindWith(OsuSetting.EditorDifficultyGraphOverlayDetail, detail);
            config.BindWith(OsuSetting.EditorDifficultyGraphOverlayShowSkills, showSkills);
            config.BindWith(OsuSetting.EditorDifficultyGraphOverlayApplyMods, applyMods);

            // in the order the osu! ruleset builds its skills: aim, then speed, then reading.
            skillColours = new[] { colourProvider.Highlight1, colours.Lime1, colours.Orange1, colours.Pink1, colours.Blue1 };

            Child = new InputBlockingContainer
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = Color4.Black,
                        Alpha = 0.7f,
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 64, Vertical = 48 },
                        Child = new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Masking = true,
                            CornerRadius = 6,
                            Children = new Drawable[]
                            {
                                new Box
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Colour = colourProvider.Background4,
                                },
                                new GridContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    RowDimensions = new[]
                                    {
                                        new Dimension(GridSizeMode.Absolute, 58),
                                        new Dimension(),
                                        new Dimension(GridSizeMode.AutoSize),
                                        new Dimension(GridSizeMode.Absolute, 56),
                                    },
                                    Content = new[]
                                    {
                                        new Drawable[] { createHeader(colourProvider, colours) },
                                        new Drawable[] { createGraph(colourProvider) },
                                        new Drawable[] { createSkillRows(colourProvider) },
                                        new Drawable[] { createFooter(colourProvider) },
                                    }
                                }
                            }
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Swallows everything the mouse does, so the editor underneath is not operated by accident while it cannot
        /// be seen.
        /// </summary>
        /// <remarks>
        /// Sits inside the overlay's padding rather than being the overlay itself, which is what leaves the bottom
        /// bar reachable: what is not covered is not blocked.
        /// </remarks>
        private partial class InputBlockingContainer : Container
        {
            protected override bool OnMouseDown(MouseDownEvent e) => true;

            protected override bool OnClick(ClickEvent e) => true;

            protected override bool OnHover(HoverEvent e) => true;

            protected override bool OnDragStart(DragStartEvent e) => true;

            protected override bool OnScroll(ScrollEvent e) => true;
        }

        private Drawable createHeader(OverlayColourProvider colourProvider, OsuColour colours) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 1,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Colour = colourProvider.Background2,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(14, 0),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 26 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = EditorStrings.DifficultyGraphDetailScreen,
                            Font = OsuFont.Default.With(size: 18, weight: FontWeight.SemiBold),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new OsuSpriteText
                        {
                            Text = editorBeatmap.BeatmapInfo.GetDisplayTitle(),
                            Font = OsuFont.Default.With(size: 13),
                            Colour = colourProvider.Content2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(8, 0),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 26 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = @"SR",
                            Font = OsuFont.Default.With(size: 13),
                            Colour = colourProvider.Content2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        starRating = new OsuSpriteText
                        {
                            Font = OsuFont.Default.With(size: 15, weight: FontWeight.Bold),
                            Colour = colours.Yellow,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new IconButton
                        {
                            Icon = FontAwesome.Solid.Times,
                            Scale = new Vector2(0.8f),
                            Action = Hide,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                }
            }
        };

        private Drawable createGraph(OverlayColourProvider colourProvider) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Padding = new MarginPadding { Horizontal = 26, Top = 26, Bottom = 8 },
            Child = stacked = new StackedGraph(colourProvider)
            {
                RelativeSizeAxes = Axes.Both,
            },
        };

        private Drawable createSkillRows(OverlayColourProvider colourProvider) => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Padding = new MarginPadding { Horizontal = 26, Bottom = 12 },
            Children = new Drawable[]
            {
                new ClickableContainer
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 26,
                    Action = () => showSkills.Value = !showSkills.Value,
                    Children = new Drawable[]
                    {
                        foldIcon = new SpriteIcon
                        {
                            Size = new Vector2(9),
                            Colour = colourProvider.Content2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new OsuSpriteText
                        {
                            Text = EditorStrings.DifficultyGraphPerSkill,
                            Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                            Margin = new MarginPadding { Left = 18 },
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                },
                skillRows = new FillFlowContainer
                {
                    RelativeSizeAxes = Axes.X,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(0, 9),
                },
            }
        };

        private Drawable createFooter(OverlayColourProvider colourProvider) => new Container
        {
            RelativeSizeAxes = Axes.Both,
            Children = new Drawable[]
            {
                new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 1,
                    Colour = colourProvider.Background2,
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(14, 0),
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 26 },
                    Children = new Drawable[]
                    {
                        // the checkbox sizes itself relative to its parent, which a flow that is sizing itself to its
                        // children cannot answer. Given a box of its own to fill, both have something to measure.
                        // the checkbox sizes itself relative to its parent, which a flow that is sizing itself to its
                        // children cannot answer — and the framework throws on the update thread rather than laying
                        // it out badly, which takes the whole editor with it. Given a box of its own to fill, both
                        // have something to measure.
                        new Container
                        {
                            Width = 210,
                            AutoSizeAxes = Axes.Y,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Child = new OsuCheckbox
                            {
                                LabelText = EditorStrings.DifficultyGraphApplyCurrentMods,
                                Current = { BindTarget = applyMods },
                            },
                        },
                        modDisplay = new ModDisplay(showExtendedInformation: false)
                        {
                            Scale = new Vector2(0.4f),
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Horizontal,
                    Spacing = new Vector2(12, 0),
                    Anchor = Anchor.CentreRight,
                    Origin = Anchor.CentreRight,
                    Margin = new MarginPadding { Right = 26 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = EditorStrings.DifficultyGraphDetail,
                            Font = OsuFont.Default.With(size: 12),
                            Colour = colourProvider.Content2,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                        new RoundedSliderBar<float>
                        {
                            Current = detail,
                            Width = 200,
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                }
            }
        };

        protected override void LoadComplete()
        {
            base.LoadComplete();

            showSkills.BindValueChanged(v =>
            {
                foldIcon.Icon = v.NewValue ? FontAwesome.Solid.CaretDown : FontAwesome.Solid.CaretRight;
                skillRows.Alpha = v.NewValue ? 1 : 0;
                skillRows.AlwaysPresent = false;
            }, true);

            detail.BindValueChanged(_ => draw());
            applyMods.BindValueChanged(_ => recalculate());

            if (difficultyTracker != null)
            {
                difficultyTracker.StarRating.BindValueChanged(stars => starRating.Text = stars.NewValue == null
                    ? @"-"
                    : $@"{stars.NewValue.Value.FormatStarRating()}*", true);
            }
            else
                starRating.Text = @"-";

            if (editor != null)
            {
                modDisplay.Current = editor.TestPlayMods.Mods;
                editor.TestPlayMods.Mods.BindValueChanged(_ =>
                {
                    if (applyMods.Value)
                        recalculate();
                });
            }
        }

        protected override void PopIn()
        {
            this.FadeIn(200, Easing.OutQuint);

            // opened deliberately rather than left sitting there, so it is worked out now rather than on a timer.
            recalculate();
        }

        protected override void PopOut() => this.FadeOut(200, Easing.OutQuint);

        protected override void Update()
        {
            base.Update();

            if (State.Value == Visibility.Visible)
                stacked.SetPlayhead(editorClock.CurrentTime);
        }

        /// <summary>
        /// Works the beatmap out again, which is the expensive half.
        /// </summary>
        private void recalculate()
        {
            calculation?.Cancel();
            calculation = new CancellationTokenSource();

            var token = calculation.Token;
            var rulesetInstance = ruleset.Value.CreateInstance();
            var beatmap = new EditorStateWorkingBeatmap(editorBeatmap.PlayableBeatmap);
            var mods = applyMods.Value && editor != null ? editor.TestPlayMods.Mods.Value.ToArray() : Array.Empty<Mod>();

            Task.Run(() =>
            {
                try
                {
                    var calculated = rulesetInstance.CreateDifficultyCalculator(beatmap).CalculateBreakdown(mods, token);

                    Schedule(() =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        breakdown = calculated;
                        draw();
                    });
                }
                catch (OperationCanceledException)
                {
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Could not calculate the difficulty graph");
                }
            }, token);
        }

        /// <summary>
        /// Redraws from the difficulty already worked out, which is what changing how finely it is cut needs.
        /// </summary>
        private void draw()
        {
            if (breakdown == null)
                return;

            double trackLength = workingBeatmap.Value.Track.Length;
            var perSkill = breakdown.GetPerSkillDifficulties();
            int columns = DifficultyGraphPart.ColumnCountFor(detail.Value);

            // everything else with nothing measured in it winds down; a break is drawn as the empty stretch it is.
            var breaks = editorBeatmap.Breaks.Select(b => (b.StartTime, b.EndTime)).ToArray();

            var built = ColumnsFor(breakdown.Times, perSkill.Select(s => s.Difficulties).ToArray(), trackLength, columns, breaks);

            stacked.Set(built, perSkill.Select(s => s.Name).ToArray(), skillColours, trackLength);

            skillRows.Clear();

            for (int i = 0; i < perSkill.Length; i++)
            {
                skillRows.Add(new SkillRow(perSkill[i].Name, skillColours[i % skillColours.Length], built, i)
                {
                    RelativeSizeAxes = Axes.X,
                    Height = skill_row_height,
                });
            }
        }

        /// <summary>
        /// Escape closes the screen rather than leaving the editor.
        /// </summary>
        /// <remarks>
        /// Taken as the global back action rather than as a key, so it follows whatever the user has back bound to
        /// and reads as the same gesture that closes everything else.
        /// </remarks>
        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Action != GlobalAction.Back || State.Value != Visibility.Visible)
                return false;

            Hide();
            return true;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            calculation?.Cancel();
        }
    }
}
