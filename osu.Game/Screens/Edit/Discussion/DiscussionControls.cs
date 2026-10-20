// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Localisation;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Localisation;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Edit.Discussion
{
    /// <summary>
    /// The website's tabs over the discussion, each with how many threads it lists (2026-10-20 request).
    /// </summary>
    /// <remarks>
    /// The counts are in the colours the threads themselves are marked in, in the list and on the timeline, so that
    /// a red number reads as what is still waiting without the label being read.
    /// </remarks>
    public partial class DiscussionTabs : CompositeDrawable
    {
        public readonly Bindable<DiscussionFilter> Current = new Bindable<DiscussionFilter>();

        private readonly Dictionary<DiscussionFilter, Tab> tabs = new Dictionary<DiscussionFilter, Tab>();

        public Drawable TabFor(DiscussionFilter filter) => tabs[filter];

        public int CountOf(DiscussionFilter filter) => tabs[filter].Threads;

        [BackgroundDependencyLoader]
        private void load(OsuColour colours, OverlayColourProvider colourProvider)
        {
            AutoSizeAxes = Axes.Both;

            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(16, 0),
            };

            foreach (var filter in Enum.GetValues<DiscussionFilter>())
            {
                var tab = tabs[filter] = new Tab(labelOf(filter), countColourOf(filter, colours, colourProvider))
                {
                    Action = () => Current.Value = filter,
                };

                flow.Add(tab);
            }

            InternalChild = flow;
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindValueChanged(c =>
            {
                foreach (var (filter, tab) in tabs)
                    tab.Selected = filter == c.NewValue;
            }, true);
        }

        public void SetCounts(IReadOnlyDictionary<DiscussionFilter, int> counts)
        {
            foreach (var (filter, tab) in tabs)
                tab.Threads = counts.GetValueOrDefault(filter);
        }

        private static LocalisableString labelOf(DiscussionFilter filter)
        {
            switch (filter)
            {
                case DiscussionFilter.Mine:
                    return DiscussionStrings.FilterMine;

                case DiscussionFilter.Notes:
                    return DiscussionStrings.FilterNotes;

                case DiscussionFilter.Resolved:
                    return DiscussionStrings.FilterResolved;

                case DiscussionFilter.Pending:
                    return DiscussionStrings.FilterPending;

                case DiscussionFilter.Praises:
                    return DiscussionStrings.FilterPraises;

                default:
                    return DiscussionStrings.FilterAll;
            }
        }

        private static Color4 countColourOf(DiscussionFilter filter, OsuColour colours, OverlayColourProvider colourProvider)
        {
            switch (filter)
            {
                case DiscussionFilter.Notes:
                    return colours.Purple;

                case DiscussionFilter.Resolved:
                    return colours.Green;

                case DiscussionFilter.Pending:
                    return colours.Red;

                case DiscussionFilter.Praises:
                    return colours.Blue;

                default:
                    return colourProvider.Content2;
            }
        }

        private partial class Tab : OsuClickableContainer
        {
            private readonly LocalisableString label;
            private readonly Color4 countColour;

            private OsuSpriteText labelText = null!;
            private OsuSpriteText countText = null!;
            private Box underline = null!;

            [Resolved]
            private OverlayColourProvider colourProvider { get; set; } = null!;

            public Tab(LocalisableString label, Color4 countColour)
            {
                this.label = label;
                this.countColour = countColour;

                // before loading: the button sizes what it holds by this as it loads.
                AutoSizeAxes = Axes.Both;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    new FillFlowContainer
                    {
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(4, 0),
                        Padding = new MarginPadding { Bottom = 6 },
                        Children = new Drawable[]
                        {
                            labelText = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Text = label,
                                Font = OsuFont.Default.With(size: 13, weight: FontWeight.SemiBold),
                            },
                            countText = new OsuSpriteText
                            {
                                Anchor = Anchor.BottomLeft,
                                Origin = Anchor.BottomLeft,
                                Font = OsuFont.Default.With(size: 12, weight: FontWeight.Bold),
                                Colour = countColour,
                            },
                        }
                    },
                    underline = new Box
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        RelativeSizeAxes = Axes.X,
                        Height = 2,
                        Colour = colourProvider.Highlight1,
                    },
                };

                updateState();
            }

            private int count;

            public int Threads
            {
                get => count;
                set
                {
                    count = value;

                    if (IsLoaded)
                        updateState();
                }
            }

            private bool selected;

            public bool Selected
            {
                get => selected;
                set
                {
                    selected = value;

                    if (IsLoaded)
                        updateState();
                }
            }

            private void updateState()
            {
                countText.Text = count.ToString();
                // nothing there is not something to look at.
                countText.Alpha = count > 0 ? 1 : 0.4f;

                labelText.Colour = selected ? colourProvider.Content1 : colourProvider.Foreground1;
                underline.Alpha = selected ? 1 : 0;
            }
        }
    }

    /// <summary>
    /// Whose threads are listed: everyone's, or one person's.
    /// </summary>
    public partial class UserDropdown : OsuDropdown<int>
    {
        /// <summary>
        /// Everyone's, which is no user's id.
        /// </summary>
        public const int EVERYONE = 0;

        private readonly IReadOnlyDictionary<int, string> names;

        public UserDropdown(IReadOnlyDictionary<int, string> names)
        {
            this.names = names;
            Items = new[] { EVERYONE };
        }

        /// <summary>
        /// Lists who started the threads, by name.
        /// </summary>
        public void SetUsers(IEnumerable<int> userIds)
        {
            var ids = userIds.Distinct().OrderBy(id => names.GetValueOrDefault(id, @"?"), StringComparer.OrdinalIgnoreCase).ToArray();

            // whoever was picked is not among them after a reload that found none of theirs.
            if (!ids.Contains(Current.Value))
                Current.Value = EVERYONE;

            Items = ids.Prepend(EVERYONE);
        }

        protected override LocalisableString GenerateItemText(int item)
            => item == EVERYONE ? DiscussionStrings.EveryUser : names.GetValueOrDefault(item, @"?");
    }

    /// <summary>
    /// The two orders the list can be read in, side by side so that either is one click away.
    /// </summary>
    public partial class DiscussionSortSwitch : CompositeDrawable
    {
        public readonly Bindable<DiscussionSort> Current = new Bindable<DiscussionSort>();

        private readonly Dictionary<DiscussionSort, OsuSpriteText> options = new Dictionary<DiscussionSort, OsuSpriteText>();

        [Resolved]
        private OverlayColourProvider colourProvider { get; set; } = null!;

        public Drawable OptionFor(DiscussionSort sort) => options[sort].Parent!;

        [BackgroundDependencyLoader]
        private void load()
        {
            AutoSizeAxes = Axes.Both;

            InternalChild = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(12, 0),
                Children = new[]
                {
                    option(DiscussionSort.Default, DiscussionStrings.SortDefault),
                    option(DiscussionSort.Updated, DiscussionStrings.SortUpdated),
                }
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindValueChanged(c =>
            {
                foreach (var (sort, text) in options)
                {
                    bool selected = sort == c.NewValue;

                    text.Colour = selected ? colourProvider.Content1 : colourProvider.Foreground1;
                    text.Font = OsuFont.Default.With(size: 12, weight: selected ? FontWeight.Bold : FontWeight.Regular);
                }
            }, true);
        }

        private Drawable option(DiscussionSort sort, LocalisableString label) => new OsuClickableContainer
        {
            AutoSizeAxes = Axes.Both,
            Action = () => Current.Value = sort,
            Child = options[sort] = new OsuSpriteText
            {
                Text = label,
                Font = OsuFont.Default.With(size: 12),
            },
        };
    }
}
