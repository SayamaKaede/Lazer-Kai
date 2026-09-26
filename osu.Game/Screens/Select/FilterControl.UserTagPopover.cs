// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Rulesets;
using osuTK;

namespace osu.Game.Screens.Select
{
    public sealed partial class FilterControl
    {
        /// <summary>
        /// The user tags on the difficulties song select shows now, by category, each adding itself to the search or taking
        /// itself out of it. What a tag would leave nothing shown for is not listed, being on none of them.
        /// </summary>
        public partial class UserTagPopover : OsuPopover
        {
            private const float width = 380;
            private const float max_list_height = 420;

            private readonly FilterControl filterControl;

            private readonly IBindable<IReadOnlyList<BeatmapInfo>> matchedBeatmaps = new Bindable<IReadOnlyList<BeatmapInfo>>();
            private readonly IBindable<string> searchText = new Bindable<string>();
            private Bindable<APITag[]?> allTags = null!;

            private OsuScrollContainer scroll = null!;
            private FillFlowContainer list = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            [Resolved]
            private IBindable<RulesetInfo> ruleset { get; set; } = null!;

            public UserTagPopover(FilterControl filterControl)
            {
                this.filterControl = filterControl;
            }

            [BackgroundDependencyLoader]
            private void load(SessionStatics sessionStatics, IAPIProvider api)
            {
                Child = new FillFlowContainer
                {
                    Width = width,
                    AutoSizeAxes = Axes.Y,
                    Direction = FillDirection.Vertical,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        new OsuSpriteText
                        {
                            Text = LazerKaiSongSelectStrings.UserTags,
                            Font = OsuFont.Default.With(weight: FontWeight.SemiBold),
                        },
                        new OsuTextFlowContainer(t =>
                        {
                            t.Font = OsuFont.Default.With(size: 13);
                            t.Colour = colourProvider.Content2;
                        })
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Text = LazerKaiSongSelectStrings.UserTagCounts,
                        },
                        scroll = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            Child = list = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.X,
                                AutoSizeAxes = Axes.Y,
                                Direction = FillDirection.Vertical,
                                Spacing = new Vector2(8),
                            },
                        },
                    },
                };

                // the descriptions are only had online, so a tag is listed without one until they come.
                allTags = sessionStatics.GetBindable<APITag[]?>(Static.AllBeatmapTags);

                if (allTags.Value == null)
                {
                    var request = new ListTagsRequest();
                    request.Success += tags => allTags.Value = tags.Tags.ToArray();
                    api.Queue(request);
                }
            }

            protected override void LoadComplete()
            {
                base.LoadComplete();

                matchedBeatmaps.BindTo(filterControl.MatchedBeatmaps);
                searchText.BindTo(filterControl.SearchText);

                matchedBeatmaps.BindValueChanged(_ => updateList());
                searchText.BindValueChanged(_ => updateList());
                allTags.BindValueChanged(_ => updateList());
                updateList();

                // so that escape closes this rather than clearing the search.
                ScheduleAfterChildren(() => GetContainingFocusManager()?.ChangeFocus(this));
            }

            protected override void Update()
            {
                base.Update();
                scroll.Height = Math.Min(list.DrawHeight, max_list_height);
            }

            private void updateList()
            {
                var counts = new Dictionary<string, int>();

                foreach (var beatmap in matchedBeatmaps.Value)
                {
                    foreach (string tag in beatmap.Metadata.UserTags.Distinct())
                        counts[tag] = counts.GetValueOrDefault(tag) + 1;
                }

                // one in the search stays listed even when nothing is shown, to be taken out again.
                foreach (string tag in filterControl.SearchedUserTags())
                    counts.TryAdd(tag, 0);

                // the official list has a tag of one name for each ruleset it is had in, so the one for the ruleset shown is preferred.
                var descriptions = (allTags.Value ?? Array.Empty<APITag>())
                                   .GroupBy(t => t.Name)
                                   .ToDictionary(g => g.Key, g => (g.FirstOrDefault(t => t.RulesetId == ruleset.Value.OnlineID)
                                                                   ?? g.FirstOrDefault(t => t.RulesetId == null)
                                                                   ?? g.First()).Description);

                list.Clear();

                if (counts.Count == 0)
                {
                    list.Add(new OsuTextFlowContainer(t => t.Font = OsuFont.Default.With(size: 14))
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Text = LazerKaiSongSelectStrings.NoUserTags,
                    });
                    return;
                }

                foreach (var category in counts.Keys.GroupBy(categoryOf).OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
                {
                    var tags = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Full,
                        Spacing = new Vector2(4),
                    };

                    // by name rather than by count, so that a tag stays where it was as the counts change.
                    foreach (string tag in category.OrderBy(t => t, StringComparer.OrdinalIgnoreCase))
                    {
                        tags.Add(new TagButton(tag, counts[tag], filterControl.IsUserTagSearched(tag))
                        {
                            TooltipText = descriptions.TryGetValue(tag, out string? description) ? description : default(LocalisableString),
                            Action = () => filterControl.ToggleUserTag(tag),
                        });
                    }

                    list.Add(new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(3),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText
                            {
                                Text = category.Key,
                                Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                                Colour = colourProvider.Content2,
                            },
                            tags,
                        }
                    });
                }
            }

            private static string categoryOf(string tag)
            {
                int slash = tag.IndexOf('/');
                return slash > 0 ? tag[..slash] : string.Empty;
            }

            private static string nameInCategory(string tag)
            {
                int slash = tag.IndexOf('/');
                return slash > 0 ? tag[(slash + 1)..] : tag;
            }

            public partial class TagButton : OsuClickableContainer
            {
                public readonly string Tag;
                /// <summary>
                /// How many of the difficulties shown have the tag.
                /// </summary>
                public readonly int Difficulties;
                public readonly bool Searched;

                private readonly Box background;
                private readonly FillFlowContainer text;

                [Resolved]
                private OverlayColourProvider colourProvider { get; set; } = null!;

                public TagButton(string tag, int count, bool searched)
                {
                    Tag = tag;
                    Difficulties = count;
                    Searched = searched;

                    AutoSizeAxes = Axes.Both;
                    Masking = true;
                    CornerRadius = 4;

                    Children = new Drawable[]
                    {
                        background = new Box { RelativeSizeAxes = Axes.Both },
                        text = new FillFlowContainer
                        {
                            AutoSizeAxes = Axes.Both,
                            Direction = FillDirection.Horizontal,
                            Spacing = new Vector2(5, 0),
                            Padding = new MarginPadding { Horizontal = 7, Vertical = 3 },
                            Children = new Drawable[]
                            {
                                new OsuSpriteText
                                {
                                    Text = nameInCategory(tag),
                                    Font = OsuFont.Default.With(size: 14, weight: searched ? FontWeight.Bold : FontWeight.Regular),
                                },
                                new OsuSpriteText
                                {
                                    Text = count.ToString(),
                                    Font = OsuFont.Default.With(size: 12, weight: FontWeight.SemiBold),
                                    Alpha = 0.6f,
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                },
                            }
                        },
                    };
                }

                protected override void LoadComplete()
                {
                    base.LoadComplete();
                    updateState();
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

                private void updateState()
                {
                    if (Searched)
                    {
                        background.Colour = IsHovered ? colourProvider.Highlight1.Lighten(0.2f) : colourProvider.Highlight1;
                        text.Colour = colourProvider.Background6;
                    }
                    else
                    {
                        background.Colour = IsHovered ? colourProvider.Background2 : colourProvider.Background5;
                        text.Colour = colourProvider.Content1;
                    }
                }
            }
        }
    }
}
