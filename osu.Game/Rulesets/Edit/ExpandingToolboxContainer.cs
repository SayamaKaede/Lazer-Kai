// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics.Containers;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Input;
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

        private readonly Bindable<string> collapsedGroups = new Bindable<string>(string.Empty);

        private readonly Bindable<string> groupOrder = new Bindable<string>(string.Empty);

        private const char pinned_separator = '|';

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager config)
        {
            config.BindWith(OsuSetting.EditorContractSidebars, contractSidebars);
            config.BindWith(OsuSetting.EditorPinnedToolboxGroups, pinnedGroups);
            config.BindWith(OsuSetting.EditorCollapsedToolboxGroups, collapsedGroups);
            config.BindWith(OsuSetting.EditorToolboxGroupOrder, groupOrder);

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

        private InputManager? inputManager;

        /// <summary>
        /// Whether the cursor was inside the toolbox last frame.
        /// </summary>
        private bool cursorWasInside;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager();

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

            // groups the user has not moved keep the order the toolbox built them in.
            var storedOrder = groupOrder.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries).ToList();
            var asBuilt = entries.ToList();

            var reordered = entries.OrderBy(e =>
                             {
                                 int at = storedOrder.IndexOf(e.Group.Title);
                                 return at < 0 ? int.MaxValue : at;
                             })
                             .ThenBy(asBuilt.IndexOf)
                             .ToList();

            entries.Clear();
            entries.AddRange(reordered);

            applyOrder();

            foreach (var entry in entries)
            {
                var captured = entry;
                entry.Group.RequestReorder = direction => move(captured, direction);
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

            var storedCollapsed = collapsedGroups.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries).ToHashSet();

            foreach (var entry in entries)
            {
                var captured = entry;

                entry.Group.Pinned.BindValueChanged(pinned =>
                {
                    setPinned(captured, pinned.NewValue);
                    savePinnedState();
                });

                // set before binding, so restoring a group's state is not itself written back as a change.
                entry.Group.Expanded.Value = !storedCollapsed.Contains(entry.Group.Title);
                entry.Group.Expanded.BindValueChanged(_ => saveCollapsedState());
            }

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

            stored.AddRange(entries.Where(e => e.Group.Pinned.Value).Select(e => e.Group.Title));

            pinnedGroups.Value = string.Join(pinned_separator, stored);
        }

        /// <summary>
        /// Writes this toolbox's folded groups back to config, leaving the other toolbox's entries alone the way the
        /// pinned state does.
        /// </summary>
        private void saveCollapsedState()
        {
            var ownTitles = entries.Select(e => e.Group.Title).ToHashSet();

            var stored = collapsedGroups.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries)
                                        .Where(title => !ownTitles.Contains(title))
                                        .ToList();

            stored.AddRange(entries.Where(e => !e.Group.Expanded.Value).Select(e => e.Group.Title));

            collapsedGroups.Value = string.Join(pinned_separator, stored);
        }

        /// <summary>
        /// Swaps a group with the one above (-1) or below (+1) it.
        /// </summary>
        private void move(ToolboxEntry entry, int direction)
        {
            int from = entries.IndexOf(entry);
            int to = from + direction;

            if (from < 0 || to < 0 || to >= entries.Count)
                return;

            entries.RemoveAt(from);
            entries.Insert(to, entry);

            applyOrder();
            saveOrder();
        }

        /// <summary>
        /// Hands the current order to the layout, both for the groups still scrolling and for whatever is pinned.
        /// </summary>
        private void applyOrder()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                entries[i].LayoutPosition = i;

                if (ReferenceEquals(entries[i].Drawable.Parent, FillFlow))
                    FillFlow.SetLayoutPosition(entries[i].Drawable, i);

                entries[i].Group.CanMoveUp.Value = i > 0;
                entries[i].Group.CanMoveDown.Value = i < entries.Count - 1;
            }

            updatePinnedOrder();
        }

        /// <summary>
        /// Writes this toolbox's order back to config, leaving the other toolbox's entries alone the way the pinned
        /// and folded states do.
        /// </summary>
        private void saveOrder()
        {
            var ownTitles = entries.Select(e => e.Group.Title).ToHashSet();

            var stored = groupOrder.Value.Split(pinned_separator, StringSplitOptions.RemoveEmptyEntries)
                                   .Where(title => !ownTitles.Contains(title))
                                   .ToList();

            stored.AddRange(entries.Select(e => e.Group.Title));

            groupOrder.Value = string.Join(pinned_separator, stored);
        }

        private void setPinned(ToolboxEntry entry, bool pinned)
        {
            // membership is tested by parent rather than `Contains`, which throws outright when the drawable belongs
            // to a different container.
            if (pinned)
            {
                if (ReferenceEquals(entry.Drawable.Parent, FillFlow))
                {
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

                pinnedArea.SetLayoutPosition(entry.Drawable, entry.LayoutPosition);
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
            //
            // this is padding rather than a shorter scrolling area on purpose. Shortening it does stop a group being
            // scrolled behind the pinned ones, but it also costs that much room at the bottom, which made checking
            // whatever hangs off the end harder than the problem it solved. Tried and reverted on 2026-08-23.
            scrollContainer.Padding = new MarginPadding { Top = pinnedContainer.DrawHeight };

            returnToTopOnEntry();

            bool requireContracting = !AlwaysExpanded.Value && (contractSidebars.Value || editor?.DrawSize.X / editor?.DrawSize.Y < 1.7f);

            if (expandOnHover != requireContracting)
            {
                expandOnHover = requireContracting;
                Expanded.Value = !expandOnHover;
            }
        }

        /// <summary>
        /// Scrolls back to the top whenever the cursor comes into the toolbox.
        /// </summary>
        /// <remarks>
        /// The pinned groups are drawn over the scrolling ones, so a group scrolled up behind them is not hidden so
        /// much as lost — there is nothing on screen to say it is there. Rather than take away the room needed to
        /// reach whatever hangs off the bottom, the scrolling area is simply put back where it started every time the
        /// cursor arrives, so no group can stay out of sight for longer than it takes to look away and back.
        ///
        /// The pinned groups do not scroll and are left alone.
        /// </remarks>
        private void returnToTopOnEntry()
        {
            bool cursorInside = inputManager != null && ReceivePositionalInputAt(inputManager.CurrentState.Mouse.Position);

            if (cursorInside && !cursorWasInside)
            {
                // the offset the scroll container holds onto is what keeps a hovered group in place as the toolbox
                // widens, and it would pull the scroll straight back.
                scrollContainer.TargetDrawable = null;
                scrollContainer.ScrollToStart();
            }

            cursorWasInside = cursorInside;
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
