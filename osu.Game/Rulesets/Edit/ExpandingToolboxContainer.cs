// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Configuration;
using osu.Game.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Overlays;
using osu.Game.Screens.Edit;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Edit
{
    public partial class ExpandingToolboxContainer : ExpandingContainer
    {
        protected override double HoverExpansionDelay => 250;

        protected override bool ExpandOnHover => expandOnHover;

        private readonly Bindable<bool> contractSidebars = new Bindable<bool>();

        /// <summary>
        /// When set, this toolbox stays expanded regardless of the contracting behaviour applied to the others.
        /// </summary>
        public readonly Bindable<bool> AlwaysExpanded = new Bindable<bool>();

        private bool expandOnHover;
        private OffsetMaintainingScrollContainer scrollContainer = null!;

        [Resolved]
        private Editor? editor { get; set; }

        public ExpandingToolboxContainer(float contractedWidth, float expandedWidth)
            : base(contractedWidth, expandedWidth)
        {
            RelativeSizeAxes = Axes.Y;

            FillFlow.Spacing = new Vector2(5);
            FillFlow.Padding = new MarginPadding { Vertical = 5 };

            Expanded.Value = true;
        }

        /// <summary>
        /// Holds groups the user pinned, above the scrolling area so they stay put.
        /// </summary>
        private FillFlowContainer pinnedArea = null!;

        private Container pinnedContainer = null!;

        [Resolved(CanBeNull = true)]
        private OverlayColourProvider? colourProvider { get; set; }

        /// <summary>
        /// The groups this toolbox lays out, in the order it lays them out.
        /// </summary>
        private readonly List<ToolboxEntry> entries = new List<ToolboxEntry>();

        private readonly Bindable<string> pinnedGroups = new Bindable<string>(string.Empty);

        private readonly Bindable<EditorPinnedGroupOrder> pinnedGroupOrder = new Bindable<EditorPinnedGroupOrder>();

        private int nextPinSequence;

        private const char pinned_separator = '|';

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorContractSidebars, contractSidebars);
            config.BindWith(OsuSetting.EditorPinnedToolboxGroups, pinnedGroups);
            config.BindWith(OsuSetting.EditorPinnedGroupOrder, pinnedGroupOrder);

            AddInternal(pinnedContainer = new Container
            {
                Name = @"Pinned groups",
                Depth = -1,
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Children = new Drawable[]
                {
                    // pinned groups sit over the scrolling ones, so they need something solid behind them.
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = colourProvider?.Background5 ?? Color4.Black,
                    },
                    pinnedArea = new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = FillFlow.Spacing,
                        Padding = new MarginPadding { Top = 5 },
                    },
                }
            });
        }

        protected override OsuScrollContainer CreateScrollContainer() => scrollContainer = new OffsetMaintainingScrollContainer();

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var inputManager = GetContainingInputManager();

            if (inputManager != null)
            {
                Expanded.BindValueChanged(_ =>
                {
                    // When state changes from expanded -> collapsed the mouse is no longer within the toolbox so there would be no
                    // hovered children if we used the mouse position directly
                    var position = new Vector2(ScreenSpaceDrawQuad.Centre.X, inputManager.CurrentState.Mouse.Position.Y);

                    scrollContainer.TargetDrawable = Children.FirstOrDefault(it => it.Contains(position));
                });
            }

            // a group may sit inside a container of its own, in which case that container is what the toolbox lays
            // out and what has to move when the group is pinned.
            foreach (var child in Children.ToArray())
            {
                var group = child as EditorToolboxGroup ?? child.ChildrenOfType<EditorToolboxGroup>().FirstOrDefault();

                if (group != null)
                    entries.Add(new ToolboxEntry(group, child));
            }

            // a flow container falls back to the order it first saw its children in, which drops anything taken out
            // and put back at the bottom. giving every entry a position of its own is what lets one return to its place.
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].LayoutPosition = i;
                FillFlow.SetLayoutPosition(entries[i].Drawable, i);
            }

            // restored in the order they were written, which is the order they were pinned in.
            foreach (string title in pinnedGroups.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries))
            {
                var restored = entries.FirstOrDefault(e => e.Group.Title == title);

                if (restored == null)
                    continue;

                restored.Group.Pinned.Value = true;
                setPinned(restored, true);
            }

            foreach (var entry in entries)
            {
                var captured = entry;

                entry.Group.Pinned.BindValueChanged(pinned =>
                {
                    setPinned(captured, pinned.NewValue);
                    savePinnedState();
                });
            }

            pinnedGroupOrder.BindValueChanged(_ => updatePinnedOrder());
        }

        /// <summary>
        /// Writes this toolbox's pinned groups back to config.
        /// </summary>
        /// <remarks>
        /// Both toolboxes share one setting, so entries belonging to groups this container does not own are left
        /// untouched rather than overwritten.
        /// </remarks>
        private void savePinnedState()
        {
            var ownTitles = entries.Select(e => e.Group.Title).ToHashSet();

            var stored = pinnedGroups.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries)
                                     .Where(title => !ownTitles.Contains(title))
                                     .ToList();

            // written in the order they were pinned, which is the only place that order is kept.
            stored.AddRange(entries.Where(e => e.Group.Pinned.Value)
                                   .OrderBy(e => e.PinSequence)
                                   .Select(e => e.Group.Title));

            pinnedGroups.Value = string.Join(pinned_separator, stored);
        }

        private void setPinned(ToolboxEntry entry, bool pinned)
        {
            // membership is tested by parent rather than `Contains`, which throws outright when the drawable belongs
            // to a different container.
            if (pinned)
            {
                if (ReferenceEquals(entry.Drawable.Parent, FillFlow))
                {
                    entry.PinSequence = nextPinSequence++;

                    FillFlow.Remove(entry.Drawable, false);
                    pinnedArea.Add(entry.Drawable);
                }
            }
            else if (ReferenceEquals(entry.Drawable.Parent, pinnedArea))
            {
                pinnedArea.Remove(entry.Drawable, false);
                FillFlow.Add(entry.Drawable);
                FillFlow.SetLayoutPosition(entry.Drawable, entry.LayoutPosition);
            }

            updatePinnedOrder();
        }

        private void updatePinnedOrder()
        {
            foreach (var entry in entries)
            {
                if (!ReferenceEquals(entry.Drawable.Parent, pinnedArea))
                    continue;

                pinnedArea.SetLayoutPosition(entry.Drawable, pinnedGroupOrder.Value == EditorPinnedGroupOrder.OrderPinned
                    ? entry.PinSequence
                    : entry.LayoutPosition);
            }
        }

        /// <summary>
        /// One group in the toolbox, together with what the toolbox actually lays out for it.
        /// </summary>
        private class ToolboxEntry
        {
            public readonly EditorToolboxGroup Group;

            /// <summary>
            /// The group itself, or the container it is wrapped in.
            /// </summary>
            public readonly Drawable Drawable;

            /// <summary>
            /// Where this sits in the toolbox while nothing is pinned.
            /// </summary>
            public float LayoutPosition;

            /// <summary>
            /// When this was pinned, relative to the others.
            /// </summary>
            public int PinSequence;

            public ToolboxEntry(EditorToolboxGroup group, Drawable drawable)
            {
                Group = group;
                Drawable = drawable;
            }
        }

        protected override void Update()
        {
            base.Update();

            // keeps the scrolling groups clear of whatever is pinned above them.
            scrollContainer.Padding = new MarginPadding { Top = pinnedContainer.DrawHeight };

            bool requireContracting = !AlwaysExpanded.Value && (contractSidebars.Value || editor?.DrawSize.X / editor?.DrawSize.Y < 1.7f);

            if (expandOnHover != requireContracting)
            {
                expandOnHover = requireContracting;
                Expanded.Value = !expandOnHover;
            }
        }

        protected override bool OnMouseDown(MouseDownEvent e) => true;

        protected override bool OnClick(ClickEvent e) => true;

        private partial class OffsetMaintainingScrollContainer : OsuScrollContainer
        {
            private Drawable? targetDrawable;
            private float targetPosition;

            public Drawable? TargetDrawable
            {
                get => targetDrawable;
                set
                {
                    targetDrawable = value;

                    if (value != null)
                        targetPosition = ToLocalSpace(value.ScreenSpaceDrawQuad.TopLeft).Y;
                }
            }

            protected override void UpdateAfterChildren()
            {
                if (targetDrawable != null)
                {
                    float currentPosition = ToLocalSpace(targetDrawable.ScreenSpaceDrawQuad.TopLeft).Y;

                    if (!Precision.AlmostEquals(targetPosition, currentPosition))
                    {
                        double offset = currentPosition - targetPosition;

                        double scrollTarget = Math.Clamp(Current + offset, 0, ScrollableExtent);

                        ScrollTo(scrollTarget, false, double.PositiveInfinity);
                    }
                }

                base.UpdateAfterChildren();
            }

            protected override void OnUserScroll(double value, bool animated = true, double? distanceDecay = null)
            {
                targetDrawable = null;

                base.OnUserScroll(value, animated, distanceDecay);
            }
        }
    }
}
