// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Localisation;
using osu.Framework.Threading;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Drawables;
using osu.Game.Configuration;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics;
using osu.Game.Graphics.Backgrounds;
using osu.Game.Graphics.Carousel;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osu.Game.Resources.Localisation.Web;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Select
{
    public partial class PanelBeatmapStandalone : Panel
    {
        public const float HEIGHT = CarouselItem.DEFAULT_HEIGHT * 1.6f;

        [Resolved]
        private IBindable<RulesetInfo> ruleset { get; set; } = null!;

        [Resolved]
        private IBindable<IReadOnlyList<Mod>> mods { get; set; } = null!;

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        [Resolved]
        private ISongSelect? songSelect { get; set; }

        [Resolved]
        private BeatmapManager beatmaps { get; set; } = null!;

        [Resolved]
        private BeatmapDifficultyCache difficultyCache { get; set; } = null!;

        private IBindable<StarDifficulty>? starDifficultyBindable;
        private CancellationTokenSource? starDifficultyCancellationSource;

        private PanelSetBackground beatmapBackground = null!;
        private ScheduledDelegate? scheduledBackgroundRetrieval;

        private OsuSpriteText titleText = null!;
        private OsuSpriteText artistText = null!;
        private PanelUpdateBeatmapButton updateButton = null!;
        private BeatmapSetOnlineStatusPill statusPill = null!;

        private ConstrainedIconContainer difficultyIcon = null!;
        private StarRatingDisplay starRatingDisplay = null!;
        private SpreadDisplay spreadDisplay = null!;
        private PanelLocalRankDisplay localRank = null!;
        private OsuSpriteText keyCountText = null!;
        private OsuSpriteText difficultyText = null!;
        private OsuSpriteText authorText = null!;
        private PanelSaveTime saveTime = null!;

        private Container stackedBackground = null!;
        private Box stackedDifficultyTint = null!;
        private TrianglesV2 stackedTriangles = null!;
        private OsuSpriteText stackedDifficultyText = null!;
        private PanelLocalRankDisplay stackedLocalRank = null!;
        private bool? showingStacked;
        private FillFlowContainer mainFill = null!;
        private DifficultyModIcons modIcons = null!;

        private Box backgroundBorder = null!;

        private BeatmapInfo beatmap => ((GroupedBeatmap)Item!.Model).Beatmap;

        [Resolved]
        private OsuConfigManager config { get; set; } = null!;

        protected override void Export() => beatmaps.ExportBeatmapFile(beatmap, config.Get<BeatmapExportFormat>(OsuSetting.BeatmapExportFormat));

        protected override bool Edit() => songSelect?.EditDifficulty(beatmap) == true;


        public PanelBeatmapStandalone()
        {
            PanelXOffset = 20;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Height = HEIGHT;

            Icon = difficultyIcon = new ConstrainedIconContainer
            {
                Size = new Vector2(12),
                Margin = new MarginPadding { Left = 4f, Right = 3f },
                Colour = colourProvider.Background5,
            };

            Background = backgroundBorder = new Box
            {
                RelativeSizeAxes = Axes.Both,
            };

            Content.Children = new Drawable[]
            {
                beatmapBackground = new PanelSetBackground(),
                // tucked behind another of its set, it is drawn the way a difficulty under a set is: the set's picture
                // repeated down a stack flickers past and says nothing the panel heading it does not (2026-10-19).
                stackedBackground = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Alpha = 0,
                    Children = new Drawable[]
                    {
                        new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                            Colour = ColourInfo.GradientHorizontal(colourProvider.Background3, colourProvider.Background4),
                        },
                        stackedDifficultyTint = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                        stackedTriangles = new TrianglesV2
                        {
                            ScaleAdjust = 1.2f,
                            Thickness = 0.01f,
                            Velocity = 0.3f,
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                },
                new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Spacing = new Vector2(5),
                    Margin = new MarginPadding { Left = 6.5f },
                    Direction = FillDirection.Horizontal,
                    Children = new Drawable[]
                    {
                        localRank = new PanelLocalRankDisplay
                        {
                            Scale = new Vector2(0.8f),
                            Origin = Anchor.CentreLeft,
                            Anchor = Anchor.CentreLeft,
                        },
                        mainFill = new FillFlowContainer
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding { Bottom = 4.8f },
                            AutoSizeAxes = Axes.Both,
                            Children = new Drawable[]
                            {
                                titleText = new OsuSpriteText
                                {
                                    Font = OsuFont.Style.Heading2.With(typeface: Typeface.TorusAlternate, weight: FontWeight.Bold),
                                },
                                artistText = new OsuSpriteText
                                {
                                    Font = OsuFont.Style.Caption1.With(weight: FontWeight.SemiBold),
                                    Padding = new MarginPadding { Top = -2 },
                                },
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Horizontal,
                                    AutoSizeAxes = Axes.Both,
                                    Padding = new MarginPadding { Top = 2, Bottom = 2 },
                                    Children = new Drawable[]
                                    {
                                        statusPill = new BeatmapSetOnlineStatusPill
                                        {
                                            Animated = false,
                                            Origin = Anchor.BottomLeft,
                                            Anchor = Anchor.BottomLeft,
                                            TextSize = OsuFont.Style.Caption2.Size,
                                            Margin = new MarginPadding { Right = 4f },
                                        },
                                        updateButton = new PanelUpdateBeatmapButton
                                        {
                                            Scale = new Vector2(0.8f),
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Margin = new MarginPadding { Right = 4f, Bottom = -1f },
                                        },
                                        keyCountText = new OsuSpriteText
                                        {
                                            Font = OsuFont.Style.Body.With(weight: FontWeight.SemiBold),
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Alpha = 0,
                                        },
                                        difficultyText = new OsuSpriteText
                                        {
                                            Font = OsuFont.Style.Body.With(weight: FontWeight.SemiBold),
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Margin = new MarginPadding { Right = 3f },
                                        },
                                        authorText = new OsuSpriteText
                                        {
                                            Colour = colourProvider.Content2,
                                            Font = OsuFont.Style.Caption1.With(weight: FontWeight.SemiBold),
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft
                                        },
                                        saveTime = new PanelSaveTime
                                        {
                                            Colour = colourProvider.Highlight1,
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            Margin = new MarginPadding { Left = 8f },
                                        },
                                    }
                                },
                                new FillFlowContainer
                                {
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(3),
                                    AutoSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        // the rank beside the panel is behind the one above while stacked, and only its room was left showing.
                                        stackedLocalRank = new PanelLocalRankDisplay
                                        {
                                            Scale = new Vector2(0.8f),
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Alpha = 0,
                                        },
                                        starRatingDisplay = new StarRatingDisplay(default, StarRatingDisplaySize.Small, animated: true)
                                        {
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Scale = new Vector2(0.875f),
                                        },
                                        // the line above, with the name, is hidden while stacked.
                                        stackedDifficultyText = new OsuSpriteText
                                        {
                                            Font = OsuFont.Style.Body.With(weight: FontWeight.SemiBold),
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Margin = new MarginPadding { Horizontal = 2 },
                                            Alpha = 0,
                                        },
                                        spreadDisplay = new SpreadDisplay
                                        {
                                            Origin = Anchor.CentreLeft,
                                            Anchor = Anchor.CentreLeft,
                                            Selected = { BindTarget = Selected },
                                        }
                                    },
                                }
                            }
                        },
                        modIcons = new DifficultyModIcons
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.CentreLeft,
                        },
                    }
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            ruleset.BindValueChanged(_ => updateKeyCount());
            mods.BindValueChanged(_ => updateKeyCount(), true);

            Selected.BindValueChanged(s =>
            {
                Expanded.Value = s.NewValue;
            }, true);
        }

        protected override void PrepareForUse()
        {
            base.PrepareForUse();

            var beatmapSet = beatmap.BeatmapSet!;

            scheduledBackgroundRetrieval = Scheduler.AddDelayed(b => beatmapBackground.Beatmap = beatmaps.GetWorkingBeatmap(b), beatmap, 50);

            titleText.Text = new RomanisableString(beatmapSet.Metadata.TitleUnicode, beatmapSet.Metadata.Title);
            artistText.Text = new RomanisableString(beatmapSet.Metadata.ArtistUnicode, beatmapSet.Metadata.Artist);
            updateButton.BeatmapSet = beatmapSet;
            statusPill.Status = beatmap.Status;

            difficultyIcon.Icon = beatmap.Ruleset.CreateInstance().CreateIcon();
            difficultyIcon.Show();

            localRank.Beatmap = beatmap;
            stackedLocalRank.Beatmap = beatmap;
            difficultyText.Text = beatmap.DifficultyName;
            authorText.Text = BeatmapsetsStrings.ShowDetailsMappedBy(beatmap.Metadata.Author.Username);
            saveTime.Saved = beatmap;
            stackedDifficultyText.Text = beatmap.DifficultyName;
            showingStacked = null;
            modIcons.Beatmap = beatmap;

            computeStarRating();
            spreadDisplay.Beatmap.Value = beatmap;
            updateKeyCount();
        }

        protected override void FreeAfterUse()
        {
            base.FreeAfterUse();

            scheduledBackgroundRetrieval?.Cancel();
            scheduledBackgroundRetrieval = null;
            beatmapBackground.Beatmap = null;
            updateButton.BeatmapSet = null;
            localRank.Beatmap = null;
            stackedLocalRank.Beatmap = null;
            modIcons.Beatmap = null;
            starDifficultyBindable = null;
            spreadDisplay.Beatmap.Value = null;

            starDifficultyCancellationSource?.Cancel();
        }

        private void computeStarRating()
        {
            starDifficultyCancellationSource?.Cancel();
            starDifficultyCancellationSource = new CancellationTokenSource();

            if (Item == null)
                return;

            starDifficultyBindable = difficultyCache.GetBindableDifficulty(beatmap, starDifficultyCancellationSource.Token, SongSelect.DIFFICULTY_CALCULATION_DEBOUNCE);
            starDifficultyBindable.BindValueChanged(starDifficulty =>
            {
                starRatingDisplay.Current.Value = starDifficulty.NewValue;
                spreadDisplay.StarDifficulty.Value = starDifficulty.NewValue;
            }, true);
        }

        protected override void Update()
        {
            base.Update();

            if (Item?.IsVisible != true)
            {
                starDifficultyCancellationSource?.Cancel();
                starDifficultyCancellationSource = null;
            }

            // Dirty hack to make sure we don't take up spacing in parent fill flow when not displaying a rank.
            // I can't find a better way to do this.
            // (and none at all for a stacked panel, whose rank is not beside it to space from.)
            mainFill.Margin = new MarginPadding { Left = 1 / starRatingDisplay.Scale.X * (localRank.HasRank || showingStacked == true ? 0 : -3) };

            var diffColour = starRatingDisplay.DisplayedDifficultyColour;

            AccentColour = diffColour;
            spreadDisplay.Current.Colour = diffColour;

            backgroundBorder.Colour = diffColour;
            difficultyIcon.Colour = starRatingDisplay.DisplayedDifficultyTextColour;

            updateStacked(diffColour);
        }

        protected override bool StaysInPlace => showingStacked == true;

        private void updateStacked(Color4 diffColour)
        {
            bool stacked = Item?.StackLevel > 0;

            if (stacked != showingStacked)
            {
                // straight away for a panel just taken from the pool, which is not changing in front of anybody.
                double duration = showingStacked == null ? 0 : DURATION / 2;
                showingStacked = stacked;

                stackedBackground.FadeTo(stacked ? 1 : 0, duration, Easing.OutQuint);
                stackedDifficultyText.FadeTo(stacked ? 1 : 0, duration, Easing.OutQuint);

                // at once: either takes room in its line while it is there at all.
                localRank.Alpha = stacked ? 0 : 1;

                RefreshXOffset(animated: duration > 0);
            }

            // without a rank it would still be spaced from the star rating.
            stackedLocalRank.Alpha = stacked && stackedLocalRank.HasRank ? 1 : 0;

            if (stacked)
            {
                stackedDifficultyTint.Colour = ColourInfo.GradientHorizontal(diffColour.Opacity(0.25f), diffColour.Opacity(0f));
                stackedTriangles.Colour = ColourInfo.GradientVertical(diffColour.Opacity(0.25f), diffColour.Opacity(0f));
            }
        }

        private void updateKeyCount()
        {
            if (Item == null)
                return;

            var rulesetInstance = ruleset.Value.CreateInstance();

            if (rulesetInstance.GameplayVariants.Count() > 1)
            {
                int variant = rulesetInstance.GetVariantForBeatmap(beatmap, mods.Value);
                var variantName = rulesetInstance.GetVariantName(variant);

                keyCountText.Alpha = 1;
                keyCountText.Text = LocalisableString.Interpolate($"[{variantName}] ");
            }
            else
                keyCountText.Alpha = 0;
        }

        public override MenuItem[] ContextMenuItems
        {
            get
            {
                if (Item == null)
                    return Array.Empty<MenuItem>();

                List<MenuItem> items = new List<MenuItem>();

                if (songSelect != null)
                {
                    items.AddRange(songSelect.GetForwardActions(beatmap));
                    items.AddRange(songSelect.GetDifficultyModActions(beatmap));
                }

                items.Add(new OsuMenuItemSpacer());
                items.Add(new OsuMenuItem(BeatmapExportStrings.ExportDifficulty, MenuItemType.Standard, Export)
                {
                    Icon = FontAwesome.Solid.FileExport
                });

                // the update mark waits to be told the beatmap changed; this asks now.
                if (beatmap.BeatmapSet is BeatmapSetInfo beatmapSet && beatmapSet.OnlineID > 0)
                {
                    items.Add(new OsuMenuItem(LazerKaiSongSelectStrings.CheckForUpdate, MenuItemType.Standard, () => songSelect?.CheckForUpdate(beatmapSet))
                    {
                        Icon = FontAwesome.Solid.SyncAlt
                    });
                }

                // the game otherwise only deletes whole sets outside the editor.
                if (beatmap.BeatmapSet?.Beatmaps.Count > 1)
                    items.Add(new OsuMenuItem(LazerKaiSongSelectStrings.DeleteDifficulty, MenuItemType.Destructive, () => songSelect?.DeleteDifficulty(beatmap)));

                return items.ToArray();
            }
        }
    }
}
